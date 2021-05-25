using System;
using Bunny.Network;
using Bunny.Packet;
using Bunny.Packet.Disassemble;
using Bunny.Stages;
using Bunny.Utility;
using Bunny.Channels;
using Bunny.Items;

namespace Bunny.Core
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                //Console.BufferWidth = Console.WindowWidth = 128;
                //Console.Title = "Bunny Emu";

                Globals.Config = Configuration.Load();
                Log.Initialize();
                Log.Write("{0}", DateTime.Now.Ticks);
                Globals.GunzDatabase = new MySQLDatabase();

                if (!Globals.GunzDatabase.Initialize())
                {
                    Log.Write("Failed to connect to database!\nPress Enter to exit!");
                    Console.ReadLine();
                    return;
                }

                ExpManager.Load();
                ChannelList.Load();
                ItemList.Load();
                ExpManager.Load();
                WorldItemManager.Load();

                Manager.InitializeHandlers<Login>();
                Manager.InitializeHandlers<ItemHandler>();
                Manager.InitializeHandlers<ChannelHandler>();
                Manager.InitializeHandlers<StageHandler>();
                Manager.InitializeHandlers<Agent>();
                Manager.InitializeHandlers<Clan>();
                Manager.InitializeHandlers<Misc>();

                if (!TcpServer.Initialize())
                {
                    Log.Write("Failed to create server!\nPress Enter to exit!");
                    Console.ReadLine();
                    return;
                }

                if (!UdpServer.Initialize())
                {
                    Log.Write("Failed to create udp server!\nPress Enter to exit!");
                    Console.ReadLine();
                    return;
                }

                EventManager.Initialize();
                
                Globals.Maps = new MapManager();
                Globals.Maps.LoadMaps();

                Log.Write("Bunny is ready to hop on port: {0}", Globals.Config.Tcp.Port);

                while (true)
                {
                    System.Threading.Thread.Sleep(1);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Error during initialization: {0}", e);
            }
        }

    }
}
