using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SQLite;

namespace DBot.Source
{
    public class DbService
    {
        private SQLiteConnection _conn;

        public SQLiteConnection Conn
        {
            get
            {
                return this._conn;
            }
        }

        public DbService()
        {
            this._conn = new SQLiteConnection(Support.DbConnectionString);
            //maybe set Timeouts
        }
    }
}
