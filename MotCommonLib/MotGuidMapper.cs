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

// ref: https://github.com/aspnet/Microsoft.Data.Sqlite/wiki/Connection-Strings

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Data.Sqlite;
using NLog;

namespace Mot.Common.Interface.Lib
{
    public class MotGuidMapper : IDisposable
    {
        private bool dbIsOpen { get; set; }
        private SqliteConnection _mDbConnection { get; set; }
        private Logger _eventLogger { get; set; }

        public MotGuidMapper()
        {
            _eventLogger = LogManager.GetLogger("keypairdb");
            OpenDb();
        }

        void CreateDb()
        {
            try
            {

                var sql = "CREATE TABLE IF NOT EXISTS `map` (`TimeStamp` TEXT NOT NULL, `guid` TEXT NOT NULL, `id` INTEGER NOT NULL, PRIMARY KEY(`guid`));";

                using (_mDbConnection = new SqliteConnection(@"Data Source=./db/map.db"))
                {
                    _mDbConnection.Open();

                    using (var command = new SqliteCommand(sql, _mDbConnection))
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

        void OpenDb()
        {
            try
            {
                if (!Directory.Exists("./db"))
                {
                    Directory.CreateDirectory("./db");
                    CreateDb();
                }

                if (!File.Exists("./db/map.db"))
                {
                    CreateDb();
                }

                _mDbConnection = new SqliteConnection(@"Data Source=./db/map.db");
                _mDbConnection.Open();
                dbIsOpen = true;
            }
            catch (Exception ex)
            {
                dbIsOpen = false;
                _eventLogger.Error(ex);
                throw;
            }
        }

        void CloseDb()
        {
            if (dbIsOpen)
            {
                _mDbConnection.Close();
            }
        }

        public string GetNext(Guid guid)
        {
            var nextNum = "1";

            if (string.IsNullOrEmpty(GetId(guid)))
            {
                var sql = @"select max(id) from map";
                using (var command = new SqliteCommand(sql, _mDbConnection))
                {
                    using (var rdr = command.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            if (rdr["max(id)"] != DBNull.Value)
                            {
                                nextNum = (Convert.ToUInt32(rdr["max(id)"]) + 1).ToString();
                            }
                        }
                    }
                }

                sql = @"replace into map (TimeStamp, guid, id) values (@TimeStamp, @guid, @id);";
                using (var command = new SqliteCommand(sql, _mDbConnection))
                {
                    command.Parameters.AddWithValue("@TimeStamp", DateTime.UtcNow.ToString());
                    command.Parameters.AddWithValue("@guid", guid.ToString());
                    command.Parameters.AddWithValue("@id", nextNum);
                    var val = command.ExecuteNonQuery();
                }
            }

            return nextNum;
        }

        // If the id exists, return tthe Guid
        public Guid GetGuid(string id)
        {
            const string sql = @"select guid from map where id = @id";
            using (var command = new SqliteCommand(sql, _mDbConnection))
            {
                command.Parameters.AddWithValue("@id", id);

                using (var rdr = command.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        if (rdr["guid"] != DBNull.Value)
                        {
                            return new Guid(rdr["guid"].ToString());
                        }
                    }
                }
            }

            // Look up the string and return the Guid
            return new Guid("0");
        }

        // If the Guid exists, return the 10 char string, else generate a new mapping
        public string GetId(Guid guid)
        {
            var sql = @"select id from map where guid = @guid";
            using (var command = new SqliteCommand(sql, _mDbConnection))
            {
                command.Parameters.AddWithValue("@guid", guid.ToString());

                using (var rdr = command.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        if (rdr["id"] != DBNull.Value)
                        {
                            return rdr["id"].ToString();
                        }
                    }
                }
            }

            return null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                CloseDb();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
