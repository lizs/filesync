using System;
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
                foreach (var kv in kvs)
                {
                    Console.WriteLine("[remote]{0}:{1}", kv.Key, kv.Value);
                }
            };

            client.OnLocalMd5 += (kvs) =>
            {
                foreach (var kv in kvs)
                {
                    Console.WriteLine("[local]{0}:{1}", kv.Key, kv.Value);
                }
            };

            client.OnDiff += diff => diff.ForEach(x => Console.WriteLine("{0} expired", x));
            client.OnFileDel += s => Console.WriteLine("{0} deleted", s);
            client.OnFileCreate += s => Console.WriteLine("{0} created", s);
            client.OnFolderDel += s => Console.WriteLine("{0} deleted", s);

            client.Sync((b, msg) => Console.WriteLine("Sync {0}", msg));

            Console.ReadLine();
        }
    }
}
