using System;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Mot.Common.Interface.Lib;
using System.Collections.Generic;
using System.Net.Sockets;
using Microsoft.Data.Sqlite;
using System.Threading;

namespace CommonTests
{
    [TestClass]
    public class DatabaseTest
    {
        private PlatformOs _platform;
        internal string _dbaUserName = "MzpkYmFFZGl0aW5nSW50ZXJmYWNlU2VydmljZXMxMjJXaXRoTGxhbWFGaWRzaA==";
        internal string _dbaPassword = "MTQ6cGM0MTBoNDI2czc2MTdFZGl0aW5nSW50ZXJmYWNlU2VydmljZXMxMjJXaXRoTGxhbWFGaWRzaA==";
        string GatewayAddress = "localhost";
        int GatewayPort = 24043;
        bool UseAscii = true;
        bool AutoTruncate = false;
        bool logRecords = false;
        int MaxLoops = 8;

        // Legacy DB handling - Legacy can't process more than a single store if using long ID's
        // so set the Store ID to one that exists in the database already, and set useLegacy to 'true'
        bool useLegacy = true;
        string StoreId = "1081";

        bool StartCleaning = false;


        #region Obfuscator
        public string EncodeString(string str)
        {
            byte[] b = Encoding.UTF8.GetBytes(str);
            return Convert.ToBase64String(b);
        }

        public string DecodeString(string str)
        {
            byte[] b = Convert.FromBase64String(str);
            return Encoding.UTF8.GetString(b);
        }
        #endregion
        #region CleanupDb
        private SqliteConnection TestDbConnection;
        private SqliteCommand TestDbCommand;

        public class TestRecord
        {
            public string Id { get; set; }
            public string RecordId { get; set; }
            public RecordType RecordType { get; set; }
            public string Name { get; set; }
            public DateTime TimeStamp { get; set; }
        }

        public void WriteTestLogRecord(TestRecord tr)
        {
            try
            {
                var sql = @"replace into map (Id, RecordId, RecordType, Name, TimeStamp) values (@Id, @RecordId, @RecordType, @Name, @TimeStamp);";

                using (var command = new SqliteCommand(sql, TestDbConnection))
                {
                    command.Parameters.AddWithValue("@Id", tr.Id.ToString());
                    command.Parameters.AddWithValue("@RecordId", tr.RecordId.ToString());
                    command.Parameters.AddWithValue("@RecordType", (int)tr.RecordType);
                    command.Parameters.AddWithValue("@Name", tr.Name);
                    command.Parameters.AddWithValue("@TimeStamp", DateTime.UtcNow.ToString());

                    var val = command.ExecuteNonQuery();
                }
            }
            catch
            {
                throw;
            }
        }

        public RecordType GetRecordTypeFromInt(int rt)
        {
            switch (rt)
            {
                case 0:
                    return RecordType.Store;
                case 1:
                    return RecordType.Prescriber;
                case 2:
                    return RecordType.Prescription;
                case 3:
                    return RecordType.Patient;
                case 4:
                    return RecordType.Facility;
                case 5:
                    return RecordType.DoseSchedule;
                case 6:
                    return RecordType.Drug;
                default:
                    break;
            }

            return RecordType.Unknown;
        }

        public List<TestRecord> GetTestLogRecords(RecordType recordType)
        {
            try
            {
                var trl = new List<TestRecord>();

                var sql = $"select * from map where RecordType = {(int)recordType};";
                using (var command = new SqliteCommand(sql, TestDbConnection))
                {
                    using (var rdr = command.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            if (rdr["Id"] != DBNull.Value)
                            {
                                trl.Add(new TestRecord()
                                {
                                    Id = rdr["Id"].ToString(),
                                    RecordId = rdr["RecordId"].ToString(),
                                    RecordType = GetRecordTypeFromInt(Convert.ToInt32(rdr["RecordType"])),
                                    Name = rdr["Name"].ToString(),
                                    TimeStamp = DateTime.Parse(rdr["TimeStamp"].ToString())
                                });
                            }
                        }
                    }
                }

                return trl;
            }
            catch
            {
                throw;
            }
        }

