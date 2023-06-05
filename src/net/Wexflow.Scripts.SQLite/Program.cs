﻿using System;
using System.Configuration;
using System.IO;
using Wexflow.Core.Db.SQLite;

namespace Wexflow.Scripts.SQLite
{
    class Program
    {
        static void Main()
        {
            try
            {
                Db db = new Db(ConfigurationManager.AppSettings["connectionString"]);
                Core.Helper.InsertWorkflowsAndUser(db);
                Core.Helper.InsertRecords(db, "sqlite");
                db.Dispose();

                bool.TryParse(ConfigurationManager.AppSettings["buildDevDatabase"], out bool buildDevDatabase);

                if (buildDevDatabase)
                {
                    BuildDatabase("Windows");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occured: {0}", e);
            }

            Console.Write("Press any key to exit...");
            Console.ReadKey();
        }

        private static void BuildDatabase(string info)
        {
            Console.WriteLine($"=== Build {info} database ===");
            string path1 = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..",
                "samples", "net", "Wexflow", "Database", "Wexflow.sqlite");

            string connString = $"Data Source={path1};Version=3;";

            if (File.Exists(path1)) File.Delete(path1);

            Db db = new Db(connString);
            Core.Helper.InsertWorkflowsAndUser(db);
            Core.Helper.InsertRecords(db, "sqlite");
            db.Dispose();
        }
    }
}
