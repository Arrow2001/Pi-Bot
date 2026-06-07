using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PiBot.Handlers
{
    public class DatabaseHandler
    {
        // got to add a function here that can used in other areas so I'm not repeating myself.

        public static SqliteConnection getConnection()
        {
            // file path to database, should look into trying to host it on a server or something just to see if i can
            string dbFile = $"Data source={Config.bot.databaseFilePath}";
            var connection = new SqliteConnection(dbFile);
            connection.Open();
            return connection;
        }
    }
}