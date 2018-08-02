using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;


namespace MotParserLib
{
    /// <summary>
    /// <c>TableConverter</c>
    /// Class for transforming motLegacy delimited data into motLegacy tagged data 
    /// </summary>
    ///<code>
    ///string __test_prescriber = @"AP\xEELastName\xEEFirstName\xEEMiddleInitial\xEEAddress1\xEEAddress2\xEECity\xEEState\xEEZip\xEEComments\xEEDEA_ID\xEETPID\xEESpeciality\xEEFax\xEEPagerInfo\xEE1025143\xE2\
    /// AP\xEEpLastName\xEEpFirstName\xEEpMiddleInitial\xEEpAddress1\xEEpAddress2\xEEpCity\xEEpState\xEEpZip\xEEpComments\xEEpDEA_ID\xEEpTPID\xEEpSpeciality\xEEpFax\xEEpPagerInfo\xEE1972834\xE2";
    ///</code>
    public class TableConverter
    {
        private readonly Dictionary<char, string> _actionTable = new Dictionary<char, string>()
            {
                {'A', "Add" },
                {'a', "Add" },
                {'C', "Change" },
                {'c', "Change" },
                {'D', "Delete" },
                {'d', "Delete" }
            };

        private readonly Dictionary<char, string> _typeTable = new Dictionary<char, string>()
            {
                {'P', "Prescriber" },
                {'p', "Prescriber" },
                {'A', "Patient" },
                {'a', "Patient" },
                {'D', "Drug" },
                {'d', "Drug" },
                {'L', "Location" },
                {'l', "Location" },
                {'R', "Rx" },
                {'r', "Rx" },
                {'S', "Store" },
                {'s', "Store" },
                {'T', "TimesQtys" },
                {'t', "TimesQtys" }
            };

        private readonly Dictionary<int, KeyValuePair<bool, string>> _practitionerTableV1 = new Dictionary<int, KeyValuePair<bool, string>>()
            {
               {1, new KeyValuePair<bool, string>(false,"DocCode") },
               {2, new KeyValuePair<bool, string>(true, "LastName") },
               {3, new KeyValuePair<bool, string>(true, "FirstName") },
               {4, new KeyValuePair<bool, string>(true, "MiddleInitial") },
               {5, new KeyValuePair<bool, string>(true, "Address1") },
               {6, new KeyValuePair<bool, string>(true, "Address2") },
               {7, new KeyValuePair<bool, string>(true, "City") },
               {8, new KeyValuePair<bool, string>(true, "State") },
               {9, new KeyValuePair<bool, string>(true, "Zip") },
               {10,new KeyValuePair<bool, string>(true, "Phone") },
               {11,new KeyValuePair<bool, string>(true, "Comments") },
               {12,new KeyValuePair<bool, string>(true, "DEA_ID") },
               {13,new KeyValuePair<bool, string>(true, "TPID") },
               {14,new KeyValuePair<bool, string>(true, "Speciality") },
               {15,new KeyValuePair<bool, string>(true, "Fax") },
               {16,new KeyValuePair<bool, string>(true, "PagerInfo") },
               {17,new KeyValuePair<bool, string>(true, "RxSys_DocID") }
           };

        private Dictionary<int, string> _practitionerTableV2 = new Dictionary<int, string>()
           {
               {1, "LastName" },
               {2, "FirstName" },
               {3, "MiddleInitial" },
               {4, "Address1" },
               {5, "Address2" },
               {6, "City" },
               {7, "State" },
               {8, "Zip" },
               {9, "Phone" },
               {10,"Comments" },
               {11,"DEA_ID" },
               {12,"TPID" },
               {13,"Speciality" },
               {14,"Fax" },
               {15,"PagerInfo" },
               {16,"RxSys_DocID" }
           };

