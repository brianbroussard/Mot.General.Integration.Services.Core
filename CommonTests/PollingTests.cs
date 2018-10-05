using System;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Mot.Common.Interface.Lib;
using Mot.Polling.Interface.Lib;
using System.Net.Http;

namespace CommonTests
{
    [TestClass]
    public class PollingTests
    {
        // Change these to the machine you want to hit
        string targetIp = "localhost";
        int targetPort = 24042;

        public string GetLorum(int amount)
        {
            // build the JSON request URL
            var requestUrl = $"http://loripsum.net/api/{amount}/long/plaintext";

            try
            {
                using (var client = new HttpClient())
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Get, new Uri(requestUrl)))
                    {
                        request.Headers.Add("Accept", "application/json");

                        using (var response = client.SendAsync(request).Result)
                        {
                            response.EnsureSuccessStatusCode();
                            var text = response.Content.ReadAsStringAsync().Result;
                            return text.Substring(text.IndexOf('.') + 2);
                        }
                    }
                }
            }
            catch (HttpRequestException hex)
            {
                Console.WriteLine(hex.Message);
                throw;
                //return default(T);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }


        // ------- Format Utilities
        string FullTrim(string val)
        {
            if (string.IsNullOrEmpty(val))
            {
                return string.Empty;
            }

            char[] junk = { ' ', '-', '.', ',', ' ', ';', ':', '(', ')' };

            while (val?.IndexOfAny(junk) > -1)
            {
                val = val.Remove(val.IndexOfAny(junk), 1);
            }

            return val;
        }

        double DRand()
        {
            return new Random((int)DateTime.Now.Ticks & 0x0000FFFF).NextDouble() * 100;
        }

        int IRand()
        {
            return new Random((int)DateTime.Now.Ticks & 0x0000FFFF).Next();
        }

        // ---- Real Work
        bool CreatedRecords = false;

        public void CreateSqlRecords()
        {
            var dataSource = "PROXYPLAYGROUND";

            if(CreatedRecords == true)
            {
                return;
            }

            try
            {
                using (var motSqlServer = new MotSqlServer($"Data Source={dataSource};Initial Catalog=McKessonTestDb;User ID=sa;Password=$MOT2018"))
                {
                    for (var i = 0; i < 16; i++)
                    {
                        CreatedRecords = true;

                        var docId = IRand(); 
                        var patId = IRand();
                        var facId = IRand();
                        var storeId = IRand();
                        var scripId = IRand();
                        var drugId = IRand();
                       
                        // Prescriber
                        var sql = $"INSERT INTO dbo.vPrescriber VALUES(" +
                                  $"'{docId}', " +
                                  $"'{FullTrim(GetLorum(1).Substring(0,25)).ToUpper()}', " +
                                  $"'{FullTrim(GetLorum(1).Substring(0,15)).ToUpper()}', " +
                                  $"'{FullTrim(GetLorum(1).Substring(0,1)).ToUpper()}', " +
                                  $"'{FullTrim(GetLorum(1).Substring(0,25)).ToUpper()}', " +
                                  $"'{FullTrim(GetLorum(1).Substring(0,25)).ToUpper()}', " +
                                  $"'{FullTrim(GetLorum(1).Substring(0,20)).ToUpper()}', " +
                                  $"'MA', " +
                                  $"'02165', " +
                                  $"'1234', " +
                                  $"'617', " +
                                  $"'9696072', " +
                                  $"'00', " +
                                  $"'{FullTrim(GetLorum(1).Substring(0, 25)).ToUpper()}', " +
                                  $"'123456', " +
                                  $"'{GetLorum(2).Substring(0,2).ToUpper()}0123456', " +
                                  $"'1', " +
                                  $"DEFAULT);";

                        motSqlServer.ExecuteNonQuery(sql);

                        // Prescriber Note
                        sql = $"INSERT INTO dbo.vPrescriberNote VALUES(" +
                              $"'{docId}', " +
                              $"'{IRand()}', " +
                              $"'{FullTrim(GetLorum(1).Substring(0, 10)).ToUpper()}', " +
                              $"'{FullTrim(GetLorum(1).Substring(0, 30)).ToUpper()}', " +
                              $"'{DateTime.Now}', " +
                              $"'{GetLorum(20)}');";

                        motSqlServer.ExecuteNonQuery(sql);



                        sql = $"INSERT INTO dbo.vMOTLocation VALUES(" +
                                  $"'{facId}', " +
                                  $"'{storeId}', " +
                                  $"'{FullTrim(GetLorum(1).Substring(0,64)).ToUpper()}', " +
                                  $"'{FullTrim(GetLorum(1).Substring(0,50)).ToUpper()}', " +
                                  $"'{FullTrim(GetLorum(1).Substring(0,50)).ToUpper()}', " +
                                  $"'{FullTrim(GetLorum(1).Substring(0,25)).ToUpper()}', " +
                                  $"'NH', " +
                                  $"'03049', " +
                                  $"'6034659622', " +
                                  $"DEFAULT);";

                        motSqlServer.ExecuteNonQuery(sql);

                        // Drug
                        sql = $"INSERT INTO dbo.vItem VALUES('{drugId}', " +                //[ITEM_ID]
                             $"'1', " +                                                     //[ITEM_VERSION]
                             $"'{FullTrim(GetLorum(1).Substring(0, 11)).ToUpper()}', " +    //[NDC_CODE]
                             $"'{FullTrim(GetLorum(1).Substring(0, 2)).ToUpper()}', " +     //[PACKAGE_CODE]
                             $"'{DRand()}', " +                                             //[PACKAGE_SIZE]
                             $"'1', " +                                                     //[CURRENT_ITEM_VERSION]
                             $"'{FullTrim(GetLorum(1).Substring(0, 3)).ToUpper()}', " +     //[ITEM_TYPE]
                             $"'{GetLorum(1).Substring(0, 40)}', " +                        //[ITEM_NAME]
                            $"'{IRand()}', " +                                              //[KDC_NUMBER]
                             $"'10', " +                                                    //[GPI_GROUP_CODE]
                             $"'8', " +                                                     //[GPI_CLASS_CODE]
                             $"'22', " +                                                    //[GPI_SUBCLASS_CODE]
                             $"'18', " +                                                    //[GPI_NAME_CODE]
                             $"'92', " +                                                    //[GPI_NAME_EXTENSION_CODE]
                             $"'33', " +                                                    //[GPI_DOSAGE_FORM_CODE]
                             $"'102', " +                                                   //[GPI_STRENGTH_CODE]
                             $"'{IRand()}', " +                                             //[HRI_NUMBER]
                             $"'{FullTrim(GetLorum(1).Substring(0, 7)).ToUpper()}', " +     //[DOSAGE_SIGNA_CODE]
                             $"'{GetLorum(1).Substring(0, 40)}', " +                        //[INSTRUCTION_SIGNA_STRING]
                             $"'{FullTrim(GetLorum(1).Substring(0, 4)).ToUpper()}', " +     //[FORM_TYPE]
                             $"'{FullTrim(GetLorum(1).Substring(0, 3)).ToUpper()}', " +     //[ROUTE_OF_ADMINISTRATION]
                             $"'{IRand()}', " +                                             //[ALTERNATE_MANUFACTURER_ID]
                             $"'{FullTrim(GetLorum(1).Substring(0, 13)).ToUpper()}', " +    //[UPC]
                             $"'{DRand()}', " +                                             //[STRENGTH]
                             $"'{FullTrim(GetLorum(1).Substring(0, 4)).ToUpper()}', " +     //[COLOR_CODE]
                             $"'{FullTrim(GetLorum(1).Substring(0, 4)).ToUpper()}', " +     //[FLAVOR_CODE]
                             $"'{FullTrim(GetLorum(1).Substring(0, 4)).ToUpper()}', " +     //[SHAPE_CODE]
                             $"'{FullTrim(GetLorum(1).Substring(0, 10)).ToUpper()}', " +    //[PRODUCT_MARKING]
                             $"'6', " +                                                     //[NARCOTIC_CODE]
                             $"'{DRand()}', " +                                             //[UNIT_SIZE]
                             $"'{FullTrim(GetLorum(1).Substring(0, 2)).ToUpper()}', " +     //[UNIT_OF_MEASURE]
                             $"'{FullTrim(GetLorum(1).Substring(0, 5)).ToUpper()}', " +     //[NDC_Manufacturer_Number]
                             $"'{FullTrim(GetLorum(1).Substring(0, 10)).ToUpper()}', " +    //[Manufacturer_Abbreviation]
                             $"DEFAULT);";                                                  //[MSSQLTS]                                                                                             

                             motSqlServer.ExecuteNonQuery(sql);

                        // Drug Caution
                        sql = $"INSERT INTO dbo.vItemCaution VALUES(" +
                              $"'{IRand()}', " +
                              $"'{FullTrim(GetLorum(1).Substring(0,4)).ToUpper()}', " +
                              $"'{GetLorum(1).Substring(0,255)}');";

                        motSqlServer.ExecuteNonQuery(sql);


                        // Patient
                        sql = $"INSERT INTO dbo.vPatient VALUES(" +
                                  $"'{patId}', " +                                                  // Patient_ID
                                  $"'{FullTrim(GetLorum(1).Substring(0,25)).ToUpper()}', " +        // Last_Name
                                  $"'{FullTrim(GetLorum(1).Substring(0,15)).ToUpper()}', " +        // First_Name
                                  $"'{FullTrim(GetLorum(1).Substring(0,1)).ToUpper()}', " +         // Middle_Initial
                                  $"'{FullTrim(GetLorum(1).Substring(0,25)).ToUpper()}', " +        // Address_Line_1   
                                  $"'{FullTrim(GetLorum(1).Substring(0,25)).ToUpper()}', " +        // Address_Line_2
                                  $"'{FullTrim(GetLorum(1).Substring(0,20)).ToUpper()}', " +        // City
                                  $"'NH', " +                                                       // State_Code
                                  $"'02165', " +                                                    // Zip_Code
                                  $"'1234', " +                                                     // Zip_Plus_4
                                  $"'{FullTrim(GetLorum(1).Substring(0,1)).ToUpper()}', " +         // Patient_Location_Code
                                  $"'{docId}', " +                                                  // Primary_Prescriber_ID
                                  $"'{IRand()}', " +                                                // SSN
                                  $"'{DateTime.Now.ToString()}', " +                                // BirthDate
                                  $"'{DateTime.Now.ToString()}', " +                                // Deceased_Date
                                  $"'{GetLorum(1).Substring(0, 1).ToUpper()}', " +                  // Sex
                                  $"DEFAULT, " +                                                    // MSSQLTS
                                  $"'617', " +                                                      // Area_Code
                                  $"'3324531', " +                                                  // Telephone_Number
                                  $"'00');";                                                        // Extension

                        motSqlServer.ExecuteNonQuery(sql);

                        // Patient Note
                        sql = $"INSERT INTO dbo.vPatientNote VALUES(" +
                              $"'{patId}', " +
                              $"'{IRand()}', " +
                              $"'{FullTrim(GetLorum(1).Substring(0, 10)).ToUpper()}', " +
                              $"'{FullTrim(GetLorum(1).Substring(0, 30)).ToUpper()}', " +
                              $"'{DateTime.Now}', " +
                              $"'{GetLorum(20)}');";

                        motSqlServer.ExecuteNonQuery(sql);

                        // Patient Allergy
                        sql = $"INSERT INTO dbo.vPatientAllergy VALUES(" +
                              $"'{patId}', " +
                              $"'{IRand()}', " +
                              $"'{FullTrim(GetLorum(1).Substring(0, 3)).ToUpper()}', " +
                              $"'{GetLorum(1).Substring(0, 80)}', " +
                              $"'{GetLorum(1).Substring(0, 70)}', " +
                              $"'{IRand()}', " +
                              $"'{DateTime.Now}');";

                        motSqlServer.ExecuteNonQuery(sql);

                        // Patient Diagnosis
                        sql = $"INSERT INTO dbo.vPatientDiagnosis VALUES(" +
                              $"'{patId}', " +
                              $"'{GetLorum(1).Substring(0, 70)}', " +
                              $"'{GetLorum(1).Substring(0, 80)}', " +
                              $"'{DateTime.Now}', " +
                              $"'{DateTime.Now}');";

                        motSqlServer.ExecuteNonQuery(sql);

                        sql = $"INSERT INTO dbo.vRx VALUES(" +
                              $"'{patId}', " +                                                          // Patient_ID
                              $"'{scripId}', " +                                                        // Rx_ID
                              $"'99999', " +                                                            // External_Rx_ID
                              $"'{docId}', " +                                                          // Prescriber_ID
                              $"'{FullTrim(GetLorum(1).Substring(0,7)).ToUpper()}', " +                 // Dosage_Signa_Code
                              $"'{GetLorum(20).Substring(0,255)}', " +                                  // Decoded_Dosage_Signa
                              $"'{GetLorum(2).Substring(0,80)}', " +                                    // Signa_String
                              $"'{GetLorum(20).Substring(0, 255)}', " +                                 // Instruction_Signa_Text
                              $"'{DateTime.Now.ToString()}', " +                                        // Date_Written
                              $"'{DateTime.Now.ToString()}', " +                                        // Dispense_Date
                              $"'{DateTime.Now.ToString()}', " +                                        // Last_Dispense_Stop_Date
                              $"'{IRand()}', " +                                                        // Total_Refiles_Authorized
                              $"'{IRand()}', " +                                                        // Total_Refills_Used
                              $"'{IRand()}', " +                                                        // Dispensed_Item_ID
                              $"'32', " +                                                               // Dispensed_Item_Version
                              $"'{GetLorum(2).Substring(0, 11)}', " +                                   // NDC_Code
                              $"'{DRand()}', " +                                                        // Quantity_Dispensed
                              $"'{IRand()}', " +                                                        // Writen_For_Item_ID
                              $"'12', " +                                                               // Written_For_Item_Version
                              $"'1', " +                                                                // Script_Status
                              $"'{DateTime.Now.ToString()}', " +                                        // Prescription_Expiration_Date
                              $"'{docId}', " +                                                          // Responsible_Prescriber_ID
                              $"'{DateTime.Now.ToString()}', " +                                        // Discontinue_Date
                              $"'{DRand()}', " +                                                        // Quantity_Written
                              $"'{DRand()}', " +                                                        // Total_Qty_Used
                              $"'{DRand()}', " +                                                        // Total_Qty_Authorized
                              $"'30', " +                                                               // Days_Supply_Written
                              $"'20', " +                                                               // Days_Supply_Remaining
                              $"'{GetLorum(1).Substring(0,3).ToUpper()}', " +                           // Script_Origin_Indicater
                              $"DEFAULT);";                                                             // MSSQLTS

                        motSqlServer.ExecuteNonQuery(sql);

                        // Rx Note
                        sql = $"INSERT INTO dbo.vRxNote VALUES(" +
                              $"'{scripId}', " +
                              $"'{IRand()}', " +
                              $"'{FullTrim(GetLorum(1).Substring(0, 10)).ToUpper()}', " +
                              $"'{FullTrim(GetLorum(1).Substring(0, 30)).ToUpper()}', " +
                              $"'{DateTime.Now}', " +
                              $"'{GetLorum(20)}');";

                        motSqlServer.ExecuteNonQuery(sql);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }
        }

        [TestMethod]
        public void CreateObjects()
        {
            try
            {
                if (!CreatedRecords)
                {
                    CreateSqlRecords();
                }

                var mutex = new Mutex();
                var gatewayIp = targetIp;
                var gatewayPort = targetPort;
                var dataSource = "PROXYPLAYGROUND";

                if (GetPlatformOs.Go() != PlatformOs.Windows)
                {
                    dataSource = gatewayIp;
                }

                using (var motSqlServer = new MotSqlServer($"Data Source={dataSource};Initial Catalog=McKessonTestDb;User ID=sa;Password=$MOT2018"))
                {
                    var patient = new PollPatient(motSqlServer, mutex, gatewayIp, gatewayPort);
                    var doc = new PollPrescriber(motSqlServer, mutex, gatewayIp, gatewayPort);
                    var facility = new PollFacility(motSqlServer, mutex, gatewayIp, gatewayPort);
                    var scrip = new PollPrescription(motSqlServer, mutex, gatewayIp, gatewayPort);
                    var drug = new PollDrug(motSqlServer, mutex, gatewayIp, gatewayPort);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void QueryPatient()
        {
            try
            {
                if (!CreatedRecords)
                {
                   CreateSqlRecords();
                }

                var mutex = new Mutex();
                var gatewayIp = targetIp;
                var gatewayPort = targetPort;
                var dataSource = "PROXYPLAYGROUND";

                if (GetPlatformOs.Go() != PlatformOs.Windows)
                {
                    dataSource = gatewayIp;
                }

                using (var motSqlServer = new MotSqlServer($"Data Source={dataSource};Initial Catalog=McKessonTestDb;User ID=sa;Password=$MOT2018"))
                {
                    using (var patient = new PollPatient(motSqlServer, mutex, gatewayIp, gatewayPort))
                    {
                        try
                        {
                            patient.ReadPatientRecords();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void QueryRx()
        {
            try
            {
                if (!CreatedRecords)
                {
                    CreateSqlRecords();
                }

                var mutex = new Mutex();
                var gatewayIp = targetIp;
                var gatewayPort = targetPort;
                var dataSource = "PROXYPLAYGROUND";

                if (GetPlatformOs.Go() != PlatformOs.Windows)
                {
                    dataSource = gatewayIp;
                }

                using (var motSqlServer = new MotSqlServer($"Data Source={dataSource};Initial Catalog=McKessonTestDb;User ID=sa;Password=$MOT2018"))
                {
                    using (var Rx = new PollPrescription(motSqlServer, mutex, gatewayIp, gatewayPort))
                    {
                        try
                        {
                            Rx.ReadPrescriptionRecords();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void QueryFacility()
        {
            try
            {
                if (!CreatedRecords)
                {
                    CreateSqlRecords();
                }

                var mutex = new Mutex();
                var gatewayIp = targetIp;
                var gatewayPort = targetPort;
                var dataSource = "PROXYPLAYGROUND";

                if (GetPlatformOs.Go() != PlatformOs.Windows)
                {
                    dataSource = gatewayIp;
                }

                using (var motSqlServer = new MotSqlServer($"Data Source={dataSource};Initial Catalog=McKessonTestDb;User ID=sa;Password=$MOT2018"))
                {
                    using (var facility = new PollFacility(motSqlServer, mutex, gatewayIp, gatewayPort))
                    {
                        try
                        {
                            facility.ReadFacilityRecords();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void QueryDoc()
        {
            try
            {
                if (!CreatedRecords)
                {
                    CreateSqlRecords();
                }

                var mutex = new Mutex();
                var gatewayIp = targetIp;
                var gatewayPort = targetPort;
                var dataSource = "PROXYPLAYGROUND";

                if (GetPlatformOs.Go() != PlatformOs.Windows)
                {
                    dataSource = gatewayIp;
                }

                using (var motSqlServer = new MotSqlServer($"Data Source={dataSource};Initial Catalog=McKessonTestDb;User ID=sa;Password=$MOT2018"))
                {
                    using (var doc = new PollPrescriber(motSqlServer, mutex, gatewayIp, gatewayPort))
                    {
                        try
                        {
                            doc.ReadPrescriberRecords();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void QueryDrug()
        {
            try
            {
                if (!CreatedRecords)
                {
                    CreateSqlRecords();
                }

                var mutex = new Mutex();
                var gatewayIp = targetIp;
                var gatewayPort = targetPort;
                var dataSource = "PROXYPLAYGROUND";

                if (GetPlatformOs.Go() != PlatformOs.Windows)
                {
                    dataSource = gatewayIp;
                }

                using (var motSqlServer = new MotSqlServer($"Data Source={dataSource};Initial Catalog=McKessonTestDb;User ID=sa;Password=$MOT2018"))
                {
                    using (var drug = new PollDrug(motSqlServer, mutex, gatewayIp, gatewayPort))
                    {
                        try
                        {
                            drug.ReadDrugRecords();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void StartPharmaserve()
        {
            var mutex = new Mutex();
            var gatewayIp = targetIp;
            var gatewayPort = targetPort;
            var dataSource = "PROXYPLAYGROUND";

            if (GetPlatformOs.Go() != PlatformOs.Windows)
            {
                dataSource = gatewayIp;
            }

            using (var motSqlServer = new MotSqlServer($"Data Source={dataSource};Initial Catalog=McKessonTestDb;User ID=sa;Password=$MOT2018"))
            {
                using (var ps = new Pharmaserve(motSqlServer, gatewayIp, gatewayPort))
                {
                    ps.RefreshRate = 1000;
                    ps.Go();
                    Thread.Sleep(10000);
                    ps.Stop();
                }
            }
        }
    }
}