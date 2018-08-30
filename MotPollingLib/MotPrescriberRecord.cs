using System;
using System.Data;
using System.Net.Sockets;
using System.Threading;
using Mot.Common.Interface.Lib;

namespace Mot.Polling.Interface.Lib
{
    public class PollPrescriber : MotSqlServerPollerBase<MotPrescriberRecord>
    {
        private MotPrescriberRecord _prescriber;
        
        public PollPrescriber(MotSqlServer db, Mutex mutex, string gatewayIp, int gatewayPort) :
               base(db, mutex, gatewayIp, gatewayPort)
        {
            _prescriber = new MotPrescriberRecord("Add");
        }

        public int ReadPrescriberRecords()
        {
            try
            {
                _prescriber.Clear();

                var tmpPhone = string.Empty;
                var tmpZip = string.Empty;
                var tmpDea = string.Empty;

                int counter = 0;

                /*
                 *  The field names in the database are generally not going to match the field names MOT uses, so we implment a pairwise 
                 *  list to do the conversion on the fly. This will work for all items except where the contents of the field are incomplete,
                 *  require transformation, or are otherwise incorrect, we generate and exception list and handle them one at a time.
                 */
                TranslationTable.Add("Prescriber_ID", "RxSys_DocID");
                TranslationTable.Add("Last_Name", "LastName");
                TranslationTable.Add("First_Name", "FirstName");
                TranslationTable.Add("Middle_Initial", "MiddleInitial");
                TranslationTable.Add("Address_Line_1", "Address1");
                TranslationTable.Add("Address_Line_2", "Address2");
                TranslationTable.Add("City", "City");
                TranslationTable.Add("State_Code", "State");
                TranslationTable.Add("Zip_Code", "Zip");              // Stored as Integer
                TranslationTable.Add("Zip_Plus_4", "Zip_Plus_4");       // Stored as Integer
                TranslationTable.Add("Area_Code", "AreaCode");         // Stored as Integer
                TranslationTable.Add("Telephone_Number", "Phone");    // Stored as Integer
                TranslationTable.Add("DEA_Number", "DEA_ID");
                TranslationTable.Add("DEA_Suffix", "DEA_SUFIX");
                TranslationTable.Add("Prescriber_Type ", "Specialty");
                TranslationTable.Add("Active_Flag", "Comments");      // 'Y' || 'N'

                Lookup.Add("DDS", "Dentist");
                Lookup.Add("DO", "Osteopath");
                Lookup.Add("DPM", "Podiatrist");
                Lookup.Add("DVM", "Veterinarian");
                Lookup.Add("IN", "Intern");
                Lookup.Add("MD", "Medical Doctor");
                Lookup.Add("NP", "Nurse Practitioner");
                Lookup.Add("OPT", "Optometrist");
                Lookup.Add("PA", "Physician Assistant");
                Lookup.Add("RN", "Registered Nurse");
                Lookup.Add("RPH", "Registered Pharmacist");

                /*
                 *  Query the database and collect a set of records where a valid set is {1..n} items.  This is not a traditional
                 *  record set as returned by access or SQL server, but a generic collection of IDataRecords and is usable accross
                 *  all database types.  If the set of records is {0} an exception will be thrown   
                 */

                var tag = string.Empty;
                var val = string.Empty;
                var tmp = string.Empty;

                DataSet recordSet = Db.ExecuteQuery($"SELECT * FROM vPrescriber WHERE MSSQLTS > {LastTouch};");

                if (ValidTable(recordSet))
                {                              
                    foreach (DataRow record in recordSet.Tables[0].Rows)
                    {
                        LastTouch = ByteArrayToHexString((System.Byte[])record["MSSQLTS"]);

                        foreach (DataColumn column in record.Table.Columns)
                        {
                            if (TranslationTable.TryGetValue(column.ColumnName, out tmp))
                            {
                                tag = tmp;
                                val = record[column.ColumnName].ToString();

                                switch (column.ColumnName)
                                {
                                    // Merge Zip Code
                                    case "Zip_Code":
                                        tmpZip = val;
                                        continue;

                                    case "Zip_Plus_4":
                                        tmpZip += val;
                                        val = tmpZip;
                                        break;

                                    // Merge Phone Number
                                    case "Area_Code":
                                        tmpPhone = val;
                                        continue;

                                    case "Telephone_Number":
                                        tmpPhone += val;
                                        val = tmpPhone;
                                        break;

                                    // Merge DEA ID
                                    case "DEA_Number":
                                        tmpDea = val;
                                        continue;

                                    case "DEA_Suffix":
                                        tmpDea += val;
                                        val = tmpDea;
                                        break;

                                    default:
                                        break;
                                }

                                _prescriber.SetField(tag, val, true);
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
                                    _prescriber.Write(stream);
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
                        
                        _prescriber.Clear();
                    }
                }

                return counter;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to add PharmaServe Prescriber Record {ex.Message}");
            }
        }
    }
}
