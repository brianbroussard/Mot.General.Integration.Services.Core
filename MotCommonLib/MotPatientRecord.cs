// 
// MIT license
//
// Copyright (c) 2018 by Peter H. Jenney and Medicine-On-Time, LLC.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace Mot.Common.Interface.Lib
{
    /// <summary>
    /// Patient Record (Key == D)
    /// </summary>
    [Serializable]
    [XmlRoot("MotPatientRecord")]
    public class MotPatientRecord : MotRecordBase
    {
        private readonly List<Field> _fieldList;

        private void createRecord(string tableAction)
        {
            try
            {
                _fieldList.Add(new Field("Table", "Patient", 10, true, 'a'));
                _fieldList.Add(new Field("Action", tableAction, 10, true, 'a'));
                _fieldList.Add(new Field("RxSys_PatID", "", 36, true, 'k'));
                _fieldList.Add(new Field("LastName", "", 30, true, 'a'));
                _fieldList.Add(new Field("FirstName", "", 20, true, 'a'));
                _fieldList.Add(new Field("MiddleInitial", "", 2, false, 'n'));
                _fieldList.Add(new Field("Address1", "", 40, true, 'w'));
                _fieldList.Add(new Field("Address2", "", 40, true, 'w'));
                _fieldList.Add(new Field("City", "", 30, true, 'w'));
                _fieldList.Add(new Field("State", "", 2, true, 'w'));
                _fieldList.Add(new Field("Zip", "", 9, true, 'w'));
                _fieldList.Add(new Field("Phone1", "", 10, true, 'w'));
                _fieldList.Add(new Field("Phone2", "", 10, false, 'n'));
                _fieldList.Add(new Field("WorkPhone", "", 10, false, 'n'));
                _fieldList.Add(new Field("RxSys_LocID", "", 36, true, 'w'));
                _fieldList.Add(new Field("Room", "", 10, true, 'w'));
                _fieldList.Add(new Field("Comments", "", 32767, false, 'n'));
                _fieldList.Add(new Field("CycleDate", "", 10, false, 'n'));
                _fieldList.Add(new Field("CycleDays", "", 2, false, 'n'));
                _fieldList.Add(new Field("CycleType", "", 2, false, 'n'));
                _fieldList.Add(new Field("Status", "", 2, false, 'n'));
                _fieldList.Add(new Field("RxSys_LastDoc", "", 36, false, 'n'));
                _fieldList.Add(new Field("RxSys_PrimaryDoc", "", 36, false, 'n'));
                _fieldList.Add(new Field("RxSys_AltDoc", "", 36, false, 'n'));
                _fieldList.Add(new Field("SSN", "", 9, true, 'w'));
                _fieldList.Add(new Field("Allergies", "", 32767, true, 'w'));
                _fieldList.Add(new Field("Diet", "", 32767, true, 'w'));
                _fieldList.Add(new Field("DxNotes", "", 32767, true, 'w'));
                _fieldList.Add(new Field("TreatmentNotes", "", 32767, true, 'w'));
                _fieldList.Add(new Field("DOB", "", 10, true, 'w'));
                _fieldList.Add(new Field("Height", "", 4, false, 'n'));
                _fieldList.Add(new Field("Weight", "", 4, false, 'n'));
                _fieldList.Add(new Field("ResponsibleName", "", 32767, false, 'n'));
                _fieldList.Add(new Field("InsName", "", 80, false, 'n'));
                _fieldList.Add(new Field("InsPNo", "", 20, false, 'n'));
                _fieldList.Add(new Field("AltInsName", "", 80, false, 'n'));
                _fieldList.Add(new Field("AltInsPNo", "", 20, false, 'n'));
                _fieldList.Add(new Field("MCareNum", "", 20, false, 'n'));
                _fieldList.Add(new Field("MCaidNum", "", 20, false, 'n'));
                _fieldList.Add(new Field("AdmitDate", "", 10, false, 'n'));
                _fieldList.Add(new Field("ChartOnly", "", 2, false, 'n'));
                _fieldList.Add(new Field("Gender", "", 2, false, 'n'));
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Empty Constructor
        /// </summary>
        public MotPatientRecord()
        {
        }

        /// <summary>
        /// Constructor with passed Action and optional Autotruncate flag
        /// </summary>
        /// <param name="Action"></param>
        /// <param name="AutoTruncate"></param>
        public MotPatientRecord(string Action, bool AutoTruncate = false)
        {
            base.AutoTruncate = AutoTruncate;

            try
            {
                _fieldList = new List<Field>();
                createRecord(Action);
            }
            catch (Exception ex)
            {
                var errorString = $"Failed to create Patient record: {ex.Message}";
                EventLogger.Error(errorString);
                throw new Exception(errorString);
            }
        }

        /// <summary>
        /// Direct access to the internal field list
        /// </summary>
        /// <param name="FieldName"></param>
        /// <param name="Val"></param>
        /// <param name="OverrideTruncation"></param>
        public void SetField(string FieldName, string Val, bool OverrideTruncation = false)
        {
            try
            {
                base.SetField(_fieldList, Val, FieldName, OverrideTruncation);
            }
            catch
            {
                throw;
            }
        }

        public void AddToQueue(MotWriteQueue NewQueue = null)
        {
            if (NewQueue != null)
            {
                LocalWriteQueue = NewQueue;
            }

            AddToQueue("D", _fieldList);
        }

        public void Write(MotSocket socket, bool doLogging = false)
        {
            try
            {
                if (QueueWrites)
                {
                    AddToQueue();
                }
                else
                {
                    Write(socket, _fieldList, doLogging);
                }
            }
            catch (Exception ex)
            {
                var errorString = $"Failed to write Patient record: {ex.Message}";
                EventLogger.Error(errorString);
                throw new Exception(errorString);
            }
        }

        public void Write(NetworkStream stream, bool doLogging = false)
        {
            try
            {
                if (QueueWrites)
                {
                    AddToQueue();
                }
                else
                {
                    Write(stream, _fieldList, doLogging);
                }
            }
            catch (Exception ex)
            {
                var errorString = $"Failed to write Patient record: {ex.Message}";
                EventLogger.Error(errorString);
                throw new Exception(errorString);
            }
        }

        public void Clear()
        {
            Clear(_fieldList);
        }

        /// <summary>
        /// Gets or sets the patient identifier.
        /// </summary>
        /// <value>The patient identifier.</value>
        [JsonProperty("PatientID")]
        [XmlElement("PatientID")]
        public string PatientID
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("rxsys_patid")));
                return f?.TagData;
            }

            set
            {
                SetField(_fieldList, value ?? throw new ArgumentException("Patient record must have an ID"), "RxSys_PatID");
            }
        }

        /// <summary>
        /// Gets or sets the location identifier.
        /// </summary>
        /// <value>The patient identifier.</value>
        [JsonProperty("LocationID")]
        [XmlElement("LocationID")]
        public string LocationID
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("rxsys_locid")));
                return f?.TagData;
            }

            set
            {
                SetField(_fieldList, value, "RxSys_LocID");
            }
        }

        /// <summary>
        /// Gets or sets the primary prescriber identifier.
        /// </summary>
        /// <value>The patient identifier.</value>
        [JsonProperty("PrimaryPrescriberID")]
        [XmlElement("PrimaryPrescriberID")]
        public string PrimaryPrescriberID
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("rxsys_primarydoc")));
                if (f == null)
                {
                    return string.Empty;
                }

                return f?.TagData;
            }

            set
            {
                SetField(_fieldList, value, "RxSys_PrimaryDoc");
            }
        }

        /// <summary>/// <summary>
        /// Gets or sets the alternate prescriber.
        /// </summary>
        /// <value>The rx sys alternate document.</value>
        [JsonProperty("AlternatePrescriberID")]
        [XmlElement("AlternatePrescriberID")]
        public string AlternatePrescriberID
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("rxsys_altdoc")));
                return f?.TagData;
            }

            set
            {
                SetField(_fieldList, value, "RxSys_AltDoc");
            }
        }

        /// <summary>
        /// Gets or sets the prvios prescriber identifier.
        /// </summary>
        /// <value>The patient identifier.</value>
        [JsonProperty("LastPrescriberID")]
        [XmlElement("LastPrescriberID")]
        public string LastPrescriberID
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("rxsys_lastdoc")));
                return f?.TagData;
            }

            set
            {
                SetField(_fieldList, value, "RxSys_LastDoc");
            }
        }

        /// <summary>
        /// Gets or sets the last name.
        /// </summary>
        /// <value>The last name.</value>
        [JsonProperty("LastName")]
        [XmlElement("LastName")]
        public string LastName
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("lastname")));
                return f?.TagData;
            }

            set
            {
                SetField(_fieldList, value, "LastName");
            }
        }

        /// <summary>
        /// Gets or sets the first name.
        /// </summary>
        /// <value>The first name.</value>
        [JsonProperty("FirstName")]
        [XmlElement("FirstName")]
        public string FirstName
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("firstname")));
                return f?.TagData;
            }

            set
            {
                SetField(_fieldList, value, "FirstName");
            }
        }

        /// <summary>
        /// Gets or sets the middle initial.
        /// </summary>
        /// <value>The middle initial.</value>
        [JsonProperty("MiddleInitial")]
        [XmlElement("MiddleInitial")]
        public string MiddleInitial
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("middleinitial")));
                return f?.TagData;
            }

            set
            {
                SetField(_fieldList, NormalizeString(value), "MiddleInitial");
            }
        }

        /// <summary>
        /// Gets or sets the address1.
        /// </summary>
        /// <value>The address1.</value>
        [JsonProperty("Address1")]
        [XmlElement("Address1")]
        public string Address1
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("address1")));
                return f?.TagData;
            }

            set
            {
                SetField(_fieldList, value, "Address1");
            }
        }

        /// <summary>
        /// Gets or sets the address2.
        /// </summary>
        /// <value>The address2.</value>
        [JsonProperty("Address2")]
        [XmlElement("Address2")]
        public string Address2
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("address2")));
                return f?.TagData;
            }

            set
            {
                SetField(_fieldList, value, "Address2");
            }
        }

        /// <summary>
        /// Gets or sets the city.
        /// </summary>
        /// <value>The city.</value>
        [JsonProperty("City")]
        [XmlElement("City")]
        public string City
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("city")));
                return f?.TagData;
            }

            set
            {
                SetField(_fieldList, value, "City");
            }
        }

        /// <summary>
        /// Gets or sets the state.
        /// </summary>
        /// <value>The state.</value>
        [JsonProperty("State")]
        [XmlElement("State")]
        public string State
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("state")));
                return f?.TagData;
            }

            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    SetField(_fieldList, !string.IsNullOrEmpty(value) ? value.Substring(0,2).ToUpper() : "XX", "State");
                }
            }
        }

        /// <summary>
        /// Gets or sets the zip.
        /// </summary>
        /// <value>The zip.</value>
        [JsonProperty("Zipcode")]
        [XmlElement("Zipcode")]
        public string Zipcode
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("zip")));
                return f?.TagData;
            }

            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    while (value.Contains("-"))
                    {
                        value = value.Remove(value.IndexOf("-"), 1);
                    }
                }

                SetField(_fieldList, NormalizeString(value), "Zip");
            }
        }

        /// <summary>
        /// Gets or sets the phone1.
        /// </summary>
        /// <value>The phone1.</value>
        [JsonProperty("Phone1")]
        [XmlElement("Phone1")]
        public string Phone1
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("phone1")));
                return f?.TagData;
            }

            set
            {
                SetField(_fieldList, NormalizeString(value), "Phone1");
            }
        }

        /// <summary>
        /// Gets or sets the phone2.
        /// </summary>
        /// <value>The phone2.</value>
        [JsonProperty("Phone2")]
        [XmlElement("Phone2")]
        public string Phone2
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("phone2")));
                return f?.TagData;
            }

            set
            {
                SetField(_fieldList, NormalizeString(value), "Phone2");
            }
        }

        /// <summary>
        /// Gets or sets the work phone.
        /// </summary>
        /// <value>The work phone.</value>
        [JsonProperty("WorkPhone")]
        [XmlElement("WorkPhone")]
        public string WorkPhone
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("workphone")));
                return f?.TagData;
            }

            set
            {
                SetField(_fieldList, NormalizeString(value), "WorkPhone");
            }
        }

        /// <summary>
        /// Gets or sets the room.
        /// </summary>
        /// <value>The room.</value>
        [JsonProperty("Room")]
        [XmlElement("Room")]
        public string Room
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("room")));
                return f?.TagData;
            }

            set
            {
                SetField(_fieldList, value, "Room");
            }
        }

        /// <summary>
        /// Gets or sets the comments.
        /// </summary>
        /// <value>The comments.</value>
        [JsonProperty("Coments")]
        [XmlElement("Comments")]
        public string Comments
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains((("comments"))));
                return f?.TagData;
            }

            set
            {
                SetField(_fieldList, value, "Comments");
            }
        }

        /// <summary>
        /// Gets or sets the cycle date.
        /// </summary>
        /// <value>The cycle date.</value>
        [JsonProperty("CycleDate", ItemConverterType = typeof(DateTime))]
        [XmlElement("CycleDate", typeof(DateTime))]
        public DateTime CycleDate
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains((("cycledate"))));
                return TransformDate(f?.TagData);
            }

            set
            {
                SetField(_fieldList, NormalizeDate(value.ToString("yyyy-MM-dd")), "CycleDate");
            }
        }

        /// <summary>
        /// Gets or sets the cycle days.
        /// </summary>
        /// <value>The cycle days.</value>
        [JsonProperty("CycleDays")]
        [XmlElement("CycleDays", typeof(int))]
        public int CycleDays
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains((("cycledays"))));
                return !string.IsNullOrEmpty(f.TagData) ? Convert.ToInt32(f.TagData) : 0;
            }

            set
            {

                if (value > 35 || value < 0)
                {
                    throw new Exception("CycleDays must be (0-35)");
                }

                SetField(_fieldList, value.ToString(), "CycleDays");
            }
        }

        /// <summary>
        /// Gets or sets the type of the cycle.
        /// </summary>
        /// <value>The type of the cycle.</value>
        [JsonProperty("CycleType")]
        [XmlElement("CycleType", typeof(int))]
        public int CycleType
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains((("cycletype"))));
                return !string.IsNullOrEmpty(f.TagData) ? Convert.ToInt32(f.TagData) : 0;
            }

            set
            {
                // Actual error - default to 0
                if (value != 0 && value != 1)
                {
                    //throw new Exception("CycleType must be '0 - Monthly' or '1 - Weekly'");
                    value = 0;
                }

                SetField(_fieldList, value.ToString(), "CycleType");
            }
        }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        [JsonProperty("Status")]
        [XmlElement("Status", typeof(int))]
        public int Status
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains((("status"))));
                return !string.IsNullOrEmpty(f.TagData) ? Convert.ToInt32(f.TagData) : 0;
            }

            set
            {
                // Actual error - Dedfault to Hold
                if (value != 0 && value != 1)
                {
                    //throw new Exception("Status must be '0 - Hold' or '1 - for Active'");
                    value = 0;
                }

                SetField(_fieldList, value.ToString(), "Status");
            }
        }
         
        /// <summary>
        /// Gets or sets the ssn.
        /// </summary>
        /// <value>The ssn.</value>
        [JsonProperty("SSN")]
        [XmlElement("SSN")]
        public string SSN
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("ssn")));
                return f?.TagData;
            }

            set
            {
                SetField(_fieldList, NormalizeString(value), "SSN");
            }
        }

        /// <summary>
        /// Gets or sets the allergies.
        /// </summary>
        /// <value>The allergies.</value>
        [JsonProperty("Allergies")]
        [XmlElement("Allergies")]
        public string Allergies
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("allergies")));
                return f?.TagData;
            }

            set
            {
                SetField(_fieldList, value ?? string.Empty, "Allergies");
            }
        }

        /// <summary>
        /// Gets or sets the diet.
        /// </summary>
        /// <value>The diet.</value>
        [JsonProperty("Diet")]
        [XmlElement("Diet")]
        public string Diet
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("diet")));
                return f?.TagData;
            }

            set
            {
                SetField(_fieldList, value ?? string.Empty, "Diet");
            }
        }

        /// <summary>
        /// Gets or sets the dx notes.
        /// </summary>
        /// <value>The dx notes.</value>
        [JsonProperty("DxNotes")]
        [XmlElement("DxNotes")]
        public string DxNotes
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("dxnotes")));
                return f?.TagData;
            }

            set
            {
                SetField(_fieldList, value ?? string.Empty, "DxNotes");
            }
        }

        /// <summary>
        /// Gets or sets the treatment notes.
        /// </summary>
        /// <value>The treatment notes.</value>
        [JsonProperty("TreatmentNotes")]
        [XmlElement("TreatmentNotes")]
        public string TreatmentNotes
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("treatmentnotes")));
                return f?.TagData;
            }

            set
            {
                SetField(_fieldList, value ?? string.Empty, "TreatmentNotes");
            }
        }

        /// <summary>
        /// Gets or sets the dob.
        /// </summary>
        /// <value>The dob.</value>
        [JsonProperty("DOB")]
        [XmlElement("DOB")]
        public DateTime DOB
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("dob")));
                return TransformDate(f?.TagData);
            }
            
            set
            {
                if (value.ToString("yyyy-MM-dd") != "1970-01-01")
                {
                    SetField(_fieldList, NormalizeDate(value.ToString("yyyy-MM-dd")), "DOB");
                }
            }
        }

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        /// <value>The height.</value>
        [JsonProperty("Height", ItemConverterType = typeof(int))]
        [XmlElement("Height", typeof(int))]
        public int Height
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("height")));
                return !string.IsNullOrEmpty(f.TagData) ? Convert.ToInt32(f.TagData) : 0;
            }

            set
            {
                SetField(_fieldList, value.ToString(), "Height");
            }
        }

        /// <summary>
        /// Gets or sets the weight.
        /// </summary>
        /// <value>The weight.</value>
        [JsonProperty("Weight", ItemConverterType = typeof(int))]
        [XmlElement("Weight", typeof(int))]
        public int Weight
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("weight")));
                return !string.IsNullOrEmpty(f.TagData) ? Convert.ToInt32(f.TagData) : 0;
            }

            set
            {
                SetField(_fieldList, value.ToString(), "Weight");
            }
        }

        /// <summary>
        /// Gets or sets the name of the responisble.
        /// </summary>
        /// <value>The name of the responisble.</value>
        [JsonProperty("ResponsibleName")]
        [XmlElement("ResponsibleName")]
        public string ResponisbleName
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("responsiblename")));
                return f?.TagData;
            }

            set
            {
                SetField(_fieldList, value ?? string.Empty, "ResponsibleName");
            }
        }

        /// <summary>
        /// Gets or sets the name of the ins.
        /// </summary>
        /// <value>The name of the ins.</value>
        [JsonProperty("InsName")]
        [XmlElement("InsName")]
        public string InsName
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("insname")));
                return f?.TagData;
            }

            set
            {
                SetField(_fieldList, value ?? string.Empty, "InsName");
            }
        }

        /// <summary>
        /// Gets or sets the ins PN.
        /// </summary>
        /// <value>The ins PN.</value>
        [JsonProperty("InsPNo")]
        [XmlElement("InsPNo")]
        public string InsPNo
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("inspno")));
                return f?.TagData;
            }

            set
            {
                SetField(_fieldList, value ?? string.Empty, "InsPNo");
            }
        }

        /// <summary>
        /// Gets or sets the name of the alternate ins.
        /// </summary>
        /// <value>The name of the alternate ins.</value>
        [JsonProperty("AltInsName")]
        [XmlElement("AltInsName")]
        public string AltInsName
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("altinsnum")));
                return f?.TagData;
            }

            set
            {
                SetField(_fieldList, value ?? string.Empty, "AltInsName");
            }
        }

        /// <summary>
        /// Gets or sets the alternate ins PN.
        /// </summary>
        /// <value>The alternate ins PN.</value>
        [JsonProperty("AltInsPNo")]
        [XmlElement("AltInsPNo")]
        public string AltInsPNo
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("altinspno")));
                return f?.TagData;
            }

            set
            {
                SetField(_fieldList, value ?? string.Empty, "AltInsPNo");
            }
        }

        /// <summary>
        /// Gets or sets the medicare number.
        /// </summary>
        /// <value>The medicare number.</value>
        [JsonProperty("MedicareNum")]
        [XmlElement("MedicareNum")]
        public string MedicareNum
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("medicarenum")));
                return f?.TagData;
            }

            set
            {
                SetField(_fieldList, value ?? string.Empty, "MCareNum");
            }
        }

        /// <summary>
        /// Gets or sets the medicaid number.
        /// </summary>
        /// <value>The medicaid number.</value>
        [JsonProperty("MedicaidNum")]
        [XmlElement("MedicaidNum")]
        public string MedicaidNum
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("medicaidnum")));
                return f?.TagData;
            }

            set
            {
                SetField(_fieldList, value ?? string.Empty, "MCaidNum");
            }
        }

        /// <summary>
        /// Gets or sets the admit date.
        /// </summary>
        /// <value>The admit date.</value>
        [JsonProperty("AdmitDate", ItemConverterType = typeof(DateTime))]
        [XmlElement("AdmitDate", typeof(DateTime))]
        public DateTime AdmitDate
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("admitdate")));
                return TransformDate(f?.TagData);
            }

            set
            {
                if (value.ToString("yyyy-MM-dd") != "1970-01-01" && 
                    value.ToString("yyyy-MM-dd") != "0001-01-01")
                {
                    SetField(_fieldList, NormalizeDate(value.ToString("yyyy-MM-dd")), "AdmitDate");
                }
            }
        }

        /// <summary>
        /// Gets or sets the chart only.
        /// </summary>
        /// <value>The chart only.</value>
        [JsonProperty("ChartOnly")]
        [XmlElement("ChartOnly")]
        public string ChartOnly
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("chartonly")));
                return f?.TagData;
            }

            set
            {
                SetField(_fieldList, value ?? "0", "ChartOnly");
            }
        }

        /// <summary>
        /// Gets or sets the gender.
        /// </summary>
        /// <value>The gender.</value>
        [JsonProperty("Gender")]
        [XmlElement("Gender")]
        public string Gender
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("gender")));
                return f?.TagData;
            }

            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    if (value?.ToUpper() != "F" && value?.ToUpper() != "M")
                    {
                        value = "U";
                    }

                    SetField(_fieldList, value, "Gender");
                }
            }
        }

        /// <summary>
        /// Sets the email.
        /// </summary>
        /// <value>The email.</value>
        [JsonProperty("Email")]
        [XmlElement("Email")]
        public string Email
        {
            set
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("comments")));
                f.TagData += $"\nEmail: {value ?? "none"}\n";
            }
        }

        /// <summary>
        /// Sets the im.
        /// </summary>
        /// <value>The im.</value>
        [JsonProperty("IM")]
        [XmlElement("IM")]
        public string IM
        {
            set
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("comments")));
                f.TagData += $"\nIM: {value ?? "none"}\n";
            }
        }
    }
}