using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using MotCommonLib;

namespace MotParserLib
{
    class ParadaParser : ParserBase, IDisposable
    {

        /// <summary>
        /// <c>Dispose</c>
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }
        /// <summary>
        /// <c>Dispose</c>
        /// </summary>
        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        public ParadaParser(string inputStream) : base(inputStream)
        {
        }

        /* Parada File Format
                   0) Patient Name (last, first)
                   1) Patient Record Number (in BestRx)
                   2) Facility Name
                   3) Patient's Unit
                   4) Patient Location/Floor
                   5) Patient Room
                   6) Patient Bed
                   7) Drug NDC
                   8) Brand Drug Name
                   9) Generic Drug name
                   10) Admin Date (for PRN this will be blank)
                   11) Admin Time (for PRN this will be "1200"
                   12) Admin Qty  (for PRN this will be "1")
                   13) Drug Strength
                   14) Prescriber Name
                   15) Rx Number
                   16) Aux Warning 1
                   17) Aux Warning 2
                   18) Sig Directions (chars 1-50)
                   19) Sig Directions (chars 51-100)
                   20) Sig Directions (chars 101-150)
                   21) Sig Directions (chars 151-200)
                   22) Order Type (P=PRN, U=Unit Dose, M=Multi Dose
                   23) Refill Number (0=original fill, 1=first refill, etc)
                   24) Total number of doses for Rx in the file
                   25) Value for barcode (you can ignore this) 
       */

        // (0)DONUT, FRED~
        // (1)25731~
        // (2)SPRINGFIELD RETIREMENT CASTLE~
        // (3)~
        // (4)2~
        // (5)201~
        // (6)13~
        // (7)12280040130~
        // (8)JANUVIA 100 MG TABLET~
        // (9)~
        // (10)20170111~
        // (11)0730~
        // (12)2~
        // (13)~
        // (14)MICHELLE SMITH~
        // (15)8880442~
        // (16)~
        // (17)~
        // (18)TAKE 2 TABLETS 4 TIMES A DAY|TOME 2 TABLETAS CUATR~
        // (19)OS VECES AL DIA~
        // (20)~
        // (21)~
        // (22)M~
        // (23)00~
        // (24)112~
        // (25)8880442_00_1/112~

        private static class DrugName
        {
            public static string Name;
            public static string Strength;
            public static string Unit;
            public static string Form;

