using System;
using System.Data;
using System.Net.Sockets;
using System.Threading;
using Mot.Common.Interface.Lib;

namespace Mot.Polling.Interface.Lib
{
    public class PollPatient : MotSqlServerPollerBase<MotPatientRecord>
    {
        private MotPatientRecord _patient { get; set; }
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
                char[] delimiters = { '/', ' ' };

                string[] items = val.Split(delimiters);
                val = $"{items[2]:D4}{Convert.ToInt32(items[0]),2:D2}{Convert.ToInt32(items[1]),2:D2}";
            }
            else
            {
                val = $"{DateTime.Now.Year:D4}{DateTime.Now.Month,2:D2}{DateTime.Now.Day,2:D2}";
            }

            return val;
        }

        void GetAllergies(string patientId)
        {
            if (patientId == null)
            {
                throw new ArgumentNullException($"GetAllergies");
            }

            var val = string.Empty;
            var tag = string.Empty;

            try
            {
                var recordSet = Db.ExecuteQuery($"SELECT * FROM vPatientAllergy WHERE Patient_ID = '{patientId}';");
                if (ValidTable(recordSet))
                {
                    foreach (DataRow allergyRecord in recordSet.Tables[0].Rows)
                    {
                        foreach (DataColumn allergyColumn in allergyRecord.Table.Columns)
                        {
                            switch (allergyColumn.ColumnName)
                            {
                                case "Patient_Allergy_ID":
                                    val += $"Allergy ID: {allergyRecord[allergyColumn.ColumnName].ToString()}\n";
                                    break;

                                case "Allergy_Class_Code":
                                    val += $"Allergy Class: {allergyRecord[allergyColumn.ColumnName].ToString()}\n";
                                    break;

                                case "Description":
                                    val += $"Description: {allergyRecord[allergyColumn.ColumnName].ToString()}\n";
                                    break;

                                case "Allergy_Free_Text":
                                    val += $"Notes: {allergyRecord[allergyColumn.ColumnName].ToString()}\n";
                                    break;

                                case "Item_ID":
                                    val += $"Item ID: {allergyRecord[allergyColumn.ColumnName].ToString()}\n";
                                    break;

                                case "Onset_Date":
                                    val += $"Onset Date: {allergyRecord[allergyColumn.ColumnName].ToString()}\n";
                                    break;

                                default:
                                    break;
                            }
                        }

                        _patient.SetField("Allergies", val, true);
                    }
                }
            }
            catch (Exception ex)
            {
                EventLogger.Info($"Failed reading patient allergies: {ex.Message}");
                //throw;
            }
        }

        void GetDiagnosis(string patientId)
        {
            if (patientId == null)
            {
                throw new ArgumentNullException($"GetDiagnosis");
            }

            var val = string.Empty;
            var tag = string.Empty;

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
                            switch (dxColumn.ColumnName)
                            {
                                case "condition_description":
                                    val += $"Condition: {diagnosisRecord[dxColumn.ColumnName].ToString()}\n";
                                    break;

                                case "Severity":
                                    val += $"Severity: {diagnosisRecord[dxColumn.ColumnName].ToString()}";
                                    break;

                                case "Onset_Date":
                                    val += $"Onset Date: {diagnosisRecord[dxColumn.ColumnName].ToString()}\n";
                                    break;

                                case "Cessation_Date":
                                    val += $"Cessation Date: {diagnosisRecord[dxColumn.ColumnName].ToString()}\n";
                                    break;

                                default:
                                    break;
                            }
                        }

                        _patient.SetField("DXNotes", val, true);
                    }
                }
            }
            catch (Exception ex)
            {
                EventLogger.Info($"Failed reading patient diagnosis: {ex.Message}");
                //throw;
            }
        }

        void GetNotes(string patientId)
        {
            if (patientId == null)
            {
                throw new ArgumentNullException($"GetNotes");
            }

            var val = string.Empty;
            var tag = string.Empty;

            try
            {
                var recordSet = Db.ExecuteQuery($"SELECT * FROM vPatientNote WHERE Patient_ID = '{patientId}';");
                if (ValidTable(recordSet))
                {
                    foreach (DataRow note in recordSet.Tables[0].Rows)
                    {
                        // Print the DataType of each column in the table. 
                        foreach (DataColumn noteColumn in note.Table.Columns)
                        {
                            switch (noteColumn.ColumnName)
                            {
                                case "Note_ID":
                                    val += $"Condition: {note[noteColumn.ColumnName].ToString()}\n";
                                    break;

                                case "Note_Type_Code":
                                    val += $"Note Type: {note[noteColumn.ColumnName].ToString()}\n";
                                    break;

                                case "Create_User":
                                    val += $"Written By: {note[noteColumn.ColumnName].ToString()}\n";
                                    break;

                                case "Create_Date":
                                    val += $"Date: {note[noteColumn.ColumnName].ToString()}\n";
                                    break;

                                case "Note_Text":
                                    val += $"Text: {val}\n";
                                    break;

                                default:
                                    break;
                            }
                        }

                        _patient.SetField("TreatmentNotes", val, true);
                    }
                }
            }
            catch (Exception ex)
            {
                EventLogger.Info($"Failed reading patient notes: {ex.Message}");
                //throw;
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
                TranslationTable.Add("Address_Line_2", "Address2");
                TranslationTable.Add("City", "City");
                TranslationTable.Add("State_Code", "State");
                TranslationTable.Add("Zip_Code", "Zip");
                TranslationTable.Add("Zip_Plus_4", "Zip");
                TranslationTable.Add("Telephone_Number", "Phone1");
                TranslationTable.Add("Area_Code", "Phone1");
                TranslationTable.Add("Extension", "Phone2");
                TranslationTable.Add("SSN", "SSN");
                TranslationTable.Add("BirthDate", "DOB"); // SqlDateTime
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

                var recordSet = Db.ExecuteQuery($"SELECT * FROM vPatient WHERE MSSQLTS > {LastTouch};");
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

                                    case "BirthDate":
                                        val = NormalizeDate(val);
                                        break;

                                    default:
                                        break;
                                }

                                _patient.SetField(tag, val, true);
                            }
                        }

                        // Now get the note fields
                       // GetAllergies(patientId);
                       // GetDiagnosis(patientId);
                        //GetNotes(patientId);

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
                        }
                        catch (Exception ex)
                        {
                            EventLogger.Error($"Failed processing patient record: {ex.Message}");
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
            catch (Exception ex)
            {
                throw new RowNotInTableException($"Patient Record Not Found");
                // throw new Exception($"Failed to get Patient Record {ex.Message}");
            }
        }
    }
}