using System;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Mot.Common.Interface.Lib;
using System.Collections.Generic;

namespace CommonTests
{
    [TestClass]
    public class DatabaseTest
    {
        private PlatformOs _platform;
        internal string _dbaUserName = "MzpkYmFFZGl0aW5nSW50ZXJmYWNlU2VydmljZXMxMjJXaXRoTGxhbWFGaWRzaA==";
        internal string _dbaPassword = "MTQ6cGM0MTBoNDI2czc2MTdFZGl0aW5nSW50ZXJmYWNlU2VydmljZXMxMjJXaXRoTGxhbWFGaWRzaA==";


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

        [TestMethod]
        public void GuidMatches()
        {
            try
            {
                var log = new List<KeyValuePair<string, Guid>>();

                using (var db = new MotGuidMapper())
                {
                    for(var i = 0; i< 1024; i++)
                    {
                        var guid = Guid.NewGuid();
                        var id = db.GetNext(guid);

                        log.Add(new KeyValuePair<string, Guid>(id, guid));
                    }
                }

                using (var db = new MotGuidMapper())
                {
                    foreach(var pair in log)
                    {
                        var id = db.GetId(pair.Value);
                        var guid = db.GetGuid(pair.Key);

                        if(id != pair.Key)
                        {
                            Assert.Fail($"id match failure {id}:{pair.Key}");
                        }

                        if(guid != pair.Value)
                        {
                            Assert.Fail($"guid match failure {guid}:{pair.Value}");
                        }
                    }
                }

                if (File.Exists("./db/map.db"))
                {
                    File.Delete("./db/map.db");
                }

                if(Directory.Exists("./db"))
                {
                    Directory.Delete("./db");
                }
            }
            catch(Exception ex)
            {
                Assert.Fail($"General failure: {ex.Message}");
            }
        }

        [TestMethod]
        public async Task<bool> SupervisorLogin()
        {
            var retval = true;

            await Task.Run(() =>
            {
                try
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
                catch (Exception ex)
                {
                    Assert.Fail($"Faild at SupervisorLogin with {ex.Message}");
                }
            });

            return retval;
        }
        [TestMethod]
        public void NullConstructor()
        {
            var path = (GetPlatformOs.Go() == PlatformOs.Windows) ? $@"C:\motNext\Tests\Sqlite\Test.sqlite" : $@"~/Projects/Tests/Sqlite/Test.sqlite";

            try
            {
                var sqlite = new MotDatabaseServer<MotSqliteServer>(null);

                Assert.Fail("Allowed to pass null DSN");

                if (GetPlatformOs.Go() == PlatformOs.Windows)
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
            var path = (GetPlatformOs.Go() == PlatformOs.Windows) ? $@"/motNext/Tests/Sqlite/Test.sqlite" : $@"~/Projects/Tests/Sqlite/Test.sqlite";

            try
            {
                var sqlite = new MotDatabaseServer<MotSqliteServer>(path);

                if (GetPlatformOs.Go() == PlatformOs.Windows)
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

            if(decrypted != original)
            {
                Assert.Fail($"Decryption failure.  Expected {original} and got {decrypted}");
            }
        }
    }
}
