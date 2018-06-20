// 
// MIT license
//
// Copyright (c) 2016 by Peter H. Jenney and Medicine-On-Time, LLC.
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

namespace MotCommonLib
{
    /// <summary>
    /// Patient Record (Key == D)
    /// </summary>
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
                _fieldList.Add(new Field("RxSys_PatID", "", 10, true, 'k'));
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
                _fieldList.Add(new Field("RxSys_LocID", "", 10, true, 'w'));
                _fieldList.Add(new Field("Room", "", 10, true, 'w'));
                _fieldList.Add(new Field("Comments", "", 32767, false, 'n'));
                _fieldList.Add(new Field("CycleDate", "", 10, false, 'n'));
                _fieldList.Add(new Field("CycleDays", "", 2, false, 'n'));
                _fieldList.Add(new Field("CycleType", "", 2, false, 'n'));
                _fieldList.Add(new Field("Status", "", 2, false, 'n'));
                _fieldList.Add(new Field("RxSys_LastDoc", "", 10, false, 'n'));
                _fieldList.Add(new Field("RxSys_PrimaryDoc", "", 10, false, 'n'));
                _fieldList.Add(new Field("RxSys_AltDoc", "", 10, false, 'n'));
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

        public void Write(MotSocket socket, bool DoLogging = false)
        {
            try
            {
                if (QueueWrites)
                {
                    AddToQueue();
                }
                else
                {
                    Write(socket, _fieldList, DoLogging);
                }
            }
            catch (Exception ex)
            {
                var errorString = $"Failed to write Patient record: {ex.Message}";
                EventLogger.Error(errorString);
                throw new Exception(errorString);
            }
        }
        public void Write(NetworkStream stream, bool LogOn = false)
        {
            try
            {
                if (QueueWrites)
                {
                    AddToQueue();
                }
                else
                {
                    Write(stream, _fieldList, LogOn);
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

        [XmlElement("ID")]
        public string ID
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("rxsys_patid")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, value, "RxSys_PatID");
            }
        }
        [Obsolete("Use ID")]
        public string RxSys_PatID
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("rxsys_patid")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, value, "RxSys_PatID");
            }
        }

        [XmlElement("PrescriberID")]
        public string PrescriberID
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("rxsys_primarydoc")));
                if (f == null)
                {
                    return string.Empty;
                }

                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, value, "RxSys_PrimaryDoc");
            }
        }
        [Obsolete("Use PrescriberID")]
        public string RxSys_DocID
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("rxsys_primarydoc")));
                if (f == null)
                {
                    return string.Empty;
                }

                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, value, "RxSys_PrimaryDoc");
            }
        }

        [XmlElement("LastName")]
        public string LastName
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("lastname")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, value, "LastName");
            }
        }

        [XmlElement("FirstName")]
        public string FirstName
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("firstname")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, value, "FirstName");
            }
        }
        [XmlElement("MiddleInitial")]
        public string MiddleInitial
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("middleinitial")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, NormalizeString(value), "MiddleInitial");
            }
        }
        [XmlElement("Address1")]
        public string Address1
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("address1")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, value, "Address1");
            }
        }
        [XmlElement("Address2")]
        public string Address2
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("address2")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, value, "Address2");
            }
        }
        [XmlElement("City")]
        public string City
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("city")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, value, "City");
            }
        }
        [XmlElement("State")]
        public string State
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("state")));
                return f?.tagData;
            }

            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    SetField(_fieldList, value?.ToUpper(), "State");
                }
            }
        }
        [XmlElement("PostalCode")]
        public string PostalCode
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("zip")));
                return f?.tagData;
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
        [XmlElement("Zip")]
        public string Zip
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("zip")));
                return f?.tagData;
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
        [XmlElement("Phone1")]
        public string Phone1
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("phone1")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, NormalizeString(value), "Phone1");
            }
        }
        [XmlElement("Phone2")]
        public string Phone2
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("phone2")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, NormalizeString(value), "Phone2");
            }
        }
        [XmlElement("WorkPhone")]
        public string WorkPhone
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("workphone")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, NormalizeString(value), "WorkPhone");
            }
        }
        [XmlElement("LocationID")]
        public string LocationID
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("rxsys_locid")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, value, "RxSys_LocID");
            }
        }
        [Obsolete("Use LocationID")]
        public string RxSys_LocID
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("rxsys_locid")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, value, "RxSys_LocID");
            }
        }
        [XmlElement("Room")]
        public string Room
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("room")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, value, "Room");
            }
        }
        [XmlElement("Comments")]
        public string Comments
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains((("comments"))));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, value, "Comments");
            }
        }
        [XmlElement("CycleDate")]
        public string CycleDate
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains((("cycledate"))));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, NormalizeDate(value), "CycleDate");
            }
        }
        [XmlElement("CycleDays", typeof(int))]
        public int CycleDays
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains((("cycledays"))));
                return !string.IsNullOrEmpty(f.tagData) ? Convert.ToInt32(f.tagData) : 0;
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
        [XmlElement("CycleType", typeof(int))]
        public int CycleType
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains((("cycletype"))));
                return !string.IsNullOrEmpty(f.tagData) ? Convert.ToInt32(f.tagData) : 0;
            }

            set
            {
                // Actual error - default to 0
                if (value != 0 && value != 1)
                {
                    //throw new Exception("CycleType must be '0 - Monthly' or '1 - Weekly'");
                    value = 0;
                }

                SetField(_fieldList, Convert.ToString(value), "CycleType");
            }
        }
        [XmlElement("Status", typeof(int))]
        public int Status
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains((("status"))));
                return !string.IsNullOrEmpty(f.tagData) ? Convert.ToInt32(f.tagData) : 0;
            }

            set
            {
                // Actual error - Dedfault to Hold
                if (value != 0 && value != 1)
                {
                    //throw new Exception("Status must be '0 - Hold' or '1 - for Active'");
                    value = 0;
                }

                SetField(_fieldList, Convert.ToString(value), "Status");
            }
        }
        [XmlElement("RxSys_LastDoc")]
        public string RxSys_LastDoc
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("rxsys_lastdoc")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, value, "RxSys_LastDoc");
            }
        }
        [XmlElement("PrimaryPrescriber")]
        public string PrimaryPrescriber
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("rxsys_primarydoc")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, value, "RxSys_PrimaryDoc");
            }
        }
        [Obsolete("Use PrimaryPrescriber")]
        public string RxSys_PrimaryDoc
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("rxsys_primarydoc")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, value, "RxSys_PrimaryDoc");
            }
        }

        [XmlElement("AlternatePrescriber")]
        public string AlternatePrescriber
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("rxsys_altdoc")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, value, "RxSys_AltDoc");
            }
        }
        [Obsolete("Use AlternatePrescriber")]
        public string RxSys_AltDoc
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("rxsys_altdoc")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, value, "RxSys_AltDoc");
            }
        }
        [XmlElement("SSN")]
        public string SSN
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("ssn")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, NormalizeString(value), "SSN");
            }
        }
        [XmlElement("Allergies")]
        public string Allergies
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("allergies")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, value, "Allergies");
            }
        }
        [XmlElement("Diet")]
        public string Diet
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("diet")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, value, "Diet");
            }
        }
        [XmlElement("DxNotes")]
        public string DxNotes
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("dxnotes")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, value, "DxNotes");
            }
        }
        [XmlElement("TreatmentNotes")]
        public string TreatmentNotes
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("treatmentnotes")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, value, "TreatmentNotes");
            }
        }
        [XmlElement("DOB")]
        public string DOB
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("dob")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, NormalizeDate(value), "DOB");
            }
        }
        [XmlElement("Height", typeof(int))]
        public int Height
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("height")));
                return !string.IsNullOrEmpty(f.tagData) ? Convert.ToInt32(f.tagData) : 0;
            }

            set
            {
                SetField(_fieldList, Convert.ToString(value), "Height");
            }
        }
        [XmlElement("Weight", typeof(int))]
        public int Weight
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("weight")));
                return !string.IsNullOrEmpty(f.tagData) ? Convert.ToInt32(f.tagData) : 0;
            }

            set
            {
                SetField(_fieldList, Convert.ToString(value), "Weight");
            }
        }
        [XmlElement("ResponsibleName")]
        public string ResponisbleName
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("responsiblename")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, value, "ResponsibleName");
            }
        }
        [XmlElement("InsName")]
        public string InsName
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("insname")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, value, "InsName");
            }
        }
        [XmlElement("InsPNo")]
        public string InsPNo
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("inspno")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, value, "InsPNo");
            }
        }
        [XmlElement("AltInsName")]
        public string AltInsName
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("altinsnum")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, value, "AltInsName");
            }
        }
        [XmlElement("AltInsPNo")]
        public string AltInsPNo
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("altinspno")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, value, "AltInsPNo");
            }
        }
        [XmlElement("MedicareNum")]
        public string MedicareNum
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("medicarenum")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, value, "MCareNum");
            }
        }
        [XmlElement("MedicaidNum")]
        public string MedicaidNum
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("medicaidnum")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, value, "MCaidNum");
            }
        }
        [XmlElement("AdmitDate")]
        public string AdmitDate
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("admitdate")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, NormalizeDate(value), "AdmitDate");
            }
        }
        [XmlElement("ChartOnly")]
        public string ChartOnly
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("chartonly")));
                return f?.tagData;
            }

            set
            {
                SetField(_fieldList, value, "ChartOnly");
            }
        }
        [XmlElement("Gender")]
        public string Gender
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("gender")));
                return f?.tagData;
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
        [XmlElement("Email")]
        public string Email
        {
            set
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("comments")));
                f.tagData += $"\nEmail: {value}\n";
            }
        }
        [XmlElement("IM")]
        public string IM
        {
            set
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("comments")));
                f.tagData += $"\nIM: {value}\n";
            }
        }
    }
}