        private readonly Dictionary<int, KeyValuePair<bool, string>> _drugTableV1 = new Dictionary<int, KeyValuePair<bool, string>>()
            {
               {1,  new KeyValuePair<bool, string>(false,"Seq_No") },
               {2,  new KeyValuePair<bool, string>(true,"LblCode") },
               {3,  new KeyValuePair<bool, string>(true,"ProdCode") },
               {4,  new KeyValuePair<bool, string>(true,"TradeName") },
               {5,  new KeyValuePair<bool, string>(true,"Strength") },
               {6,  new KeyValuePair<bool, string>(true,"Unit") },
               {7,  new KeyValuePair<bool, string>(true,"RxOtc")},
               {8,  new KeyValuePair<bool, string>(true,"DoseForm") },
               {9,  new KeyValuePair<bool, string>(true,"Route") },
               {10, new KeyValuePair<bool, string>(false,"FirmSeqNo")},
               {11, new KeyValuePair<bool, string>(true,"DrugSchedule")  },
               {12, new KeyValuePair<bool, string>(true,"VisualDescription")},
               {13, new KeyValuePair<bool, string>(true,"DrugName") },
               {14, new KeyValuePair<bool, string>(true,"ShortName")  },
               {15, new KeyValuePair<bool, string>(true,"NDCNum") },
               {16, new KeyValuePair<bool, string>(false,"FDARec")  },
               {17, new KeyValuePair<bool, string>(false,"SizeFactor") },
               {18, new KeyValuePair<bool, string>(false,"ShowInPickList") },
               {19, new KeyValuePair<bool, string>(true,"Template") },
               {20, new KeyValuePair<bool, string>(true,"ConsultMesg")  },
               {21, new KeyValuePair<bool, string>(true,"GenericFor") },
               {22, new KeyValuePair<bool, string>(true,"RxSys_DrugID") }
            };

        private Dictionary<int, string> _drugTableV2 = new Dictionary<int, string>()
            {
               {1, "LblCode" },
               {2, "ProdCode" },
               {3, "TradeName" },
               {4, "Strength" },
               {5, "Unit" },
               {6, "RxOtc" },
               {7, "DoseForm" },
               {8, "Route" },
               {9, "DrugSchedule" },
               {10,"VisualDescription" },
               {11,"DrugName" },
               {12,"ShortName" },
               {13,"NDCNum" },
               {14,"SizeFactor" },
               {15,"Template" },
               {16,"ConsultMesg" },
               {17,"GenericFor" },
               {18,"RxSys_DrugID" }
            };

        private readonly Dictionary<int, KeyValuePair<bool, string>> _facilityTableV1 = new Dictionary<int, KeyValuePair<bool, string>>()
            {
               {1, new KeyValuePair<bool, string>(true, "RxSys_StoreID") },
               {2, new KeyValuePair<bool, string>(true, "LocationName") },
               {3, new KeyValuePair<bool, string>(true, "Address1") },
               {4, new KeyValuePair<bool, string>(true, "Address2") },
               {5, new KeyValuePair<bool, string>(true, "City") },
               {6, new KeyValuePair<bool, string>(true, "State") },
               {7, new KeyValuePair<bool, string>(true, "Zip") },
               {8, new KeyValuePair<bool, string>(true, "Phone") },
               {9, new KeyValuePair<bool, string>(true, "Comments") },
               {10, new KeyValuePair<bool, string>(false, "ColorTbl") },
               {11, new KeyValuePair<bool, string>(false, "ImportID") },
               {12, new KeyValuePair<bool, string>(false, "ShowLotAndExp") },
               {13, new KeyValuePair<bool, string>(true, "RxSys_LocID") }, // PRNSwitch (wrong usage, fix post read)
               {14, new KeyValuePair<bool, string>(true, "CycleDays") },
               {15, new KeyValuePair<bool, string>(true, "CycleType") },
               {16, new KeyValuePair<bool, string>(false, "RFReminderDays") }
           };

        private readonly Dictionary<int, string> _facilityTableV2 = new Dictionary<int, string>()
            {
               {1, "RxSys_StoreID" },
               {2, "LocationName" },
               {3, "Address1" },
               {4, "Address2" },
               {5, "City" },
               {6, "State" },
               {7, "Zip" },
               {8, "Phone" },
               {9, "Comments" },
               {10,"RxSys_LocID" },
               {11,"CycleDays" },
               {12,"CycleType" }
           };

