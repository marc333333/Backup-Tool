using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace BackupTool
{
    public static class CBackup
    {
        public static bool doBackup(string _strSourcePath, string _strDestinationPath)
        {
            if (_strSourcePath.Last() == '/') _strSourcePath.Remove(_strSourcePath.Length - 1);
            if (_strDestinationPath.Last() == '/') _strDestinationPath.Remove(_strDestinationPath.Length - 1);

            if (Directory.Exists(_strSourcePath) && Directory.Exists(_strDestinationPath))
            {
                SQLiteConnection db;
                if (!File.Exists(_strDestinationPath + "/hashes.sqlite3"))
                {
                    SQLiteConnection.CreateFile(_strDestinationPath + "/");
                    db = new SQLiteConnection("Data Source=MyDatabase.sqlite;Version=3;");
                    db.Open();
                }
                else
                {
                    db = new SQLiteConnection("Data Source=MyDatabase.sqlite;Version=3;");
                    db.Open();
                }

                db.Clone();
                return true;
            }
            return false;
        }
    }
}
