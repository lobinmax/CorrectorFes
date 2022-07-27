
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

using Корректор_ФЭС.XSD;

namespace Корректор_ФЭС
{
    public static class Extentions
    {
        public static СообщОперКОИнформЧастьСведКООперацияУчастникОп[] DeleteAllSendersFromOperation(
            this СообщОперКОИнформЧастьСведКООперацияУчастникОп[] информЧастьСведКоОперацияУчастникОпs)
        {
            var участникList = информЧастьСведКоОперацияУчастникОпs.ToList();
            участникList.RemoveAll(оп => !оп.СтатусУчастника.Equals("2", StringComparison.CurrentCultureIgnoreCase));

            return участникList.ToArray();
        }
        
        public static void ExportClassToXmlFile(this СообщОперКО сообщОперКо, string path)
        {
            var dt = сообщОперКо.tableОперации;
            сообщОперКо.tableОперации = null;
            try
            {
                using (var myWriter = new StreamWriter(path, false))
                {
                    var mySerializer = new XmlSerializer(typeof(СообщОперКО));
                    mySerializer.Serialize(myWriter, сообщОперКо);
                    myWriter.Flush();
                }
            }
            finally
            {
                сообщОперКо.tableОперации = dt;
            } 
        }

        public static object GetFieldValueDataRow(this DataRow row, string fieldName)
        {
            if (row[fieldName] == DBNull.Value || string.IsNullOrEmpty(row[fieldName].ToString()))
            {
                return null;
            }

            return row[fieldName].ToString();
        }

        // TODO сделать метод удаления операции из класса
    }
}
