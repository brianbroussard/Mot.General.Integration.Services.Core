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
using System.Runtime.Serialization.Formatters.Binary;

namespace MotCommonLib
{
    /// <summary>
    /// Prescription Record  (Key == G)
    /// </summary>
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
                _fieldList.Add(new Field("RxSys_RxNum", "", 12, true, 'k'));
                _fieldList.Add(new Field("RxSys_PatID", "", 10, true, 'a'));
                _fieldList.Add(new Field("RxSys_DocID", "", 10, true, 'a'));
                _fieldList.Add(new Field("RxSys_DrugID", "", 11, true, 'a'));
                _fieldList.Add(new Field("Sig", "", 32767, true, 'a'));
                _fieldList.Add(new Field("RxStartDate", "", 10, true, 'w'));
                _fieldList.Add(new Field("RxStopDate", "", 10, true, 'w'));
                _fieldList.Add(new Field("DiscontinueDate", "", 10, true, 'w'));
                _fieldList.Add(new Field("DoseScheduleName", "", 10, false, 'n'));
                _fieldList.Add(new Field("Comments", "", 32767, false, 'n'));
                _fieldList.Add(new Field("Refills", "", 4, true, 'a'));
                _fieldList.Add(new Field("RxSys_NewRxNum", "", 10, false, 'w'));
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
#pragma warning disable 1591
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
        public MotPrescriptionRecord()
        {
        }

        /// <inheritdoc />
        public MotPrescriptionRecord(string action, bool autoTruncate = false)
        {
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
        public void AddToQueue(MotWriteQueue newQueue = null)
        {
            if (newQueue != null)
            {
                LocalWriteQueue = newQueue;
            }

            AddToQueue("G", _fieldList);
        }

        public void Write(MotSocket socket, bool doLogging = false)
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
                    Write(socket, _fieldList, doLogging);
                }
            }
            catch (Exception ex)
            {
                var errorString = $"Failed to write Prescription record: {ex.Message}";
                EventLogger.Error(errorString);
                throw new Exception(errorString);
            }
        }
        public void Write(NetworkStream stream, bool DoLogging = false)
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
                    Write(stream, _fieldList, DoLogging);
                }
            }
            catch (Exception ex)
            {
                var errorString = $"Failed to write Prescription record: {ex.Message}";
                EventLogger.Error(errorString);
                throw new Exception(errorString);
            }
        }
        public void Clear()
        {
            Clear(_fieldList);
        }

        [XmlElement("RxSys_RxNum")]
        public string RxSys_RxNum
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("rxsys_rxnum")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? "0", "RxSys_RxNum");
        }
        [XmlElement("PatientID")]
        public string PatientID
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("rxsys_patid")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? "0", "RxSys_PatID");
        }
        [XmlElement("RxSys_PatID")]
        public string RxSys_PatID
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("rxsys_patid")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? "0", "RxSys_PatID");
        }
        [XmlElement("PrescriberID")]
        public string PrescriberID
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("rxsys_docid")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? "0", "RxSys_DocID");
        }
        [XmlElement("RxSys_DocID")]
        public string RxSys_DocID
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("rxsys_docid")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? "0", "RxSys_DocID");
        }
        [XmlElement("RxSys_DrugID")]
        public string RxSys_DrugID
        {
            get
            {

                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("rxsys_drugid")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? "0", "RxSys_DrugID");
        }
        [XmlElement("Sig")]
        public string Sig
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("sig")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? "Empty Sig", "Sig");
        }
        [XmlElement("RxStartDate")]
        public string RxStartDate
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("rxstartdate")));
                return f?.tagData;
            }

            set => SetField(_fieldList, NormalizeDate(value ?? "1970-01-01"), "RxStartDate");
        }
        [XmlElement("RxStopDate")]
        public string RxStopDate
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("rxstopdate")));
                return f?.tagData;
            }

            set => SetField(_fieldList, NormalizeDate(value ?? ""), "RxStopDate");
        }
        [XmlElement("DiscontinueDate")]
        public string DiscontinueDate
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("discontinuedate")));
                return f?.tagData;
            }

            set => SetField(_fieldList, NormalizeDate(value ?? "1970-01-01"), "DiscontinueDate");
        }
        [XmlElement("DoseScheduleName")]
        public string DoseScheduleName
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("doseschedulename")));
                return f?.tagData;
            }

            set => SetField(_fieldList, NormalizeString(value ?? "MissingName"), "DoseScheduleName");
        }
        [XmlElement("Comments")]
        public string Comments
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("comments")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? " ", "Comments");
        }
        [XmlElement("Refills")]
        public string Refills
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("refills")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? "0", "Refills");
        }
        [XmlElement("RxSys_NewRxNum")]
        public string RxSys_NewRxNum
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("rxsys_newrxnum")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? "0", "RxSys_NewRxNum");
        }
        [XmlElement("Isolate")]
        public string Isolate
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("isolate")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? "1", "Isolate");
        }
        [XmlElement("RxType")]
        public string RxType
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("rxtype")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? "1", "RxType");
        }
        [XmlElement("MDOMStart")]
        public string MDOMStart
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("mdomstart")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? "0", "MDOMStart");
        }
        [XmlElement("MDOMEnd")]
        public string MDOMEnd
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("mdomend")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? "28", "MDOMEnd");
        }
        [XmlElement("QtyPerDose")]
        public string QtyPerDose
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("qtyperdose")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? "0", "QtyPerDose");
        }
        [XmlElement("QtyDispensed")]
        public string QtyDispensed
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("qtydispensed")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? "0", "QtyDispensed");
        }
        [XmlElement("Status")]
        public string Status
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("status")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? "0", "Status");
        }
        [XmlElement("DoW")]
        public string DoW
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("dow")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? "0000000", "DoW");
        }
        [XmlElement("SpecialDoses")]
        public string SpecialDoses
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("specialdoses")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? "0", "SpecialDoses");
        }
        [XmlElement("doseTimesQtys")]
        public string DoseTimesQtys
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("dosetimesqtys")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? "00.00", "DoseTimesQtys");
        }
        [XmlElement("ChartOnly")]
        public string ChartOnly
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("chartonly")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? "1", "ChartOnly");
        }
        [XmlElement("AnchorDate")]
        public string AnchorDate
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("anchordate")));
                return f?.tagData;
            }

            set => SetField(_fieldList, NormalizeDate(value ?? "1970-01-01"), "AnchorDate");
        }
    }
}
#pragma warning restore 1591
