using System;
using DevExpress.Spreadsheet;

using System.Data;
using System.Linq;
using Корректор_ФЭС.XSD;

namespace Корректор_ФЭС
{
    public abstract class PaymentSystemsBase
    {
        private readonly Workbook WorkbookPaymentSystem;

        private DataTable mDataTablePaymentSystem;

        public DataTable DataTablePaymentSystem
        {
            get
            {
                if (mDataTablePaymentSystem == null)
                {
                    var range = WorkbookPaymentSystem.Worksheets[0].GetDataRange();

                    mDataTablePaymentSystem = WorkbookPaymentSystem.Worksheets[0].CreateDataTable(range, true);
                    foreach (DataColumn dataTableColumn in mDataTablePaymentSystem.Columns)
                    {
                        dataTableColumn.DataType = typeof(string);
                    }

                    for (var i = 1; i < range.RowCount; i++)
                    {
                        mDataTablePaymentSystem.Rows.Add(mDataTablePaymentSystem.NewRow());
                    }
                    foreach (var cell in range)
                    {
                        if (cell.RowIndex == 0) { continue; }

                        mDataTablePaymentSystem.Rows[cell.RowIndex - 1][cell.ColumnIndex] = cell.Value.ToString();
                    }
                }

                return mDataTablePaymentSystem;
            }
        }

        protected PaymentSystemsBase(string pathFilePaymentSystem)
        {
            WorkbookPaymentSystem = new Workbook();
            WorkbookPaymentSystem.LoadDocument(
                pathFilePaymentSystem,
                DocumentFormat.Xlsx);
        }

        public abstract void VerifyDataPsWithDataXml(СообщОперКО сообщОперКо);

        public virtual void CreateSenderInXmlManually(СообщОперКО сообщОперКо, string номерОперации, DataRow dataRowFromPs)
        {
            var row = сообщОперКо.tableОперации.Rows
                .Cast<DataRow>()
                .Single(dataRow => dataRow["НомерОперация"].ToString().Equals(номерОперации, StringComparison.CurrentCultureIgnoreCase));
            try
            {
                row["CheckStatus"] = "0"; // Данные получены

                var сообщОперацияResponse = GetOperationFromMessageXml(сообщОперКо, номерОперации);
                var сообщОперация = сообщОперацияResponse.СведКоОперация;
                if (сообщОперация == null)
                {
                    row["CheckStatus"] = сообщОперацияResponse.CodeResult;
                    return;
                }

                var сообщУчастникОпResponse = GetMemberOperationFromDiasoftXml(сообщОперация);
                if (сообщУчастникОпResponse.УчастникОп == null)
                {
                    row["CheckStatus"] =
                        сообщОперацияResponse.CodeResult; // В сообщении не найдена информация об операции
                    return;
                }

                var сообщУчастникОп = сообщУчастникОпResponse.УчастникОп;

                CreateSenderInXml(сообщОперация, dataRowFromPs, сообщУчастникОп);
            }
            catch (Exception /*ex*/)
            {
                row["CheckStatus"] = "1"; // Неизвестная ошибка
            }
        }

        protected abstract void CreateSenderInXml(
            СообщОперКОИнформЧастьСведКООперация сведКоОперация, DataRow dataRowFromPs, СообщОперКОИнформЧастьСведКООперацияУчастникОп сообщУчастникОпИзДиасофта);
        
        protected OperationFromXml GetOperationFromMessageXml(СообщОперКО сообщОперКо, string operationNumber)
        {
            var response = new OperationFromXml();
            response.CodeResult = "0";

            var сообщОперация = сообщОперКо.ИнформЧасть.СведКО[0].Операция
                .SingleOrDefault(оп => оп.НомерОперация.Equals(operationNumber, StringComparison.CurrentCultureIgnoreCase));
            if (сообщОперация == null)
            {
                response.CodeResult = "2"; // В сообщении не найдена информация об операции
                return response;
            }
            response.СведКоОперация = сообщОперация;

            return response;
        }

        protected MemberOperationFromXml GetMemberOperationFromDiasoftXml(СообщОперКОИнформЧастьСведКООперация сведКоОперация)
        {
            var response = new MemberOperationFromXml();
            var сообщУчастникОпEnum = сведКоОперация.УчастникОп
                .Where(оп => оп.УчастникФЛИП != null);
            if (сообщУчастникОпEnum.Count() > 1)
            {
                response.CodeResult = "5"; // "В операции не удалось идентифицировать клиента. Возможно их несколько и они идентичны"
                return response;
            }
            var сообщУчастникОп = сообщУчастникОпEnum.SingleOrDefault();
            if (сообщУчастникОп == null)
            {
                response.CodeResult = "3"; // Операция не содержит данные участника ФЛ
                return response;
            }
            response.УчастникОп = сообщУчастникОп;
            
            return response;
        }
        
        protected class OperationFromXml
        {
            protected internal СообщОперКОИнформЧастьСведКООперация СведКоОперация;
            protected internal string CodeResult;
        }

        protected class MemberOperationFromXml
        {
            protected internal СообщОперКОИнформЧастьСведКООперацияУчастникОп УчастникОп;
            protected internal string CodeResult;
        }
    }

}
