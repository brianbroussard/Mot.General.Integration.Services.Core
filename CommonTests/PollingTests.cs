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

                        var docId = new Random((int)DateTime.Now.Ticks & 0x0000FFFF).Next().ToString();
                        var patId = new Random((int)DateTime.Now.Ticks & 0x0000FFFF).Next().ToString();
                        var facId = new Random((int)DateTime.Now.Ticks & 0x0000FFFF).Next().ToString();
                        var storeId = new Random((int)DateTime.Now.Ticks & 0x0000FFFF).Next().ToString();
                        var scripId = new Random((int)DateTime.Now.Ticks & 0x0000FFFF).Next().ToString();
                        var drugId = new Random((int)DateTime.Now.Ticks & 0x0000FFFF).Next().ToString();


                        var doc = new MotPrescriberRecord("Add")
                        {
                            AutoTruncate = true,
                            PrescriberID = docId,
                            LastName = GetLorum(1).Substring(0, 25),
                            FirstName = GetLorum(1).Substring(0, 15),
                            MiddleInitial = GetLorum(1).Substring(0, 1),
                            Address1 = GetLorum(2).Substring(0, 25),
                            Address2 = GetLorum(2).Substring(0, 25),
                            City = GetLorum(1).Substring(0, 20),
                            State = "VT",
                            Zipcode = "02660",
                            DEA_ID = $"{GetLorum(1).Substring(0, 2).ToUpper()}0123456"

                        };

                        var sql = $"INSERT INTO dbo.vPrescriber VALUES(" +
                                  $"'{docId}', " +
                                  $"'{doc.LastName}', " +
                                  $"'{doc.FirstName}', " +
                                  $"'{doc.MiddleInitial}', " +
                                  $"'{doc.Address1}', " +
                                  $"'{doc.Address2}', " +
                                  $"'{doc.City}', " +
                                  $"'{doc.State}', " +
                                  $"'{doc.Zipcode}', " +
                                  $"'1234', " +
                                  $"'617', " +
                                  $"'9696072', " +
                                  $"'00', " +
                                  $"'{doc.DEA_ID}', " +
                                  $"'123456', " +
                                  $"'{GetLorum(2).Substring(0, 4).ToUpper()}', " +
                                  $"'1', " +
                                  $"DEFAULT);";

                        motSqlServer.ExecuteNonQuery(sql);

                        sql = $"INSERT INTO dbo.vPatientNote VALUES(" +
                              $"'{patId}', " +
                              $"'{new Random((int)DateTime.Now.Ticks & 0x0000FFFF).Next()}', " +
                              $"'{doc.FirstName.Substring(0, 10)}', " +
                              $"'{doc.LastName}', " +
                              $"'{DateTime.Now}', " +
                              $"'{GetLorum(20)}');";

                        motSqlServer.ExecuteNonQuery(sql);

                        var fac = new MotFacilityRecord("Add")
                        {
                            AutoTruncate = true,
                            LocationID = facId,
                            LocationName = GetLorum(2).Substring(0,64),
                            Address1 = GetLorum(2).Substring(0,50),
                            Address2 = GetLorum(2).Substring(0,50),
                            City = GetLorum(1).Substring(0,25),
                            State = "MA",
                            Zipcode = "02660",
                            Phone = "6179696072"
                        };

                        sql = $"INSERT INTO dbo.vMOTLocation VALUES(" +
                                  $"'{facId}', " +
                                  $"'{storeId}', " +
                                  $"'{fac.LocationName}', " +
                                  $"'{fac.Address1}', " +
                                  $"'{fac.Address2}', " +
                                  $"'{fac.City}', " +
                                  $"'{fac.State}', " +
                                  $"'{doc.Zipcode}', " +
                                  $"'{fac.Phone}', " +
                                  $"DEFAULT);";

                        motSqlServer.ExecuteNonQuery(sql);

                        var drug = new MotDrugRecord("Add")
                        {
                            AutoTruncate = true,
                            DrugID = drugId,
                            TradeName = GetLorum(2),
                            GenericFor = GetLorum(2),
                        };

                        sql = $"INSERT INTO dbo.vItem (ITEM_ID, UNIT_OF_MEASURE, STRENGTH, NDC_CODE, PACKAGE_CODE, ITEM_TYPE, ITEM_NAME, INSTRUCTION_SIGNA_STRING, FORM_TYPE,  UNIT_SIZE, COLOR_CODE, FLAVOR_CODE, SHAPE_CODE, Manufacturer_Abbreviation, MSSQLTS) " +
                            $"VALUES('{drugId}'," +                                                   // ITEM_ID
                            $"'ZZ', " +                                                               // UNIT_OF_MEASURE
                            $"'{GetLorum(1).Substring(0,15)}', " +                                    // STRENGTH
                            $"'{GetLorum(1).Substring(0,11)}', " +                                    // NDC_CODE  
                            $"'{GetLorum(1).Substring(0,2).ToUpper()}', " +                           // PACKAGE_CODE
                            $"'{GetLorum(1).Substring(0,3).ToUpper()}', " +                           // ITEM_TYPE
                            $"'{GetLorum(2).Substring(0,40)}', " +                                    // ITEM_NAME
                            $"'{GetLorum(5).Substring(0,80)}', " +                                    // INSTRUCTION_SIGNA_STRING
                            $"'{GetLorum(1).Substring(0,4).ToUpper()}', " +                           // FORM_TYPE
                            $"'{new Random((int)DateTime.Now.Ticks & 0x0000FFFF).NextDouble()}', " +  // UNIT_SIZE
                            $"'{GetLorum(1).Substring(0,3).ToUpper().Trim()}', " +                    // COLOR_CODE
                            $"'{GetLorum(1).Substring(0,3).ToUpper().Trim()}', " +                    // FLAVOR_CODE
                            $"'{GetLorum(1).Substring(0,3).ToUpper().Trim()}', " +                    // SHAPE_CODE
                            $"'{GetLorum(2).Substring(0,10)}', " +                                    // Manufacturer_Abbreviation 
                            $"DEFAULT);";                                                             // MSSQLTS

                        motSqlServer.ExecuteNonQuery(sql);


                        var patient = new MotPatientRecord("Add")
                        {
                            AutoTruncate = true,
                            PatientID = patId,
                            PrimaryPrescriberID = docId,
                            LocationID = facId,
                            FirstName = GetLorum(2).Substring(0,15),
                            LastName = GetLorum(2).Substring(0,25),
                            MiddleInitial = GetLorum(1).Substring(0,1).ToUpper(),
                            Address1 = GetLorum(2).Substring(0,25),
                            Address2 = GetLorum(2).Substring(0,25),
                            City = GetLorum(1).Substring(0,20),
                            State = "NH",
                            Zipcode = "02660"
                        };

                        sql = $"INSERT INTO dbo.vPatient VALUES(" +
                                  $"'{patId}', " +                                  // Patient_ID
                                  $"'{patient.LastName}', " +                       // Last_Name
                                  $"'{patient.FirstName}', " +                      // First_Name
                                  $"'{patient.MiddleInitial}', " +                  // Middle_Initial
                                  $"'{patient.Address1}', " +                       // Address_Line_1   
                                  $"'{patient.Address2}', " +                       // Address_Line_2
                                  $"'{patient.City}', " +                           // City
                                  $"'{patient.State}', " +                          // State_Code
                                  $"'{patient.Zipcode}', " +                        // Zip_Code
                                  $"'1234', " +                                     // Zip_Plus_4
                                  $"'{GetLorum(1).Substring(0,1).ToUpper()}', " +   // Patient_Location_Code
                                  $"'{docId}', " +                                  // Primary_Prescriber_ID
                                  $"'{new Random((int)DateTime.Now.Ticks & 0x0000FFFF).Next().ToString()}', " +  // SSN
                                  $"'{DateTime.Now.ToString()}', " +                // BirthDate
                                  $"'{DateTime.Now.ToString()}', " +                // Deceased_Date
                                  $"'{GetLorum(1).Substring(0, 1).ToUpper()}', " +  // Sex
                                  $"DEFAULT, " +                                    // MSSQLTS
                                  $"'617', " +                                      // Area_Code
                                  $"'3324531', " +                                  // Telephone_Number
                                  $"'00');";                                        // Extension

                        motSqlServer.ExecuteNonQuery(sql);

                        var scrip = new MotPrescriptionRecord("Add")
                        {
                            RxSys_DocID = docId,
                            RxSys_PatID = patId,
                            DrugID = drugId,
                            RxStartDate = DateTime.Now,
                            RxStopDate = DateTime.Now.AddMonths(12),
                            DoseScheduleName = GetLorum(2)
                        };

                        sql = $"INSERT INTO dbo.vRx VALUES(" +
                              $"'{patId}', " +                                                          // Patient_ID
                              $"'{scripId}', " +                                                        // Rx_ID
                              $"'99999', " +                                                            // External_Rx_ID
                              $"'{docId}', " +                                                          // Prescriber_ID
                              $"'{GetLorum(1).Substring(0,7).ToUpper()}', " +                           // Dosage_Signa_Code
                              $"'{GetLorum(20).Substring(0,255)}', " +                                  // Decoded_Dosage_Signa
                              $"'{GetLorum(2).Substring(0,80)}', " +                                    // Signa_String
                              $"'{GetLorum(20).Substring(0, 255)}', " +                                 // Instruction_Signa_Text
                              $"'{DateTime.Now.ToString()}', " +                                        // Date_Written
                              $"'{DateTime.Now.ToString()}', " +                                        // Dispense_Date
                              $"'{DateTime.Now.ToString()}', " +                                        // Last_Dispense_Stop_Date
                              $"'{new Random((int)DateTime.Now.Ticks & 0x0000FFFF).Next()}', " +        // Total_Refiles_Authorized
                              $"'{new Random((int)DateTime.Now.Ticks & 0x0000FFFF).Next()}', " +        // Total_Refills_Used
                              $"'{new Random((int)DateTime.Now.Ticks & 0x0000FFFF).Next()}', " +        // Dispensed_Item_ID
                              $"'32', " +                                                               // Dispensed_Item_Version
                              $"'{GetLorum(2).Substring(0, 11)}', " +                                   // NDC_Code
                              $"'{new Random((int)DateTime.Now.Ticks & 0x0000FFFF).NextDouble()}', " +  // Quantity_Dispensed
                              $"'{new Random((int)DateTime.Now.Ticks & 0x0000FFFF).Next()}', " +        // Writen_For_Item_ID
                              $"'12', " +                                                               // Written_For_Item_Version
                              $"'1', " +                                                                // Script_Status
                              $"'{DateTime.Now.ToString()}', " +                                        // Prescription_Expiration_Date
                              $"'{docId}', " +                                                          // Responsible_Prescriber_ID
                              $"'{DateTime.Now.ToString()}', " +                                        // Discontinue_Date
                              $"'{new Random((int)DateTime.Now.Ticks & 0x0000FFFF).NextDouble()}', " +  // Quantity_Written
                              $"'{new Random((int)DateTime.Now.Ticks & 0x0000FFFF).NextDouble()}', " +  // Total_Qty_Used
                              $"'{new Random((int)DateTime.Now.Ticks & 0x0000FFFF).NextDouble()}', " +  // Total_Qty_Authorized
                              $"'30', " +                                                               // Days_Supply_Written
                              $"'20', " +                                                               // Days_Supply_Remaining
                              $"'{GetLorum(1).Substring(0,3).ToUpper()}', " +                           // Script_Origin_Indicater
                              $"DEFAULT);";                                                             // MSSQLTS

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