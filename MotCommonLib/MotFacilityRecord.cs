using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Xml.Serialization;

namespace MotCommonLib
{
    /// <summary>
    /// Location/Facility Record (Key == B)
    /// </summary>
    [XmlRoot("MotFacilityRecord")]
    public class MotFacilityRecord : MotRecordBase
    {
        private readonly List<Field> _fieldList;

        private void createRecord(string tableAction)
        {
            try
            {
                _fieldList.Add(new Field("Table", "Location", 10, true, 'a'));
                _fieldList.Add(new Field("Action", tableAction, 10, true, 'a'));
                _fieldList.Add(new Field("RxSys_LocID", "", 11, true, 'k'));
                _fieldList.Add(new Field("RxSys_StoreID", "", 11, false, 'n'));
                _fieldList.Add(new Field("LocationName", "", 60, true, 'a'));
                _fieldList.Add(new Field("Address1", "", 40, true, 'w'));
                _fieldList.Add(new Field("Address2", "", 40, false, 'w'));
                _fieldList.Add(new Field("City", "", 30, true, 'w'));
                _fieldList.Add(new Field("State", "", 10, true, 'w'));
                _fieldList.Add(new Field("Zip", "", 9, true, 'w'));
                _fieldList.Add(new Field("Phone", "", 10, true, 'w'));
                _fieldList.Add(new Field("Comments", "", 32767, false, 'n'));
                _fieldList.Add(new Field("CycleDays", "", 2, false, 'n'));
                _fieldList.Add(new Field("CycleType", "", 2, false, 'n'));
            }
            catch
            {
                throw;
            }
        }
        /// <summary>
        /// MotFacilityRecord
        /// </summary>
        public MotFacilityRecord()
        {
        }

        /// <summary>
        /// MotFacilitty Record Constructor
        /// </summary>
        /// <param name="action"></param>
        /// <param name="autoTruncate"></param>
        /// <exception cref="Exception"></exception>
        public MotFacilityRecord(string action, bool autoTruncate = false)
        {
            AutoTruncate = autoTruncate;

            try
            {
                _fieldList = new List<Field>();
                createRecord(action);
            }
            catch (Exception ex)
            {
                var errorString = $"Failed to create Location record: {ex.Message}";
                EventLogger.Error(errorString);
                throw new Exception(errorString);
            }
        }

        public void SetField(string fieldName, string val, bool overrideTruncation = false)
        {
            try
            {
                base.SetField(_fieldList, val, fieldName, overrideTruncation);
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

            AddToQueue("B", _fieldList);
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
                var errorString = $"Failed to write Location record: {ex.Message}";
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
                var errorString = $"Failed to write Location record: {ex.Message}";
                EventLogger.Error(errorString);
                throw new Exception(errorString);
            }
        }
        public void Clear()
        {
            Clear(_fieldList);
        }

        [XmlElement("RxSys_LocID")]
        public string RxSys_LocID
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("rxsys_locid")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "RxSys_LocID");
        }
        [XmlElement("RxSys_StoreID")]
        public string RxSys_StoreID
        {
            get
            {

                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("rxsys_storeid")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "RxSys_StoreID");
        }
        [XmlElement("LocationName")]
        public string LocationName
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("locationname")));
                return f?.tagData;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    var tmp = AutoTruncate;
                    AutoTruncate = false;

                    SetField(_fieldList, "Pharmacist Attention - Missing Location Name", "LocationName");

                    AutoTruncate = tmp;
                    return;
                }

                SetField(_fieldList, value, "LocationName");
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

            set => SetField(_fieldList, value ?? string.Empty, "Address1");
        }
        [XmlElement("Address2")]
        public string Address2
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("address2")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "Address2");
        }
        [XmlElement("City")]
        public string City
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("city")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "City");
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
        [XmlElement("Phone")]
        public string Phone
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("phone")));
                return f?.tagData;
            }

            set => SetField(_fieldList, NormalizeString(value ?? "999-999-9999"), "Phone");
        }
        [XmlElement("Comments")]
        public string Comments
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("comments")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "Comments");
        }
        [XmlElement("CycleDays")]
        public int CycleDays
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("cycledays")));
                return !string.IsNullOrEmpty(f.tagData) ? Convert.ToInt32(f.tagData) : 0;
            }

            set
            {
                if (value > 35 || value < 0)
                {
                    throw new Exception("CycleDays must be (0-35)");
                }

                SetField(_fieldList, Convert.ToString(value), "CycleDays");
            }
        }
        [XmlElement("CycleType")]
        public int CycleType
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("cycletype")));
                return !string.IsNullOrEmpty(f.tagData) ? Convert.ToInt32(f.tagData) : 0;
            }

            set
            {
                // Actual error - it would be wrong to convert it to a default value
                if (value != 0 && value != 1)
                {
                    throw new Exception("CycleType must be '0 - Monthly' or '1 - Weekly'");
                }

                SetField(_fieldList, Convert.ToString(value), "CycleType");
            }
        }
    }
}