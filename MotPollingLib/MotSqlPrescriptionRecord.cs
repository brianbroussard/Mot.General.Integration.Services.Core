using System;
using System.Data;
using System.Net.Sockets;
using System.Threading;
using Mot.Common.Interface.Lib;

namespace Mot.Polling.Interface.Lib
{
    public class PollPrescription : MotSqlServerPollerBase<MotPrescriptionRecord>
    {
        private MotPrescriptionRecord _scrip;

        public PollPrescription(MotSqlServer db, Mutex mutex, string gatewayIp, int gatewayPort) :
            base(db, mutex, gatewayIp, gatewayPort)
        {
            _scrip = new MotPrescriptionRecord("Add");
            _scrip._preferAscii = PreferAscii;
        }

        private void GetNotes(string rxID)
        {
            try
            {
                var notes = string.Empty;

                // Now get all the notes for the record
                var recordSet = Db.ExecuteQuery($"SELECT * FROM vRxNote WHERE Rx_ID = '{rxID}';");
                if (ValidTable(recordSet))
                {
                    foreach (DataRow record in recordSet.Tables[0].Rows)
                    {

                        // Print the DataType of each column in the table. 
                        foreach (DataColumn column in record.Table.Columns)
                        {
                            switch (column.ColumnName)
                            {
                                case "Note_ID":
                                    notes += $"Condition: {record[column.ColumnName].ToString()}\n";
                                    break;

                                case "Note_Type_Code":
                                    notes += $"Note Type: {record[column.ColumnName].ToString()}\n";
                                    break;

                                case "Create_User":
                                    notes += $"Written By:  {record[column.ColumnName].ToString()}\n";
                                    break;

                                case "Create_Date":
                                    notes += $"Date:  {record[column.ColumnName].ToString()}\n";
                                    break;

                                case "Note_Text":
                                    notes += $"Text:  {record[column.ColumnName].ToString()}\n";
                                    break;

                                default:
                                    break;
                            }
                        }
                    }
                }

                _scrip.SetField("Comments", notes, true);
            }
            catch (RowNotInTableException)
            {
                return;  // No records
            }
            catch (Exception ex)
            {
                EventLogger.Error($"Failed getting Rx notes: {ex.Message}");
                throw;
            }
        }


        public void ReadPrescriptionRecords()
        {
            try
            {
                _scrip.Clear();

                var rxID = string.Empty;
                int refills = 0, refillsUsed = 0;

                // Load the translaton table -- Database Column Name to Gateway Tag Name                
                TranslationTable.Add("Rx_ID", "RxSys_RxNum");
                TranslationTable.Add("Patient_ID", "RxSys_PatID");
                TranslationTable.Add("Prescriber_ID", "RxSys_DocID");
                TranslationTable.Add("Dispensed_Item_ID", "RxSys_DrugID");

                TranslationTable.Add("NDC_Code", "DrugID");
                TranslationTable.Add("Instruction_Signa_Text", "Sig");
                TranslationTable.Add("Dispense_Date", "RxStartDate");
                TranslationTable.Add("Last_Dispense_Stop_Date", "RxStopDate");

                TranslationTable.Add("Script_Status", "Status");
                TranslationTable.Add("Discontinue_Date", "DiscontinueDate");

                TranslationTable.Add("Comments", "Comments");
                TranslationTable.Add("Total_Refills_Authorized", "Refills");

                TranslationTable.Add("Dosage_Signa_Code", "DoseScheduleName");
                TranslationTable.Add("QtyPerDose", "QtyPerDose");
                TranslationTable.Add("Quantity_Dispensed", "QtyDispensed");


                var recordSet = Db.ExecuteQuery($"SELECT * FROM dbo.vRx WHERE MSSQLTS > {LastTouch}; ");

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
                                    case "Rx_ID":
                                        rxID = val;
                                        GetNotes(rxID);
                                        break;

                                    case "Total_Refills_Authorized":
                                        refills = Convert.ToInt32(val ?? "0");
                                        _scrip.Refills = refills;
                                        continue;

                                    case "Total_Refills_Used":
                                        refillsUsed = Convert.ToInt32(val ?? "0");

                                        if (refills >= refillsUsed)
                                        {
                                            refills -= refillsUsed;
                                            _scrip.Refills = refills;
                                        }
                                        break;

                                    case "Dispense_Date":
                                    case "Discontinue_Date":
                                        val = DateTime.Parse(val).ToString("yyyy-MM-dd");
                                        break;

                                    default:
                                        break;
                                }

                                // Update the local drug record
                                if (!string.IsNullOrEmpty(val) && !string.IsNullOrEmpty(tag))
                                {
                                    _scrip.SetField(tag, val, true);
                                }
                                else
                                {
                                    EventLogger.Warn($"Empty value trapped: tag = {tag} val = {val}");
                                }
                            }
                        }

                        try
                        {
                            Mutex.WaitOne();

                            using (var gw = new TcpClient(GatewayIp, GatewayPort))
                            {
                                using (var stream = gw.GetStream())
                                {
                                    _scrip.Write(stream);
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