        private readonly Dictionary<int, KeyValuePair<bool, string>> _patientTableV1 = new Dictionary<int, KeyValuePair<bool, string>>()
            {
               {1, new KeyValuePair<bool, string>(false, "MOTPatID")  },
               {2, new KeyValuePair<bool, string>(true,  "RxSys_PatID") },
               {3, new KeyValuePair<bool, string>(true,  "LastName") },
               {4, new KeyValuePair<bool, string>(true,  "FirstName") },
               {5, new KeyValuePair<bool, string>(true,  "MiddleInitial") },
               {6, new KeyValuePair<bool, string>(true,  "Address1") },
               {7, new KeyValuePair<bool, string>(true,  "Address2") },
               {8, new KeyValuePair<bool, string>(true,  "City") },
               {9, new KeyValuePair<bool, string>(true,  "State") },
               {10, new KeyValuePair<bool, string>(true, "Zip") },
               {11, new KeyValuePair<bool, string>(true, "Phone1") },
               {12, new KeyValuePair<bool, string>(true, "Phone2") },
               {13, new KeyValuePair<bool, string>(true, "WorkPhone") },
               {14, new KeyValuePair<bool, string>(true, "RxSys_LocID") }, // LocCode
               {15, new KeyValuePair<bool, string>(true, "Room") },
               {16, new KeyValuePair<bool, string>(true, "Comments") },
               {17, new KeyValuePair<bool, string>(true, "Gender") },  // ColorTbl
               {18, new KeyValuePair<bool, string>(false, "RFReminder") },
               {19, new KeyValuePair<bool, string>(true, "CycleDate") },
               {20, new KeyValuePair<bool, string>(true, "CycleDays") },
               {21, new KeyValuePair<bool, string>(true, "CycleType") },
               {22, new KeyValuePair<bool, string>(true, "Status") },
               {23, new KeyValuePair<bool, string>(true, "RxSys_LastDoc") },
               {24, new KeyValuePair<bool, string>(true, "RxSys_PrimaryDoc") },
               {25, new KeyValuePair<bool, string>(true, "RxSys_AltDoc") },
               {26, new KeyValuePair<bool, string>(false, "DefTimes") },
               {27, new KeyValuePair<bool, string>(true, "SSN") },
               {28, new KeyValuePair<bool, string>(true, "Allergies") },
               {29, new KeyValuePair<bool, string>(true, "Diet") },
               {30, new KeyValuePair<bool, string>(true, "DxNotes") },
               {31, new KeyValuePair<bool, string>(true, "TreatmentNotes") },
               {32, new KeyValuePair<bool, string>(true, "DOB") },
               {33, new KeyValuePair<bool, string>(true, "Height") },
               {34, new KeyValuePair<bool, string>(true, "Weight") },
               {35, new KeyValuePair<bool, string>(true, "ResponsibleName") },
               {36, new KeyValuePair<bool, string>(true, "InsName") },
               {37, new KeyValuePair<bool, string>(true, "InsPNo") },
               {38, new KeyValuePair<bool, string>(true, "AltInsName") },
               {39, new KeyValuePair<bool, string>(true, "AltInsPNo") },
               {40, new KeyValuePair<bool, string>(true, "MCareNum") },
               {41, new KeyValuePair<bool, string>(true, "MCaidNum") },
               {42, new KeyValuePair<bool, string>(true, "AdmitDate") },
               {43, new KeyValuePair<bool, string>(false, "CycleEndDate") },
               {44, new KeyValuePair<bool, string>(false, "Ok") },
               {45, new KeyValuePair<bool, string>(true, "ChartOnly") }
            };

        private readonly Dictionary<int, string> _patientTableV2 = new Dictionary<int, string>()
            {
               {1, "RxSys_PatID" },
               {2, "LastName" },
               {3, "FirstName" },
               {4, "MiddleInitial" },
               {5, "Address1" },
               {6, "Address2" },
               {7, "City" },
               {8, "State" },
               {9, "Zip" },
               {10, "Phone1" },
               {11, "Phone2" },
               {12, "WorkPhone" },
               {13, "RxSys_LocID" },
               {14, "Room" },
               {15, "Comments" },
               {16, "Gender" },
               {17, "CycleDate" },
               {18, "CycleType" },
               {19, "Status" },
               {20, "RxSys_LastDoc" },
               {21, "RxSys_PrimaryDoc" },
               {22, "RxSys_AltDoc" },
               {23, "SSN" },
               {24, "Allergies" },
               {25, "Diet" },
               {26, "DxNotes" },
               {27, "TreatmentNotes" },
               {28, "DOB" },
               {29, "Height" },
               {30, "Weight" },
               {31, "ResponsibleName" },
               {32, "InsName" },
               {33, "InsPNo" },
               {34, "AltInsName" },
               {35, "AltInsPNo" },
               {36, "MCareNum" },
               {37, "MCaidNum" },
               {38, "AdmitDate" },
               {39, "ChartOnly" }
            };

