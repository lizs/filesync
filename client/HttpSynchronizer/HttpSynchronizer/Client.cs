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
            ScanMd5();

            try
            {
                // get md5
                using (var httpClient = new WebClient())
                {
                    httpClient.DownloadStringCompleted += (sender, args) =>
                    {
                        // parse md5 json
                        ParseRemoteMd5(args.Result);

                        // differences
                        var differences = Differences();

                        // download
                        using (var downloader = new WebClient())
                        {
                            foreach (var path in differences)
                            {
                                var drinfo = new DirectoryInfo(path);
                                if (Directory.Exists(drinfo.FullName))
                                    Directory.CreateDirectory(drinfo.FullName);

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

        private void ScanMd5()
        {
            var dic = new DirectoryInfo(LocalPath);
            try
            {
                using (var md5 = MD5.Create())
                {
                    foreach (var fi in dic.EnumerateFiles())
                    {
                        var text = File.ReadAllText(fi.FullName);
                        _localMd5Map[fi.FullName] = GetMd5Hash(md5, text);
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
