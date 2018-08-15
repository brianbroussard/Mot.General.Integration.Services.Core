using System;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MotCommonLib;

namespace CommonTests
{
    [TestClass]
    public class DatabaseTest
    {
        private PlatformOs _platform;
        internal string _dbaUserName = "ZGJh";
        internal string _dbaPassword = "cGM0MTBoNDI2czc2MTc=";


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

                if(GetPlatformOs.Go() == PlatformOs.Windows)
                { 
                    // This fails because MOT only has 32 bit drivers and the unit test doesn't seem to want to enable "prefer 32 bit"
                    var odbc = new MotDatabaseServer<MotOdbcServer>($"dsn=MOT8;UID={DecodeString(_dbaUserName)};PWD={DecodeString(_dbaPassword)}"); 
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
    }
}
