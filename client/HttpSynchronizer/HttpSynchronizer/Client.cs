using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using LitJson;

namespace HttpSynchronizer
{
    public class Client
    {
        public string Url { get; private set; }
        public string RemoteMd5Path { get; private set; }
        public string LocalPath { get; private set; }

        private readonly Dictionary<string, string> _localMd5Map = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _remoteMd5Map = new Dictionary<string, string>(); 

        public Client(string url, string remoteMd5Path, string localPath)
        {
            Url = url;
            RemoteMd5Path = remoteMd5Path;
            LocalPath = localPath;

            if (!Directory.Exists(localPath))
            {
                Directory.CreateDirectory(localPath);
            }
        }

        public void Sync(Action<bool> cb)
        {
            // scan local md5
            ScanLocalMd5();

            try
            {
                // get md5
                using (var httpClient = new WebClient())
                {
                    httpClient.DownloadStringCompleted += (sender, args) =>
                    {
                        if (args.Error != null)
                        {
                            Console.WriteLine(args.Error.InnerException != null ? args.Error.InnerException.Message : args.Error.Message);
                            cb(false);
                            return;
                        }

                        // parse md5 json
                        ParseRemoteMd5(args.Result);

                        // files
                        var differences = Differences();

                        // download
                        DownLoad(differences);

                        // del unusable files
                        Clear();

                        cb(true);
                    };
                    httpClient.DownloadStringAsync(new Uri(Url + RemoteMd5Path));
                }
            }
            catch (Exception e)
            {
                cb(false);
            }
        }

        private void Clear()
        {
            // rescan md5
            ReScanLocalMd5();

            // delete expired files
            //DeleteExpiredFiles();

            // remove directory
            //DeleteEmptyFolders(LocalPath);
        }

        private void DeleteExpiredFiles()
        {
            var unused = (from kv in _localMd5Map
                where !_remoteMd5Map.ContainsKey(kv.Key)
                select kv.Key).ToList();

            // remove file
            foreach (var file in unused.Where(File.Exists))
            {
                File.Delete(file);
            }
        }

        private void ReScanLocalMd5()
        {
            _localMd5Map.Clear();
            ScanLocalMd5();
        }

        private void DeleteEmptyFolders(string root)
        {
            foreach (var directory in Directory.GetDirectories(root))
            {
                DeleteEmptyFolders(directory);
                if (Directory.GetFiles(directory).Length == 0 &&
                    Directory.GetDirectories(directory).Length == 0)
                {
                    Directory.Delete(directory, false);
                }
            }
        }

        private void DownLoad(IEnumerable<string> files)
        {
            using (var downloader = new WebClient())
            {
                foreach (var path in files)
               { 
                    var drinfo = new DirectoryInfo(LocalPath + path);
                    if (!Directory.Exists(drinfo.Parent.FullName))
                        Directory.CreateDirectory(drinfo.Parent.FullName);

                    try
                    {
                        downloader.DownloadFile(new Uri(Url + path), LocalPath + path);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.InnerException != null ? e.InnerException.Message : e.Message);
                    }
                }
            }
        }

        /// <summary>
        ///     计算本地与远端差异
        /// </summary>
        /// <returns></returns>
        private IEnumerable<string> Differences()
        {
            return (from kv in _remoteMd5Map
                    where !_localMd5Map.ContainsKey(kv.Key) || _remoteMd5Map[kv.Key] != _localMd5Map[kv.Key] 
                    select kv.Key).ToList();
        }

        private void ParseRemoteMd5(string jsonText)
        {
            var data = (IDictionary)JsonMapper.ToObject(jsonText);
            foreach (var key in data.Keys)
            {
                _remoteMd5Map[(string)key] = data[key].ToString();
            }
        }

        private void ScanLocalMd5()
        {
            var dir = new DirectoryInfo(LocalPath);
            try
            {
                using (var md5 = MD5.Create())
                {
                    foreach (var fi in dir.EnumerateFiles("*.*", SearchOption.AllDirectories))
                    {
                        var text = File.ReadAllText(fi.FullName);

                        var safePath = fi.FullName.Replace('\\', '/');
                        var idx = safePath.LastIndexOf(LocalPath);
                        if (idx != -1)
                        {
                            var refPath = safePath.Substring(idx + LocalPath.Length);
                            _localMd5Map[refPath] = GetMd5Hash(md5, text);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static string GetMd5Hash(MD5 md5Hash, string input)
        {
            // Convert the input string to a byte array and compute the hash.
            var data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            var sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            foreach (var t in data)
            {
                sBuilder.Append(t.ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
    }
}
