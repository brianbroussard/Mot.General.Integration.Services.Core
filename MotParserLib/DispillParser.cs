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
//

using System;
using MotCommonLib;

namespace MotParserLib
{
    class DispillParser : ParserBase, IDisposable
    {
        /// <summary>
        /// Dispill uses a simple CSV file made of three components, with each component identified by the first character of each line.
        /// </summary>
        /*
         NEW RECORD P                                   (Pharmacy)
            STORE NAME PBestRx Pharmacy,
            STORE ADDR 1 1200 Jorie Blvd,
            STORE ADR 2 Suite 310,
            STORE CITY Oak Brook,
            STORE STATE NY,
            STORE ZIP 60523,
            STORE PHONE 6308939210
         NEW RECORD F REPEATS N TIMES                   (Patient)
            PAT ID 25731,
            PAT LAST DOE,
            PAT FIRST JOHN,
            PAT BIRTH NAME,
            PAT DOB 1980-04-23,
            PAT ADDR 1 167 W GRAND AVE,
            PAT ADDR 2 UNIT 1407,
            PAT CITY CHICAGO,
            PAT STATE IL,
            PAT ZIP 60654,
            PAT PHONE ,
            FIRST DOSE 2017-02-07,
            NUM DAYS 28,
            LANGUAGE ES,
            PAT LOC 2/201/13 [FLOOR/ROOM/BED]
         NEW RECORD M REPEATS N TIMES FOR EACH PATIENT  (Rx)
            RX NUM 8880340,
            DRUG NAME LOSARTAN POTASSIUM 100MG TABS 1000 EA,
            DRUG FORM TAB,
            DRUG DESC,
            DRUG QTY 30,
            INSTRUCTION TAKE 1 TABLET DAILY,
            PRESCRIBER JACK GERARD,
            INTAKE CODE 01;01;01;01;01;01;00;00;00,
            NDC(9) 435470362,
            REFILLS 4,
            PRINT COPIES,
            MANUFACTURER SOLCO HEAL,
            BRAND NAME ,
            BATCH # ,
            MED EXP DATE,
            DOC FIRST JACK,
            DOC LAST GERARD,
            DOC MI ,
            DOC ADDR 1 599 SO.FEDERAL HWY,
            DOC ADDR 2 ,
            DOC CITY CHICAGO,
            DOC STATE IL,
            DOC ZIP 60601,
            DOC PHONE 773-222-9999,
            MED SIDE-EFF
            <EOF>

            SAMPLE FILE
            PBestRx Pharmacy,1200 Jorie Blvd,Suite 310,Oak Brook,NY,60523,6308939210
            F25989,CRUZ-KATZ, GABRIEL,,1977-03-21,8 KAATESKILL PLACE,,SCARSDALE,NY,10583,917-519-4156,2017-02-07,28,EN,//
            M8880394,ZOLPIDEM 10 MG TABLET,TAB,Oval;White to Off-white;Txt:E+79;Film-coated,30,1 TABLET BY MOUTH AT BEDTIME DO NOT COMBINE WITH ALCOHOL. DO NOT DRIVE WHEN USING. MDD: 10 MG/DAY,RICHARD GABEL,00;00;00;01,167140622,0,,NORTHSTAR,,,,RICHARD,GABEL,,12 GREENRIDGE AVE STE 404,,WHITE PLAINS,NY,10605,914-681-0202,Drowsiness; Dizziness; Headache,
            M8880396,VICODIN 5/500MG TAB,CAP,,30,TAKE 1 TABLET DAILY,RICHARD GABEL,00;01;00;00,000780350,0,,KNOLL,,,,RICHARD,GABEL,,12 GREENRIDGE AVE STE 404,,WHITE PLAINS,NY,10605,914-681-0202,,
            M8880398,LEVAQUIN 250 MG TABLET,TAB,Oblong;Txt:LEVAQUIN+250;Film-Coated,30,TAKE 1 TABLET DAILY BY MOUTH,RICHARD GABEL,01;00;00;00,504580920,5,,JANSSEN PH,,,,RICHARD,GABEL,,12 GREENRIDGE AVE STE 404,,WHITE PLAINS,NY,10605,914-681-0202,Diarrhea; Vomiting; Nausea,
            F25731,DOE, JOHN,,1980-04-23,167 W GRAND AVE,UNIT 1407,CHICAGO,IL,60654,,2017-02-07,28,ES,2/201/13
            M8880344,COMPOUND ASD,TAB,,200,AS DIRECTED.|SEGUN LAS INSTRUCCIONES,MICHELLE SMITH,01;01;00;01,0,2,,,,,,MICHELLE,SMITH,,WEST CITY MEDICAL CENTER,SECOND FLOOR,CHICAGO,IL,60612,773-222-9987,,
            M8880371,VIOGEN C CAPSULE,CAP,,60,AS DIRECTED.|SEGUN LAS INSTRUCCIONES,MICHELLE SMITH,01;00;01;00,243850940,0,,AMERISOURC,,,,MICHELLE,SMITH,,WEST CITY MEDICAL CENTER,SECOND FLOOR,CHICAGO,IL,60612,773-222-9987,DIARREA.,
            M8880384,COLCRYS 0.6 MG TABLET,TAB,Capsule-Shaped;Purple;Txt:AR 374,10,TAKE 1 TABLET AS DIRECTED.|TOME 1 TABLET SEGUN LAS INSTRUCCIONES,MICHELLE SMITH,00;00;01;00,647640119,5,,TAKEDA PHA,,,,MICHELLE,SMITH,,WEST CITY MEDICAL CENTER,SECOND FLOOR,CHICAGO,IL,60612,773-222-9987,,
            M8880403,VIAGRA 25 MG TABLET,TAB,Diamond-Shaped;Purple-Blue;Txt:Pfizer+VGR 25;Film-Coated,30,AS DIRECTED.|SEGUN LAS INSTRUCCIONES,JOHN QUAGLIARELLO,01;00;00;00,000694200,5,,PFIZER LAB,,,,JOHN,QUAGLIARELLO,,530 FIRST AV,,CHICAGO,IL,60601,773-222-9999,SENSACION DE CALOR Y ENROJECIMIENTO DE LA CARA; DOLOR DE CABEZA; CONGESTION NASAL,
            M8880439,IBUPROFEN 200 MG TABLET,TAB,,120,TAKE 2 TABLETS TWICE DAILY|TOME 2 TABLETAS DOS VECES AL DIA,MICHELLE SMITH,02;02;00;00,009047915,5,,MAJOR PHAR,,,,MICHELLE,SMITH,,WEST CITY MEDICAL CENTER,SECOND FLOOR,CHICAGO,IL,60612,773-222-9987,,
            M8880440,IRBESARTAN 150 MG TABLET,TAB,Oval;White to Off-white;Txt:LU+M12,30,TAKE 1 TABLET DAILY IN THE MORNING,MICHELLE SMITH,01;00;00;00,681800411,11,,LUPIN PHAR,,,,MICHELLE,SMITH,,WEST CITY MEDICAL CENTER,SECOND FLOOR,CHICAGO,IL,60612,773-222-9987,INFECCION RESPIRATORIA; CANSANCIO O DEBILIDAD ATIPICA; ARDOR DE ESTOMAGO,
            M8880441,ROSUVASTATIN TB 10MG 30,TAB,Round;Pink;Txt:RU10;Convex coated,30,TAKE 1 TABLET DAILY IN THE EVENING,MICHELLE SMITH,00;00;00;01,162520616,11,,COBALT LAB,,,,MICHELLE,SMITH,,WEST CITY MEDICAL CENTER,SECOND FLOOR,CHICAGO,IL,60612,773-222-9987,CONFUSION; DIFICULTAD CON LA MEMORIA; ALTO NIVEL DE AZUCAR EN LA SANGRE,
            M8880442,JANUVIA 100 MG TABLET,TAB,,240,TAKE 2 TABLETS 4 TIMES A DAY|TOME 2 TABLETAS CUATROS VECES AL DIA,MICHELLE SMITH,02;02;02;02,122800401,11,,DISPENSEXP,,,,MICHELLE,SMITH,,WEST CITY MEDICAL CENTER,SECOND FLOOR,CHICAGO,IL,60612,773-222-9987,,
            M8880457,VICODIN 5-300 MG TABLET,TAB,Capsule-Shaped;White;Txt:VICODIN+5/300,10,TAKE 1 TABLET DAILY|TOME 1 TABLET DAILY,CHIRAG PATEL,01;00;00;00,000743041,0,,ABBOTT LAB,,,,CHIRAG,PATEL,,9500 EUCLID AVE,,CLEVELAND,OH,44195,216-444-7007,sindrome de la serotonina; ATURDIMIENTO; VOMITOS,
            M8880458,DIAZEPAM 10 MG TABLET,TAB,Round;Light-Blue;Txt:DAN 5620+10;Scored Tab,30,TAKE 1 TABLET DAILY|TOME 1 TABLET DAILY,CHIRAG PATEL,01;00;00;00,518620064,1,,WATSON PHA,,,,CHIRAG,PATEL,,9500 EUCLID AVE,,CLEVELAND,OH,44195,216-444-7007,SOMNOLENCIA; MAREOS; TRASTORNOS VISUALES,
            F22326,MENDOZA, JENNIFER,,1999-07-02,691 MAIN ST,,CHICAGO,IL,60601,773-123-4567,2017-02-07,28,ES,//
            M8880334,NEXIUM 20 MG CAPSULE,CAP,Capsule;Purple;Txt:TWO RADIAL BARS+20 mg;IMPRINTS IN YELLOW,60,TAKE 1 TABLET DAILY,JACK GERARD,00;01;00;00,001865020,4,,ASTRAZENEC,,,,JACK,GERARD,,599 SO.FEDERAL HWY,,CHICAGO,IL,60601,773-222-9999,DOLOR DE CABEZA; DIARREA; NAUSEA,
            M8880335,BERBERIS VULG TABLET,TAB,,60,TAKE 1 TABLET TWICE DAILY|TOME 1 TABLET DOS VECES AL DIA,JACK GERARD,02;02;00;00,006241042,4,,LUYTIES PH,,,,JACK,GERARD,,599 SO.FEDERAL HWY,,CHICAGO,IL,60601,773-222-9999,,
            M8880336,A & D JR. CAPSULE,CAP,,30,TAKE 1 TABLET DAILY|TOME 1 TABLET DAILY,JACK GERARD,00;00;01;00,116940693,4,,KEY COMPAN,,,,JACK,GERARD,,599 SO.FEDERAL HWY,,CHICAGO,IL,60601,773-222-9999,,
            M8880337,CABERGOLINE 0.5 MG TABLET,TAB,,30,TAKE 1 TABLET DAILY|TOME 1 TABLET DAILY,JACK GERARD,00;01;00;00,680840245,4,,AMERISOURC,,,,JACK,GERARD,,599 SO.FEDERAL HWY,,CHICAGO,IL,60601,773-222-9999,NAUSEA; DOLOR DE CABEZA; MAREOS,
            M8880338,DECADRON TAB .75 MG 100,TAB,,30,TAKE 1 TABLET DAILY|TOME 1 TABLET DAILY,JACK GERARD,00;00;01;00,000060063,4,,,,,,JACK,GERARD,,599 SO.FEDERAL HWY,,CHICAGO,IL,60601,773-222-9999,,
           */

