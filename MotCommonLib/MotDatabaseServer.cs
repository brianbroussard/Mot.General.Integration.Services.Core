// 
// MIT license
//
// Copyright (c) 2016 by Peter H. Jenney and Medicine-On-Time, LLC.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 

using System;
using System.Collections.Generic;
using System.Data;
using Npgsql;
using System.Data.Odbc;
using System.Data.SqlClient;
using NLog;
using System.Threading;


namespace motCommonLib
{
    /// <summary>
    /// <c>dbType</c>
    /// Enumeration for supported databases
    /// </summary>
    public enum DbType
    {
#pragma warning disable 1591
        NullServer,
        SqlServer,
        NpgServer,
        MySqlServer,
        SqlightServer,
        SqlAnywhereServer,
        OdbcServer
#pragma warning restore 1591
    };
    
    /// <summary>
    /// <c>Action</c>
    /// Enumeration for database actions
    /// </summary>
    public enum ActionType
    {
#pragma warning disable 1591
        Add,
        Change,
        Delete
#pragma warning restore 1591
    };

    public class MotDbBase : IDisposable
    {
        protected static Mutex dbConMutex;
        protected string DSN;
        protected DataSet records;

        public MotDbBase(string dsn)
        {
            if (dbConMutex == null)
            {
                dbConMutex = new Mutex(false, "MotDb");
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// <c>Dispose</c>
        /// </summary>
        protected virtual void Dispose(bool Disposing)
        {
            if (Disposing)
            {

            }
        }
        protected DataSet ValidateReturn(string tableName)
        {
            if (records.Tables.Count > 1 && records.Tables[tableName].Rows.Count > 0)
            {
                return records;
            }

            throw new Exception($"Query did not return any data");
        }
    }
    /// <summary>
    /// <c>MotPostgreSQLServer</c>
    /// A database instance for manageing PostegreSQL
    /// </summary>
    public class MotPostgreSqlServer : MotDbBase
    {
        private NpgsqlConnection Connection;
        private NpgsqlDataAdapter Adapter;
        private NpgsqlCommand Command;

        /// <summary>
        /// <c>Dispose</c>
        /// </summary>
        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// <c>Dispose</c>
        /// </summary>
        /// <param name="Disposing"></param>
        protected override void Dispose(bool Disposing)
        {
            if (Disposing)
            {
                Connection?.Dispose();
                Adapter?.Dispose();
            }
        }
        /// <summary>
        /// <c>executeNonQuery</c>
        /// Executes SQL commands from the passed string
        /// </summary>
        /// <param name="strQuery"></param>
        public void executeNonQuery(string strQuery, List<KeyValuePair<string, string>> parameterList = null)
        {
            try
            {
                dbConMutex.WaitOne();

                using (Connection = new NpgsqlConnection(DSN))
                {
                    Connection.Open();

                    using (var command = new NpgsqlCommand())
                    {
                        command.Connection = Connection;
                        command.CommandText = strQuery;
                        command.ExecuteNonQuery();
                    }

                    Connection.Close();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"PostgreSQL executeNonQuery failed: {ex.Message}");
            }
            finally
            {
                dbConMutex?.ReleaseMutex();
            }
        }
        /// <summary>
        /// <c>executeQuery</c>
        /// Executes the SQL query and populates the passed DataSet
        /// </summary>
        /// <param name="strQuery"></param>
        /// <param name="parameterList"></param>
        /// <param name="tableName"></param>
        /// <returns>A DataSet resulting from the query.  If there is no valid DataSet it will throw an exception</returns> 
#pragma warning disable 1570
        /// <code>
        ///         try
        ///         {
        ///             var parameterList1 = new List<KeyValuePair<string, string>>()
        ///             {

        ///                 new KeyValuePair<string, string>("firstName", "Fred"),
        ///                 new KeyValuePair<string, string>( "lastName", "Flintstone")
        ///             };
        ///
        ///             var db = new MotPostgreSqlServer("select * from members where firstName = ? and lastName = ?");
        ///             Dataset ds = db.executeQuery(query, parametherList, "LoyalOrderOfWaterBuffalos");
        ///         }
        ///         catch(Exception ex)
        ///         {
        ///             Console.Write($"Query did not return any date: {ex.Message}");
        ///         }
        /// 
        ///</code>
#pragma warning restore 1570
        ///
        public DataSet executeQuery(string strQuery, List<KeyValuePair<string, string>> parameterList = null, string tableName = null)
        {

            try
            {
                dbConMutex.WaitOne();
                records.Clear();

                using (Connection = new NpgsqlConnection(DSN))
                {
                    Connection.Open();

                    using (Command = new NpgsqlCommand(strQuery, Connection))
                    {
                        if (parameterList != null)
                        {
                            foreach (var param in parameterList)
                            {
                                Command.Parameters.AddWithValue(param.Key, param.Value);
                            }
                        }

                        using (Adapter = new NpgsqlDataAdapter(strQuery, Connection))
                        {
                            Adapter.SelectCommand = Command;
                            Adapter.Fill(records, tableName ?? "strTable");
                        }
                    }

                    Connection.Close();

                    return ValidateReturn(tableName ?? "Table");
                }
            }
            catch (NpgsqlException ex)
            {
                throw new Exception($"PostgreSQL executeQuery failed: {ex.Message}");
            }
            finally
            {
                dbConMutex?.ReleaseMutex();
            }
        }
        /// <summary>
        /// <c>motPostgreSQLServer</c>
        /// Constructor, connects to the database using the passed complete DSN
        /// </summary>
        /// <param name="dsn"></param>
        public MotPostgreSqlServer(string dsn) : base(dsn)
        {

            dbConMutex.WaitOne();

            try
            {
                Connection = new NpgsqlConnection(dsn);
                Connection.Open();
                Connection.Close();
            }
            catch (Exception ex)
            {
                throw new Exception($"PostgreSQL Connection Failed: {ex.Message}");
            }
            finally
            {
                dbConMutex.ReleaseMutex();
            }
        }
        /// <summary>
        /// <c>~motPostgreSQLServer</c>
        /// Destructor, disposes local instance
        /// </summary>
        ~MotPostgreSqlServer()
        {
            Dispose(false);
        }
    }
    /// <summary>
    /// <c>MotSQLServer</c>
    /// A database instance for manageing Microsoft SQL Server
    /// </summary>
    public class MotSqlServer : MotDbBase
    {
        private SqlConnection Connection;
        private SqlDataAdapter Adapter;
        private SqlCommand Command;

        /// <summary>
        /// <c>Dispose</c>
        /// </summary>
        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// <c>Dispose</c>
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Connection?.Dispose();
                Adapter?.Dispose();
            }
        }
        /// <summary>
        /// <c>executeNonQuery</c>
        /// Executes SQL commands from the passed string
        /// </summary>
        /// <param name="strQuery"></param>
        /// <param name="parameterList"></param>
        public void executeNonQuery(string strQuery, List<KeyValuePair<string, string>> parameterList = null)
        {

#if !PharmaServe

            try
            {
                dbConMutex.WaitOne();

                using (Connection = new SqlConnection(DSN))
                {
                    Connection.Open();

                    using (SqlCommand command = new SqlCommand())
                    {
                        if (parameterList != null)
                        {
                            foreach (var param in parameterList)
                            {
                                command.Parameters.AddWithValue(param.Key, param.Value);
                            }
                        }

                        command.Connection = Connection;
                        command.CommandText = strQuery;
                        command.ExecuteNonQuery();
                    }

                    Connection.Close();
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                dbConMutex?.ReleaseMutex();
            }
#else
            // Following is for CPR+ Interface

            try
            {
                using (connection = new SqlConnection(DSN))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand())
                    {
                        command.Connection = connection;

                        string[] scripts = Regex.Split(strQuery, @"^\w+GO$", RegexOptions.Multiline);

                        //string[] scripts = strQuery.Split("GO");

                        foreach (string splitScript in scripts)
                        {
                            command.CommandText = splitScript.Substring(0, splitScript.ToLower().IndexOf("go");
                            command.ExecuteNonQuery();
                        }
                    }

                    connection.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to execute nonQuery {0}", e.Message);
            }
#endif
        }
        /// <summary>
        /// <c>executeQuery</c>
        /// Executes the SQL query and populates the passed DataSet
        /// </summary>
        /// <param name="strQuery"></param>
        /// <param name="parameterList"></param>
        /// <param name="tableName"></param>
        /// <returns>A DataSet resulting from the query.  If there is no valid DataSet it will throw an exception</returns> 
#pragma warning disable 1570
        /// <code>
        ///         try
        ///         {
        ///             var parameterList1 = new List<KeyValuePair<string, string>>()
        ///             {

        ///                 new KeyValuePair<string, string>("firstName", "Fred"),
        ///                 new KeyValuePair<string, string>( "lastName", "Flintstone")
        ///             };
        ///
        ///             var db = new MotSqlServer("select * from members where firstName = ? and lastName = ?");
        ///             Dataset ds = db.executeQuery(query, parametherList, "LoyalOrderOfWaterBuffalos");
        ///         }
        ///         catch(Exception ex)
        ///         {
        ///             Console.Write($"Query did not return any date: {ex.Message}");
        ///         }
        /// 
        ///</code>
#pragma warning restore 1570
        ///
        public DataSet executeQuery(string strQuery, List<KeyValuePair<string, string>> parameterList = null, string tableName = null)
        {
            try
            {
                dbConMutex.WaitOne();
                records.Clear();

                using (Connection = new SqlConnection(DSN))
                {
                    Connection.Open();

                    using (Command = new SqlCommand(strQuery, Connection))
                    {
                        if (parameterList != null)
                        {
                            foreach (var param in parameterList)
                            {
                                Command.Parameters.AddWithValue(param.Key, param.Value);
                            }
                        }

                        using (Adapter = new SqlDataAdapter(strQuery, Connection))
                        {
                            Adapter.SelectCommand = Command;
                            Adapter.Fill(records, tableName ?? "Table");
                        }
                    }

                    Connection.Close();

                    return (ValidateReturn(tableName ?? "Table"));
                }
            }
            catch (SqlException ex)
            {
                throw new Exception($"SQLServer executeQuery failed: {ex.Errors}");
            }
            finally
            {
                dbConMutex?.ReleaseMutex();
            }
        }
        /// <summary>
        /// <c>motSQLServer</c>
        /// Constructor, connects to the database using the passed complete DSN
        /// </summary>
        /// <param name="dsn"></param>
        public MotSqlServer(string dsn) : base(dsn)
        {
            dbConMutex.WaitOne();

            try
            {
                Connection = new SqlConnection(dsn);
                Connection.Open();
                Connection.Close();
            }
            catch (Exception ex)
            {
                throw new Exception($"SQLServer Connection Failed: {ex.Message}");
            }
            finally
            {
                dbConMutex.ReleaseMutex();
            }
        }
        /// <summary>
        /// <c>~motSQLServer</c>
        /// Destructor, disposes local instance
        /// </summary>
        ~MotSqlServer()
        {
            Dispose(false);
        }
    }
    /// <summary>
    /// <c>MotODBCServer</c>
    /// A database instance for manageing ODBC Databases
    /// </summary>
    public class MotOdbcServer : MotDbBase
    {
        private OdbcConnection Connection;
        private OdbcDataAdapter Adapter;
        private OdbcCommand Command;

