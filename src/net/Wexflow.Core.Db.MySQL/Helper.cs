﻿using MySqlConnector;

namespace Wexflow.Core.Db.MySQL
{
    public class Helper
    {
        private readonly string _connectionString;

        public Helper(string connectionString)
        {
            _connectionString = connectionString;
        }

        public static void CreateDatabaseIfNotExists(string server, string userId, string password, string databaseName, int port)
        {
            using (MySqlConnection conn = new MySqlConnection("Server=" + server + ";Uid=" + userId + ";Pwd=" + password + ";" + "Port=" + port))
            {
                conn.Open();

                using (MySqlCommand command = new MySqlCommand("CREATE DATABASE IF NOT EXISTS " + databaseName + ";", conn))
                {

                    command.ExecuteNonQuery();
                }

            }
        }

        public void CreateTableIfNotExists(string tableName, string tableStruct)
        {
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();

                using (MySqlCommand command = new MySqlCommand("CREATE TABLE IF NOT EXISTS " + tableName + tableStruct + ";", conn))
                {

                    command.ExecuteNonQuery();
                }
            }
        }

    }
}
