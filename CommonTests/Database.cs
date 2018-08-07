using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using MotCommonLib;

namespace CommonTests
{
    [TestClass]
    public class DatabaseTest
    {
        private PlatformOs _platform;
        string userName = "dba";
        string passWord = "pc410h426s7617";

      
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
                    var odbc = new MotDatabaseServer<MotOdbcServer>($"dsn=MOT8;UID={userName};PWD={passWord}");
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
            var path = (GetPlatformOs.Go() == PlatformOs.Windows) ? $@"C:\motNext\Tests\Sqlite\Test.sqlite" : $@"~/Projects/Tests/Sqlite/Test.sqlite";

            try
            {
                var sqlite = new MotDatabaseServer<MotSqliteServer>(path);

                if(GetPlatformOs.Go() == PlatformOs.Windows)
                { 
                    var odbc = new MotDatabaseServer<MotOdbcServer>($"dsn=MOT8;UID={userName};PWD={passWord}"); 
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
