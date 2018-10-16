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
using System.Globalization;
using System.Net.Sockets;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace Mot.Common.Interface.Lib
{
    /// <summary>
    ///  Drug Record (Key == E)
    /// </summary>
    [Serializable]
    [XmlRoot("MotDrugRecord")]
    [JsonObject(id: "MotDrugRecord")]
    public class MotDrugRecord : MotRecordBase
    {
        private readonly List<Field> _fieldList;

        private void CreateRecord(string tableAction)
        {
            TableAction = tableAction.ToLower();

            try
            {
                _fieldList.Add(new Field("Table", "Drug", 10, true, 'a'));
                _fieldList.Add(new Field("Action", tableAction, 10, true, 'a'));
                _fieldList.Add(new Field("RxSys_DrugID", "", 36, true, 'k'));
                _fieldList.Add(new Field("LblCode", "", 6, false, 'n', true));
                _fieldList.Add(new Field("ProdCode", "", 4, false, 'n'));
                _fieldList.Add(new Field("TradeName", "", 100, false, 'n'));
                _fieldList.Add(new Field("Strength", "", 10, false, 'n'));
                _fieldList.Add(new Field("Unit", "", 10, false, 'n'));
                _fieldList.Add(new Field("RxOTC", "", 1, false, 'n'));
                _fieldList.Add(new Field("DoseForm", "", 11, false, 'n'));
                _fieldList.Add(new Field("Route", "", 9, false, 'n'));
                _fieldList.Add(new Field("DrugSchedule", "", 1, false, 'n'));
                _fieldList.Add(new Field("VisualDescription", "", 12, false, 'n', true));
                _fieldList.Add(new Field("DrugName", "", 40, true, 'a'));
                _fieldList.Add(new Field("ShortName", "", 16, false, 'n'));
                _fieldList.Add(new Field("NDCNum", "", 11, true, 'w'));
                _fieldList.Add(new Field("SizeFactor", "", 2, false, 'n'));
                _fieldList.Add(new Field("Template", "", 1, false, 'n', true));
                _fieldList.Add(new Field("DefaultIsolate", "", 1, false, 'n'));
                _fieldList.Add(new Field("ConsultMsg", "", 45, false, 'n'));
                _fieldList.Add(new Field("GenericFor", "", 40, false, 'n'));
            }
            // ReSharper disable once RedundantCatchClause
            catch
            {
                throw;
            }
        }


        /// <summary>
        /// <c>motDrugRecord</c>
        /// Default constructor
        /// </summary>
        public MotDrugRecord()
        {
            recordType = RecordType.Drug;
        }

        /// <summary>
        /// <c>motRecord</c>
        /// Costructor with an explicit purpose (Add, Change, Delete)
        /// </summary>
        /// <param name="action"></param>
        /// <param name="autoTruncate"></param> 
        public MotDrugRecord(string action, bool autoTruncate = false)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            recordType = RecordType.Drug;
            AutoTruncate = autoTruncate;

            try
            {
                _fieldList = new List<Field>();
                CreateRecord(action);
            }
            catch (Exception e)
            {
                var errorString = $"Failed to create Drug record: {e.Message}";
                EventLogger.Error(errorString);
                Console.Write(errorString);

                throw;
            }
        }

        /// <summary>
        /// <c>AddToQueue</c>
        /// Adds the FieldList to a specfic queue
        /// </summary>
        /// <param name="newQueue"></param>
        public void AddToQueue(MotWriteQueue newQueue = null)
        {
            if (newQueue != null)
            {
                LocalWriteQueue = newQueue;
            }

            AddToQueue("E", _fieldList);
        }

        /// <summary>
        /// <c>Write</c>
        /// Writes the current FieldList to the passed socket or queues it
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="doLogging"></param>
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
                    Write(socket, _fieldList);
                }
            }
            catch (Exception ex)
            {
                var errorString = $"Failed to write Drug record: {ex.Message}";
                EventLogger.Error(errorString);
                throw new Exception(errorString);
            }
        }

        /// <summary>
        /// <c>Write</c>
        /// Writes the current FieldList to the passed socket or queues it
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="doLogging"></param>
        public void Write(NetworkStream stream)
        {
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
                var errorString = $"Failed to write Drug record: {ex.Message}";
                EventLogger.Error(errorString);

                throw new Exception(errorString);
            }
        }

        /// <summary>
        /// <c>Clear</c>
        /// Empties the FieldList
        /// </summary>
        public void Clear()
        {
            Clear(_fieldList);
        }

        /// <summary>
        /// <c>SetField</c>
        /// Sets the value of a field and optionally overrides truncation if set
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="val"></param>
        /// <param name="overrideTruncation"></param>
        public void SetField(string fieldName, string val, bool overrideTruncation = false)
        {
            try
            {
                SetField(_fieldList, val, fieldName, overrideTruncation);
            }
            // ReSharper disable once RedundantCatchClause
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Gets or sets the drug identifier.
        /// </summary>
        /// <value>The rx sys drug identifier.</value>
        [JsonProperty("RxSys_DrugID")]
        [XmlElement("RxSys_DrugID")]
        // ReSharper disable once InconsistentNaming
        public string RxSys_DrugID
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("rxsys_drugid")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? throw new ArgumentException("Drug record must have an ID"), "RxSys_DrugID");
        }

        /// <summary>
        /// DrugID
        /// </summary>
        [JsonProperty("DrugID")]
        [XmlElement("DrugID")]
        public string DrugID
        {
            get { return RxSys_DrugID; }
            set => RxSys_DrugID = value;
        }

        /// <summary>
        /// <c>LabelCode</c>
        /// The label code portion of the NDC
        /// </summary>
        [JsonProperty("LabelCodeName")]
        [XmlElement("LabelCodeName")]
        public string LabelCode
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("lblcode")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? "0", "LblCode");
        }

        /// <summary>
        /// <c>ProductCode</c>
        /// The product code part of the NDC
        /// </summary>
        [JsonProperty("ProductCode")]
        [XmlElement("ProductCode")]
        public string ProductCode
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("prodcode")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? "0", "ProdCode");
        }

        /// <summary>
        /// <c>TradeName</c>
        /// Common trade name for the drug
        /// </summary>
        [JsonProperty("TradeName")]
        [XmlElement("TradeName")]
        public string TradeName
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("tradename")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "TradeName");
        }

        /// <summary>
        /// <c>Strength</c>
        /// Number of units in he drug
        /// </summary>
        [JsonProperty("Strength", ItemConverterType = typeof(double))]
        [XmlElement("Strength", typeof(double))]
        public double Strength
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("strength")));
                return !string.IsNullOrEmpty(f?.TagData) ? Convert.ToDouble(f.TagData) : 0;
            }

            set => SetField(_fieldList, value.ToString(CultureInfo.InvariantCulture), "Strength");
        }

        /// <summary>
        /// <c>Unit</c>
        /// Type of units the drug strength is measured in
        /// </summary>
        [JsonProperty("Unit")]
        [XmlElement("Unit")]
        public string Unit
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("unit")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? "0", "Unit");
        }


        /// <summary>
        /// <c>RxOTC</c>
        /// True if the drug is available over the counter
        /// </summary>
        [JsonProperty("RxOTC")]
        [XmlElement("RxOTC")]
        public string RxOTC
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("rxotc")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? "R", "RxOTC");
        }

        /// <summary>
        /// <c>DoseForm</c>
        /// How the drug is delivered e.g. Tablet, Capsule, ...
        /// </summary>
        [JsonProperty("DoseForm")]
        [XmlElement("DoseForm")]
        public string DoseForm
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("doseform")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "DoseForm");
        }

        /// <summary>
        /// <c>Route</c>
        /// How the drug is taken by a patient e.g. Oral, Injection, ...
        /// </summary>
        [JsonProperty("Route")]
        [XmlElement("Route")]
        public string Route
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("route")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "Route");
        }

        /// <summary>
        /// <c>DrugSchedule</c>
        /// The DEA Schedule number (1-7)
        /// </summary>
        [JsonProperty("DrugSchedule")]
        [XmlElement("DrugSchedule")]
        public int DrugSchedule
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("drugschedule")));
                return !string.IsNullOrEmpty(f?.TagData) ? Convert.ToInt32(f.TagData) : 0;
            }

            set
            {
                if ((value < 2 && value > 7) && value != 99)
                {
                    throw new ArgumentException("Drug Schedule must be 2-7");
                }

                SetField(_fieldList, Convert.ToString(value), "DrugSchedule");
            }
        }

        /// <summary>
        /// <c>VisualDescription</c>
        /// A shorthand version of what the drug looks like e.g. RND/WHT/412
        /// </summary>
        [JsonProperty("VisualDescription")]
        [XmlElement("VisualDescription")]
        public string VisualDescription
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("visualdescription")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "VisualDescription");
        }

        /// <summary>
        /// <c>DrugName</c>
        /// The full manufacturer or clinical name of the drug
        /// </summary>
        [JsonProperty("DrugName")]
        [XmlElement("DrugName")]
        public string DrugName
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("drugname")));
                return f?.TagData;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    var tmp = AutoTruncate;
                    AutoTruncate = false;

                    SetField(_fieldList, "Pharmacist Attention - Missing Drug Name", "DrugName");

                    AutoTruncate = tmp;

                    return;
                }

                SetField(_fieldList, value, "DrugName");
            }
        }

        /// <summary>
        /// <c>ShortName</c>
        /// A 12 character name for the drug, short enough to fit on a cup label
        /// </summary>
        [JsonProperty("ShortName")]
        [XmlElement("ShortName")]
        public string ShortName
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("shortname")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "ShortName");
        }

        /// <summary>
        /// <c>NDCNum</c>
        /// National Drug Classification.  Format is typically nnnnn-nnnn-nnnn 
        /// </summary>
        [JsonProperty("NDCNum")]
        [XmlElement("NDCNum")]
        public string NDCNum
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("ndcnum")));
                return f?.TagData;
            }

            set => SetField(_fieldList, NormalizeString(value ?? "0"), "NDCNum");
        }

        /// <summary>
        /// <c>SizeFactor</c>
        /// The number of doses that fit in a cup.  A 99 means its a bulk item
        /// </summary>
        [JsonProperty("SizeFactor")]
        [XmlElement("SizeFactor")]
        public int SizeFactor
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("sizefactor")));
                return !string.IsNullOrEmpty(f?.TagData) ? Convert.ToInt32(f.TagData) : 0;
            }

            set => SetField(_fieldList, Convert.ToString(value), "SizeFactor");
        }

        /// <summary>
        /// <c>Template</c>
        /// The template letter from MOT's filling documentation.  The size factor and template code are related. 
        /// 'XX' means it's a bulk item
        /// </summary>
        [JsonProperty("Template")]
        [XmlElement("Template")]
        public string Template
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("template")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "Template");
        }

        /// <summary>
        /// <c>DefaultIsolate</c>
        /// True if the medication has to be allone in a cup
        /// </summary>
        [JsonProperty("DefaultIsolate")]
        [XmlElement("DefaultIsolate")]
        public int DefaultIsolate
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("defaultisolate")));
                return !string.IsNullOrEmpty(f?.TagData) ? Convert.ToInt32(f.TagData) : 0;
            }

            set => SetField(_fieldList, Convert.ToString(value), "DefaultIsolate");
        }

        /// <summary>
        /// <c>ConsultMsg</c>
        /// Any special instructions associated with the drug
        /// </summary>
        [JsonProperty("ConsultMsg")]
        [XmlElement("ConsultMsg")]
        public string ConsultMsg
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("consultmsg")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "ConsultMsg");
        }

        /// <summary>
        /// <c>GenericFor</c>
        /// If the drug is a generic, the original trade name e.g.  lorazepam is the generic for Ativan
        /// </summary>
        [JsonProperty("GenericFor")]
        [XmlElement("GenericFor")]
        public string GenericFor
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("genericfor")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "GenericFor");
        }
    }
}