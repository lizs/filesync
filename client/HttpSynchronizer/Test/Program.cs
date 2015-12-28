using System;
using System.Linq;
using HttpSynchronizer;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new Client("http://localhost:8080/", @"md5", @"downloaded/");
            client.OnRemoteMd5 += (kvs) =>
            {
                Console.WriteLine("============remote md5==============");
                foreach (var kv in kvs)
                {
                    Console.WriteLine("[remote]{0}:{1}:{2}", kv.Key, kv.Value["md5"], kv.Value["size"]);
                }
            };

            client.OnLocalMd5 += (kvs) =>
            {
                Console.WriteLine("============local md5==============");
                foreach (var kv in kvs)
                {
                    Console.WriteLine("[remote]{0}:{1}:{2}", kv.Key, kv.Value["md5"], kv.Value["size"]);
                }
            };

            client.OnDiff += diff =>
            {
                Console.WriteLine("============expired files==============");
                var totalSize = diff.Sum(x => x.Value);
                Console.WriteLine("Download size : {0} KB", totalSize/1024.0f);
                diff.ForEach(x => Console.WriteLine("{0} expired", x));
            };

            client.OnFileDel += s => Console.WriteLine("{0} deleted", s);
            client.OnFileCreate += s => Console.WriteLine("{0} created", s);
            client.OnFolderDel += s => Console.WriteLine("{0} deleted", s);

            client.Sync((b, msg) => Console.WriteLine("Sync {0}", msg));

            Console.ReadLine();
        }
    }
}