        /// <summary>
        /// <c>Dispose</c>
        /// </summary>
        /// <param name="disposing"></param>
        protected new virtual void Dispose(bool disposing)
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

        public DispillParser(string inputStream) : base(inputStream)
        { }

        // Missing DEA for Pharmacy and Prescribers
        // NDC-9 with the Manufacturer a short name
        public void Go()
        {
            try
            {
                var workingData = data.Split('\n');
                var writeQueue = new MotWriteQueue
                {
                    SendEof = false,  // Has to be off so we don't lose the socket.
                    debugMode = DebugMode
                };

                string lastPatId = string.Empty;

                foreach (var line in workingData)
                {
                    switch (line.Substring(0, 1))
                    {
                        case "P":
                            var s1 = line.Substring(1);
                            var f1 = s1.Split(',');

                            var store = new MotStoreRecord("Add", AutoTruncate)
                            {
                                LocalWriteQueue = writeQueue,
                                QueueWrites = true,

                                StoreID = f1[0].Replace(" ", ""),
                                StoreName = f1[0],
                                Address1 = f1[1],
                                Address2 = f1[2],
                                City = f1[3],
                                Zipcode = f1[4],
                                Phone = f1[5]
                            };

                            store.AddToQueue();
                            break;

                        case "F":
                            var s2 = line.Substring(1);
                            var f2 = s2.Split(',');
                            var patient = new MotPatientRecord("Add", AutoTruncate)
                            {
                                LocalWriteQueue = writeQueue,
                                QueueWrites = true,

                                PatientID = lastPatId = f2[0],
                                LastName = f2[1],
                                FirstName = f2[2],
                                Address1 = f2[5],
                                Address2 = f2[6],
                                City = f2[7],
                                State = f2[8],
                                Zipcode = f2[9],
                                Phone1 = f2[10],
                                
                                CycleDays = Convert.ToInt16(f2[12]),
                                Room = f2[14]
                            };

                            patient.CycleDate = patient.TransformDate(f2[11] ?? "");
                            patient.DOB = patient.TransformDate(f2[4] ?? "");

                            patient.AddToQueue();
                            break;

                        case "M":
                            var s3 = line.Substring(1);
                            var f3 = s3.Split(',');
                            var rx = new MotPrescriptionRecord("Add", AutoTruncate);
                            var doc = new MotPrescriberRecord("Add", AutoTruncate);
                            var drug = new MotDrugRecord("Add", AutoTruncate);

                            rx.LocalWriteQueue = doc.LocalWriteQueue = drug.LocalWriteQueue = writeQueue;
                            rx.QueueWrites = doc.QueueWrites = drug.QueueWrites = true;

                            doc.PrescriberID = f3[15]?.Substring(0, 3) + f3[16]?.Substring(0, 3);
                            doc.FirstName = f3[15];
                            doc.LastName = f3[16];
                            doc.MiddleInitial = f3[17];
                            doc.Address1 = f3[18];
                            doc.Address2 = f3[19];
                            doc.City = f3[20];
                            doc.State = f3[21];
                            doc.Zipcode = f3[22];
                            doc.Phone = f3[23];

                            // M8880338,DECADRON TAB .75 MG 100,TAB,,30,TAKE 1 TABLET DAILY|TOME 1 TABLET DAILY,JACK GERARD,00;00;01;00,000060063,4,,,,,,JACK,GERARD,,599 SO.FEDERAL HWY,,CHICAGO,IL,60601,773-222-9999,,

                            drug.DrugID = f3[8];

                            if (f3[8].Length > 9)
                            {
                                drug.NDCNum = f3[8];          // Unlikely, but the NDC is already 11
                            }
                            else if (f3[8].Length == 9)
                            {
                                drug.NDCNum = f3[8] + "00";   // Accepted conversion to NDC11
                            }
                            else
                            {
                                drug.NDCNum = "00000000000";
                            }

                            drug.DrugName = f3[1];
                            drug.DoseForm = f3[2];
                            drug.VisualDescription = f3[3];
                            drug.TradeName = f3[12];

                            rx.PrescriptionID = f3[0];
                            rx.DrugID = drug.DrugID;
                            rx.PrescriberID = doc.PrescriberID;
                            rx.PatientID = lastPatId;
                            rx.Sig = f3[5];
                            rx.QtyDispensed = Convert.ToDouble(f3[4] ?? "0.00");
                            rx.Refills = Convert.ToInt32(f3[9] ?? "0");
                            rx.RxStopDate = rx.TransformDate(f3[14] ?? ""); // Actually the Expire Date

                            // Intake Code - 00,00,00,00 = 0800[q], 1200[q], 1800[q], 2100[q]
                            var dq = f3[7].Split(';');

                            string[] time =  // Need to get the actual dose times from the RxSystem or User, for now just use defaults
                            {
                                "0800",
                                "1200",
                                "1800",
                                "2100"
                            };

                            var i = 0;

                            foreach (var d in dq)
                            {
                                if (d != "00")
                                {
                                    rx.DoseTimesQtys = $"{time[i]}{Convert.ToDouble(d):00.00}";
                                }

                                i++;
                            }
                            rx.DoseScheduleName = "Dispill";

                            doc.AddToQueue();
                            drug.AddToQueue();
                            rx.AddToQueue();
                            break;
                    }
                }

                if (workingData.Length > 0)
                {
                    writeQueue.Write(GatewaySocket);
                }
            }
            catch (Exception ex)
            {
                EventLogger.Error("Failed to parse Dispill file:  {0}", ex.Message);
                throw;
            }
        }
    }
}
