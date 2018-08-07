// 
// MIT license
//
// Copyright (c) 2018 by Peter H. Jenney and Medicine-On-Time, LLC.
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
using System.IO;
//using Microsoft.Data.Sqlite;
using NLog;
using System.Threading;

namespace MotCommonLib
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
        protected string dsn;
        protected DataSet records;

        public MotDbBase(string fullPath)
        {
            if(string.IsNullOrEmpty(fullPath))
            {
                throw new ArgumentNullException($"Bad DSN passed to constructor");
            }

            dsn = fullPath;

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
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
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
        private NpgsqlConnection _connection;
        private NpgsqlDataAdapter _adapter;
        private NpgsqlCommand _command;

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
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _connection?.Dispose();
                _adapter?.Dispose();
            }
        }

        /// <summary>
        /// <c>executeNonQuery</c>
        /// Executes SQL commands from the passed string
        /// </summary>
        /// <param name="strQuery"></param>
        public void ExecuteNonQuery(string strQuery, List<KeyValuePair<string, string>> parameterList = null)
        {
            try
            {
                dbConMutex.WaitOne();

                using (_connection = new NpgsqlConnection(dsn))
                {
                    _connection.Open();

                    using (var command = new NpgsqlCommand())
                    {
                        command.Connection = _connection;
                        command.CommandText = strQuery;
                        command.ExecuteNonQuery();
                    }

                    _connection.Close();
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
        ///
        public DataSet ExecuteQuery(string strQuery, List<KeyValuePair<string, string>> parameterList = null, string tableName = null)
        {
            try
            {
                dbConMutex.WaitOne();
                records.Clear();

                using (_connection = new NpgsqlConnection(dsn))
                {
                    _connection.Open();

                    using (_command = new NpgsqlCommand(strQuery, _connection))
                    {
                        if (parameterList != null)
                        {
                            foreach (var param in parameterList)
                            {
                                _command.Parameters.AddWithValue(param.Key, param.Value);
                            }
                        }

                        using (_adapter = new NpgsqlDataAdapter(strQuery, _connection))
                        {
                            _adapter.SelectCommand = _command;
                            _adapter.Fill(records, tableName ?? "strTable");
                        }
                    }

                    _connection.Close();

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
        /// <param name="fullPath"></param>
        public MotPostgreSqlServer(string fullPath) : base(fullPath)
        {
            dbConMutex.WaitOne();

            try
            {
                _connection = new NpgsqlConnection(fullPath);
                _connection.Open();
                _connection.Close();
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
        private SqlConnection _connection;
        private SqlDataAdapter _adapter;
        private SqlCommand _command;

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
                _connection?.Dispose();
                _adapter?.Dispose();
            }
        }

        /// <summary>
        /// <c>executeNonQuery</c>
        /// Executes SQL commands from the passed string
        /// </summary>
        /// <param name="strQuery"></param>
        /// <param name="parameterList"></param>
        public void ExecuteNonQuery(string strQuery, List<KeyValuePair<string, string>> parameterList = null)
        {

#if !PharmaServe
            try
            {
                dbConMutex.WaitOne();

                using (_connection = new SqlConnection(dsn))
                {
                    _connection.Open();

                    using (SqlCommand command = new SqlCommand())
                    {
                        if (parameterList != null)
                        {
                            foreach (var param in parameterList)
                            {
                                command.Parameters.AddWithValue(param.Key, param.Value);
                            }
                        }

                        command.Connection = _connection;
                        command.CommandText = strQuery;
                        command.ExecuteNonQuery();
                    }

                    _connection.Close();
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
        ///
        public DataSet ExecuteQuery(string strQuery, List<KeyValuePair<string, string>> parameterList = null, string tableName = null)
        {
            try
            {
                dbConMutex.WaitOne();
                records.Clear();

                using (_connection = new SqlConnection(dsn))
                {
                    _connection.Open();

                    using (_command = new SqlCommand(strQuery, _connection))
                    {
                        if (parameterList != null)
                        {
                            foreach (var param in parameterList)
                            {
                                _command.Parameters.AddWithValue(param.Key, param.Value);
                            }
                        }

                        using (_adapter = new SqlDataAdapter(strQuery, _connection))
                        {
                            _adapter.SelectCommand = _command;
                            _adapter.Fill(records, tableName ?? "Table");
                        }
                    }

                    _connection.Close();

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
        /// <param name="fullPath"></param>
        public MotSqlServer(string fullPath) : base(fullPath)
        {
            dbConMutex.WaitOne();

            try
            {
                _connection = new SqlConnection(fullPath);
                _connection.Open();
                _connection.Close();
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
        private OdbcConnection _connection;
        private OdbcDataAdapter _adapter;
        private OdbcCommand _command;

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
            if (!disposing)
            {
                return;
            }

            _connection?.Dispose();
            _adapter?.Dispose();
        }

        /// <summary>
        /// <c>motODBCServer</c>
        /// Constructor, connects to the database using the passed complete DSN
        /// </summary>
        /// <param name="fullPath"></param>
        public MotOdbcServer(string fullPath) : base(fullPath)
        {
            try
            {
                dbConMutex.WaitOne();

                using (_connection = new OdbcConnection(fullPath))
                {
                    _connection.Open();
                    _connection.Close();
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
        public void ExecuteNonQuery(string strQuery, List<KeyValuePair<string, string>> parameterList = null)
        {
            try
            {
                dbConMutex.WaitOne();

                using (_connection = new OdbcConnection(dsn))
                {
                    _connection.Open();

                    using (_command = new OdbcCommand())
                    {
                        _command.Connection = _connection;
                        _command.CommandText = strQuery;

                        if (parameterList != null)
                        {
                            foreach (var param in parameterList)
                            {
                                _command.Parameters.AddWithValue(param.Key, param.Value);
                            }
                        }

                        _command.ExecuteNonQuery();
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
        /// 
        public DataSet ExecuteQuery(string strQuery, List<KeyValuePair<string, string>> parameterList = null, string tableName = null)
        {
            try
            {
                dbConMutex.WaitOne();
                records.Clear();

                using (_connection = new OdbcConnection(dsn))
                {
                    _connection.Open();

                    using (_command = new OdbcCommand(strQuery, _connection))
                    {
                        if (parameterList != null)
                        {
                            foreach (var param in parameterList)
                            {
                                _command.Parameters.AddWithValue(param.Key, param.Value);
                            }
                        }

                        records = new DataSet(tableName ?? "Table");

                        using (_adapter = new OdbcDataAdapter(strQuery, _connection))
                        {
                            _adapter.SelectCommand = _command;
                            _adapter.Fill(records, tableName ?? "strTable");
                        }
                    }
                }

                _connection.Close();

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
    /// <c>MotSqliteServer</c>
    /// A database instance for manageing Sqlite Databases
    /// </summary>
    public class MotSqliteServer : MotDbBase
    {

        // private SqliteConnection _connection;
        //private SqliteCommand _command;

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
            if (!disposing)
            {
                return;
            }

            //_connection?.Dispose();
        }

        /// <summary>
        /// <c>motODBCServer</c>
        /// Constructor, connects to the database using the passed complete DSN
        /// Note that in the case of Sqlite the fullPath needs to be a fully qualified path
        /// </summary>
        /// <param name="fullPath"></param>
        public MotSqliteServer(string fullPath) : base(fullPath)
        {
            try
            {
                dbConMutex.WaitOne();

                if (!Directory.Exists(Path.GetPathRoot(fullPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                }

                // using (_connection = new SqliteConnection($"Data Source={fullPath};Version=3;"))
                //{
                //     _connection.Open();
                //     _connection.Close();
                // }
            }
            catch (Exception ex)
            {
                throw new Exception($"Sqlite Connection Failed: {ex.Message}");
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
        public void ExecuteNonQuery(string strQuery, List<KeyValuePair<string, string>> parameterList = null)
        {
            if (strQuery == null)
            {
                throw new ArgumentNullException($"Null Query String");
            }
            /*
            try
            {
                dbConMutex.WaitOne();

                using (_connection = new SqliteConnection($"Data Source={dsn};Version=3;"))
                {
                    _connection.Open();


                    using (_command = new SqliteCommand(strQuery, _connection))
                    {
                        if (parameterList != null)
                        {
                            foreach (var param in parameterList)
                            {
                                if (param.Key.Contains("@"))
                                {
                                    _command.Parameters.AddWithValue(param.Key, param.Value);
                                }
                                else
                                {
                                    _command.Parameters.AddWithValue($"@{param.Key}", param.Value);
                                }
                            }
                        }

                        _command.ExecuteNonQuery();
                    }                   
                 }
            }
            catch (SqliteException ex)
            {
                throw new Exception($"Sqlite executeNonQuery failed: {ex.Message}");
            }
            finally
            {
                dbConMutex?.ReleaseMutex();
            }
            */
        }

        /// <summary>
        /// <c>executeQuery</c>
        /// Executes the SQL query and populates the passed DataSet
        /// </summary>
        /// <param name="strQuery"></param>
        /// <param name="parameterList"></param>
        /// <param name="tableName"></param>
        /// <returns>A DataSet resulting from the query.  If there is no valid DataSet it will throw an exception</returns> 
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
        /// 
        public DataSet ExecuteQuery(string strQuery, List<KeyValuePair<string, string>> parameterList = null, string tableName = null)
        {
            return new DataSet("foo");
            /*
            try
            {
                dbConMutex.WaitOne();
                records.Clear();

                using (_connection = new SqliteConnection($"Data Source={dsn};Version=3;"))
                {
                    _connection.Open();

                    using (_command = new SqliteCommand(strQuery, _connection))
                    {
                        if (parameterList != null)
                        {
                            foreach (var param in parameterList)
                            {
                                if (param.Key.Contains("@"))
                                {
                                    _command.Parameters.AddWithValue(param.Key, param.Value);
                                }
                                else
                                {
                                    _command.Parameters.AddWithValue($"@{param.Key}", param.Value);
                                }
                            }
                        }

                        records = new DataSet(tableName ?? "Table");
                        DataTable dt = new DataTable();

                        using (var rdr = _command.ExecuteReader(CommandBehavior.SingleResult))
                        {
                            dt.Load(rdr);
                        }

                        records.Tables.Add(dt);
                    }
                }

                return (ValidateReturn(tableName ?? "Table"));
            }
            catch (SqliteException ex)
            {
                throw new Exception($"Sqlite executeQuery failed: {ex.Message}");
            }
            finally
            {
                dbConMutex?.ReleaseMutex();
            }
            */
        }

        /// <summary>
        /// <c>~motODBCServer</c>
        /// Destructor, disposes local instance
        /// </summary>
        ~MotSqliteServer()
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
    public class MotDatabaseServer<T> : IDisposable
    {
        private readonly DbType _thisDbType = DbType.NullServer;
        private readonly MotSqlServer _sqlServer;
        private readonly MotPostgreSqlServer _npgServer;
        private readonly MotOdbcServer _odbcServer;
        private readonly MotSqliteServer _sqliteServer;

        private readonly Logger _eventLogger;
        private Type _typeParameterType;

        /// <summary>
        /// <c>recordSet</c>
        /// A global DataSet for use by underlying classes
        /// </summary>
        private readonly DataSet _recordSet;

        /// <summary>
        /// <c>executeQuery</c>
        /// Executes the SQL query and populates the passed DataSet
        /// </summary>
        /// <param name="strQuery"></param>
        /// <param name="parameterList"></param>
        /// <param name="tableName"></param>
        /// <returns>bool indicating succcess or failure of the call</returns>
        public DataSet ExecuteQuery(string strQuery, List<KeyValuePair<string, string>> parameterList = null, string tableName = null)
        {
            try
            {
                switch (_thisDbType)
                {
                    case DbType.NpgServer:
                        return _npgServer.ExecuteQuery(strQuery, parameterList, tableName);

                    case DbType.OdbcServer:
                        return _odbcServer.ExecuteQuery(strQuery, parameterList, tableName);

                    case DbType.SqlServer:
                        return _sqlServer.ExecuteQuery(strQuery, parameterList, tableName);

                    case DbType.SqlightServer:
                        return _sqliteServer.ExecuteQuery(strQuery, parameterList, tableName);

                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                _eventLogger.Error("Failed while executing Query: {0}", e.Message);
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
        public bool ExecuteNonQuery(string strQuery, List<KeyValuePair<string, string>> parameterList = null)
        {
            try
            {
                switch (_thisDbType)
                {
                    case DbType.NpgServer:
                        _npgServer.ExecuteNonQuery(strQuery, parameterList);
                        break;

                    case DbType.OdbcServer:
                        _odbcServer.ExecuteNonQuery(strQuery, parameterList);
                        break;

                    case DbType.SqlServer:
                        _sqlServer.ExecuteNonQuery(strQuery, parameterList);
                        break;

                    case DbType.SqlightServer:
                        _sqliteServer.ExecuteNonQuery(strQuery, parameterList);
                        break;

                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                _eventLogger.Error("Failed while executing NonQuery: {0}", e.Message);
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
        public DataSet SetView(string View)
        {
            try
            {
                switch (_thisDbType)
                {
                    case DbType.NpgServer:
                        return _npgServer.ExecuteQuery(View);

                    case DbType.OdbcServer:
                        return _odbcServer.ExecuteQuery(View);

                    case DbType.SqlServer:
                        return _sqlServer.ExecuteQuery(View);

                    case DbType.SqlightServer:
                        return _sqliteServer.ExecuteQuery(View);

                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                _eventLogger.Warn("Failed to set view: {0}", e.Message);
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
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                ((IDisposable)_recordSet).Dispose();

                switch (_thisDbType)
                {
                    case DbType.NpgServer:
                        _npgServer?.Dispose();
                        break;

                    case DbType.OdbcServer:
                        _odbcServer?.Dispose();
                        break;

                    case DbType.SqlServer:
                        _sqlServer?.Dispose();
                        break;

                    case DbType.SqlightServer:
                        _sqliteServer?.Dispose();
                        break;

                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// <c>motDatabase</c>
        /// </summary>
        /// <param name="connectString"></param>
        public MotDatabaseServer(string connectString)
        {
            try
            {
                _recordSet = new DataSet("GenericTable");
                _eventLogger = LogManager.GetLogger("motInboundLib.Database");
                _typeParameterType = typeof(T);

                switch (_typeParameterType.Name)
                {
                    case "MotSqlServer":
                        _sqlServer = new MotSqlServer(connectString);
                        _thisDbType = DbType.SqlServer;
                        _eventLogger.Info("Setting up as Microsoft SQL Server");
                        break;

                    case "MotOdbcServer":
                        _odbcServer = new MotOdbcServer(connectString);
                        _thisDbType = DbType.OdbcServer;
                        _eventLogger.Info("Setting up as ODBC Server");
                        break;

                    case "MotNpgServer":
                        _npgServer = new MotPostgreSqlServer(connectString);
                        _thisDbType = DbType.NpgServer;
                        _eventLogger.Info("Setting up as PostgreSQL Server");
                        break;

                    case "MotSqliteServer":
                        _sqliteServer = new MotSqliteServer(connectString);
                        _thisDbType = DbType.SqlightServer;
                        _eventLogger.Info("Setting up as Sqlite Server");
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
