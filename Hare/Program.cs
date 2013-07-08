using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hare
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.BufferWidth = Console.WindowWidth = 128;
            //Console.Title = "Hare Login Server";
            Globals.Configuration = Configuration.Load();

            UdpServer.Initialize();
            Console.WriteLine("Locator Initialized. Listening on: {0}:{1}", Globals.Configuration.Locator.Ip, Globals.Configuration.Locator.Port);
            while (true)
            {
                System.Threading.Thread.Sleep(1);
            }
        }
    }
}