        private readonly Dictionary<int, KeyValuePair<bool, string>> _rxTableV1 = new Dictionary<int, KeyValuePair<bool, string>>()
            {
               {1, new KeyValuePair<bool, string>(true, "RxSys_PatID") },
               {2, new KeyValuePair<bool, string>(false,"MOT_RxID") },
               {3, new KeyValuePair<bool, string>(true, "RxSys_RxNum") },
               {4, new KeyValuePair<bool, string>(true, "RxSys_DocID") },
               {5, new KeyValuePair<bool, string>(true, "Sig") },
               {6, new KeyValuePair<bool, string>(true, "RxStartDate") },
               {7, new KeyValuePair<bool, string>(true, "RxStopDate") },
               {8, new KeyValuePair<bool, string>(true, "DoseScheduleName") },
               {9, new KeyValuePair<bool, string>(true, "Comments") },
               {10, new KeyValuePair<bool, string>(true, "Refills") },
               {11, new KeyValuePair<bool, string>(true, "RxSys_NewRxNum") },
               {12, new KeyValuePair<bool, string>(true, "Isolate") },
               {13, new KeyValuePair<bool, string>(true, "MDoMStart") },
               {14, new KeyValuePair<bool, string>(true, "MDoMEnd") },
               {15, new KeyValuePair<bool, string>(true, "NDCNum") },
               {16, new KeyValuePair<bool, string>(false,"Ok") },
               {17, new KeyValuePair<bool, string>(true, "QtyPerDose") },
               {18, new KeyValuePair<bool, string>(true, "QtyDispensed") },
               {19, new KeyValuePair<bool, string>(true, "RxType") },
               {20, new KeyValuePair<bool, string>(true, "Status") },
               {21, new KeyValuePair<bool, string>(true, "DoW") },
               {22, new KeyValuePair<bool, string>(true, "SpecialDoses") },
               {23, new KeyValuePair<bool, string>(true, "DoseTimesQtys") },
               {24, new KeyValuePair<bool, string>(true, "RxSys_DrugID") }   //v2 only
            };

        private readonly Dictionary<int, string> _rxTableV2 = new Dictionary<int, string>()
            {
               {1, "RxSys_PatID" },
               {2, "RxSys_RxNum" },
               {3, "RxSys_DocID" },
               {4, "Sig" },
               {5, "RxStartDate" },
               {6, "RxStopDate" },
               {7, "DoseScheduleName" },
               {8, "Comments" },
               {9, "Refills" },
               {10, "RxSys_NewRxNum" },
               {11, "Isolate" },
               {12, "MDoMStart" },
               {13, "MDoMEnd" },
               {14, "NDCNum" },
               {15, "QtyPerDose" },
               {16, "QtyDispensed" },
               {17, "RxType" },
               {18, "Status" },
               {19, "DoW" },
               {20, "SpecialDoses" },
               {21, "DoseTimesQtys" },
               {22, "RxSys_DrugID" }
            };

        private readonly Dictionary<int, string> _storeTable = new Dictionary<int, string>()
            {
               {1, "RxSys_StoreID" },
               {2, "StoreName" },
               {3, "Address1" },
               {4, "Address2" },
               {5, "City" },
               {6, "State" },
               {7, "Zip" },
               {8, "Phone" },
               {9, "Fax" },
               {10,"DEANum" }
           };

        private readonly Dictionary<int, string> _tqTable = new Dictionary<int, string>()
            {
               {1, "RxSys_LocID" },
               {2, "DoseScheduleName" },
               {3, "DoseTimesQtys" }
            };