        /// <summary>
        /// <c>Dispose</c>
        /// </summary>
        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// <c>Dispose</c>
        /// </summary>
        protected override void Dispose(bool Disposing)
        {
            if (!Disposing)
            {
                return;
            }

            Connection?.Dispose();
            Adapter?.Dispose();
        }
        /// <summary>
        /// <c>motODBCServer</c>
        /// Constructor, connects to the database using the passed complete DSN
        /// </summary>
        /// <param name="dsn"></param>
        public MotOdbcServer(string dsn) : base(dsn)
        {
            try
            {
                dbConMutex.WaitOne();

                using (Connection = new OdbcConnection(dsn))
                {
                    Connection.Open();
                    Connection.Close();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"ODBC Connection Failed: {ex.Message}");
            }
            finally
            {
                dbConMutex?.ReleaseMutex();
            }
        }
        /// <summary>
        /// <c>executeNonQuery</c>
        /// Executes SQL commands from the passed string
        /// </summary>
        /// <param name="strQuery"></param>
        /// <param name="parameterList"></param>
        public void executeNonQuery(string strQuery, List<KeyValuePair<string, string>> parameterList = null)
        {
            try
            {
                dbConMutex.WaitOne();

                using (Connection = new OdbcConnection(DSN))
                {
                    Connection.Open();

                    using (Command = new OdbcCommand())
                    {
                        Command.Connection = Connection;
                        Command.CommandText = strQuery;

                        if (parameterList != null)
                        {
                            foreach (var param in parameterList)
                            {
                                Command.Parameters.AddWithValue(param.Key, param.Value);
                            }
                        }

                        Command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"ODBC executeNonQuery failed: {ex.Message}");
            }
            finally
            {
                dbConMutex?.ReleaseMutex();
            }
        }
        /// <summary>
        /// <c>executeQuery</c>
        /// Executes the SQL query and populates the passed DataSet
        /// </summary>
        /// <param name="strQuery"></param>
        /// <param name="parameterList"></param>
        /// <param name="tableName"></param>
        /// <returns>A DataSet resulting from the query.  If there is no valid DataSet it will throw an exception</returns> 
#pragma warning disable 1570
        /// <code>
        ///         try
        ///         {
        ///             var parameterList1 = new List<KeyValuePair<string, string>>()
        ///             {

        ///                 new KeyValuePair<string, string>("firstName", "Fred"),
        ///                 new KeyValuePair<string, string>( "lastName", "Flintstone")
        ///             };
        ///
        ///             var db = new MotOdbcServer("select * from members where firstName = ? and lastName = ?");
        ///             Dataset ds = db.executeQuery(query, parametherList, "LoyalOrderOfWaterBuffalos");
        ///         }
        ///         catch(Exception ex)
        ///         {
        ///             Console.Write($"Query did not return any date: {ex.Message}");
        ///         }
        /// 
        ///</code>
#pragma warning restore 1570
        /// 
        public DataSet executeQuery(string strQuery, List<KeyValuePair<string, string>> parameterList = null, string tableName = null)
        {
            try
            {


                dbConMutex.WaitOne();
                records.Clear();

                using (Connection = new OdbcConnection(DSN))
                {
                    Connection.Open();

                    using (Command = new OdbcCommand(strQuery, Connection))
                    {
                        if (parameterList != null)
                        {
                            foreach (var param in parameterList)
                            {
                                Command.Parameters.AddWithValue(param.Key, param.Value);
                            }
                        }

                        records = new DataSet(tableName ?? "Table");

                        using (Adapter = new OdbcDataAdapter(strQuery, Connection))
                        {
                            Adapter.SelectCommand = Command;
                            Adapter.Fill(records, tableName ?? "strTable");
                        }
                    }
                }

                Connection.Close();

                return (ValidateReturn(tableName ?? "Table"));
            }
            catch (OdbcException ex)
            {
                throw new Exception($"ODBC executeQuery failed: {ex.Errors}");
            }
            finally
            {
                dbConMutex?.ReleaseMutex();
            }
        }
        /// <summary>
        /// <c>~motODBCServer</c>
        /// Destructor, disposes local instance
        /// </summary>
        ~MotOdbcServer()
        {
            Dispose(false);
        }
    }
    /// <summary>
    /// <c>MotDatabase</c>
    /// A class to abstract and normalize the use of multiple SQL database types
    /// 
    /// <code>
    ///      using(var db = new MotDatabase(DSN, ODBCServer)
    ///      {        
    ///          db.executeQuery("SELECT * from Patients", MainDS, "Patients");
    ///      }
    /// </code>
    /// </summary>
    public class MotDatabaseServer : IDisposable
    {
        private readonly DbType thisDbType = DbType.NullServer;
        private readonly MotSqlServer sqlServer;
        private readonly MotPostgreSqlServer npgServer;
        private readonly MotOdbcServer odbcServer;
        private readonly Logger EventLogger;

