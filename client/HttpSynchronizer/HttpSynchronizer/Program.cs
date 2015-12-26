
using System;

namespace HttpSynchronizer
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new Client("http://localhost:8080/", "md5", "./downloaded");
            client.Sync(b => { Console.WriteLine("Sync success ? " + b.ToString()); });

            Console.ReadLine();
        }
    }
}