        void CreateDb()
        {
            try
            {
                var sql = "CREATE TABLE IF NOT EXISTS `map` (`Id` TEXT NOT NULL, `RecordId` TEXT NOT NULL, `RecordType` INTEGER, `Name` TEXT NOT NULL, `TimeStamp` TEXT NOT NULL, PRIMARY KEY(`Id`));";

                using (TestDbConnection = new SqliteConnection(@"Data Source=./db/testrun.db3"))
                {
                    TestDbConnection.Open();

                    using (var command = new SqliteCommand(sql, TestDbConnection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public void OpenTestLogDb()
        {
            try
            {
                if (!Directory.Exists("./db"))
                {
                    Directory.CreateDirectory("./db");
                }

                if (!File.Exists("./db/testrun.db3"))
                {
                    CreateDb();
                }

                TestDbConnection = new SqliteConnection(@"Data Source=./db/testrun.db3");
                TestDbConnection.Open();
            }
            catch
            {
                throw;
            }
        }

        public void CloseTestLogDb()
        {
            if (TestDbConnection != null)
            {
                TestDbConnection.Close();
            }
        }

        public void DestroyTestLogDb()
        {
            CloseTestLogDb();

            if (File.Exists("./db/testrun.db3"))
            {
                File.Delete("./db/testrun.db3");
            }
        }
        #endregion

        [TestMethod]
        public void GuidMatches()
        {
            try
            {
                var log = new List<KeyValuePair<string, Guid>>();

                using (var db = new MotGuidMapper())
                {
                    for (var i = 0; i < 1024; i++)
                    {
                        var guid = Guid.NewGuid();
                        var id = db.GetNext(guid);

                        log.Add(new KeyValuePair<string, Guid>(id, guid));
                    }
                }

                using (var db = new MotGuidMapper())
                {
                    foreach (var pair in log)
                    {
                        var id = db.GetId(pair.Value);
                        var guid = db.GetGuid(pair.Key);

                        if (id != pair.Key)
                        {
                            Assert.Fail($"id match failure {id}:{pair.Key}");
                        }

                        if (guid != pair.Value)
                        {
                            Assert.Fail($"guid match failure {guid}:{pair.Value}");
                        }
                    }
                }

                if (File.Exists("./db/map.db"))
                {
                    File.Delete("./db/map.db");
                }

                if (Directory.Exists("./db"))
                {
                    Directory.Delete("./db");
                }
            }
            catch (Exception ex)
            {
                Assert.Fail($"General failure: {ex.Message}");
            }
        }

        [TestMethod]
        public void SupervisorLogin()
        {
            try
            {
                if (GetPlatformOs.Current == PlatformOs.Windows)
                {
                    var _dbServer = new MotDatabaseServer<MotOdbcServer>($"dsn=MOT8;UID={DecodeString(_dbaUserName)};PWD={DecodeString(_dbaPassword)}");
                    using (var test = new DataSet())
                    {
                        var db = _dbServer.ExecuteQuery(@"SELECT * FROM SYS.SYSTABLE where SYS.SYSTABLE.table_name = 'SynMed2Send'");
                        if (db.Tables.Count > 0 && db.Tables[0].Rows.Count > 0)
                        {
                            var _supportsSynMed = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Assert.Fail($"Faild at SupervisorLogin with {ex.Message}");
            }
        }

        [TestMethod]
        public void NullConstructor()
        {
            var path = (GetPlatformOs.Current == PlatformOs.Windows) ? $@"C/motNext/Tests/Sqlite/Test.sqlite" : $@"~/Projects/Tests/Sqlite/Test.sqlite";

            try
            {
                var sqlite = new MotDatabaseServer<MotSqliteServer>(null);

                Assert.Fail("Allowed to pass null DSN");

                if (GetPlatformOs.Current == PlatformOs.Windows)
                {
                    var odbc = new MotDatabaseServer<MotOdbcServer>($"dsn=MOT8;UID={_dbaUserName};PWD={_dbaPassword}");
                    Assert.Fail("Allowed to pass null DSN");
                }

                // CleanUp
                File.Delete(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        [TestMethod]
        public void Construct()
        {
            var path = (GetPlatformOs.Current == PlatformOs.Windows) ? $@"/motNext/Tests/Sqlite/Test.sqlite" : $@"~/Projects/Tests/Sqlite/Test.sqlite";

            try
            {
                var sqlite = new MotDatabaseServer<MotSqliteServer>(path);

                if (GetPlatformOs.Current == PlatformOs.Windows)
                {
                    // This fails because MOT only has 32 bit drivers and the unit test doesn't seem to want to enable "prefer 32 bit"
                    var odbc = new MotDatabaseServer<MotOdbcServer>($"dsn=MOT8;UID={MotAccessSecurity.DecodeString(_dbaUserName)};PWD={MotAccessSecurity.DecodeString(_dbaPassword)}");
                }

                // CleanUp
                File.Delete(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void TestDecoder()
        {
            var original = "The quick brown fox jumped over the lazy dog";

            var encrypted = MotAccessSecurity.EncodeString(original);
            var decrypted = MotAccessSecurity.DecodeString(encrypted);

            if (decrypted != original)
            {
                Assert.Fail($"Decryption failure.  Expected {original} and got {decrypted}");
            }
        }

        public void CleanDatabase()
        {
            if (!StartCleaning)
            {
                Thread.Yield();

                while (!StartCleaning)
                {
                    Thread.Sleep(10000);
                }
            }

            try
            {
                OpenTestLogDb();

                using (var gateway = new TcpClient(GatewayAddress, GatewayPort))
                {
                    using (var stream = gateway.GetStream())
                    {
                        // Delete Rxs
                        var RxList = GetTestLogRecords(RecordType.Prescription);
                        foreach (var rx in RxList)
                        {
                            var RxToDelete = new MotPrescriptionRecord("Delete")
                            {
                                RxSys_RxNum = rx.Name
                            };

                            RxToDelete.Write(stream);
                        }

                        // Delete Drugs
                        var DrugList = GetTestLogRecords(RecordType.Drug);
                        foreach (var drug in DrugList)
                        {
                            var DrugToDelete = new MotDrugRecord("Delete")
                            {
                                DrugID = drug.RecordId.ToString()
                            };

                            DrugToDelete.Write(stream);
                        }

                        // Delete Patients
                        var PatientList = GetTestLogRecords(RecordType.Patient);
                        foreach (var patient in PatientList)
                        {
                            var PatientToDelete = new MotPatientRecord("Delete")
                            {
                                PatientID = patient.RecordId.ToString()
                            };

                            PatientToDelete.Write(stream);
                        }

                        // Delete Facilities
                        var FacilityList = GetTestLogRecords(RecordType.Facility);
                        foreach (var facility in FacilityList)
                        {
                            var FacilityToDelete = new MotFacilityRecord("Delete")
                            {
                                LocationID = facility.RecordId.ToString()
                            };

                            FacilityToDelete.Write(stream);
                        }

                        // Delete Prescribers
                        var PrescriberList = GetTestLogRecords(RecordType.Prescriber);
                        foreach (var prescriber in PrescriberList)
                        {
                            var PrescriberToDelete = new MotPrescriberRecord("Delete")
                            {
                                PrescriberID = prescriber.RecordId.ToString()
                            };

                            PrescriberToDelete.Write(stream);
                        }

                        // Delete Stores
                        if (!useLegacy)
                        {
                            var StoreList = GetTestLogRecords(RecordType.Store);
                            foreach (var store in StoreList)
                            {
                                var StoreToDelete = new MotStoreRecord("Delete")
                                {
                                    StoreID = store.RecordId.ToString()
                                };

                                StoreToDelete.Write(stream);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed to clean database: {ex.Message}");
            }
        }

        [TestMethod]
        public void BigRandomTestAndCleanup()
        {
            try
            {
                RandomDataCycle();
                CleanDatabase();
                DestroyTestLogDb();
            }
            catch(Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        public void RandomDataCycle()
        {
            try
            {
                string StoreId = "1081";

                OpenTestLogDb();

                using (var gateway = new TcpClient(GatewayAddress, GatewayPort))
                {
                    using (var stream = gateway.GetStream())
                    {
                        for (var s = 0; s < 3; s++)
                        {
                            if (!useLegacy)
                            {
                                var Store = new MotStoreRecord("Add")
                                {
                                    AutoTruncate = AutoTruncate,
                                    logRecords = true,
                                    _preferAscii = UseAscii,

                                    StoreID = Guid.NewGuid().ToString(),
                                    StoreName = $"{DateTime.Now.ToLongTimeString()}{RandomData.String()}",
                                    Address1 = RandomData.TrimString(),
                                    Address2 = RandomData.TrimString(),
                                    City = RandomData.TrimString(),
                                    State = "NH",
                                    Zipcode = $"{RandomData.Integer(0, 100000).ToString("D5")}-{RandomData.Integer(0, 100000).ToString("D4")}",
                                    DEANum = RandomData.ShortDEA(),
                                    Phone = RandomData.USPhoneNumber(),
                                    Fax = RandomData.USPhoneNumber()
                                };

                                Store.Write(stream);

                                WriteTestLogRecord(new TestRecord()
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    RecordId = new Guid(Store.StoreID).ToString(),
                                    RecordType = RecordType.Store,
                                    Name = Store.StoreName,
                                    TimeStamp = DateTime.UtcNow
                                });

                                StoreId = Store.StoreID;
                            }
                            
                            for (var i = 0; i < MaxLoops; i++)
                            {
                                var Facility = new MotFacilityRecord("Add")
                                {
                                    AutoTruncate = AutoTruncate,
                                    logRecords = true,
                                    _preferAscii = UseAscii,

                                    LocationID = Guid.NewGuid().ToString(),
                                    StoreID = StoreId,
                                    LocationName = RandomData.String(),
                                    Address1 = RandomData.TrimString(),
                                    Address2 = RandomData.TrimString(),
                                    City = RandomData.TrimString(),
                                    State = "NH",
                                    Zipcode = $"0{RandomData.Integer(1000, 10000)}",
                                    Phone = RandomData.USPhoneNumber(),
                                    CycleDays = RandomData.Integer(1, 32),
                                    CycleType = RandomData.Bit(),
                                    Comments = RandomData.String(2048)
                                };

                                Facility.Write(stream);

                                WriteTestLogRecord(new TestRecord()
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    RecordId = new Guid(Facility.LocationID).ToString(),
                                    RecordType = RecordType.Facility,
                                    Name = Facility.LocationName,
                                    TimeStamp = DateTime.UtcNow
                                });

                                var Prescriber = new MotPrescriberRecord("Add")
                                {
                                    AutoTruncate = AutoTruncate,
                                    logRecords = true,
                                    _preferAscii = UseAscii,

                                    RxSys_DocID = Guid.NewGuid().ToString(),
                                    LastName = RandomData.TrimString(),
                                    FirstName = RandomData.TrimString(),
                                    MiddleInitial = RandomData.TrimString(1),
                                    Address1 = RandomData.TrimString(),
                                    Address2 = RandomData.TrimString(),
                                    City = RandomData.TrimString(),
                                    State = "NH",
                                    Zipcode = $"0{RandomData.Integer(1000, 10000)}",
                                    DEA_ID = RandomData.ShortDEA(),
                                    TPID = RandomData.Integer(100000).ToString(),
                                    Phone = RandomData.USPhoneNumber(),
                                    Comments = RandomData.String(2048),
                                    Fax = RandomData.USPhoneNumber()
                                };

                                Prescriber.Write(stream);

                                WriteTestLogRecord(new TestRecord()
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    RecordId = new Guid(Prescriber.PrescriberID).ToString(),
                                    RecordType = RecordType.Prescriber,
                                    Name = $"{Prescriber.LastName}, {Prescriber.FirstName}, {Prescriber.MiddleInitial}",
                                    TimeStamp = DateTime.UtcNow
                                });

                                for (var f = 0; f < MaxLoops; f++)
                                {

                                    var rxId = Guid.NewGuid().ToString();
                                    var drugId = Guid.NewGuid().ToString();

                                    var Patient = new MotPatientRecord("Add")
                                    {
                                        AutoTruncate = AutoTruncate,
                                        logRecords = true,
                                        _preferAscii = UseAscii,

                                        PatientID = Guid.NewGuid().ToString(),
                                        LocationID = Facility.LocationID,
                                        PrimaryPrescriberID = Prescriber.PrescriberID,
                                        LastName = RandomData.TrimString(),
                                        FirstName = RandomData.TrimString(),
                                        MiddleInitial = RandomData.TrimString(1),
                                        Address1 = RandomData.TrimString(),
                                        Address2 = RandomData.TrimString(),
                                        City = RandomData.TrimString(),
                                        State = "NH",
                                        Zipcode = $"0{RandomData.Integer(1000, 10000)}",
                                        Gender = RandomData.TrimString(1),
                                        CycleDate = RandomData.Date(DateTime.Now.Year),
                                        CycleDays = RandomData.Integer(0, 32),
                                        CycleType = RandomData.Bit(),
                                        AdmitDate = RandomData.Date(),
                                        ChartOnly = RandomData.Bit().ToString(),
                                        Phone1 = RandomData.USPhoneNumber(),
                                        Phone2 = RandomData.USPhoneNumber(),
                                        WorkPhone = RandomData.USPhoneNumber(),
                                        DOB = RandomData.Date(),
                                        SSN = RandomData.SSN(),
                                        Allergies = RandomData.String(1024),
                                        Diet = RandomData.String(1024),
                                        DxNotes = RandomData.String(1024),
                                        InsName = RandomData.String(),
                                        InsPNo = RandomData.Integer().ToString(),
                                        AltInsName = RandomData.String(),
                                        AltInsPNo = RandomData.Integer().ToString()
                                    };

                                    Patient.Write(stream);

                                    WriteTestLogRecord(new TestRecord()
                                    {
                                        Id = Guid.NewGuid().ToString(),
                                        RecordId = new Guid(Patient.PatientID).ToString(),
                                        RecordType = RecordType.Patient,
                                        Name = $"{Patient.LastName}, {Patient.FirstName}, {Patient.MiddleInitial}",
                                        TimeStamp = DateTime.UtcNow
                                    });

                                    for (var rx = 0; rx < 8; rx++)
                                    {
                                        var Drug = new MotDrugRecord("Add")
                                        {
                                            AutoTruncate = AutoTruncate,
                                            logRecords = true,
                                            _preferAscii = UseAscii,

                                            DrugID = Guid.NewGuid().ToString(),
                                            DrugName = RandomData.TrimString(),
                                            NDCNum = RandomData.TrimString().ToUpper(),
                                            ProductCode = RandomData.TrimString().ToUpper(),
                                            LabelCode = RandomData.TrimString().ToUpper(),
                                            TradeName = RandomData.TrimString(),
                                            DrugSchedule = RandomData.Integer(2, 8),
                                            Strength = RandomData.Double(100),
                                            Route = RandomData.TrimString(),
                                            RxOTC = RandomData.Bit() == 1 ? "R" : "O",
                                            VisualDescription = $"{RandomData.TrimString(3)}/{RandomData.TrimString(3)}/{RandomData.TrimString(3)}",
                                            DoseForm = RandomData.TrimString(),
                                            DefaultIsolate = RandomData.Bit(),
                                            ShortName = RandomData.TrimString(),
                                            ConsultMsg = RandomData.String()
                                        };

                                        WriteTestLogRecord(new TestRecord()
                                        {
                                            Id = Guid.NewGuid().ToString(),
                                            RecordId = new Guid(Drug.DrugID).ToString(),
                                            RecordType = RecordType.Drug,
                                            Name = $"{Drug.TradeName}",
                                            TimeStamp = DateTime.UtcNow
                                        });

                                        Drug.Write(stream);

                                        var Rx = new MotPrescriptionRecord("Add")
                                        {
                                            AutoTruncate = AutoTruncate,
                                            logRecords = true,
                                            _preferAscii = UseAscii,

                                            PatientID = Patient.PatientID,
                                            PrescriberID = Prescriber.PrescriberID,
                                            DrugID = Drug.DrugID,
                                            RxSys_RxNum = RandomData.Integer(1, 1000000000).ToString(),
                                            RxStartDate = RandomData.Date(DateTime.Now.Year),
                                            RxStopDate = RandomData.Date(DateTime.Now.Year),
                                            DoseScheduleName = RandomData.TrimString().ToUpper(),
                                            QtyPerDose = RandomData.Double(10),
                                            QtyDispensed = RandomData.Integer(1, 120),
                                            RxType = RandomData.Integer(1, 21),
                                            DoseTimesQtys = RandomData.DoseTimes(RandomData.Integer(1, 25)),
                                            Isolate = RandomData.Bit().ToString(),
                                            Refills = RandomData.Integer(1, 100),
                                            Sig = RandomData.String()
                                        };

                                        WriteTestLogRecord(new TestRecord()
                                        {
                                            Id = Guid.NewGuid().ToString(),
                                            RecordId = Guid.NewGuid().ToString(),
                                            RecordType = RecordType.Prescription,
                                            Name = $"{Rx.RxSys_RxNum}",
                                            TimeStamp = DateTime.UtcNow
                                        });

                                        Rx.Write(stream);
                                    }
                                }
                            }
                        }
                    }
                }

                CloseTestLogDb();
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
            finally
            {
                StartCleaning = true;
            }
        }

        [TestMethod]
        public void ForceIdOverflowWithGuid()
        {
            // This will force a record rejection bu issuing a Guid as an ID
            // Note that the motNext gatewaty returns a 6 (success) even though it fails

            var patientId1 = Guid.NewGuid().ToString();
            var patientId2 = Guid.NewGuid().ToString();
            var prescriberId = Guid.NewGuid().ToString();
            var facilityId = Guid.NewGuid().ToString();

            using (var localTcpClient = new TcpClient("localhost", 24042))
            {
                using (var stream = localTcpClient.GetStream())
                {
                    try
                    {
                        var facility = new MotFacilityRecord("Add");
                        facility.LocationName = "Banzai Institute";
                        facility.LocationID = facilityId;
                        facility.logRecords = logRecords;
                        facility.Write(stream);

                        var doc = new MotPrescriberRecord("Add")
                        {
                            PrescriberID = prescriberId,
                            DEA_ID = "AD1234567",
                            LastName = "Lizardo",
                            FirstName = "Emillio"
                        };

                        doc.Write(stream);

                        using (var AddBuckaroo = new MotPatientRecord("Add"))
                        {
                            AddBuckaroo.PatientID = patientId1;
                            AddBuckaroo.FirstName = "Buckaroo";
                            AddBuckaroo.LastName = "Banzai";
                            AddBuckaroo.PrimaryPrescriberID = prescriberId;
                            AddBuckaroo.DOB = DateTime.Now;
                            AddBuckaroo.CycleDate = DateTime.Now;
                            AddBuckaroo.CycleDays = 30;
                            AddBuckaroo.LocationID = facilityId;
                            AddBuckaroo.logRecords = logRecords;

                            AddBuckaroo.Write(stream);
                        }

                        var AddPenny = new MotPatientRecord("Add")
                        {
                            PatientID = patientId2,
                            FirstName = "Penny",
                            LastName = "Priddy",
                            PrimaryPrescriberID = prescriberId,
                            DOB = DateTime.Now,
                            CycleDate = DateTime.Now,
                            CycleDays = 30,
                            LocationID = facilityId,
                            logRecords = logRecords
                        };

                        AddPenny.Write(stream);

                        using (var deletePenny = new MotPatientRecord("Delete"))
                        {
                            deletePenny.PatientID = patientId2;
                            deletePenny.Write(stream);
                        }

                        using (var deleteBuckaroo = new MotPatientRecord("Delete"))
                        {
                            deleteBuckaroo.PatientID = patientId1;
                            deleteBuckaroo.Write(stream);
                        }

                        using (var deleteLizardo = new MotPrescriberRecord("Delete"))
                        {
                            deleteLizardo.PrescriberID = prescriberId;
                            deleteLizardo.Write(stream);
                        }

                        using (var deleteFacility = new MotFacilityRecord("Delete"))
                        {
                            deleteFacility.LocationID = facilityId;
                            deleteFacility.Write(stream);
                        }
                    }
                    catch (Exception ex)
                    {
                        Assert.Fail(ex.Message);
                    }
                }
            }
        }
    }
}

