using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Net.Sockets;
using System.Threading;
using MotCommonLib;

namespace TransformerPollingService
{
    public class PollPatient : MotSqlServerPollerBase<MotPrescriberRecord>
    {
        private MotPatientRecord _patient;
        private int _counter;

        public PollPatient(MotSqlServer db, Mutex mutex, string gatewayIp, int gatewayPort) :
            base(db, mutex, gatewayIp, gatewayPort)
        {
            _patient = new MotPatientRecord("Add");
        }

        public string NormalizeDate(string val)
        {
            if (!string.IsNullOrEmpty(val))
            {
                char[] delimiters = {'/', ' '};

                string[] items = val.Split(delimiters);
                val = string.Format("{0:D4}{1,2:D2}{2,2:D2}", items[2], Convert.ToInt32(items[0]), Convert.ToInt32(items[1]));
            }
            else
            {
                val = string.Format("{0:D4}{1,2:D2}{2,2:D2}", DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
            }

            return val;
        }

        void GetAllergies(string patientId)
        {
            if (patientId == null)
            {
                throw new ArgumentNullException($"GetAllergies");
            }

            string val = string.Empty;
            string tag;

            try
            {
                var recordSet = Db.ExecuteQuery($"SELECT * FROM vPatientAllergy WHERE Patient_ID = '{patientId}';");
                if (ValidTable(recordSet))
                {
                    foreach (DataRow allergyRecord in recordSet.Tables[0].Rows)
                    {
                        foreach (DataColumn allergyColumn in allergyRecord.Table.Columns)
                        {
                            if (TranslationTable.TryGetValue(allergyColumn.ColumnName, out var tmp))
                            {
                                tag = tmp;
                                val = allergyRecord[allergyColumn.ColumnName].ToString();

                                switch (allergyColumn.ColumnName)
                                {
                                    case "Patient_Allergy_ID":
                                        val += $"Allergy ID: {val}\n";
                                        break;

                                    case "Allergy_Class_Code":
                                        val += $"Allergy Class: {val}\n";
                                        break;

                                    case "Description":
                                        val += $"Description: {val}\n";
                                        break;

                                    case "Allergy_Free_Text":
                                        val += $"Notes: {val}\n";
                                        break;

                                    case "Item_ID":
                                        val += $"Item ID: {val}\n";
                                        break;

                                    case "Onset_Date":
                                        val += $"Onset Date: {val}\n";
                                        break;

                                    default:
                                        break;
                                }
                            }
                        }
                    }

                    _patient.SetField("Allergies", val, true);
                }
            }
            catch (Exception ex)
            {
                EventLogger.Error($"Failed reading patient allergies: {ex.Message}");
                throw;
            }
        }

        void GetDiagnosis(string patientId)
        {
            if (patientId == null)
            {
                throw new ArgumentNullException($"GetDiagnosis");
            }

            string val = string.Empty;
            string tag;

            try
            {
                var recordSet = Db.ExecuteQuery($"SELECT * FROM vPatientDiagnosis  WHERE Patient_ID = '{patientId}';");
                if (ValidTable(recordSet))
                {
                    foreach (DataRow diagnosisRecord in recordSet.Tables[0].Rows)
                    {
                        // Print the DataType of each column in the table. 
                        foreach (DataColumn dxColumn in diagnosisRecord.Table.Columns)
                        {
                            if (TranslationTable.TryGetValue(dxColumn.ColumnName, out var tmp))
                            {
                                tag = tmp;
                                val = diagnosisRecord[dxColumn.ColumnName].ToString();

                                switch (dxColumn.ColumnName)
                                {
                                    case "condition_description":
                                        val += $"Condition: {val}\n";
                                        break;

                                    case "Severity":
                                        val += $"Severity: {val}";
                                        break;

                                    case "Onset_Date":
                                        val += $"Onset Date: {val}\n";
                                        break;

                                    case "Cessation_Date":
                                        val += $"Cessation Date: {val}\n";
                                        break;

                                    default:
                                        break;
                                }
                            }
                        }
                    }

                    _patient.SetField("DXNotes", val, true);
                }
            }
            catch (Exception ex)
            {
                EventLogger.Error($"Failed reading patient diagnosis: {ex.Message}");
                throw;
            }
        }

        void GetNotes(string patientId)
        {
            if (patientId == null)
            {
                throw new ArgumentNullException($"GetNotes");
            }

            string val = string.Empty;
            string tag;

            try
            {
                var recordSet = Db.ExecuteQuery($"SELECT * FROM vPatientNote WHERE Patient_ID = '{patientId}';");
                if (ValidTable(recordSet))
                {
                    foreach (DataRow note in recordSet.Tables["__table"].Rows)
                    {
                        // Print the DataType of each column in the table. 
                        foreach (DataColumn column in note.Table.Columns)
                        {
                            if (TranslationTable.TryGetValue(column.ColumnName, out var tmp))
                            {
                                tag = tmp;
                                val = note[column.ColumnName].ToString();

                                switch (column.ColumnName)
                                {
                                    case "Note_ID":
                                        val += $"Condition: {val}\n";
                                        break;

                                    case "Note_Type_Code":
                                        val += $"Note Type: {val}\n";
                                        break;

                                    case "Create_User":
                                        val += $"Written By: {val}\n";
                                        break;

                                    case "Create_Date":
                                        val += $"Date: {val}\n";
                                        break;

                                    case "Note_Text":
                                        val += $"Text: {val}\n";
                                        break;

                                    default:
                                        break;
                                }
                            }
                        }
                    }

                    _patient.SetField("TreatmentNotes", val, true);
                }
            }
            catch (Exception ex)
            {
                EventLogger.Error($"Failed reading patient notes: {ex.Message}");
                throw;
            }
        }

        public void ReadPatientRecords()
        {
            try
            {
                // Load the translaton table -- Database Column Name to Gateway Tag Name  
                TranslationTable.Add("Patient_ID", "RxSys_PatID");
                TranslationTable.Add("Patient_Location_Code", "RxSys_LocID");
                TranslationTable.Add("Primary_Prescriber_ID", "RxSys_PrimaryDoc");
                TranslationTable.Add("Last_Name", "LastName");
                TranslationTable.Add("First_Name", "FirstName");
                TranslationTable.Add("Middle_Initial", "MiddleInitial");
                TranslationTable.Add("Address_Line_1", "Address1");
                TranslationTable.Add("Address_Line_2", "Address1");
                TranslationTable.Add("City", "City");
                TranslationTable.Add("State_Code", "State");
                TranslationTable.Add("Zip_Code", "Zip");
                TranslationTable.Add("Zip_Plus_4", "Zip_Plus_4");
                TranslationTable.Add("Telephone_Number", "Phone1");
                TranslationTable.Add("Area_Code", "AreaCode");
                TranslationTable.Add("Extension", "WorkPhone");
                TranslationTable.Add("SSN", "SSN");
                TranslationTable.Add("Birth_Date", "DOB"); // SqlDateTime
                TranslationTable.Add("Deceased_Date", "Comments"); // SqlDateTime
                TranslationTable.Add("Sex", "Gender");

                // Search for the patient
                // Load Patient Record
                // Search for vPatientAlergyc by Patient_ID - returns {0...1} records
                //      Update Patient Record 
                // Search for vPatientDiagnosis by Patient_ID - returns {0...1} records
                //      Update Patient Record 
                // Search for vPatientNote by Patient_ID - returns {0...1} records
                //      Update Patient Record 
                // Write Patient Record 

                var tmpPhone = string.Empty;
                var tmpZip = string.Empty;
                
                var patientId = string.Empty;

                var recordSet = Db.ExecuteQuery($"SELECT * FROM vPatient WHERE MSSQLTS > '{LastTouch}';");
                if (ValidTable(recordSet))
                {                               
                    foreach (DataRow record in recordSet.Tables[0].Rows)
                    {
                        if ((long)record["MSSQLTS"] > LastTouch)
                        {
                            LastTouch = (long) record["MSSQLTS"];
                        }

                        // Print the DataType of each column in the table. 
                        foreach (DataColumn column in record.Table.Columns)
                        {
                            if (TranslationTable.TryGetValue(column.ColumnName, out var tmp))
                            {
                                var tag = tmp;
                                var val = record[column.ColumnName].ToString();

                                switch (column.ColumnName)
                                {
                                    case "Patient_ID":
                                        patientId = val;
                                        break;

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

                                    case "Birth_Date":
                                        val = NormalizeDate(val);
                                        break;

                                    default:
                                        break;
                                }
                              
                                _patient.SetField(tag, val, true);
                            }


                            // Now get the note fields
                            GetAllergies(patientId);
                            GetDiagnosis(patientId);
                            GetNotes(patientId);
                  
                            // Finally,  write the recort to the Gateway
                            try
                            {
                                Mutex.WaitOne();

                                using (var gw = new TcpClient(GatewayIp, GatewayPort))
                                {
                                    using (var stream = gw.GetStream())
                                    {
                                        _patient.Write(stream);
                                    }
                                }

                                Mutex.ReleaseMutex();
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
                            
                            _patient.Clear();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get Patient Record {ex.Message}");
            }
        }
    }
}