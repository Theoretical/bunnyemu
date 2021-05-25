using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Hare
{
    public class Pair<T1, T2>
    {
        public T1 First;
        public T2 Second;

        public Pair(T1 first, T2 second)
        {
            First = first;
            Second = second;
        }
    }

    class UdpServer
    {
        private static Socket ListenSocket;
        private static LockFreeQueue<Pair<IPEndPoint, PacketReader>> UdpReceiveQueue = new LockFreeQueue<Pair<IPEndPoint, PacketReader>>();
        private static byte[] UdpBuffer = new byte[4096];

        public static bool Initialize()
        {

            try
            {
                ListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                ListenSocket.Bind(new IPEndPoint(IPAddress.Any, Globals.Configuration.Locator.Port));
                if (Type.GetType("Mono.Runtime") == null)
                    ListenSocket.IOControl(-1744830452, new byte[] { Convert.ToByte(false) }, null);

                EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                ListenSocket.BeginReceiveFrom(UdpBuffer, 0, UdpBuffer.Length, SocketFlags.None, ref ep, OnUDPRecv, null);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void OnUDPRecv(IAsyncResult iResult)
        {
            EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
            int nRecv = ListenSocket.EndReceiveFrom(iResult, ref ep);
            if (nRecv > 6)
            {
                UInt16 nTotal = BitConverter.ToUInt16(UdpBuffer, 2);
                UInt16 operation = BitConverter.ToUInt16(UdpBuffer, 8);
                if (nRecv >= nTotal)
                {
                    UdpReceiveQueue.Enqueue(new Pair<IPEndPoint, PacketReader>((IPEndPoint)ep, new PacketReader(UdpBuffer, nTotal)));
                    ThreadPool.QueueUserWorkItem(ProcessUdp);
                }
            }
            ep = new IPEndPoint(IPAddress.Any, 0);
            ListenSocket.BeginReceiveFrom(UdpBuffer, 0, UdpBuffer.Length, SocketFlags.None, ref ep, OnUDPRecv, null);

        }

        private static void ProcessUdp(object o)
        {
            try
            {
                while (UdpReceiveQueue.Count > 0)
                {
                    IPEndPoint ep = null;
                    PacketReader packet = null;
                    Pair<IPEndPoint, PacketReader> next = UdpReceiveQueue.Dequeue();
                    ep = next.First;
                    packet = next.Second;
                    
                    var buffer = CreateServerList();
                    ListenSocket.SendTo(buffer, 0, buffer.Length, SocketFlags.None, ep);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Error: {0}", e.Message);
            }
        }

        private static byte[] CreateServerList()
        {
            var ipAddress = Globals.Configuration.Locator.Ip.Split(".".ToCharArray());
            var serverType = 0;

            switch (Globals.Configuration.Server.Mode.ToLower())
            {
                case "match":
                    serverType = 1;
                    break;
                case "clan":
                    serverType = 2;
                    break;
                case "test":
                    serverType = 3;
                    break;
                case "developer":
                    serverType = 4;
                    break;
                default:
                    serverType = 2;
                    break;

            }
            var packetWriter = new PacketWriter(0x9C42, 0x64);
            packetWriter.Write(1, 15);
            packetWriter.Write(byte.Parse(ipAddress[0]));
            packetWriter.Write(byte.Parse(ipAddress[1]));
            packetWriter.Write(byte.Parse(ipAddress[2]));
            packetWriter.Write(byte.Parse(ipAddress[3]));
            packetWriter.Write((Int32)Globals.Configuration.Tcp.Port);
            packetWriter.Write((byte)serverType);
            packetWriter.Write(Globals.Configuration.Server.Capacity);
            packetWriter.Write((short)0);
            packetWriter.Write((byte)serverType);
            packetWriter.Write((byte)1);
            //packetWriter.Write(Globals.Configuration.Server.Name, 64);
            return packetWriter.Process(1, new byte[32]);
        }
    }
}