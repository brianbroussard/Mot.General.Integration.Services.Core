using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using motCommonLib;

namespace CommonTests
{
    [TestClass]
    public class DatabaseTest
    {
        [TestMethod]
        public void ConstructSqlite()
        {
            //var path = ()
            var sqlite = new MotDatabaseServer<MotSqliteServer>("~/Projects/Tests/Sqlite/Test.sqlite");



            // CleanUp
            File.Delete("~/Projects/Tests/Test.sqlite");

        }
    }
}
