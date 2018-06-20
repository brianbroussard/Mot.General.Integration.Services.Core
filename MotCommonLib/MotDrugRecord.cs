﻿using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Xml.Serialization;

namespace MotCommonLib
{
    /// <summary>
    ///  Drug Record (Key == E)
    /// </summary>
    [XmlRoot("MotDrugRecord")]
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
                _fieldList.Add(new Field("RxSys_DrugID", "", 11, true, 'k'));
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
                    Write(socket, _fieldList, doLogging);
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
        /// <param name="DoLogging"></param>
        public void Write(NetworkStream stream, bool DoLogging = false)
        {
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
        /// <param name="OverrideTruncation"></param>
        public void SetField(string fieldName, string val, bool OverrideTruncation = false)
        {
            try
            {
                SetField(_fieldList, val, fieldName, OverrideTruncation);
            }
            catch
            {
                throw;
            }
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

        /// <summary>
        /// <c>LabelCode</c>
        /// The label code portion of the NDC
        /// </summary>
        [XmlElement("LabelCodeName")]
        public string LabelCode
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("lblcode")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? "0", "LblCode");
        }

        /// <summary>
        /// <c>ProductCode</c>
        /// The product code part of the NDC
        /// </summary>
        [XmlElement("ProductCode")]
        public string ProductCode
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("prodcode")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? "0", "ProdCode");
        }

        /// <summary>
        /// <c>TradeName</c>
        /// Common trade name for the drug
        /// </summary>
        [XmlElement("TradeName")]
        public string TradeName
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("tradename")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? String.Empty, "TradeName");
        }

        /// <summary>
        /// <c>Strength</c>
        /// Number of units in he drug
        /// </summary>
        [XmlElement("Strength")]
        public string Strength
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("strength")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? "0", "Strength");
        }

        /// <summary>
        /// <c>Unit</c>
        /// Type of units the drug strength is measured in
        /// </summary>
        [XmlElement("Unit")]
        public string Unit
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("unit")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? "0", "Unit");
        }


        /// <summary>
        /// <c>RxOTC</c>
        /// True if the drug is available over the counter
        /// </summary>
        [XmlElement("RxOTC")]
        public string RxOTC
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("rxotc")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? "R", "RxOTC");
        }

        /// <summary>
        /// <c>DoseForm</c>
        /// How the drug is delivered e.g. Tablet, Capsule, ...
        /// </summary>
        [XmlElement("DoseForm")]
        public string DoseForm
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("doseform")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "DoseForm");
        }

        /// <summary>
        /// <c>Route</c>
        /// How the drug is taken by a patient e.g. Oral, Injection, ...
        /// </summary>
        [XmlElement("Route")]
        public string Route
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("route")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "Route");
        }

        /// <summary>
        /// <c>DrugSchedule</c>
        /// The DEA Schedule number (1-7)
        /// </summary>
        [XmlElement("DrugSchedule")]
        public int DrugSchedule
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("drugschedule")));
                return !string.IsNullOrEmpty(f.tagData) ? Convert.ToInt32(f.tagData) : 0;
            }

            set
            {
                if ((value < 2 && value > 7) && value != 99)
                {
                    throw new Exception("Drug Schedule must be 2-7");
                }

                SetField(_fieldList, Convert.ToString(value), "DrugSchedule");
            }
        }

        /// <summary>
        /// <c>VisualDescription</c>
        /// A shorthand version of what the drug looks like e.g. RND/WHT/412
        /// </summary>
        [XmlElement("VisualDescription")]
        public string VisualDescription
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("visualdescription")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "VisualDescription");
        }

        /// <summary>
        /// <c>DrugName</c>
        /// The full manufacturer or clinical name of the drug
        /// </summary>
        [XmlElement("DrugName")]
        public string DrugName
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("drugname")));
                return f?.tagData;
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
        [XmlElement("ShortName")]
        public string ShortName
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("shortname")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "ShortName");
        }

        /// <summary>
        /// <c>NDCNum</c>
        /// National Drug Classification.  Format is typically nnnnn-nnnn-nnnn 
        /// </summary>
        [XmlElement("NDCNum")]
        public string NDCNum
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("ndcnum")));
                return f?.tagData;
            }

            set => SetField(_fieldList, NormalizeString(value ?? "0"), "NDCNum");
        }

        /// <summary>
        /// <c>SizeFactor</c>
        /// The number of doses that fit in a cup.  A 99 means its a bulk item
        /// </summary>
        [XmlElement("SizeFactor")]
        public int SizeFactor
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("sizefactor")));
                return !string.IsNullOrEmpty(f.tagData) ? Convert.ToInt32(f.tagData) : 0;
            }

            set => SetField(_fieldList, Convert.ToString(value), "SizeFactor");
        }

        /// <summary>
        /// <c>Template</c>
        /// The template letter from MOT's filling documentation.  The size factor and template code are related. 
        /// 'XX' means it's a bulk item
        /// </summary>
        [XmlElement("Template")]
        public string Template
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("template")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "Template");
        }

        /// <summary>
        /// <c>DefaultIsolate</c>
        /// True if the medication has to be allone in a cup
        /// </summary>
        [XmlElement("DefaultIsolate")]
        public int DefaultIsolate
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("defaultisolate")));
                return !string.IsNullOrEmpty(f.tagData) ? Convert.ToInt32(f.tagData) : 0;
            }

            set => SetField(_fieldList, Convert.ToString(value), "DefaultIsolate");
        }

        /// <summary>
        /// <c>ConsultMsg</c>
        /// Any special instructions associated with the drug
        /// </summary>
        [XmlElement("ConsultMsg")]
        public string ConsultMsg
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("consultmsg")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "ConsultMsg");
        }

        /// <summary>
        /// <c>GenericFor</c>
        /// If the drug is a generic, the original trade name e.g.  lorazepam is the generic for Ativan
        /// </summary>
        [XmlElement("GenericFor")]
        public string GenericFor
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("genericfor")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "GenericFor");
        }
    }
}