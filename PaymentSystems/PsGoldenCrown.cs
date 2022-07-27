using System;
using System.Data;
using System.Linq;

using Корректор_ФЭС.XSD;

namespace Корректор_ФЭС
{
    public class PsGoldenCrown: PaymentSystemsBase
    {
        public PsGoldenCrown(string pathFilePaymentSystem) : base(pathFilePaymentSystem)
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

                    var opRowPs = psEnum.Where((dataRow) =>
                        dataRow["СведФЛПолФам"].ToString().Trim().Equals(сообщУчастникОп.УчастникФЛИП.СведФЛИП.ФИОФЛИП.Фам.Trim(), StringComparison.CurrentCultureIgnoreCase) &&
                        dataRow["СведФЛПолИмя"].ToString().Trim().Equals(сообщУчастникОп.УчастникФЛИП.СведФЛИП.ФИОФЛИП.Имя.Trim(), StringComparison.CurrentCultureIgnoreCase) &&
                        dataRow["СведФЛПолОтч"].ToString().Trim().Equals(сообщУчастникОп.УчастникФЛИП.СведФЛИП.ФИОФЛИП.Отч.Trim(), StringComparison.CurrentCultureIgnoreCase) &&
                        Convert.ToDecimal(dataRow["СумОперации"]) == Convert.ToDecimal(сообщОперация.СумОперации));

                    if (!opRowPs.Any())
                    {
                        opRowPs = psEnum.Where((dataRow) =>
                            dataRow["СведФЛПолСерияДок"].ToString().Trim()
                                .Equals(сообщУчастникОп.УчастникФЛИП.СведФЛИП.СведДокУдЛичн.СерияДок.Trim(), StringComparison.CurrentCultureIgnoreCase) &&
                            dataRow["СведФЛПолНомДок"].ToString().Trim()
                                .Equals(сообщУчастникОп.УчастникФЛИП.СведФЛИП.СведДокУдЛичн.НомДок.Trim(), StringComparison.CurrentCultureIgnoreCase) &&
                            Convert.ToDecimal(dataRow["СумОперации"]) == Convert.ToDecimal(сообщОперация.СумОперации));
                    }

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
                catch (Exception /*ex*/)
                {
                    row["CheckStatus"] = "1"; // Неизвестная ошибка
                }
            }
        }

        protected override void CreateSenderInXml(
            СообщОперКОИнформЧастьСведКООперация сведКоОперация, DataRow dataRowFromPs, СообщОперКОИнформЧастьСведКООперацияУчастникОп сообщУчастникОпИзДиасофта)
        {
            сведКоОперация.УчастникОп = сведКоОперация.УчастникОп.DeleteAllSendersFromOperation();

            сведКоОперация.НаимПлатежнаяСистема1 = dataRowFromPs.GetFieldValueDataRow("НаимПлатежнаяСистема1")?.ToString();
            сведКоОперация.КодОперации = dataRowFromPs.GetFieldValueDataRow("КодОперации")?.ToString();

            if (сведКоОперация.СведенияПереводыДС == null)
            {
                throw new Exception("Не заполнены сведения о переводе ДС (узел файла : 'СведенияПереводыДС')");
            }

            сведКоОперация.СведенияПереводыДС.КодТерИнГос = dataRowFromPs.GetFieldValueDataRow("КодТерИнГос")?.ToString();
            сведКоОперация.СведенияПереводыДС.ТипОператорДС = dataRowFromPs.GetFieldValueDataRow("ТипОператорДС")?.ToString();
            сведКоОперация.СведенияПереводыДС.СведБанкПлательщик = new БанкПлательщикПолучатель()
            {
                БИККО = dataRowFromPs.GetFieldValueDataRow("СведБанкПлательщик(БИККО)")?.ToString(),
                НаимКО = dataRowFromPs.GetFieldValueDataRow("СведБанкПлательщик(НаимКО)")?.ToString()
            };
            сведКоОперация.СведенияПереводыДС.СтатусПеревод = dataRowFromPs.GetFieldValueDataRow("СтатусПеревод")?.ToString();
            сведКоОперация.СведенияПереводыДС.СведПриемНалДС = new МестоПриемаВыдача()
            {
                БИККО = dataRowFromPs.GetFieldValueDataRow("СведМестоПриема(БИККО)")?.ToString(),
                НаимКО = dataRowFromPs.GetFieldValueDataRow("СведМестоПриема(НаимКО)")?.ToString(),
                АдрМестаПриемаВыдача = new Адрес()
                {
                    АдресСтрока = dataRowFromPs.GetFieldValueDataRow("СведПриемНалДАдресСтрока")?.ToString()
                }
            };

            var newIndex = сведКоОперация.УчастникОп.Length;
            var newУчастникОп = сведКоОперация.УчастникОп;
            Array.Resize(ref newУчастникОп, newIndex + 1);

            var участник = new СообщОперКОИнформЧастьСведКООперацияУчастникОп()
            {
                СтатусУчастника = dataRowFromPs.GetFieldValueDataRow("СтатусУчастника")?.ToString(),
                ТипУчастника = dataRowFromPs.GetFieldValueDataRow("ТипУчастника")?.ToString(),
                ПризнУчастника = dataRowFromPs.GetFieldValueDataRow("ПризнУчастника")?.ToString(),
                ПризнКлиент = dataRowFromPs.GetFieldValueDataRow("ПризнКлиент")?.ToString(),
                УчастникФЛИП = new СообщОперКОИнформЧастьСведКООперацияУчастникОпУчастникФЛИП()
                {
                    СведФЛИП = new СведенияФЛИП()
                    {
                        ФИОФЛИП = new ФИО()
                        {
                            Фам = dataRowFromPs.GetFieldValueDataRow("Фам")?.ToString(),
                            Имя = dataRowFromPs.GetFieldValueDataRow("Имя")?.ToString(),
                            Отч = dataRowFromPs.GetFieldValueDataRow("Отч")?.ToString()
                        },
                        АдрРег = new Адрес()
                        {
                            АдресСтрока = dataRowFromPs.GetFieldValueDataRow("СведФЛОтпрАдрРегАдресСтрока")?.ToString()
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
