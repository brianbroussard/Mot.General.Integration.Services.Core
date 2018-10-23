using System;
using System.Data;
using System.Net.Sockets;
using System.Threading;
using Mot.Common.Interface.Lib;

namespace Mot.Polling.Interface.Lib
{
    public class PollFacility : MotSqlServerPollerBase<MotFacilityRecord>
    {
        private MotFacilityRecord _facility;

        public PollFacility(MotSqlServer db, Mutex mutex, string gatewayIp, int gatewayPort) :
            base(db, mutex, gatewayIp, gatewayPort)
        {
            _facility = new MotFacilityRecord("Add");
            _facility.UseAscii = UseAscii;
        }

        public void ReadFacilityRecords()
        {
            _facility.Clear();
 
            try
            {
                // Load the translaton table -- Database Column Name to Gateway Tag Name                
                TranslationTable.Add("RxSys_LocID", "RxSys_LocID");
                TranslationTable.Add("RxSys_StoreID", "RxSys_StoreID");
                TranslationTable.Add("LocationName", "LocationName");
                TranslationTable.Add("Address1", "Address1");
                TranslationTable.Add("Address2", "Address2");
                TranslationTable.Add("CITY", "City");
                TranslationTable.Add("STATE", "State");
                TranslationTable.Add("ZIP", "Zip");
                TranslationTable.Add("PHONE", "Phone");


                var recordSet = Db.ExecuteQuery($"SELECT * FROM dbo.vMOTLocation WHERE Touchdate > {LastTouch}; ");

                if (ValidTable(recordSet))
                {
                    foreach (DataRow record in recordSet.Tables[0].Rows)
                    {
                        LastTouch = ByteArrayToHexString((System.Byte[])record["Touchdate"]);

                        // Print the DataType of each column in the table. 
                        foreach (DataColumn column in record.Table.Columns)
                        {
                            if (TranslationTable.TryGetValue(column.ColumnName, out var tmp))
                            {
                                var tag = tmp;
                                var val = record[column.ColumnName].ToString();

                                switch (column.ColumnName)
                                {
                                    case "ZIP":
                                        val = "0" + val;
                                        break;

                                    default:
                                        break;
                                }

                                // Update the local location record
                                _facility.SetField(tag, val, true);
                            }
                        }

                        try
                        {
                            // Write the record to the gateway
                            Mutex.WaitOne();

                            using (var gw = new TcpClient(GatewayIp, GatewayPort))
                            {
                                using (var stream = gw.GetStream())
                                {
                                    _facility.Write(stream);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            EventLogger.Error($"Error processing facility record: {ex.Message}");
                            throw;
                        }
                        finally
                        {
                            Mutex.ReleaseMutex();
                        }

                        _facility.Clear();
                    }
                }
            }
            catch (RowNotInTableException)
            {
                return;  // No records
            }
            catch (Exception ex)
            {
                throw new RowNotInTableException($"Facility record failure: {ex.Message}");
            }
        }
    }
}