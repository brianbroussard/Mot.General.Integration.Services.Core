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
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace Mot.Common.Interface.Lib
{
    /// <summary>
    /// Prescription Record  (Key == G)
    /// </summary>
    [Serializable]
    [XmlRoot("MotPrescriptionRecord")]
    public class MotPrescriptionRecord : MotRecordBase
    {
        private readonly List<Field> _fieldList;

        private void CreateRecord(string tableAction)
        {
            try
            {
                _fieldList.Add(new Field("Table", "Rx", 10, true, 'a'));
                _fieldList.Add(new Field("Action", tableAction, 10, true, 'a'));
                _fieldList.Add(new Field("RxSys_RxNum", "", 36, true, 'k'));
                _fieldList.Add(new Field("RxSys_PatID", "", 36, true, 'a'));
                _fieldList.Add(new Field("RxSys_DocID", "", 36, true, 'a'));
                _fieldList.Add(new Field("RxSys_DrugID", "", 36, true, 'a'));
                _fieldList.Add(new Field("Sig", "", 32767, true, 'a'));
                _fieldList.Add(new Field("RxStartDate", "", 10, true, 'w'));
                _fieldList.Add(new Field("RxStopDate", "", 10, true, 'w'));
                _fieldList.Add(new Field("DiscontinueDate", "", 10, true, 'w'));
                _fieldList.Add(new Field("DoseScheduleName", "", 10, false, 'n'));
                _fieldList.Add(new Field("Comments", "", 32767, false, 'n'));
                _fieldList.Add(new Field("Refills", "", 4, true, 'a'));
                _fieldList.Add(new Field("RxSys_NewRxNum", "", 36, false, 'w'));
                _fieldList.Add(new Field("Isolate", "", 2, false, 'n'));
                _fieldList.Add(new Field("RxType", "", 2, true, 'w'));
                _fieldList.Add(new Field("MDOMStart", "", 2, false, 'n'));
                _fieldList.Add(new Field("MDOMEnd", "", 2, false, 'n'));
                _fieldList.Add(new Field("QtyPerDose", "", 6, true, 'w'));
                _fieldList.Add(new Field("QtyDispensed", "", 10, true, 'a'));
                _fieldList.Add(new Field("Status", "", 3, true, 'w'));
                _fieldList.Add(new Field("DoW", "", 7, true, 'w'));
                _fieldList.Add(new Field("SpecialDoses", "", 32767, false, 'n'));
                _fieldList.Add(new Field("DoseTimesQtys", "", 32767, true, 'w'));
                _fieldList.Add(new Field("ChartOnly", "", 2, true, 'w'));
                _fieldList.Add(new Field("AnchorDate", "", 10, true, 'w'));
            }
            catch (Exception ex)
            {
                EventLogger.Error($"Prescription construction {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Shallow copy.
        /// </summary>
        /// <returns>The copy.</returns>
        public MotPrescriptionRecord ShallowCopy()
        {
            return (MotPrescriptionRecord)this.MemberwiseClone();
        }

        public static MotPrescriptionRecord DeepCopy(MotPrescriptionRecord other)
        {
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, other);
                ms.Position = 0;
                return (MotPrescriptionRecord)formatter.Deserialize(ms);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Mot.Common.Interface.Lib.MotPrescriptionRecord"/> class.
        /// </summary>
        public MotPrescriptionRecord()
        {
            recordType = RecordType.Prescription;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Mot.Common.Interface.Lib.MotPrescriptionRecord"/> class.
        /// </summary>
        /// <param name="action">Action.</param>
        /// <param name="autoTruncate">If set to <c>true</c> auto truncate.</param>
        public MotPrescriptionRecord(string action, bool autoTruncate = false)
        {
            recordType = RecordType.Prescription;
            AutoTruncate = autoTruncate;

            try
            {
                _fieldList = new List<Field>();
                CreateRecord(action);
            }
            catch (Exception ex)
            {
                var errorString = $"Failed to create Prescription record: {ex.Message}";
                EventLogger.Error(errorString);
                throw new Exception(errorString);
            }
        }

        /// <summary>
        /// Sets the field.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <param name="val">Value.</param>
        /// <param name="OverrideTruncation">If set to <c>true</c> override truncation.</param>
        public void SetField(string fieldName, string val, bool OverrideTruncation = false)
        {
            if (fieldName == null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            if (val == null)
            {
                throw new ArgumentNullException(nameof(val));
            }

            try
            {
                base.SetField(_fieldList, val, fieldName, OverrideTruncation);
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Adds to queue.
        /// </summary>
        /// <param name="newQueue">New queue.</param>
        public void AddToQueue(MotWriteQueue newQueue = null)
        {
            if (newQueue != null)
            {
                LocalWriteQueue = newQueue;
            }

            AddToQueue("G", _fieldList);
        }

        /// <summary>
        /// Write the specified socket and doLogging.
        /// </summary>
        /// <param name="socket">Socket.</param>
        /// <param name="doLogging">If set to <c>true</c> do logging.</param>
        public void Write(MotSocket socket)
        {
            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }

            try
            {
                if (QueueWrites)
                {
                    AddToQueue();
                }
                else
                {
                    Write(socket, _fieldList);
                }
            }
            catch (Exception ex)
            {
                var errorString = $"Failed to write Prescription record: {ex.Message}";
                EventLogger.Error(errorString);
                throw new Exception(errorString);
            }
        }

        /// <summary>
        /// Write the specified stream and DoLogging.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <param name="DoLogging">If set to <c>true</c> do logging.</param>
        public void Write(NetworkStream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            try
            {
                if (QueueWrites)
                {
                    AddToQueue();
                }
                else
                {
                    Write(stream, _fieldList);
                }
            }
            catch (Exception ex)
            {
                var errorString = $"Failed to write Prescription record: {ex.Message}";
                EventLogger.Error(errorString);
                throw new Exception(errorString);
            }
        }

        /// <summary>
        /// Clear this instance.
        /// </summary>
        public void Clear()
        {
            Clear(_fieldList);
        }

        /// <summary>
        /// Gets or sets the rx sys rx number.
        /// </summary>
        /// <value>The rx sys rx number.</value>
        [JsonProperty("RxSys_RxNum")]
        [XmlElement("RxSys_RxNum")]
        public string RxSys_RxNum
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("rxsys_rxnum")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? throw new ArgumentException("Prescription record must have an ID"), "RxSys_RxNum");
        }

        /// <summary>
        /// Gets or sets the rx sys rx number.
        /// </summary>
        /// <value>The rx sys rx number.</value>
        [JsonProperty("PrescriptionID")]
        [XmlElement("PrescriptionID")]
        public string PrescriptionID
        {
            get { return RxSys_RxNum; }
            set => RxSys_RxNum = value;
        }

        /// <summary>
        /// Gets or sets the patient identifier.
        /// </summary>
        /// <value>The patient identifier.</value>
        [JsonProperty("RxSys_PatID")]
        [XmlElement("RxSys_PatID")]
        public string RxSys_PatID
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("rxsys_patid")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? throw new ArgumentException("Prescription record must have a a patient ID"), "RxSys_PatID");
        }

        /// <summary>
        /// Gets or sets the patient id
        /// </summary>
        /// <value>The rx sys rx number.</value>
        [JsonProperty("PatientID")]
        [XmlElement("PatientID")]
        public string PatientID
        {
            get { return RxSys_PatID; }
            set => RxSys_PatID = value;
        }

        /// <summary>
        /// Gets or sets the prescriber identifier.
        /// </summary>
        /// <value>The prescriber identifier.</value>
        [JsonProperty("RxSys_DocID")]
        [XmlElement("RxSys_DocID")]
        public string RxSys_DocID
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("rxsys_docid")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? "0", "RxSys_DocID");
        }

        /// <summary>
        /// Gets or sets the patient id
        /// </summary>
        /// <value>The rx sys rx number.</value>
        [JsonProperty("PrescriberID")]
        [XmlElement("PrescriberID")]
        public string PrescriberID
        {
            get { return RxSys_DocID; }
            set => RxSys_DocID = value;
        }

        /// <summary>
        /// Gets or sets the drug identifier.
        /// </summary>
        /// <value>The rx sys drug identifier.</value>
        [JsonProperty("DrugID")]
        [XmlElement("DrugID")]
        public string DrugID
        {
            get
            {

                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("rxsys_drugid")));
                return f?.TagData;
            }

            set
            {              
                SetField(_fieldList, value?.Trim() ?? "0", "RxSys_DrugID");
            }
        }

        /// <summary>
        /// Gets or sets the sig.
        /// </summary>
        /// <value>The sig.</value>
        [JsonProperty("Sig")]
        [XmlElement("Sig")]
        public string Sig
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("sig")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? "Empty Sig", "Sig");
        }

        /// <summary>
        /// Gets or sets the rx start date.
        /// </summary>
        /// <value>The rx start date.</value>
        [JsonProperty("RxStartDate", ItemConverterType = typeof(DateTime))]
        [XmlElement("RxStartDate", typeof(DateTime))]
        public DateTime RxStartDate
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("rxstartdate")));
                return TransformDate(f?.TagData);
            }

            set
            {
                if (value.ToString("yyyy-MM-dd") != "1970-01-01" &&
                    value.ToString("yyyy-MM-dd") != "0001-01-01")
                {
                    SetField(_fieldList, NormalizeDate(value.ToString("yyyy-MM-dd") ?? ""), "RxStartDate");
                }
            }
        }

        /// <summary>
        /// Gets or sets the rx stop date.
        /// </summary>
        /// <value>The rx stop date.</value>
        [JsonProperty("RxStopDate")]
        [XmlElement("RxStopDate", typeof(DateTime))]
        public DateTime RxStopDate
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("rxstopdate")));
                return TransformDate(f?.TagData);
            }

            set
            {
                if (value.ToString("yyyy-MM-dd") != "1970-01-01" &&
                    value.ToString("yyyy-MM-dd") != "0001-01-01")
                {                 
                    if(RxStartDate > DateTime.MinValue)
                    {
                        if(value < RxStartDate)
                        {
                            EventLogger.Info($"Overrode StopDate. Start: {RxStartDate.ToShortDateString()}, Stop: {value.ToShortDateString()}");
                            value = RxStartDate;                           
                        }
                    }

                    SetField(_fieldList, NormalizeDate(value.ToString("yyyy-MM-dd") ?? ""), "RxStopDate");
                }
            }
        }

        /// <summary>
        /// Gets or sets the discontinue date.
        /// </summary>
        /// <value>The discontinue date.</value>
        [JsonProperty("DiscontinueDate")]
        [XmlElement("DiscontinueDate", typeof(DateTime))]
        public DateTime DiscontinueDate
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("discontinuedate")));
                return TransformDate(f?.TagData);
            }

            set
            {
                if (value.ToString("yyyy-MM-dd") != "1970-01-01" &&
                    value.ToString("yyyy-MM-dd") != "0001-01-01")
                {
                    SetField(_fieldList, NormalizeDate(value.ToString("yyyy-MM-dd") ?? ""), "DiscontinueDate");
                }
                else
                {
                    if (RxStartDate > DateTime.MinValue)
                    {
                        if (value < RxStartDate)
                        {
                            EventLogger.Info($"Overrode DCDate. Start: {RxStartDate.ToShortDateString()}, Stop: {value.ToShortDateString()}");
                            value = RxStartDate;
                        }
                    }

                    SetField(_fieldList, DateTime.MinValue.ToString(), "DiscontinueDate");
                }
            }
        }

        /// <summary>
        /// Gets or sets the discontinue date.
        /// </summary>
        /// <value>The discontinue date.</value>
        [JsonProperty("DCDate")]
        [XmlElement("DCDate", typeof(DateTime))]
        public DateTime DCDate
        {
            get { return DiscontinueDate; }
            set => DiscontinueDate = value;
        }

        /// <summary>
        /// Gets or sets the name of the dose schedule.
        /// </summary>
        /// <value>The name of the dose schedule.</value>
        [JsonProperty("DoseScheduleName")]
        [XmlElement("DoseScheduleName")]
        public string DoseScheduleName
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("doseschedulename")));
                return f?.TagData;
            }

            set => SetField(_fieldList, NormalizeString(value ?? "Undefined"), "DoseScheduleName");
        }

        /// <summary>
        /// Gets or sets the comments.
        /// </summary>
        /// <value>The comments.</value>
        [JsonProperty("Comments")]
        [XmlElement("Comments")]
        public string Comments
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("comments")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "Comments");
        }

        /// <summary>
        /// Gets or sets the refills.
        /// </summary>Refills
        /// <value>The refills.</value>
        [JsonProperty("Refills", ItemConverterType = typeof(int))]
        [XmlElement("Refills", typeof(int))]
        public int Refills
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("refills")));
                return !string.IsNullOrEmpty(f.TagData) ? Convert.ToInt32(f.TagData) : 0;
            }

            set
            {
                if(value > 254)
                {
                    value = 254;
                }

                SetField(_fieldList, value.ToString(), "Refills");
            }
        }

        /// <summary>
        /// Gets or sets the rx sys new rx number.
        /// </summary>
        /// <value>The rx sys new rx number.</value>
        [JsonProperty("RxSys_NewRxNum")]
        [XmlElement("RxSys_NewRxNum")]
        public string RxSys_NewRxNum
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("rxsys_newrxnum")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? "0", "RxSys_NewRxNum");
        }

        /// <summary>
        /// Gets or sets the isolate.
        /// </summary>
        /// <value>The isolate.</value>
        [JsonProperty("Isolate")]
        [XmlElement("Isolate")]
        public string Isolate
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("isolate")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? "1", "Isolate");
        }

        /// <summary>
        /// Gets or sets the type of the rx.
        /// </summary>
        /// <value>The type of the rx.</value>
        [JsonProperty("RxType", ItemConverterType = typeof(int))]
        [XmlElement("RxType", typeof(int))]
        public int RxType
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("rxtype")));
                return !string.IsNullOrEmpty(f.TagData) ? Convert.ToInt32(f.TagData) : 0;
            }

            set => SetField(_fieldList, value.ToString(), "RxType");
        }


        /// <summary>
        /// Gets or sets the MDOMS tart.
        /// </summary>
        /// <value>The MDOMS tart.</value>
        [JsonProperty("MDOMStart")]
        [XmlElement("MDOMStart")]
        public string MDOMStart
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("mdomstart")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? "0", "MDOMStart");
        }

        /// <summary>
        /// Gets or sets the MDOME nd.
        /// </summary>
        /// <value>The MDOME nd.</value>
        [JsonProperty("MDOMEnd")]
        [XmlElement("MDOMEnd")]
        public string MDOMEnd
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("mdomend")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? "28", "MDOMEnd");
        }

        /// <summary>
        /// Gets or sets the qty per dose.
        /// </summary>
        /// <value>The qty per dose.</value>
        [JsonProperty("QtyPerDose", ItemConverterType = typeof(double))]
        [XmlElement("QtyPerDose", typeof(double))]
        public double QtyPerDose
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("qtyperdose")));
                return !string.IsNullOrEmpty(f.TagData) ? Convert.ToDouble(f.TagData) : 0;
            }

            set => SetField(_fieldList, value.ToString(), "QtyPerDose");
        }

        /// <summary>
        /// Gets or sets the qty dispensed.
        /// </summary>
        /// <value>The qty dispensed.</value>
        [JsonProperty("QtyDispensed", ItemConverterType = typeof(double))]
        [XmlElement("QtyDispensed", typeof(double))]
        public double QtyDispensed
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("qtydispensed")));
                return !string.IsNullOrEmpty(f.TagData) ? Convert.ToDouble(f.TagData) : 0;
            }

            set => SetField(_fieldList, value.ToString(), "QtyDispensed");
        }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        [JsonProperty("Status", ItemConverterType = typeof(int))]
        [XmlElement("Status", typeof(int))]
        public int Status
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("status")));
                return !string.IsNullOrEmpty(f.TagData) ? Convert.ToInt32(f.TagData) : 0;
            }

            set => SetField(_fieldList, value.ToString(), "Status");
        }

        /// <summary>
        /// Gets or sets the do w.
        /// </summary>
        /// <value>The do w.</value>
        [JsonProperty("DoW")]
        [XmlElement("DoW")]
        public string DoW
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("dow")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? "0000000", "DoW");
        }

        /// <summary>
        /// Gets or sets the special doses.
        /// </summary>
        /// <value>The special doses.</value>
        [JsonProperty("SpecialDoses")]
        [XmlElement("SpecialDoses")]
        public string SpecialDoses
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("specialdoses")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? "0", "SpecialDoses");
        }

        /// <summary>
        /// Gets or sets the dose times qtys.
        /// </summary>
        /// <value>The dose times qtys.</value>
        [JsonProperty("DoseTimesQtys")]
        [XmlElement("DoseTimesQtys")]
        public string DoseTimesQtys
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("dosetimesqtys")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? "00.00", "DoseTimesQtys");
        }

        /// <summary>
        /// Gets or sets the chart only.
        /// </summary>
        /// <value>The chart only.</value>
        [JsonProperty("ChartOnly", ItemConverterType = typeof(int))]
        [XmlElement("ChartOnly", typeof(int))]
        public int ChartOnly
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("chartonly")));
                return !string.IsNullOrEmpty(f.TagData) ? Convert.ToInt32(f.TagData) : 0;
            }

            set => SetField(_fieldList, value.ToString(), "ChartOnly");
        }

        [JsonProperty("AnchorDate", ItemConverterType = typeof(DateTime))]
        [XmlElement("AnchorDate", typeof(DateTime))]
        public DateTime AnchorDate
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("anchordate")));
                return TransformDate(f?.TagData);
            }

            set
            {
                if (value.ToString("yyyy-MM-dd") != "1970-01-01" &&
                    value.ToString("yyyy-MM-dd") != "0001-01-01")
                {
                    SetField(_fieldList, NormalizeDate(value.ToString("yyyy-MM-dd")), "AnchorDate");
                }
            }
        }
    }
}
