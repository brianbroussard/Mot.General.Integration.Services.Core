using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Runtime.InteropServices;
using motCommonLib;
using MotParserLib;

namespace CommonTests
{
    [TestClass]
    public class DatabaseTest
    {
        private PlatformOs _platform;

        protected PlatformOs GetOs()
        {
            // just worry about Nix and Win for now
            if (RuntimeInformation.OSDescription.Contains("Unix") || RuntimeInformation.OSDescription.Contains("Linux"))
            {
                _platform = PlatformOs.Unix;
            }
            else if (RuntimeInformation.OSDescription.Contains("Windows"))
            {
                _platform = PlatformOs.Windows;
            }
            else
            {
                _platform = PlatformOs.Unknown;
            }

            return _platform;
        }

        [TestMethod]
        public void Construct()
        {
            var userName = "dba";
            var passWord = "pc410h426s7617";

            var path = (GetOs() == PlatformOs.Windows) ? $@"C:\motNext\Tests\Sqlite\Test.sqlite" : $@"~/Projects/Tests/Sqlite/Test.sqlite";

            try
            {
                var sqlite = new MotDatabaseServer<MotSqliteServer>(path);
                var odbc = new MotDatabaseServer<MotOdbcServer>($"dsn=MOT8;UID={userName};PWD={passWord}");


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
