using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BackupTool
{
    public static class CBackup
    {
        private struct dbFile
        {
            public dbFile(long _Id, string _Path, string _Hash)
            {
                Id = _Id;
                Path = _Path;
                Hash = _Hash;
            }

            public long Id { get; set; }
            public string Path { get; set; }
            public string Hash { get; set; }
        }

        public static string doBackup(string _strSourcePath, string _strDestinationPath)
        {

            if (Directory.Exists(_strSourcePath))
            {
                if (Directory.Exists(_strDestinationPath))
                {
                    SQLiteConnection db;
                    if (File.Exists(_strDestinationPath + "\\.~hashes.sqlite3"))
                    {
                        db = new SQLiteConnection("Data Source=" + _strDestinationPath + "\\.~hashes.sqlite3;Version=3;");
                        db.Open();
                    }
                    else
                    {
                        SQLiteConnection.CreateFile(_strDestinationPath + "\\.~hashes.sqlite3");
                        db = new SQLiteConnection("Data Source=" + _strDestinationPath + "\\.~hashes.sqlite3;Version=3;");
                        db.Open();

                        string SqlCreateTable = "CREATE TABLE Files (FileId INTEGER PRIMARY KEY AUTOINCREMENT, FilePath TEXT, FileHash TEXT)";
                        SQLiteCommand cmdCreateTable = new SQLiteCommand(SqlCreateTable, db);
                        cmdCreateTable.ExecuteNonQuery();
                    }

                    string SqlSelectFiles = "SELECT * FROM Files";
                    SQLiteCommand cmdSelectFiles = new SQLiteCommand(SqlSelectFiles, db);
                    SQLiteDataReader dbFiles = cmdSelectFiles.ExecuteReader();

                    List<dbFile> lstdbFiles = new List<dbFile>();
                    while (dbFiles.Read())
                    {
                        lstdbFiles.Add(new dbFile((long)dbFiles["FileId"], (string)dbFiles["FilePath"], (string)dbFiles["FileHash"]));
                    }
                    lstdbFiles = lstdbFiles.OrderBy(c => c.Path).ToList();

                    List<string> lstFiles = new List<string>();
                    DirSearch(_strSourcePath, ref lstFiles);
                    lstFiles = lstFiles.OrderBy(c => c).ToList();

                    int currFirstId = 0;
                    foreach (dbFile dbfile in lstdbFiles)
                    {
                        for (int i = currFirstId; i < lstFiles.Count; i++)
                        {
                            if (dbfile.Path == MakeRelative(lstFiles[i], _strSourcePath + "\\"))
                            {
                                if (dbfile.Hash != GetFileHash(lstFiles[i]))
                                {
                                    File.Copy(lstFiles[i], _strDestinationPath + "\\" + dbfile.Path, true);

                                    string SqlUpdateFile = string.Format("UPDATE Files SET FileHash = '{0}' WHERE FileId = {1}", GetFileHash(lstFiles[i]), dbfile.Id);
                                    SQLiteCommand cmdUpdateFile = new SQLiteCommand(SqlUpdateFile, db);
                                    cmdUpdateFile.ExecuteNonQuery();
                                }
                                lstFiles.RemoveAt(i);
                                currFirstId = i;
                                break;
                            }

                            if (!(currFirstId + 1 < lstFiles.Count))
                            {
                                File.Delete(_strDestinationPath + "\\" + dbfile.Path);

                                string SqlDeleteFile = "DELETE FROM Files WHERE FileId = " + dbfile.Id;
                                SQLiteCommand cmdDeleteFile = new SQLiteCommand(SqlDeleteFile, db);
                                cmdDeleteFile.ExecuteNonQuery();
                            }
                        }
                    }

                    foreach (string file in lstFiles)
                    {
                        string relativePath = MakeRelative(file, _strSourcePath + "\\");

                        File.Copy(file, _strDestinationPath + "\\" + relativePath);

                        string SqlInsertFile = string.Format("INSERT INTO Files (FilePath, FileHash) VALUES ('{0}', '{1}')", relativePath, GetFileHash(file));
                        SQLiteCommand cmdInsertFile = new SQLiteCommand(SqlInsertFile, db);
                        cmdInsertFile.ExecuteNonQuery();
                    }

                    db.Close();

                    return "Opération réussie.";
                }
                else
                {
                    return "Le dossier de destination est introuvable.";
                }
            }
            else
            {
                return "Le dossier source est introuvable.";
            }
        }

        private static void DirSearch(string _rootDir, ref List<string> _lstPaths)
        {
            if (_lstPaths != null)
            {
                foreach (string dir in Directory.GetDirectories(_rootDir))
                {
                    DirSearch(dir, ref _lstPaths);
                }
                foreach (string file in Directory.GetFiles(_rootDir))
                {
                    if (file != ".~hashes.sqlite3")
                        _lstPaths.Add(file);
                }
            }
        }

        private static string MakeRelative(string _filePath, string _referencePath)
        {
            Uri fileUri = new Uri(_filePath);
            Uri referenceUri = new Uri(_referencePath);
            return referenceUri.MakeRelativeUri(fileUri).ToString();
        }

        private static string GetFileHash(string _fileName)
        {
            HashAlgorithm sha1 = HashAlgorithm.Create();
            using (FileStream stream = new FileStream(_fileName, FileMode.Open, FileAccess.Read))
                return Encoding.Default.GetString(sha1.ComputeHash(stream));
        }
    }
}
