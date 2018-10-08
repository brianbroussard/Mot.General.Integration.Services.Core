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

        // Set to false to generate new sql records with a fresh timestamp
        bool CreatedRecords = false;

        // Various ways of accessing the SQL Server instance
        string sqlServerName = "PROXYPLAYGROUND";
        string sqlServerIp = "192.168.1.160";
        int sqlServerPort = 1433;

        string dataSource;

        public void SetupSqlServer()
        {
            if (GetPlatformOs.Current() != PlatformOs.Windows)
            {
                dataSource = sqlServerIp;
            }
            else
            {
                dataSource = sqlServerName;
            }
        }

        // ---- Real Work

        public void CreateSqlRecords()
        {
            SetupSqlServer();

            if (CreatedRecords == true)
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

                        var docId = RandomDataGenerator.Integer();
                        var patId = RandomDataGenerator.Integer();
                        var facId = RandomDataGenerator.TrimString(2).ToUpper();
                        var storeId = "PHARMASERVE1";
                        var scripId = RandomDataGenerator.Integer();
                        var drugId = RandomDataGenerator.Integer();

                        // Prescriber
                        var sql = $"INSERT INTO dbo.vPrescriber VALUES(" +
                                  $"'{docId}', " +                                                  // Prescriber_ID
                                  $"'{RandomDataGenerator.TrimString(25).ToUpper()}', " +        // Last_Name
                                  $"'{RandomDataGenerator.TrimString(15).ToUpper()}', " +        // First_Name
                                  $"'{RandomDataGenerator.TrimString(1).ToUpper()}', " +         // Middle_Initial
                                  $"'{RandomDataGenerator.TrimString(25).ToUpper()}', " +        // Address_Line_1
                                  $"'{RandomDataGenerator.TrimString(25).ToUpper()}', " +        // Address_Line_2
                                  $"'{RandomDataGenerator.TrimString(20).ToUpper()}', " +        // City
                                  $"'MA', " +                                                       // State_Code
                                  $"'02165', " +                                                    // Zip_Code
                                  $"'1234', " +                                                     // Zip_Plus_4
                                  $"'617', " +                                                      // Area_Code
                                  $"'9696072', " +                                                  // Telephone_Number
                                  $"'00', " +                                                       // Exchange
                                  $"'{RandomDataGenerator.String(2).ToUpper()}0123456', " +            // DEA_Number
                                  $"'123456', " +                                                   // DEA_Suffix
                                  $"'{RandomDataGenerator.TrimString(4).ToUpper()}', " +        // Prescriber_Type
                                  $"'1', " +                                                        // Active_Flag
                                  $"DEFAULT);";                                                     // MSSQLTS

                        motSqlServer.ExecuteNonQuery(sql);

                        // Prescriber Note
                        sql = $"INSERT INTO dbo.vPrescriberNote VALUES(" +
                              $"'{docId}', " +
                              $"'{RandomDataGenerator.Integer()}', " +
                              $"'{RandomDataGenerator.TrimString(10).ToUpper()}', " +
                              $"'{RandomDataGenerator.TrimString(30).ToUpper()}', " +
                              $"'{DateTime.Now}', " +
                              $"'{RandomDataGenerator.String()}');";

                        motSqlServer.ExecuteNonQuery(sql);


                        sql = $"INSERT INTO dbo.vMOTLocation VALUES(" +
                                  $"'{facId}', " +
                                  $"'{storeId}', " +
                                  $"'{RandomDataGenerator.TrimString(64).ToUpper()}', " +
                                  $"'{RandomDataGenerator.TrimString(40).ToUpper()}', " +
                                  $"'{RandomDataGenerator.TrimString(40).ToUpper()}', " +
                                  $"'{RandomDataGenerator.TrimString(25).ToUpper()}', " +
                                  $"'NH', " +
                                  $"'03049', " +
                                  $"'6034659622', " +
                                  $"DEFAULT);";

                        motSqlServer.ExecuteNonQuery(sql);

                        var ndc = RandomDataGenerator.TrimString(11).ToUpper();

                        // Drug
                        sql = $"INSERT INTO dbo.vItem " +
                             $"VALUES('{drugId}', " +                                       //[ITEM_ID]
                             $"'1', " +                                                     //[ITEM_VERSION]
                             $"'{ndc}', " +    //[NDC_CODE]
                             $"'{RandomDataGenerator.TrimString(2).ToUpper()}', " +     //[PACKAGE_CODE]
                             $"'{RandomDataGenerator.Double(100)}', " +                                          //[PACKAGE_SIZE]
                             $"'1', " +                                                     //[CURRENT_ITEM_VERSION]
                             $"'{RandomDataGenerator.TrimString(3).ToUpper()}', " +     //[ITEM_TYPE]
                             $"'{RandomDataGenerator.String(40)}', " +                        //[ITEM_NAME]
                             $"'{RandomDataGenerator.Integer()}', " +                                             //[KDC_NUMBER]
                             $"'10', " +                                                    //[GPI_GROUP_CODE]
                             $"'8', " +                                                     //[GPI_CLASS_CODE]
                             $"'22', " +                                                    //[GPI_SUBCLASS_CODE]
                             $"'18', " +                                                    //[GPI_NAME_CODE]
                             $"'92', " +                                                    //[GPI_NAME_EXTENSION_CODE]
                             $"'33', " +                                                    //[GPI_DOSAGE_FORM_CODE]
                             $"'102', " +                                                   //[GPI_STRENGTH_CODE]
                             $"'{RandomDataGenerator.Integer()}', " +                                             //[HRI_NUMBER]
                             $"'{RandomDataGenerator.TrimString(7).ToUpper()}', " +     //[DOSAGE_SIGNA_CODE]
                             $"'{RandomDataGenerator.String(80)}', " +                        //[INSTRUCTION_SIGNA_STRING]
                             $"'{RandomDataGenerator.TrimString(4).ToUpper()}', " +     //[FORM_TYPE]
                             $"'{RandomDataGenerator.TrimString(3).ToUpper()}', " +     //[ROUTE_OF_ADMINISTRATION]
                             $"'{RandomDataGenerator.Integer()}', " +                                             //[ALTERNATE_MANUFACTURER_ID]
                             $"'{RandomDataGenerator.TrimString(13).ToUpper()}', " +    //[UPC]
                             $"'{RandomDataGenerator.Double(10).ToString().Substring(0, 15)}', " +                //[STRENGTH]
                             $"'{RandomDataGenerator.TrimString(4).ToUpper()}', " +     //[COLOR_CODE]
                             $"'{RandomDataGenerator.TrimString(4).ToUpper()}', " +     //[FLAVOR_CODE]
                             $"'{RandomDataGenerator.TrimString(4).ToUpper()}', " +     //[SHAPE_CODE]
                             $"'{RandomDataGenerator.TrimString(10).ToUpper()}', " +    //[PRODUCT_MARKING]
                             $"'6', " +                                                     //[NARCOTIC_CODE]
                             $"'{RandomDataGenerator.Double(100)}', " +                                             //[UNIT_SIZE]
                             $"'{RandomDataGenerator.TrimString(2).ToUpper()}', " +     //[UNIT_OF_MEASURE]
                             $"'{RandomDataGenerator.TrimString(5).ToUpper()}', " +     //[NDC_Manufacturer_Number]
                             $"'{RandomDataGenerator.TrimString(10).ToUpper()}', " +    //[Manufacturer_Abbreviation]
                             $"DEFAULT);";                                                  //[MSSQLTS]                                                                                             

                        motSqlServer.ExecuteNonQuery(sql);

                        // Drug Caution
                        sql = $"INSERT INTO dbo.vItemCaution VALUES(" +
                              $"'{RandomDataGenerator.Integer()}', " +
                              $"'{RandomDataGenerator.TrimString(4).ToUpper()}', " +
                              $"'{RandomDataGenerator.String(255)}');";

                        motSqlServer.ExecuteNonQuery(sql);


                        // Patient
                        sql = $"INSERT INTO dbo.vPatient VALUES(" +
                                  $"'{patId}', " +                                                  // Patient_ID
                                  $"'{RandomDataGenerator.TrimString(25).ToUpper()}', " +        // Last_Name
                                  $"'{RandomDataGenerator.TrimString(15).ToUpper()}', " +        // First_Name
                                  $"'{RandomDataGenerator.TrimString(1).ToUpper()}', " +         // Middle_Initial
                                  $"'{RandomDataGenerator.TrimString(25).ToUpper()}', " +        // Address_Line_1   
                                  $"'{RandomDataGenerator.TrimString(25).ToUpper()}', " +        // Address_Line_2
                                  $"'{RandomDataGenerator.TrimString(20).ToUpper()}', " +        // City
                                  $"'NH', " +                                                       // State_Code
                                  $"'02165', " +                                                    // Zip_Code
                                  $"'1234', " +                                                     // Zip_Plus_4
                                  $"'{facId}', " +         // Patient_Location_Code
                                  $"'{docId}', " +                                                  // Primary_Prescriber_ID
                                  $"'{RandomDataGenerator.Integer()}', " +                                                // SSN
                                  $"'{DateTime.Now.ToString()}', " +                                // BirthDate
                                  $"'{DateTime.Now.ToString()}', " +                                // Deceased_Date
                                  $"'{RandomDataGenerator.String(1).ToUpper()}', " +                  // Sex
                                  $"DEFAULT, " +                                                    // MSSQLTS
                                  $"'617', " +                                                      // Area_Code
                                  $"'3324531', " +                                                  // Telephone_Number
                                  $"'00');";                                                        // Extension

                        motSqlServer.ExecuteNonQuery(sql);

                        // Patient Note
                        sql = $"INSERT INTO dbo.vPatientNote VALUES(" +
                              $"'{patId}', " +
                              $"'{RandomDataGenerator.Integer()}', " +
                              $"'{RandomDataGenerator.TrimString(10).ToUpper()}', " +
                              $"'{RandomDataGenerator.TrimString(30).ToUpper()}', " +
                              $"'{DateTime.Now}', " +
                              $"'{RandomDataGenerator.String()}');";

                        motSqlServer.ExecuteNonQuery(sql);

                        // Patient Allergy
                        sql = $"INSERT INTO dbo.vPatientAllergy VALUES(" +
                              $"'{patId}', " +
                              $"'{RandomDataGenerator.Integer()}', " +
                              $"'{RandomDataGenerator.TrimString(3).ToUpper()}', " +
                              $"'{RandomDataGenerator.String(80)}', " +
                              $"'{RandomDataGenerator.String(70)}', " +
                              $"'{RandomDataGenerator.Integer()}', " +
                              $"'{DateTime.Now}');";

                        motSqlServer.ExecuteNonQuery(sql);

                        // Patient Diagnosis
                        sql = $"INSERT INTO dbo.vPatientDiagnosis VALUES(" +
                              $"'{patId}', " +
                              $"'{RandomDataGenerator.String(70)}', " +
                              $"'{RandomDataGenerator.String(80)}', " +
                              $"'{DateTime.Now}', " +
                              $"'{DateTime.Now}');";

                        motSqlServer.ExecuteNonQuery(sql);

                        var refills = RandomDataGenerator.Integer();

                        sql = $"INSERT INTO dbo.vRx VALUES(" +
                              $"'{patId}', " +                                                          // Patient_ID
                              $"'{scripId}', " +                                                        // Rx_ID
                              $"'99999', " +                                                            // External_Rx_ID
                              $"'{docId}', " +                                                          // Prescriber_ID
                              $"'{RandomDataGenerator.TrimString(7).ToUpper()}', " +                 // Dosage_Signa_Code
                              $"'{RandomDataGenerator.String(255)}', " +                                  // Decoded_Dosage_Signa
                              $"'{RandomDataGenerator.String(80)}', " +                                    // Signa_String
                              $"'{RandomDataGenerator.String(255)}', " +                                 // Instruction_Signa_Text
                              $"'{DateTime.Now.ToString()}', " +                                        // Date_Written
                              $"'{DateTime.Now.ToString()}', " +                                        // Dispense_Date
                              $"'{DateTime.Now.ToString()}', " +                                        // Last_Dispense_Stop_Date
                              $"'{refills}', " +                                                        // Total_Refiles_Authorized
                              $"'{refills - 10}', " +                                                   // Total_Refills_Used
                              $"'{RandomDataGenerator.Integer()}', " +                                                        // Dispensed_Item_ID
                              $"'32', " +                                                               // Dispensed_Item_Version
                              $"'{ndc}', " +                                                            // NDC_Code
                              $"'{RandomDataGenerator.Double(100)}', " +                                                     // Quantity_Dispensed
                              $"'{RandomDataGenerator.Integer()}', " +                                                        // Writen_For_Item_ID
                              $"'12', " +                                                               // Written_For_Item_Version
                              $"'1', " +                                                                // Script_Status
                              $"'{DateTime.Now.ToString()}', " +                                        // Prescription_Expiration_Date
                              $"'{docId}', " +                                                          // Responsible_Prescriber_ID
                              $"'{DateTime.Now.ToString()}', " +                                        // Discontinue_Date
                              $"'{RandomDataGenerator.Double(10)}', " +                                                      // Quantity_Written
                              $"'{RandomDataGenerator.Double(10)}', " +                                                      // Total_Qty_Used
                              $"'{RandomDataGenerator.Double(10)}', " +                                                      // Total_Qty_Authorized
                              $"'30', " +                                                               // Days_Supply_Written
                              $"'20', " +                                                               // Days_Supply_Remaining
                              $"'{RandomDataGenerator.String(3).ToUpper()}', " +                           // Script_Origin_Indicater
                              $"DEFAULT);";                                                             // MSSQLTS

                        motSqlServer.ExecuteNonQuery(sql);

                        // Rx Note
                        sql = $"INSERT INTO dbo.vRxNote VALUES(" +
                              $"'{scripId}', " +
                              $"'{RandomDataGenerator.Integer()}', " +
                              $"'{RandomDataGenerator.TrimString(10).ToUpper()}', " +
                              $"'{RandomDataGenerator.TrimString(30).ToUpper()}', " +
                              $"'{DateTime.Now}', " +
                              $"'{RandomDataGenerator.String()}');";

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

                SetupSqlServer();

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

                SetupSqlServer();

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

                SetupSqlServer();

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

                SetupSqlServer();

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

                SetupSqlServer();

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

                SetupSqlServer();

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
        public void QueryObjects()
        {
            try
            {
                QueryDoc();
                QueryFacility();
                QueryPatient();
                QueryDrug();
                QueryRx();
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

            SetupSqlServer();

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