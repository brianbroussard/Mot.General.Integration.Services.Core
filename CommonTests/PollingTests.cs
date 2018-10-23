using System;
using System.Data;
using System.Globalization;
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

        // Set char type
        bool useAscii = true;

        // Various ways of accessing the SQL Server instance
        string sqlServerName = "PROXYPLAYGROUND";
        string sqlServerIp = "192.168.1.160";
        int sqlServerPort = 1433;

        string dataSource;

        public void SetupSqlServer()
        {
            if (GetPlatformOs.Current != PlatformOs.Windows)
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
                using (var motSqlServer =
                    new MotSqlServer(
                        $"Data Source={dataSource};Initial Catalog=McKessonTestDb;User ID=sa;Password=$MOT2018"))
                {
                    for (var i = 0; i < 16; i++)
                    {
                        CreatedRecords = true;

                        var docId = RandomData.Integer();
                        var patId = RandomData.Integer();
                        var facId = RandomData.TrimString(2).ToUpper();
                        var storeId = "PHARMASERVE1";
                        var scripId = RandomData.Integer();
                        var drugId = RandomData.Integer();

                        // Prescriber
                        var sql = $"INSERT INTO dbo.vPrescriber VALUES(" +
                                  $"'{docId}', " + // Prescriber_ID
                                  $"'{RandomData.TrimString(25).ToUpper()}', " +   // Last_Name
                                  $"'{RandomData.TrimString(15).ToUpper()}', " +   // First_Name
                                  $"'{RandomData.TrimString(1).ToUpper()}', " +    // Middle_Initial
                                  $"'{RandomData.TrimString(25).ToUpper()}', " +   // Address_Line_1
                                  $"'{RandomData.TrimString(25).ToUpper()}', " +   // Address_Line_2
                                  $"'{RandomData.TrimString(20).ToUpper()}', " +   // City
                                  $"'MA', " +                                      // State_Code
                                  $"'0{RandomData.Integer(1000, 10000)}', " +      // Zip_Code
                                  $"'{RandomData.Integer(1000, 10000)}', " +       // Zip_Plus_4
                                  $"'{RandomData.Integer(100, 1000)}', " +         // Area_Code
                                  $"'{RandomData.Integer(1000000, 10000000)}', " + // Telephone_Number
                                  $"'{RandomData.Integer(1, 100)}', " +            // Exchange
                                  $"'{RandomData.TrimString(2).ToUpper()}{RandomData.Integer(1000000, 10000000)}', " + // DEA_Number
                                  $"'{RandomData.Integer(100000, 1000000)}', " + // DEA_Suffix
                                  $"'{RandomData.TrimString(4).ToUpper()}', " + // Prescriber_Type//
                                  $"'{RandomData.Bit()}', " + // Active_Flag
                                  $"DEFAULT);"; // MSSQLTS

                        motSqlServer.ExecuteNonQuery(sql);

                        // Prescriber Note
                        sql = $"INSERT INTO dbo.vPrescriberNote VALUES(" +
                              $"'{docId}', " +
                              $"'{RandomData.Integer()}', " +
                              $"'{RandomData.TrimString(10).ToUpper()}', " +
                              $"'{RandomData.TrimString(30).ToUpper()}', " +
                              $"'{DateTime.Now}', " +
                              $"'{RandomData.String()}');";

                        motSqlServer.ExecuteNonQuery(sql);


                        sql = $"INSERT INTO dbo.vMOTLocation VALUES(" +
                              $"'{facId}', " +
                              $"'{storeId}', " +
                              $"'{RandomData.TrimString(64).ToUpper()}', " +
                              $"'{RandomData.TrimString(40).ToUpper()}', " +
                              $"'{RandomData.TrimString(40).ToUpper()}', " +
                              $"'{RandomData.TrimString(25).ToUpper()}', " +
                              $"'NH', " +
                              $"'0{RandomData.Integer(1000, 10000)}', " +
                              $"'{RandomData.USPhoneNumber()}', " +
                              $"DEFAULT);";

                        motSqlServer.ExecuteNonQuery(sql);

                        var ndc = RandomData.TrimString(11).ToUpper();

                        // Drug
                        sql = $"INSERT INTO dbo.vItem " +
                              $"VALUES('{drugId}', " + //[ITEM_ID]
                              $"{RandomData.Integer(1, short.MaxValue)}, " + //[ITEM_VERSION]
                              $"'{ndc}', " + //[NDC_CODE]
                              $"'{RandomData.TrimString(2).ToUpper()}', " + //[PACKAGE_CODE]
                              $"'{RandomData.Double(100)}', " + //[PACKAGE_SIZE]
                              $"{RandomData.Integer(1, 10)}, " + //[CURRENT_ITEM_VERSION]
                              $"'{RandomData.TrimString(3).ToUpper()}', " + //[ITEM_TYPE]
                              $"'{RandomData.TrimString(40)}', " + //[ITEM_NAME]
                              $"'{RandomData.Integer()}', " + //[KDC_NUMBER]
                              $"'{RandomData.Integer(1, byte.MaxValue)}', " + //[GPI_GROUP_CODE]
                              $"'{RandomData.Integer(1, byte.MaxValue)}', " + //[GPI_CLASS_CODE]
                              $"'{RandomData.Integer(1, byte.MaxValue)}', " + //[GPI_SUBCLASS_CODE]
                              $"'{RandomData.Integer(1, byte.MaxValue)}', " + //[GPI_NAME_CODE]
                              $"'{RandomData.Integer(1, byte.MaxValue)}', " + //[GPI_NAME_EXTENSION_CODE]
                              $"'{RandomData.Integer(1, byte.MaxValue)}', " + //[GPI_DOSAGE_FORM_CODE]
                              $"'{RandomData.Integer(1, byte.MaxValue)}', " + //[GPI_STRENGTH_CODE]
                              $"'{RandomData.Integer()}', " + //[HRI_NUMBER]
                              $"'{RandomData.TrimString(7).ToUpper()}', " + //[DOSAGE_SIGNA_CODE]
                              $"'{RandomData.TrimString(80).ToUpper()}', " + //[INSTRUCTION_SIGNA_STRING]
                              $"'{RandomData.TrimString(4).ToUpper()}', " + //[FORM_TYPE]
                              $"'{RandomData.TrimString(3).ToUpper()}', " + //[ROUTE_OF_ADMINISTRATION]
                              $"'{RandomData.Integer()}', " + //[ALTERNATE_MANUFACTURER_ID]
                              $"'{RandomData.TrimString(13).ToUpper()}', " + //[UPC]
                              $"'{RandomData.Double(10).ToString(CultureInfo.InvariantCulture).Substring(0, 15)}', " + //[STRENGTH]
                              $"'{RandomData.TrimString(4).ToUpper()}', " + //[COLOR_CODE]
                              $"'{RandomData.TrimString(4).ToUpper()}', " + //[FLAVOR_CODE]
                              $"'{RandomData.TrimString(4).ToUpper()}', " + //[SHAPE_CODE]
                              $"'{RandomData.TrimString(10).ToUpper()}', " + //[PRODUCT_MARKING]
                              $"'{RandomData.Integer(1, 8)}', " + //[NARCOTIC_CODE]
                              $"'{RandomData.Double(100)}', " + //[UNIT_SIZE]
                              $"'{RandomData.TrimString(2).ToUpper()}', " + //[UNIT_OF_MEASURE]
                              $"'{RandomData.TrimString(5).ToUpper()}', " + //[NDC_Manufacturer_Number]
                              $"'{RandomData.TrimString(10).ToUpper()}', " + //[Manufacturer_Abbreviation]
                              $"DEFAULT);"; //[MSSQLTS]                                                                                             

                        motSqlServer.ExecuteNonQuery(sql);

                        // Drug Caution
                        sql = $"INSERT INTO dbo.vItemCaution VALUES(" +
                              $"'{RandomData.Integer()}', " +
                              $"'{RandomData.Integer(1, 10000)}', " +
                              $"'{RandomData.TrimString(255)}');";

                        motSqlServer.ExecuteNonQuery(sql);

                        var mf = new[] {"M", "F"};

                        // Patient
                        sql = $"INSERT INTO dbo.vPatient VALUES(" +
                              $"'{patId}', " + // Patient_ID
                              $"'{RandomData.TrimString(25).ToUpper()}', " + // Last_Name
                              $"'{RandomData.TrimString(15).ToUpper()}', " + // First_Name
                              $"'{RandomData.TrimString(1).ToUpper()}', " + // Middle_Initial
                              $"'{RandomData.TrimString(25).ToUpper()}', " + // Address_Line_1   
                              $"'{RandomData.TrimString(25).ToUpper()}', " + // Address_Line_2
                              $"'{RandomData.TrimString(20).ToUpper()}', " + // City
                              $"'NH', " + // State_Code
                              $"'0{RandomData.Integer(1000, 10000)}', " + // Zip_Code
                              $"'{RandomData.Integer(1000, 10000)}', " + // Zip_Plus_4
                              $"'{facId}', " + // Patient_Location_Code
                              $"'{docId}', " + // Primary_Prescriber_ID
                              $"'{RandomData.Integer(100000000, 1000000000)}', " + // SSN
                              $"'{DateTime.Now.ToString(CultureInfo.InvariantCulture)}', " + // BirthDate
                              $"'{DateTime.Now.ToString(CultureInfo.InvariantCulture)}', " + // Deceased_Date
                              $"'{mf[RandomData.Bit()]}', " + // Sex
                              $"DEFAULT, " + // MSSQLTS
                              $"'{RandomData.Integer(100, 1000)}', " + // Area_Code
                              $"'{RandomData.Integer(1000000, 10000000)}', " + // Telephone_Number
                              $"'{RandomData.Integer(1, 100)}');"; // Extension

                        motSqlServer.ExecuteNonQuery(sql);

                        // Patient Note
                        sql = $"INSERT INTO dbo.vPatientNote VALUES(" +
                              $"'{patId}', " +
                              $"'{RandomData.Integer()}', " +
                              $"'{RandomData.TrimString(10).ToUpper()}', " +
                              $"'{RandomData.TrimString(30).ToUpper()}', " +
                              $"'{DateTime.Now}', " +
                              $"'{RandomData.String()}');";

                        motSqlServer.ExecuteNonQuery(sql);

                        // Patient Allergy
                        sql = $"INSERT INTO dbo.vPatientAllergy VALUES(" +
                              $"'{patId}', " +
                              $"'{RandomData.Integer()}', " +
                              $"'{RandomData.TrimString(3).ToUpper()}', " +
                              $"'{RandomData.TrimString(80)}', " +
                              $"'{RandomData.TrimString(70)}', " +
                              $"'{RandomData.Integer()}', " +
                              $"'{DateTime.Now}');";

                        motSqlServer.ExecuteNonQuery(sql);

                        // Patient Diagnosis
                        sql = $"INSERT INTO dbo.vPatientDiagnosis VALUES(" +
                              $"'{patId}', " +
                              $"'{RandomData.TrimString(70)}', " +
                              $"'{RandomData.TrimString(80)}', " +
                              $"'{DateTime.Now}', " +
                              $"'{DateTime.Now}');";

                        motSqlServer.ExecuteNonQuery(sql);

                        var refills = RandomData.Integer(1, 100);
                        var refillsRemaining = RandomData.Integer(1, refills);
                        var daysSupply = RandomData.Integer(1, 366);
                        var daysRemaining = RandomData.Integer(1, daysSupply);

                        /*
                                                sql = $"INSERT INTO dbo.vRx VALUES(" +
                                                      $"'{patId}', " + // Patient_ID
                                                      $"'{scripId}', " + // Rx_ID
                                                      $"{RandomData.Integer()}, " + // External_Rx_ID
                                                      $"'{docId}', " + // Prescriber_ID
                                                      $"'{RandomData.TrimString(7).ToUpper()}', " + // Dosage_Signa_Code
                                                      $"'{RandomData.TrimString(255)}', " + // Decoded_Dosage_Signa
                                                      $"'{RandomData.TrimString(80)}', " + // Signa_String
                                                      $"'{RandomData.TrimString(255)}', " + // Instruction_Signa_Text
                                                      $"'{DateTime.Now.ToString(CultureInfo.InvariantCulture)}', " + // Date_Written
                                                      $"'{DateTime.Now.ToString(CultureInfo.InvariantCulture)}', " + // Dispense_Date
                                                      $"'{DateTime.Now.ToString(CultureInfo.InvariantCulture)}', " + // Last_Dispense_Stop_Date
                                                      $"'{refills}', " + // Total_Refiles_Authorized
                                                      $"'{refillsRemaining}', " + // Total_Refills_Used
                                                      $"'{RandomData.Integer()}', " + // Dispensed_Item_ID
                                                      $"'{RandomData.Integer(1, short.MaxValue)}', " + // Dispensed_Item_Version
                                                      $"'{ndc}', " + // NDC_Code
                                                      $"'{RandomData.Double(100)}', " + // Quantity_Dispensed
                                                      $"'{RandomData.Integer()}', " + // Writen_For_Item_ID
                                                      $"'{RandomData.Integer(1, short.MaxValue)}', " + // Written_For_Item_Version
                                                      $"'{RandomData.Bit()}', " + // Script_Status
                                                      $"'{DateTime.Now.ToString(CultureInfo.InvariantCulture)}', " + // Prescription_Expiration_Date
                                                      $"'{docId}', " + // Responsible_Prescriber_ID
                                                      $"'{DateTime.Now.ToString(CultureInfo.InvariantCulture)}', " + // Discontinue_Date
                                                      $"'{RandomData.Double(10)}', " + // Quantity_Written
                                                      $"'{RandomData.Double(10)}', " + // Total_Qty_Used
                                                      $"'{RandomData.Double(10)}', " + // Total_Qty_Authorized
                                                      $"'{daysSupply}', " + // Days_Supply_Written
                                                      $"'{daysRemaining}', " + // Days_Supply_Remaining
                                                      $"'{RandomData.TrimString(3).ToUpper()}', " + // Script_Origin_Indicater
                                                      $"DEFAULT);"; // MSSQLTS
                                                */

                        sql = $"INSERT INTO dbo.vRx VALUES(" +
                             $"'{patId}', " + // Patient_ID
                             $"'{scripId}', " + // Rx_ID
                             $"{RandomData.Integer()}, " + // External_Rx_ID
                             $"'{docId}', " + // Prescriber_ID
                             $"'{RandomData.TrimString(7).ToUpper()}', " + // Dosage_Signa_Code
                             $"'{RandomData.TrimString(255)}', " + // Decoded_Dosage_Signa
                             $"'{RandomData.TrimString(80)}', " + // Signa_String
                             $"'{RandomData.TrimString(255)}', " + // Instruction_Signa_Text
                             $"'{DateTime.Now.ToString(CultureInfo.InvariantCulture)}', " + // Date_Written
                             $"'{DateTime.Now.ToString(CultureInfo.InvariantCulture)}', " + // Dispense_Date
                             $"NULL, " + // Last_Dispense_Stop_Date
                             $"'{refills}', " + // Total_Refiles_Authorized
                             $"'{refillsRemaining}', " + // Total_Refills_Used
                             $"'{RandomData.Integer()}', " + // Dispensed_Item_ID
                             $"'{RandomData.Integer(1, short.MaxValue)}', " + // Dispensed_Item_Version
                             $"'{ndc}', " + // NDC_Code
                             $"'{RandomData.Double(100)}', " + // Quantity_Dispensed
                             $"'{RandomData.Integer()}', " + // Writen_For_Item_ID
                             $"'{RandomData.Integer(1, short.MaxValue)}', " + // Written_For_Item_Version
                             $"'{RandomData.Bit()}', " + // Script_Status
                             $"'{RandomData.Date(2019)}', " + // Prescription_Expiration_Date
                             $"'{docId}', " + // Responsible_Prescriber_ID
                             $"NULL, " + // Discontinue_Date
                             $"'{RandomData.Double(10)}', " + // Quantity_Written
                             $"'{RandomData.Double(10)}', " + // Total_Qty_Used
                             $"'{RandomData.Double(10)}', " + // Total_Qty_Authorized
                             $"'{daysSupply}', " + // Days_Supply_Written
                             $"'{daysRemaining}', " + // Days_Supply_Remaining
                             $"'{RandomData.TrimString(3).ToUpper()}', " + // Script_Origin_Indicater
                             $"DEFAULT);"; // MSSQLTS

                        motSqlServer.ExecuteNonQuery(sql);

                        // Rx Note
                        sql = $"INSERT INTO dbo.vRxNote VALUES(" +
                              $"'{scripId}', " +
                              $"'{RandomData.Integer()}', " +
                              $"'{RandomData.TrimString(10).ToUpper()}', " +
                              $"'{RandomData.TrimString(30).ToUpper()}', " +
                              $"'{DateTime.Now}', " +
                              $"'{RandomData.String(512)}');";

                        motSqlServer.ExecuteNonQuery(sql);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
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
                        patient.UseAscii = useAscii;

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
                        Rx.UseAscii = useAscii;

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
                        facility.UseAscii = useAscii;

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
                        doc.UseAscii = useAscii;

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
                        drug.UseAscii = useAscii;

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