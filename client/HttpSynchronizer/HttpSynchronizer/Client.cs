﻿using System;
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
        public event Action<Dictionary<string, Dictionary<string, string>>> OnRemoteMd5;
        public event Action<Dictionary<string, Dictionary<string, string>>> OnLocalMd5;
        public event Action<string> OnFileCreate;
        public event Action<string> OnFileDel;
        public event Action<string> OnFolderDel;
        public event Action<List<KeyValuePair<string, int>>> OnDiff; 

        public string Url { get; private set; }
        public string RemoteMd5Path { get; private set; }
        public string LocalPath { get; private set; }

        private readonly Dictionary<string, Dictionary<string, string>> _localMd5Map =
            new Dictionary<string, Dictionary<string, string>>();

        private readonly Dictionary<string, Dictionary<string, string>> _remoteMd5Map =
            new Dictionary<string, Dictionary<string, string>>(); 

        public Client(string url, string remoteMd5Path, string localPath)
        {
            Url = url;
            RemoteMd5Path = Conver2SafePath(remoteMd5Path);
            LocalPath = Conver2SafePath(localPath);

            if (!Directory.Exists(localPath))
                Directory.CreateDirectory(localPath);
        }

        public void Sync(Action<bool, string> cb)
        {
            try
            {
                // scan local md5
                ScanLocalMd5();

                // get md5
                using (var httpClient = new WebClient())
                {
                    httpClient.DownloadStringCompleted += (sender, args) =>
                    {
                        if (args.Error != null)
                        {
                            var msg = MsgFromException(args.Error);
                            Console.WriteLine(msg);
                            cb(false, msg);
                            return;
                        }

                        // parse md5 json
                        ParseRemoteMd5(args.Result);

                        // files
                        var differences = Differences().ToList();
                        if (OnDiff != null)
                            OnDiff(differences);

                        // download
                        DownLoad(differences.Select(x=>x.Key));

                        // del unusable files
                        Clear();

                        cb(true, "ok");
                    };
                    httpClient.DownloadStringAsync(new Uri(Url + RemoteMd5Path));
                }
            }
            catch (Exception e)
            {
                cb(false, MsgFromException(e));
            }
        }

        private void Clear()
        {
            // rescan md5
            ScanLocalMd5();

            // delete expired files
            DeleteExpiredFiles();

            // remove directory
            DeleteEmptyFolders(LocalPath);
        }

        private void DeleteExpiredFiles()
        {
            var unused = (from kv in _localMd5Map
                where !_remoteMd5Map.ContainsKey(kv.Key)
                select LocalPath + kv.Key).ToList();

            // remove file
            foreach (var file in unused.Where(File.Exists))
            {
                File.Delete(file);

                if (OnFileDel != null)
                    OnFileDel(file);
            }
        }

        private void DeleteEmptyFolders(string root)
        {
            foreach (var directory in Directory.GetDirectories(root))
            {
                DeleteEmptyFolders(directory);
                if (Directory.GetFiles(directory).Length != 0 || Directory.GetDirectories(directory).Length != 0)
                    continue;

                Directory.Delete(directory, false);
                if (OnFolderDel != null)
                    OnFolderDel(directory);
            }
        }

        private void DownLoad(IEnumerable<string> files)
        {
            using (var downloader = new WebClient())
            {
                foreach (var path in files)
               { 
                    var drinfo = new DirectoryInfo(LocalPath + path);
                    if (drinfo.Parent != null && !Directory.Exists(drinfo.Parent.FullName))
                        Directory.CreateDirectory(drinfo.Parent.FullName);

                    try
                    {
                        downloader.DownloadFile(new Uri(Url + path), LocalPath + path);

                        if (OnFileCreate != null)
                            OnFileCreate(LocalPath + path);
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
        private IEnumerable<KeyValuePair<string, int>> Differences()
        {
            return (from kv in _remoteMd5Map
                where !_localMd5Map.ContainsKey(kv.Key) || _remoteMd5Map[kv.Key]["md5"] != _localMd5Map[kv.Key]["md5"]
                    select new KeyValuePair<string, int>(kv.Key, int.Parse(_remoteMd5Map[kv.Key]["size"])));
        }

        private void ParseRemoteMd5(string jsonText)
        {
            _remoteMd5Map.Clear();

            var data = JsonMapper.ToObject(jsonText);
            foreach (var key in ((IDictionary)data).Keys)
            {
                var k = (string)key;
                var dic = (IDictionary)data[k];
                _remoteMd5Map[k] = new Dictionary<string, string>
                {
                    {"md5", dic["md5"].ToString()},
                    {"size", dic["size"].ToString()}
                };
            }

            if (OnRemoteMd5 != null)
                OnRemoteMd5(_remoteMd5Map);
        }

        private void ScanLocalMd5()
        {
            _localMd5Map.Clear();
            var dir = new DirectoryInfo(LocalPath);

            using (var md5 = MD5.Create())
            {
                foreach (var fi in dir.GetFiles("*.*", SearchOption.AllDirectories))
                {
                    var safePath = Conver2SafePath(fi.FullName);
                    var refPath = GetRefPath(safePath, LocalPath);
                    if(string.IsNullOrEmpty(refPath)) continue;

                    var data = File.ReadAllBytes(fi.FullName);
                    _localMd5Map[refPath] = new Dictionary<string, string>
                    {
                        {"md5", GetMd5Hash(md5, data)},
                        {"size", data.Length.ToString()}
                    };
                }

                if (OnLocalMd5 != null)
                    OnLocalMd5(_localMd5Map);
            }
        }

        private static string GetMd5Hash(MD5 md5Hash, byte[] input)
        {
            // Convert the data string to a byte array and compute the hash.
            var data = md5Hash.ComputeHash(input);

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

        private static string MsgFromException(Exception e)
        {
            return e.InnerException != null
                ? e.InnerException.Message
                : e.Message;
        }

        private static string Conver2SafePath(string path)
        {
            return path.Replace('\\', '/');
        }

        private static string GetRefPath(string path, string root)
        {
            var idx = path.IndexOf(root, StringComparison.Ordinal);
            if (idx == -1) return string.Empty;

            return path.Substring(idx + root.Length);
        }

    }
}