        /// <summary>
        /// <c>recordSet</c>
        /// A global DataSet for use by underlying classes
        /// </summary>
        private readonly DataSet RecordSet;
        /// <summary>
        /// <c>executeQuery</c>
        /// Executes the SQL query and populates the passed DataSet
        /// </summary>
        /// <param name="strQuery"></param>
        /// <param name="parameterList"></param>
        /// <returns>bool indicating succcess or failure of the call</returns>
        public DataSet executeQuery(string strQuery, List<KeyValuePair<string, string>> parameterList = null, string tableName = null)
        {
            try
            {
                switch (thisDbType)
                {
                    case DbType.NpgServer:
                        return npgServer.executeQuery(strQuery, parameterList, tableName);

                    case DbType.OdbcServer:
                        return odbcServer.executeQuery(strQuery, parameterList, tableName);

                    case DbType.SqlServer:
                        return sqlServer.executeQuery(strQuery, parameterList, tableName);

                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                EventLogger.Error("Failed while executing Query: {0}", e.Message);
                throw;
            }

            return null;
        }
        /// <summary>
        /// <c>executeNonQuery</c>
        /// Executes SQL commands from the passed string
        /// </summary>
        /// <param name="strQuery"></param>
        /// <param name="parameterList"></param>
        public bool executeNonQuery(string strQuery, List<KeyValuePair<string, string>> parameterList = null)
        {
            try
            {
                switch (thisDbType)
                {
                    case DbType.NpgServer:
                        npgServer.executeNonQuery(strQuery, parameterList);
                        break;

                    case DbType.OdbcServer:
                        odbcServer.executeNonQuery(strQuery, parameterList);
                        break;

                    case DbType.SqlServer:
                        sqlServer.executeNonQuery(strQuery, parameterList);
                        break;

                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                EventLogger.Error("Failed while executing NonQuery: {0}", e.Message);
                throw;
            }

            return false;
        }
        /// <summary>
        /// <c>setView</c>
        /// Queries against a view instead of a simple query string
        /// </summary>
        /// <param name="View"></param>
        /// <returns></returns>
        public DataSet setView(string View)
        {
            try
            {
                switch (thisDbType)
                {
                    case DbType.NpgServer:
                        return npgServer.executeQuery(View);

                    case DbType.OdbcServer:
                        return odbcServer.executeQuery(View);

                    case DbType.SqlServer:
                        return sqlServer.executeQuery(View);

                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                EventLogger.Warn("Failed to set view: {0}", e.Message);
                throw;
            }

            return null;
        }
        /// <summary>
        /// <c>Dispose</c>
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="Disposing"></param>
        protected virtual void Dispose(bool Disposing)
        {
            if (Disposing)
            {
                ((IDisposable)RecordSet).Dispose();

                switch (thisDbType)
                {
                    case DbType.NpgServer:
                        npgServer?.Dispose();
                        break;

                    case DbType.OdbcServer:
                        odbcServer?.Dispose();
                        break;

                    case DbType.SqlServer:
                        sqlServer?.Dispose();
                        break;

                    default:
                        break;
                }
            }
        }
        /// <summary>
        /// <c>motDatabase</c>
        /// Constructor
        /// </summary>
        public MotDatabaseServer() { }
        /// <summary>
        /// <c>motDatabase</c>
        /// </summary>
        /// <param name="connectString"></param>
        /// <param name="dbType"></param>
        public MotDatabaseServer(string connectString, DbType dbType)
        {
            try
            {
                RecordSet = new DataSet("GenericTable");
                EventLogger = LogManager.GetLogger("motInboundLib.Database");

                switch (dbType)
                {
                    case DbType.SqlServer:
                        sqlServer = new MotSqlServer(connectString);
                        thisDbType = DbType.SqlServer;

                        EventLogger.Info("Setting up as Microsoft SQL Server");

                        break;

                    case DbType.OdbcServer:
                        odbcServer = new MotOdbcServer(connectString);
                        thisDbType = DbType.OdbcServer;

                        EventLogger.Info("Setting up as ODBC Server");

                        break;

                    case DbType.NpgServer:
                        npgServer = new MotPostgreSqlServer(connectString);
                        thisDbType = DbType.NpgServer;

                        EventLogger.Info("Setting up as PostgreSQL Server");

                        break;

                    default:
                        break;
                }
            }
            catch
            {
                throw;
            }
        }
    }
}
