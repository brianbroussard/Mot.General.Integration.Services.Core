using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Net.Sockets;
using System.Threading;
using MotCommonLib;

namespace TransformerPollingService
{
    //
    // There's no evidence that McKesson has any notion of 'Store' so this is moire of a placeholder
    //
    public class PollStore : MotSqlServerPollerBase<MotStoreRecord>
    {
        private MotStoreRecord _store;

        public PollStore(MotSqlServer db, Mutex mutex, string gatewayIp, int gatewayPort) :
            base(db, mutex, gatewayIp, gatewayPort)
        {
            _store = new MotStoreRecord("Add");
        }

        public void ReadStoreRecords()
        {
            _store.Clear();

            try
            {
                // Load the translaton table -- Database Column Name to Gateway Tag Name                
                TranslationTable.Add("RxSys_StoreID", "RxSys_StoreID");
                TranslationTable.Add("StoreName", "StoreName");
                TranslationTable.Add("Address1", "Address1");
                TranslationTable.Add("Address2", "Address2");
                TranslationTable.Add("CITY", "City");
                TranslationTable.Add("STATE", "State");
                TranslationTable.Add("ZIP", "Zip");
                TranslationTable.Add("PHONE", "Phone");
                TranslationTable.Add("FAX", "Fax");
                TranslationTable.Add("DEANum", "DEANum");

                var recordSet = Db.ExecuteQuery($"SELECT * FROM dbo.vMOTStore WHERE MSSQLTS > '{LastTouch.ToString()}'; ");

                if (ValidTable(recordSet))
                {
                    foreach (DataRow record in recordSet.Tables[0].Rows)
                    {
                        if ((long)record["MSSQLTS"] > LastTouch)
                        {
                            LastTouch = (long)record["MSSQLTS"];
                        }

                        foreach (DataColumn column in record.Table.Columns)
                        {
                            if (TranslationTable.TryGetValue(column.ColumnName, out var tmp))
                            {
                                var tag = tmp;
                                var val = record[column.ColumnName].ToString();

                                // Conversion rules
                                switch (column.ColumnName)
                                {
                                    default:
                                        break;
                                }

                                // Update the local drug record
                                _store.SetField(tag, val, true);
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
                                    _store.Write(stream);
                                }
                            }

                            Mutex.ReleaseMutex();
                        }
                        catch (Exception ex)
                        {
                            EventLogger.Error($"Error processing prescriptin record: {ex.Message}");
                            throw;
                        }
                        finally
                        {
                            Mutex.ReleaseMutex();
                        }

                        _store.Clear();
                    }
                }
            }
            catch (System.Exception e)
            {
                throw new Exception("Failed to get Store Record " + e.Message);
            }
        }
    }
}