            public static bool ParseExact(string source)
            {
                string[] parts;

                // ALENDRONATE SODIUM 35 MG TABLET
                //  Parts[0] = ALENDRONATE
                //  Parts[1] = SODIUM
                //  Parts[2] = 35
                //  Parts[3] = MG
                //  Parts[4] = TABLET
                //  ROSUVASTATIN TB 10MG 30
                //  Parts[0] = ROUSUVASTATIN
                //  Parts[1] = TB
                //  Parts[2] = 10MG
                //  Parts[3] = 30
                //  IRBESARTAN 150 MG TABLET
                //  Parts[0] = IRBESARTAN
                //  Parts[1] = 150
                //  Parts[2] = MG
                //  Parts[3] = TABLET


                const string mashedPattern = @"[[\d]+[A-Za-z]+"; // Matches mashed drug units --> 10MG
                const string mashedNumber = @"[[\d]+";
                //string MashedUnit = @"[[A-Za-z]+";
                const string standardPattern = @"[A-Za-z]+\s[\0-9]+\s[A-za-z]+\s[A-za-z]+"; // IRBESARTAN 150 MG TABLET
                //string StandardMashedPattern = @"[A-Za-z]+\s[\0-9]+[A-za-z]+\s[A-za-z]+";           // IRBESARTAN 150MG TABLET
                const string splitNamePattern = @"[A-Za-z]+\s[A-Za-z]+\s[\0-9]+\s[A-za-z]+\s[A-za-z]+"; // ALENDRONATE SODIUM 35 MG TABLET
                //string SplitMashedPattern = @"[A-Za-z]+\s[A-Za-z]+\s[\0-9]+[A-za-z]+\s[A-za-z]+";   // ALENDRONATE SODIUM 35MG TABLET
                const string postMashed = @"[A-Za-z]+\s[A-Za-z]+\s[\0-9]+\s[A-Za-z]+";
                const string oyster = @"[A-Za-z]+\s[A-Za-z]+\s[A-Za-z]+\s[\0-9]+-[\0-9]+\s[A-Za-z]+-[A-Za-z]+\s[A-Za-z]+"; // OYSTER SHELL CALCIUM 500-200 MG-IU TABLET
                const string aspirin = @"[A-Za-z]+\s[A-Za-z]+\s[A-Za-z]+\s[\0-9]+\s[A-Za-z]+\s[A-Za-z]+";

                // Normalize spaces
                while (source.Contains("  "))
                {
                    source = source.Replace("  ", " ");
                }

                // Crack the pieces apart if needed
                if (Regex.Match(source, mashedPattern).Success)
                {
                    var num = Regex.Match(source, mashedNumber);
                    source = source.Insert(num.Index + num.Length, " ");
                }
                // Try to match a basic patterns

                if (Regex.Match(source, aspirin).Success)
                {
                    parts = source.Split(' ');
                    Name = parts[0] + " " + parts[1] + " " + parts[2];
                    Strength = parts[3];
                    Unit = parts[4];
                    Form = parts[5];
                    return true;

                }
                if (Regex.Match(source, splitNamePattern).Success)
                {
                    parts = source.Split(' ');
                    Name = parts[0] + " " + parts[1];
                    Strength = parts[2];
                    Unit = parts[3];
                    Form = parts[4];
                    return true;
                }

                if (Regex.Match(source, standardPattern).Success)
                {
                    parts = source.Split(' ');
                    Name = parts[0];
                    Strength = parts[1];
                    Unit = parts[2];
                    Form = parts[3];
                    return true;
                }


                if (Regex.Match(source, oyster).Success)
                {
                    parts = source.Split(' ');
                    Name = parts[0] + " " + parts[1] + parts[2];
                    Strength = parts[3];
                    Unit = parts[4];
                    Form = parts[5];
                    return true;
                }


                if (Regex.Match(source, postMashed).Success)
                {
                    parts = source.Split(' ');
                    Name = parts[0] + " " + parts[1];
                    Strength = parts[2];
                    Unit = parts[3];
                    Form = "Form Unknown";
                    return true;
                }

                return false;
            }
        }
        // (0)DOE, JOHN ~(1)25731~(2)SPRINGFIELD RETIREMENT CASTLE~(3)~(4)2~(5)201~(6)13~(7)68180041109~(8)AVAPRO 150 MG TABLET~(9)IRBESARTAN 150 MG TABLET~(10)20170204~0730~1~~MICHELLE SMITH ~8880440~INFORME piel o reaccion alergica.~NO conducir si mareado o vision borrosa.~TAKE 1 TABLET DAILY IN THE MORNING ~~~~M~00~28~8880440_00_25/28~
        /// <summary>
        /// <c>Go</c>
        /// </summary>
        /// Built as a bridge interface to BestRx, the method reads a Parada packing machine file and pulls out as 
        /// much useable information as it can ad pushes it into the DB.  It's too lightweight to be considered a
        /// true interface, but can be used in a pinch.
        /// <param name="data"></param>
        public void Go()
        {
            var rows = data.Split('\n');
            var lastScrip = string.Empty;
            var lastDoseQty = string.Empty;
            var lastDoseTime = string.Empty;
            var tempTradeName = string.Empty;
            var firstScrip = true;
            var committed = false;

            var doseTimes = new List<string>();
            var nameVersion = 1;

            var scrip = new MotPrescriptionRecord("Add", AutoTruncate);
            var patient = new MotPatientRecord("Add", AutoTruncate);
            var facility = new MotFacilityRecord("Add", AutoTruncate);
            var drug = new MotDrugRecord("Add", AutoTruncate);
            var practitioner = new MotPrescriberRecord("Add", AutoTruncate);

            var writeQueue = new MotWriteQueue();
            patient.LocalWriteQueue =
            scrip.LocalWriteQueue =
            facility.LocalWriteQueue =
            practitioner.LocalWriteQueue =
            drug.LocalWriteQueue =
                writeQueue;

            patient.QueueWrites =
            scrip.QueueWrites =
            facility.QueueWrites =
            practitioner.QueueWrites =
            drug.QueueWrites =
                true;

            writeQueue.SendEof = false;  // Has to be off so we don't lose the socket.
            writeQueue.LogRecords = DebugMode;

            try
            {
                foreach (var row in rows)  // This will generate new records for every row - need to optimize
                {
                    if (row.Length > 0)  // --- The latest test file has a trailing "\r" and a messed up drug name,  need to address both
                    {
                        var rawRecord = row.Split('~');  // There's no guarantee that each field has data but the count is right

                        if (lastScrip != rawRecord[15]) // Start a new scrip
                        {
                            if (firstScrip == false)  // Commit the previous scrip
                            {
                                if (doseTimes.Count > 1)
                                {
                                    // Build and write TQ Record
                                    var tq = new MotTimesQtysRecord("Add", AutoTruncate)
                                    {
                                        // New name
                                        DoseScheduleName = facility.LocationName?.Substring(0, 3) + nameVersion
                                    };

                                    nameVersion++;

                                    // Fill the record
                                    tq.LocationID = patient.LocationID;
                                    tq.DoseTimesQtys = scrip.DoseTimesQtys;

                                    // Assign the DoseSchcedulw name to the scrip & clear the scrip DoseTimeSchedule
                                    scrip.DoseScheduleName = tq.DoseScheduleName;
                                    scrip.DoseTimesQtys = "";

                                    // Write the record
                                    tq.LocalWriteQueue = writeQueue;
                                    tq.AddToQueue();
                                }

                                Console.WriteLine($"Committing on Thread {Thread.CurrentThread.Name}");

                                scrip.Commit(GatewaySocket);
                                committed = true;

                                practitioner.Clear();
                                facility.Clear();
                                drug.Clear();
                                patient.Clear();
                                scrip.Clear();
                            }

                            firstScrip = committed = false;
                            lastScrip = rawRecord[15];
                            doseTimes.Clear();

                            var names = rawRecord[0].Split(',');
                            patient.LastName = (names[0] ?? "Warning[PFN]").Trim();
                            patient.FirstName = (names[1] ?? "Warning[PLN]").Trim();

                            if (rawRecord[2].Length >= 4)
                            {
                                patient.LocationID = rawRecord[2]?.Trim()?.Substring(0, 4);
                            }

                            patient.PatientID = rawRecord[1];
                            patient.Room = rawRecord[4];
                            patient.Address1 = "";
                            patient.Address2 = "";
                            patient.City = "";
                            patient.State = "NH";
                            patient.Zipcode = "";
                            patient.Status = 1;

                            facility.LocationName = rawRecord[2];
                            facility.Address1 = "";
                            facility.Address2 = "";
                            facility.City = "";
                            facility.State = "NH";
                            facility.Zipcode = "";

                            if (rawRecord[2].Length >= 4)
                            {
                                facility.LocationID = rawRecord[2]?.Trim()?.Substring(0, 4);
                            }

                            drug.NDCNum = rawRecord[7];
                            drug.DrugID = rawRecord[7];

                            if (drug.NDCNum == "0")
                            {
                                // It's something goofy like a compound; ignore it
                                EventLogger.Warn($"Ignoring thig with NDC == 0 named {rawRecord[8]}");
                                continue;

                                //__scrip.ChartOnly = "1";
                                //__drug.NDCNum = "00000000000";
                                //var __rand = new Random();
                                //__drug.RxSys_DrugID = __rand.Next().ToString();
                                //__drug.DefaultIsolate = 1;
                            }

                            //
                            // Some funny stuff happens here as the umber of items from the split is varible - we've seen 2,3,4, & 5
                            // Examples
                            //          -- Trade And Generic Pair - Each 4 items, same format {[name][strength][unit][route]}
                            //          AVAPRO 150 MG TABLE
                            //          IRBESARTAN 150 MG TABLET
                            //
                            //          -- Trade and Generic Name - 4 items for each but different strength formats
                            //          CRESTOR 10 MG TABLET
                            //          ROSUVASTATIN TB 10MG 30          
                            //
                            //          -- Trade and Generic Name  - 4 items for trade, 5 for Generic
                            //          ZOLOFT 100 MG TABLET
                            //          SERTRALINE HCL 100 MG TABLET
                            //
                            //          -- Trade and Generic Name - 6 items for Trade, 5 for Generic
                            //          500 + D 500-200 MG-IU TABLET
                            //          OYSTER SHELL CALCIUM 500-200 MG
                            //
                            //          Another Trade and Genmeric Name with 4 items for the Trade and 5 for the generic
                            //          FOSAMAX 35 MG TABLET
                            //          ALENDRONATE SODIUM 35 MG TABLET
                            //
                            //  Note that it's also only required to have one or the other, not both
                            //
                            //
                            //  Push everything down into a structure and have it return a struct defining both

                            tempTradeName = string.Empty;

                            if (DrugName.ParseExact(rawRecord[8]))
                            {
                                drug.TradeName = drug.DrugName = $"{DrugName.Name} {DrugName.Strength} {DrugName.Unit} {DrugName.Form}";
                                drug.ShortName = $"{DrugName.Name} {DrugName.Strength} {DrugName.Unit}";
                                drug.Unit = DrugName.Unit;
                                drug.Strength = Convert.ToDouble(DrugName.Strength ?? "0.00");
                                drug.DoseForm = DrugName.Form;

                                tempTradeName = $"{DrugName.Name} {DrugName.Strength} {DrugName.Unit} {DrugName.Form}";
                            }

                            if (DrugName.ParseExact(rawRecord[9]))
                            {
                                drug.TradeName = drug.DrugName = $"{DrugName.Name} {DrugName.Strength} {DrugName.Unit} {DrugName.Form}";
                                drug.ShortName = $"{DrugName.Name} {DrugName.Strength} {DrugName.Unit}";
                                drug.Unit = DrugName.Unit;
                                drug.Strength = Convert.ToDouble(DrugName.Strength ?? "0.00");
                                drug.DoseForm = DrugName.Form;
                                drug.GenericFor = tempTradeName;
                            }

                            if (!string.IsNullOrEmpty(rawRecord[9]) && string.IsNullOrEmpty(drug.TradeName))
                            {
                                drug.DrugName = drug.TradeName = rawRecord[9] ?? "Unknown";
                            }

                            string[] practitionerName = rawRecord[14].Split(' ');
                            practitioner.FirstName = (practitionerName[0] ?? "Warning[DFN]").Trim();
                            practitioner.LastName = (practitionerName[1] ?? "Warning[DLN]").Trim();
                            practitioner.Address1 = "";
                            practitioner.Address2 = "";
                            practitioner.City = "";
                            practitioner.State = "NH";
                            practitioner.Zipcode = "";

                            try
                            {
                                switch (practitionerName.Count())
                                {
                                    case 2:
                                        if (practitionerName[0].Length >= 3 && practitionerName[1].Length >= 3)
                                        {
                                            practitioner.PrescriberID = practitionerName[0]?.Trim()?.Substring(0, 3) + practitionerName[1]?.Trim()?.Substring(0, 3);
                                        }
                                        practitioner.FirstName = (practitionerName[0] ?? "Warning[DFN]").Trim();
                                        practitioner.LastName = (practitionerName[1] ?? "Warning[DLN]").Trim();
                                        break;

                                    case 3:
                                        if (practitionerName[1].Length >= 3 && practitionerName[2].Length >= 3)
                                        {
                                            practitioner.PrescriberID = practitionerName[0]?.Trim()?.Substring(0, 3) + practitionerName[2]?.Trim()?.Substring(0, 3);
                                        }
                                        practitioner.FirstName = practitionerName[0]?.Trim();
                                        practitioner.MiddleInitial = practitionerName[1].Trim();
                                        practitioner.LastName = practitionerName[2]?.Trim();
                                        break;

                                    default:
                                        practitioner.PrescriberID = new Random(40000).ToString();
                                        break;
                                }
                            }
                            catch
                            {
                                practitioner.PrescriberID = new Random(60000).ToString();
                            }

                            patient.PrimaryPrescriberID = practitioner.PrescriberID;

                            scrip.PrescriptionID = rawRecord[15];
                            scrip.PatientID = patient.PatientID;
                            scrip.PrescriberID = patient.PrimaryPrescriberID;
                            scrip.Status = 1;

                            if (string.IsNullOrEmpty(rawRecord[10]))  // It's a PRN
                            {
                                scrip.RxStartDate = DateTime.Now;
                            }
                            else
                            {
                                scrip.RxStartDate = DateTime.Parse(rawRecord[10] ?? "1970-01-01");
                            }

                            if (rawRecord[11] != null && rawRecord[12] != null)
                            {
                                scrip.DoseTimesQtys = $"{Convert.ToInt32(rawRecord[11]):0000}{Convert.ToDouble(rawRecord[12]):00.00}";
                            }

                            scrip.QtyPerDose = Convert.ToDouble(rawRecord[12] ?? "0.00");
                            scrip.QtyDispensed = Convert.ToDouble(rawRecord[24] ?? "0.00");
                            scrip.Sig = $"{rawRecord[18]}\n{rawRecord[19]}\n{rawRecord[20]}";
                            scrip.Comments = $"{rawRecord[16]}\n{rawRecord[17]}";
                            scrip.Refills = Convert.ToInt32(rawRecord[23] ?? "0");
                            scrip.DrugID = drug.NDCNum;
                            scrip.RxType = rawRecord[22].Trim().ToUpper() == "P" ? 2 : 1;

                            lastDoseTime = rawRecord[11];
                            lastDoseQty = rawRecord[12];
                            doseTimes.Add(lastDoseTime);

                            practitioner.AddToQueue();
                            patient.AddToQueue();
                            facility.AddToQueue();
                            drug.AddToQueue();
                            scrip.AddToQueue();
                        }
                        else
                        {
                            if (lastDoseTime != rawRecord[11] || lastDoseQty != rawRecord[12])
                            {
                                if (!doseTimes.Contains(rawRecord[11]))  // Create a new TQ Dose Schedule here!!
                                {
                                    // Kludge
                                    if (rawRecord[11] != null)
                                    {
                                        var tempTest = $"{Convert.ToInt32(rawRecord[11]):0000}";

                                        if (tempTest.Substring(2, 2) == "15" ||
                                            tempTest.Substring(2, 2) == "45")
                                        {
                                            tempTest = tempTest.Replace(tempTest.Substring(2, 2), "00");
                                            scrip.DoseTimesQtys += $"{tempTest}{Convert.ToDouble(rawRecord[12]):00.00}";

                                        }
                                        else
                                        {
                                            scrip.DoseTimesQtys += $"{Convert.ToInt32(rawRecord[11]):0000}{Convert.ToDouble(rawRecord[12]):00.00}";
                                        }
                                    }
                                    lastDoseQty = rawRecord[12];
                                    lastDoseTime = rawRecord[11];
                                    doseTimes.Add(lastDoseTime);
                                }
                            }
                        }
                    }
                    else
                    {
                        // Fell through to here because the row length was 0
                        if (committed)
                        {
                            continue;
                        }

                        scrip.Commit(GatewaySocket);
                        committed = true;
                    }
                }
            }
            catch (Exception ex)
            {
                EventLogger.Error($"Error parsing Parada record {data}: {ex.Message}");
                throw;
            }
        }
    }
}