        /// <summary>
        /// <c>IsV1Delimited</c>
        /// Determines if the input data is V1 binary and determines the record type based on the number 
        /// of Field delimieters
        /// </summary>
        /// <param name="dataIn"></param>
        /// <returns></returns>
        public bool IsV1Delimited(string dataIn)
        {
            // byte[1] == P && delim_count == 17
            // byte[1] == D && delim_count == 21
            // byte[1] == L && delim_count == 16
            // byte[1] == A && delim_count == 45
            // byte[1] == R && delim_count == 24

            string[] rows;

            if (dataIn.Contains('\xE2'))
            {
                rows = dataIn.Split('\xE2');

                foreach (var row in rows)
                {
                    if (row.Substring(1, 1).ToUpper() == "P" && row.Count(x => x == '\xEE') == 17 ||
                        row.Substring(1, 1).ToUpper() == "D" && row.Count(x => x == '\xEE') == 21 ||
                        row.Substring(1, 1).ToUpper() == "L" && row.Count(x => x == '\xEE') == 16 ||
                        row.Substring(1, 1).ToUpper() == "A" && row.Count(x => x == '\xEE') == 45 ||
                        row.Substring(1, 1).ToUpper() == "R" && row.Count(x => x == '\xEE') == 24)
                    {
                        return true;
                    }
                }
            }
            else if (dataIn.Contains("S\r") && dataIn.Count(x => x == '~') > 3)
            {
                rows = dataIn.Split('S');

                foreach (var row in rows)
                {
                    if (row.Substring(1, 1).ToUpper() == "P" && row.Count(x => x == '~') == 17 ||
                        row.Substring(1, 1).ToUpper() == "D" && row.Count(x => x == '~') == 21 ||
                        row.Substring(1, 1).ToUpper() == "L" && row.Count(x => x == '~') == 16 ||
                        row.Substring(1, 1).ToUpper() == "A" && row.Count(x => x == '~') == 45 ||
                        row.Substring(1, 1).ToUpper() == "R" && row.Count(x => x == '~') == 24)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private string NormalizeDate(string origDate)
        {
            if (string.IsNullOrEmpty(origDate))
            {
                return string.Empty;
            }


            while (origDate.Contains(" "))
            {
                origDate = origDate.Remove(origDate.IndexOf(" ", StringComparison.Ordinal), 1);
            }

            string[] datePatterns =  // Hope I got them all
                {
                "yyyyMMdd",
                "yyyyMMd",
                "yyyyMdd",
                "yyyyMd",

                "yyyyddMM",
                "yyyyddM",
                "yyyydMM",
                "yyyydM",

                "ddMMyyyy",
                "ddMyyyy",
                "dMMyyyy",
                "dMyyyy",

                "MMddyyyy",
                "MMdyyyy",
                "Mddyyyy",
                "Mdyyyy",

                "dd/MM/yyyy",
                "dd/M/yyyy",
                "d/MM/yyyy",
                "d/M/yyyy",

                "MM/dd/yyyy",
                "MM/dd/yyyy hh:mm:ss tt",
                "MM/dd/yyyy h:mm:ss tt",
                "MM/dd/yyyy hh:m:ss tt",
                "MM/dd/yyyy h:m:ss tt",
                "MM/dd/yyyyhhmmss",            // HL7 Full Date Format 20110802085759
                "yyyyMMddhhmmss",

                "MM/d/yyyy",
                "MM/d/yyyy hh:mm:ss tt",

                "M/dd/yyyy",
                "M/dd/yyyy hh:mm:ss tt",

                "M/d/yyyy",
                "M/d/yyyy hh:mm:ss tt",

                "yyyy-MM-dd",
                "yyyy-M-dd",
                "yyyy-MM-d",
                "yyyy-M-d",

                "yyyy-dd-MM",
                "yyyy-d-MM",
                "yyyy-dd-M",
                "yyyy-d-M"
            };

            if (DateTime.TryParseExact(origDate, datePatterns, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                return dt.ToString("yyyy-MM-dd"); // return MOT Legacy Gateway Format
            }

            //DateFormatError = true;
            origDate = "BADDATE";

            return origDate;
        }
        /// <summary>
        /// <c>parse</c>
        /// Table type parser to convert a text delimited stream into a tagged record.  The transformations
        /// are relativly simple and Dictionary based
        /// </summary>
        /// <param name="items"></param>
        /// <param name="usingV1"></param>
        /// <returns></returns>
        public string Parse(string[] items, bool usingV1 = false)
        {
            StringBuilder taggedString = new StringBuilder(4096);

            if (string.IsNullOrEmpty(items[0]) || items[0][0] == '\n')
            {
                return null;
            }

            items[0] = items[0].Trim();

            _typeTable.TryGetValue(items[0][0], out string tableType);

            if (string.IsNullOrEmpty(tableType))
            {
                return null;
            }

            //TaggedString.Append(@"<?xml version=""1.0"" encoding=""utf-8""?>");
            taggedString.Append("<Record>");
            taggedString.Append($"<Table>{_typeTable[items[0][0]]}</Table>");
            taggedString.Append($"<Action>{_actionTable[items[0][1]]}</Action>");

            int i;
            switch (items[0][0])
            {
                case 'P':
                case 'p':
                    for (i = 1; i < items.Length - 1; i++)  // Length - 1 to compensate for the checksum
                    {
                        if (i > _practitionerTableV1.Count)
                        {
                            break;
                        }

                        if (usingV1 && _practitionerTableV1[i].Key == false)
                        {
                            continue;
                        }

                        if (_practitionerTableV1[i].Key != true)
                        {
                            continue;
                        }

                        if (_practitionerTableV1[i].Value.ToUpper().Contains("DATE"))
                        {
                            items[i] = NormalizeDate(items[i]);
                        }
                        // QS/1 puts the NPI in with the NPI -- <DEA_ID>MT4359798 |1922543198</DEA_ID>
                        if (_practitionerTableV1[i].Value == "DEA_ID" && items[i].Contains("|"))
                        {
                            var split = items[i].Split('|');
                            taggedString.Append($"<{_practitionerTableV1[i].Value}>{split[0].Trim()}</{_practitionerTableV1[i].Value}>");
                            taggedString.Append($"<TPID>{split[1].Trim()}</TPID>");
                        }
                        else
                        {
                            if (_practitionerTableV1[i].Value.ToUpper() == "STATUS" && string.IsNullOrEmpty(items[i]))
                            {
                                items[i] = "1";
                            }

                            taggedString.Append($"<{_practitionerTableV1[i].Value}>{items[i].Trim()}</{_practitionerTableV1[i].Value}>");
                        }
                    }

                    /*
                        TaggedString.Append("</motPatientRecord>");

                        using (StringReader reader = new StringReader(TaggedString.ToString()))
                        {
                            XmlSerializer serializer = new XmlSerializer(typeof(motPrescriberRecord));
                            motPrescriberRecord doc = (motPrescriberRecord)serializer.Deserialize(reader);
                            doc.Write(new motSocket("localhost", 24042));
                        }
                        */
                    break;

                case 'D':
                case 'd':
                    for (i = 1; i < items.Length - 1; i++)
                    {
                        if (i > _drugTableV1.Count)
                        {
                            break;
                        }

                        if (usingV1 && _drugTableV1[i].Key == false)
                        {
                            continue;
                        }

                        if (!_drugTableV1[i].Key)
                        {
                            continue;
                        }

                        if (_drugTableV1[i].Value.ToUpper().Contains("DATE"))
                        {
                            items[i] = NormalizeDate(items[i]);
                        }
                        if (_drugTableV1[i].Value.ToUpper() == "STATUS" && string.IsNullOrEmpty(items[i]))
                        {
                            items[i] = "1";
                        }
                        taggedString.Append(string.Format("<{0}>{1}</{0}>", _drugTableV1[i].Value, items[i].Trim()));
                    }

                    break;

                case 'L':
                case 'l':
                    for (i = 1; i < items.Length - 1; i++)
                    {
                        if (i > _facilityTableV1.Count)
                        {
                            break;
                        }

                        if (usingV1 && _facilityTableV1[i].Key == false)
                        {
                            continue;
                        }

                        if (!_facilityTableV1[i].Key)
                        {
                            continue;
                        }

                        if (_facilityTableV1[i].Value.ToUpper().Contains("DATE"))
                        {
                            items[i] = NormalizeDate(items[i]);
                        }
                        if (_facilityTableV1[i].Value.ToUpper() == "STATUS" && string.IsNullOrEmpty(items[i]))
                        {
                            items[i] = "1";
                        }
                        taggedString.Append(string.Format("<{0}>{1}</{0}>", _facilityTableV1[i].Value, items[i].Trim()));
                    }
                    break;

                case 'A':
                case 'a':

                    for (i = 1; i < items.Length - 1; i++)
                    {
                        if (i > _patientTableV1.Count)
                        {
                            break;
                        }

                        if (usingV1 && _patientTableV1[i].Key == false)
                        {
                            continue;
                        }

                        if (_patientTableV1[i].Key != true)
                        {
                            continue;
                        }

                        if (_patientTableV1[i].Value.ToUpper().Contains("DATE"))
                        {
                            items[i] = NormalizeDate(items[i]);
                        }
                        if (_patientTableV1[i].Value.ToUpper() == "STATUS" && string.IsNullOrEmpty(items[i]))
                        {
                            items[i] = "1";
                        }

                        taggedString.Append(string.Format("<{0}>{1}</{0}>", _patientTableV1[i].Value, items[i].Trim()));
                    }

                    /*
                       TaggedString.Append("</motPatientRecord>");

                        using (StringReader reader = new StringReader(TaggedString.ToString()))
                        {
                            XmlSerializer serializer = new XmlSerializer(typeof(motPrescriberRecord));
                            motPrescriberRecord patient = (motPrescriberRecord)serializer.Deserialize(reader);
                            patient.Write(new motSocket("localhost", 24042));
                        }
                        */
                    break;

                case 'R':
                case 'r':
                    for (i = 1; i < items.Length - 1; i++)
                    {
                        if (i > _rxTableV1.Count)
                        {
                            break;
                        }

                        if (usingV1 && _rxTableV1[i].Key == false)
                        {
                            continue;
                        }

                        if (_rxTableV1[i].Key != true)
                        {
                            continue;
                        }

                        if (_rxTableV1[i].Value.ToUpper() == "DOSETIMESQTYS" && (items[i].Trim().Length % 9) != 0)
                        {
                            // There are two valid formats:
                            //            HHHMM0.00
                            //            HHMM00.00
                            // and everything needs to be transformed to HHMM00.00
                            var transformed = string.Empty;
                            var source = items[i].Trim();
                            var loops = 9 - (items[i].Trim().Length % 9);
                            var offset = 0;


                            while (loops > 0)
                            {
                                transformed += source.Substring(offset, 4) + "0" + source.Substring(offset + 4, 4);
                                offset += 8;
                                loops--;
                            }
                            if (_rxTableV1[i].Value.ToUpper().Contains("DATE"))
                            {
                                items[i] = NormalizeDate(items[i]);
                            }
                            if (_rxTableV1[i].Value.ToUpper() == "STATUS" && string.IsNullOrEmpty(items[i]))
                            {
                                items[i] = "1";
                            }
                            taggedString.Append($"<{_rxTableV1[i].Value}>{transformed}</{_rxTableV1[i].Value}>");
                        }
                        else
                        {
                            taggedString.Append($"<{_rxTableV1[i].Value}>{items[i].Trim()}</{_rxTableV1[i].Value}>");
                        }
                    }
                    break;

                case 'S':
                case 's':
                    for (i = 1; i < items.Length - 1; i++)
                    {
                        if (i > _storeTable.Count)
                        {
                            break;
                        }

                        taggedString.Append($"\t<{_storeTable[i]}>{items[i].Trim()}</{_storeTable[i]}>");
                    }
                    break;

                case 'T':
                case 't':
                    for (i = 1; i < items.Length - 1; i++)
                    {
                        if (i > _tqTable.Count)
                        {
                            break;
                        }

                        taggedString.Append($"\t<{_tqTable[i]}>{items[i].Trim()}</{_tqTable[i]}>");
                    }
                    break;
            }

            taggedString.Append("</Record>");

            return taggedString.ToString();
        }
    }

    public class LegacyDelimitedParser : ParserBase
    {
        /// <inheritdoc />
        public LegacyDelimitedParser(string inputStream) : base(inputStream)
        {

        }
        /// <summary>
        /// <c>PreParseQs1</c>
        /// Converts binary delimited data into text delimited using the "observed" Qs1 format
        /// 
        /// MOT Delimited Spec
        /// ------------------
        ///  AA\xEEData\xEE...\xEENN\xE2  
        ///  A = Table and Action Identifiers
        ///  N = Checksum
        ///  \xEE = field delimiter
        ///  \xE2 = record delimiter
        ///
        ///  QS/1 Implementation
        ///  -------------------
        ///  AA~Data~..[0-9]{10}S
        ///  A = Table and Action Identifiers
        ///  ~ = field delimiter
        ///  S = record delimeter
        /// </summary>
        /// <param name="inboundData">Binary data to convert</param>
        /// <returns>Plain Text (string) containing the converted delimiters or null on failure</returns>
        public string PreParseQs1(string inboundData)
        {
            // The QS/1 interpretation of the Delimited spec replaces '\xEE' with '~' and '\xE2' with 'S'
            // The following pattern represents the 10 digit checksum followd by an 'S'
            string qs1Pattern = "\\d{10}S";

            Match match = Regex.Match(inboundData, qs1Pattern);
            if (!match.Success)
            {
                return string.Empty;
            }

            while (match.Success)
            {
                var replace = inboundData.Substring(match.Index, match.Length - 1) + '^';
                inboundData = inboundData.Replace(inboundData.Substring(match.Index, match.Length), replace);
                match = Regex.Match(inboundData, qs1Pattern);
            }

            return inboundData;
        }
        private static string PreParseByteStream(string dataIn)
        {
            if (!dataIn.ToUpper().Contains("EE") || !dataIn.ToUpper().Contains("E2"))
            {
                return string.Empty;
            }

            var asciiString = string.Empty;

            for (var i = 0; i < dataIn.Length; i += 2)
            {
                var hs = dataIn.Substring(i, 2);
                var unicodeVal = Convert.ToUInt32(hs, 16);
                var character = Convert.ToChar(unicodeVal);
                asciiString += character;
            }

            return asciiString;
        }
        /// <summary>
        /// <c>NormalizeDelimiters</c>
        /// Convert binary delimiters to text
        /// </summary>
        /// <param name="dataIn"></param>
        /// <returns></returns>
        public string NormalizeDelimiters(string dataIn)
        {
            var cleanString = string.Empty;

            foreach (var c in dataIn)
            {
                if (c == '\xEE')
                    cleanString += '~';

                else if (c == '\xE2')
                    cleanString += '^';

                else
                    cleanString += c;
            }

            return cleanString;
        }

        /// <summary>
        /// <c>parseDelimited</c>
        /// </summary>
        /// Converts motLegacy delimited data format to motLegacy tagged format and passes it to the gateway
        /// There are two formats in play.  One uses binary delimiters and one uses plain text, which is
        /// undocumented and appears to be used by Qs1.  FrameworksLTC uses the binary version
        /// <param name="v1Data">Indicates documented binary delimited format if true</param>
        public void Go(bool v1Data = false)
        {
            var retVal = PreParseQs1(data);

            if (retVal != string.Empty)
            {
                data = retVal;
                v1Data = true;
            }

            retVal = PreParseByteStream(data);
            if (retVal != string.Empty)
            {
                data = retVal;
            }

            data = NormalizeDelimiters(data);

            var tc = new TableConverter();

            char[] fieldDelimiter = { '~' };
            char[] recordDelimiter = { '^' };
            var table = string.Empty;

            // Unravel the delimited stream
            var items = data.Split(recordDelimiter);
            string[] fields = null;

            foreach (var item in items)
            {
                try
                {
                    if (string.IsNullOrEmpty(item))
                    {
                        continue;
                    }

                    fields = item.Split(fieldDelimiter);
                    table = tc.Parse(fields, v1Data);
                    ParseTagged(table);
                }
                catch (Exception ex)
                {
                    EventLogger.Error(ex.Message);

                    if (fields != null)
                    {
                        foreach (var f in fields)
                        {
                            EventLogger.Debug($"ERROR: ParseDelimited Field: {f}");
                        }
                    }

                    EventLogger.Debug($"ERROR: ParseDelimited Record: {table}");

                    throw;
                }
            }
        }
    }
}
