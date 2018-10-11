using System;
using System.Data;
using System.Net.Sockets;
using System.Threading;
using Mot.Common.Interface.Lib;

namespace Mot.Polling.Interface.Lib
{
    public class PollDrug : MotSqlServerPollerBase<MotDrugRecord>
    {
        private MotDrugRecord _drug;

        public PollDrug(MotSqlServer db, Mutex mutex, string gatewayIp, int gatewayPort) :
               base(db, mutex, gatewayIp, gatewayPort)
        {
            _drug = new MotDrugRecord("Add");
        }

        public void ReadDrugRecords()
        {
            try
            {
                _drug.Clear();
                _drug._preferAscii = PreferAscii;

                var itemType = string.Empty;
                var itemColor = string.Empty;
                var itemShape = string.Empty;

                TranslationTable.Add("ITEM_ID", "RxSys_DrugID");
                TranslationTable.Add("ITEM_NAME", "DrugName");
                TranslationTable.Add("Manufacturer_Abbreviation", "ShortName");
                TranslationTable.Add("STRENGTH", "Strength");
                TranslationTable.Add("UNIT_OF_MEASURE", "Unit");
                TranslationTable.Add("NARCOTIC_CODE", "DrugSchedule");
                TranslationTable.Add("NDC_CODE", "NDCNum");
                TranslationTable.Add("COLOR_CODE", "VisualDescription");
                TranslationTable.Add("ITEM_TYPE", "VisualDescription");
                TranslationTable.Add("SHAPE_CODE", "VisualDescription");
                TranslationTable.Add("PACKAGE_CODE", "ProdCode");
                TranslationTable.Add("ROUTE_OF_ADMINISTRATION", "Route");
                TranslationTable.Add("FORM_TYPE", "DoseForm");


                var recordSet = Db.ExecuteQuery($"select * FROM dbo.vItem WHERE MSSQLTS > {LastTouch};");
                   
                if (ValidTable(recordSet))
                {
                    foreach (DataRow record in recordSet.Tables[0].Rows)
                    {
                        LastTouch = ByteArrayToHexString((System.Byte[])record["MSSQLTS"]);

                        // Print the DataType of each column in the table. 
                        foreach (DataColumn column in record.Table.Columns)
                        {
                            if (TranslationTable.TryGetValue(column.ColumnName, out var tmp))
                            {
                                var tag = tmp;
                                var val = record[column.ColumnName].ToString();

                                switch (column.ColumnName)
                                {
                                    case "ITEM_TYPE":
                                        itemType = val;
                                        continue;

                                    case "COLOR_CODE":
                                        itemColor = val;
                                        continue;

                                    case "SHAPE_CODE":
                                        itemShape = val;
                                        continue;

                                    default:
                                        break;
                                }

                                _drug.SetField(tag, val, true);
                            }
                        }

                        _drug.VisualDescription = string.Format("{0}/{1}/{2}", itemShape, itemColor, itemType);

                        try
                        {
                            Mutex.WaitOne();
                        
                            using (var gw = new TcpClient(GatewayIp, GatewayPort))
                            {
                                using (var stream = gw.GetStream())
                                {
                                    _drug.Write(stream);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            EventLogger.Error($"Failed reading patient record: {ex.Message}");
                            throw;
                        }
                        finally
                        {
                            Mutex.ReleaseMutex();
                        }

                        _drug.Clear();
                    }
                }
            }
            catch (RowNotInTableException)
            {
                return;  // No records
            }

            catch (Exception ex)
            {
                throw;
            }
        }
    }
}