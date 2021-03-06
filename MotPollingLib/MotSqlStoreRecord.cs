﻿using System;
using System.Data;
using System.Net.Sockets;
using System.Threading;
using Mot.Common.Interface.Lib;

namespace Mot.Polling.Interface.Lib
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
            _store.UseAscii = UseAscii;
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

                var recordSet = Db.ExecuteQuery($"SELECT * FROM dbo.vMOTStore WHERE MSSQLTS > {LastTouch}; ");

                if (ValidTable(recordSet))
                {
                    foreach (DataRow record in recordSet.Tables[0].Rows)
                    {
                        LastTouch = ByteArrayToHexString((System.Byte[])record["MSSQLTS"]);

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