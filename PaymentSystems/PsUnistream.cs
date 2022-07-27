using System;
using System.Data;
using System.Linq;

using Корректор_ФЭС.XSD;

namespace Корректор_ФЭС
{
    public class PsUnistream: PaymentSystemsBase
    {
        public PsUnistream(string pathFilePaymentSystem) : base(pathFilePaymentSystem)
        {
        }

        public override void VerifyDataPsWithDataXml(СообщОперКО сообщОперКо)
        {
            var psEnum = DataTablePaymentSystem.Rows.Cast<DataRow>().ToList();
            foreach (DataRow row in сообщОперКо.tableОперации.Rows.Cast<DataRow>()
                .Where((row) => !row["CheckStatus"].ToString().Equals("0", StringComparison.CurrentCultureIgnoreCase)))
            {
                try
                {
                    row["CheckStatus"] = "0"; // Данные получены

                    var сообщОперацияResponse = GetOperationFromMessageXml(сообщОперКо, row["НомерОперация"].ToString());
                    var сообщОперация = сообщОперацияResponse.СведКоОперация;
                    if (сообщОперация == null)
                    {
                        row["CheckStatus"] = сообщОперацияResponse.CodeResult;
                        continue;
                    }

                    var сообщУчастникОпResponse = GetMemberOperationFromDiasoftXml(сообщОперация);
                    if (сообщУчастникОпResponse.УчастникОп == null)
                    {
                        row["CheckStatus"] = сообщУчастникОпResponse.CodeResult; // В сообщении не найдена информация об операции
                        continue;
                    }
                    var сообщУчастникОп = сообщУчастникОпResponse.УчастникОп;

                    var участникФиоСтрока = $"{сообщУчастникОп.УчастникФЛИП.СведФЛИП.ФИОФЛИП.Фам} " +
                                            $"{сообщУчастникОп.УчастникФЛИП.СведФЛИП.ФИОФЛИП.Имя} " +
                                            $"{сообщУчастникОп.УчастникФЛИП.СведФЛИП.ФИОФЛИП.Отч}";
                    участникФиоСтрока = участникФиоСтрока.Replace("  ", " ").Trim();
                    var opRowPs = psEnum.Where((dataRow) =>
                        dataRow["Получатель"].ToString().Trim().Equals(участникФиоСтрока, StringComparison.CurrentCultureIgnoreCase) &&
                        Convert.ToDecimal(dataRow["Сумма"]) == Convert.ToDecimal(сообщОперация.СумОперации));
                    
                    if (!opRowPs.Any())
                    {
                        row["CheckStatus"] = "4"; // В файле платежной системы не найдена информация об операции
                        continue;
                    }

                    if (opRowPs.Count() > 1)
                    {
                        row["CheckStatus"] = "5"; // В файле платежной системы найдено несколько оправителей по операции
                        continue;
                    }

                    CreateSenderInXml(сообщОперация, opRowPs.Single(), сообщУчастникОп);
                }
                catch (Exception ex)
                {
                    row["CheckStatus"] = "1"; // Неизвестная ошибка
                }
            }
        }

        protected override void CreateSenderInXml(СообщОперКОИнформЧастьСведКООперация сведКоОперация, DataRow dataRowFromPs,
            СообщОперКОИнформЧастьСведКООперацияУчастникОп сообщУчастникОпИзДиасофта)
        {
            сведКоОперация.УчастникОп = сведКоОперация.УчастникОп.DeleteAllSendersFromOperation();

            сведКоОперация.НаимПлатежнаяСистема1 = null;
            сведКоОперация.КодОперации = "5016";

            if (сведКоОперация.СведенияПереводыДС == null)
            {
                throw new Exception("Не заполнены сведения о переводе ДС (узел файла : 'СведенияПереводыДС')");
            }

            сведКоОперация.СведенияПереводыДС.КодТерИнГос = dataRowFromPs.GetFieldValueDataRow("Код страны")?.ToString();
            сведКоОперация.СведенияПереводыДС.ТипОператорДС = "2";
            сведКоОперация.СведенияПереводыДС.СведБанкПлательщик = new БанкПлательщикПолучатель()
            {
                БИККО = string.IsNullOrEmpty(dataRowFromPs.GetFieldValueDataRow("SWIFT Банка отправителя")?.ToString()) 
                    ? "НР" 
                    : dataRowFromPs.GetFieldValueDataRow("SWIFT Банка отправителя")?.ToString(),
                НаимКО = dataRowFromPs.GetFieldValueDataRow("Банк отправителя")?.ToString()
            };
            сведКоОперация.СведенияПереводыДС.СтатусПеревод = "1";
            сведКоОперация.СведенияПереводыДС.СведПриемНалДС = new МестоПриемаВыдача()
            {
                БИККО = string.IsNullOrEmpty(dataRowFromPs.GetFieldValueDataRow("SWIFT Точки отправителя")?.ToString())
                    ? "НР"
                    : dataRowFromPs.GetFieldValueDataRow("SWIFT Точки отправителя")?.ToString(),
                НаимКО = string.IsNullOrEmpty(dataRowFromPs.GetFieldValueDataRow("Точка отправителя")?.ToString()) 
                    ? "Информация отсутствует" 
                    : dataRowFromPs.GetFieldValueDataRow("Точка отправителя")?.ToString(),
                АдрМестаПриемаВыдача = new Адрес()
                {
                    АдресСтрока = (string.IsNullOrEmpty(dataRowFromPs.GetFieldValueDataRow("Адрес точки отправления")?.ToString()))
                        ? "Информация отсутствует"
                        : dataRowFromPs.GetFieldValueDataRow("Адрес точки отправления")?.ToString()
                }
            };

            var newIndex = сведКоОперация.УчастникОп.Length;
            var newУчастникОп = сведКоОперация.УчастникОп;
            Array.Resize(ref newУчастникОп, newIndex + 1);

            var участник = new СообщОперКОИнформЧастьСведКООперацияУчастникОп()
            {
                СтатусУчастника = "1",
                ТипУчастника = "2",
                ПризнУчастника = "9",
                ПризнКлиент = "0",
                УчастникФЛИП = new СообщОперКОИнформЧастьСведКООперацияУчастникОпУчастникФЛИП()
                {
                    СведФЛИП = new СведенияФЛИП()
                    {
                        ФИОФЛИП = new ФИО()
                        {
                            ФИОСтрока = dataRowFromPs.GetFieldValueDataRow("Отправитель")?.ToString()
                        },
                        ДокУдЛичн = "0",
                        ПризнакПубЛицо = "0"
                    }
                }
            };
            newУчастникОп[newIndex] = участник;

            сведКоОперация.УчастникОп = newУчастникОп;
            сведКоОперация.УчастникОп = сведКоОперация.УчастникОп.OrderBy(оп => оп.СтатусУчастника).ToArray();
        }
    }
}
