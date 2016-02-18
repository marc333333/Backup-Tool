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

        public static int Current = 0;
        public static int Total = 0;
        public static bool Finish = false;
        public static string Message = "";

        public static void doBackup(string _strSourcePath, string _strDestinationPath)
        {
            Current = 0;
            Total = 0;
            Finish = false;
            Message = "";

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

                    Total = lstFiles.Count;

                    int currFirstId = 0;
                    foreach (dbFile dbfile in lstdbFiles)
                    {
                        if (currFirstId >= lstFiles.Count)
                        {
                            File.Delete(_strDestinationPath + "\\" + dbfile.Path);

                            SQLiteCommand SqlDeleteFile = new SQLiteCommand(db);
                            SqlDeleteFile.CommandText = "DELETE FROM Files WHERE FileId = ?";
                            SqlDeleteFile.Parameters.AddWithValue("param1", dbfile.Id);
                            SqlDeleteFile.ExecuteNonQuery();

                            continue;
                        }
                        for (int i = currFirstId; i < lstFiles.Count; i++)
                        {
                            if (dbfile.Path == MakeRelative(lstFiles[i], _strSourcePath + "\\"))
                            {
                                if (dbfile.Hash != GetFileHash(lstFiles[i]))
                                {
                                    File.Copy(lstFiles[i], _strDestinationPath + "\\" + dbfile.Path, true);

                                    SQLiteCommand SqlUpdateFile = new SQLiteCommand(db);
                                    SqlUpdateFile.CommandText = "UPDATE Files SET FileHash = ? WHERE FileId = ?";
                                    SqlUpdateFile.Parameters.AddWithValue("param1", GetFileHash(lstFiles[i]));
                                    SqlUpdateFile.Parameters.AddWithValue("param2", dbfile.Id);
                                    SqlUpdateFile.ExecuteNonQuery();
                                }
                                lstFiles.RemoveAt(i);
                                currFirstId = i;
                                break;
                            }

                            if (!(i + 1 < lstFiles.Count))
                            {
                                File.Delete(_strDestinationPath + "\\" + dbfile.Path);

                                SQLiteCommand SqlDeleteFile = new SQLiteCommand(db);
                                SqlDeleteFile.CommandText = "DELETE FROM Files WHERE FileId = ?";
                                SqlDeleteFile.Parameters.AddWithValue("param1", dbfile.Id);
                                SqlDeleteFile.ExecuteNonQuery();
                            }
                        }
                        Current = currFirstId;
                        Total = lstFiles.Count;
                    }

                    foreach (string file in lstFiles)
                    {
                        string relativePath = MakeRelative(file, _strSourcePath + "\\");

                        (new FileInfo(_strDestinationPath + "\\" + relativePath)).Directory.Create();
                        File.Copy(file, _strDestinationPath + "\\" + relativePath);

                        SQLiteCommand cmdInsertFile = new SQLiteCommand(db);
                        cmdInsertFile.CommandText = "INSERT INTO Files (FilePath, FileHash) VALUES (?, ?)";
                        cmdInsertFile.Parameters.AddWithValue("param1", relativePath);
                        cmdInsertFile.Parameters.AddWithValue("param2", GetFileHash(file));
                        cmdInsertFile.ExecuteNonQuery();

                        Current++;
                    }

                    db.Close();

                    DeleteEmptyDirectory(_strDestinationPath);

                    Message = "Opération réussie.";
                    Finish = true;
                }
                else
                {
                    Message = "Le dossier de destination est introuvable.";
                    Finish = true;
                }
            }
            else
            {
                Message = "Le dossier source est introuvable.";
                Finish = true;
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
            return Uri.UnescapeDataString(referenceUri.MakeRelativeUri(fileUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        private static string GetFileHash(string _fileName)
        {
            HashAlgorithm sha1 = HashAlgorithm.Create();
            using (FileStream stream = new FileStream(_fileName, FileMode.Open, FileAccess.Read))
                return Encoding.Default.GetString(sha1.ComputeHash(stream));
        }

        private static void DeleteEmptyDirectory(string startLocation)
        {
            foreach (var directory in Directory.GetDirectories(startLocation))
            {
                DeleteEmptyDirectory(directory);
                if (Directory.GetFileSystemEntries(directory).Length == 0)
                {
                    Directory.Delete(directory, false);
                }
            }
        }
    }
}
