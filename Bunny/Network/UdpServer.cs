using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Bunny.Core;
using Bunny.Enums;
using Bunny.Packet;
using Bunny.Packet.Assembled;
using Bunny.Utility;

namespace Bunny.Network
{
    class UdpServer
    {
        private static readonly LockFreeQueue<Pair<IPEndPoint, PacketReader>> _udpReceiveQueue = new LockFreeQueue<Pair<IPEndPoint, PacketReader>>();
        private static Socket _listenSocket;
        private static readonly byte[] _udpBuffer = new byte[Globals.Config.Udp.Buffer];

        public static bool Initialize()
        {
            try
            {
                EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _listenSocket.Bind(new IPEndPoint(IPAddress.Any, Globals.Config.Udp.Port));

                if (Type.GetType("Mono.Runtime") == null)
                    _listenSocket.IOControl(-1744830452, new byte[] { Convert.ToByte(false) }, null);

                _listenSocket.BeginReceiveFrom(_udpBuffer, 0, _udpBuffer.Length, SocketFlags.None, ref ep, RecieveFromCallback, null);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void RecieveFromCallback(IAsyncResult iResult)
        {
            EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
            int nRecv = _listenSocket.EndReceiveFrom(iResult, ref ep);

            if (nRecv > 11)
            {
                UInt16 nTotal = BitConverter.ToUInt16(_udpBuffer, 2);
                var operation = (Operation)BitConverter.ToUInt16(_udpBuffer, 8);
                if (nRecv >= nTotal)
                {
                    _udpReceiveQueue.Enqueue(new Pair<IPEndPoint, PacketReader>((IPEndPoint)ep, new PacketReader(_udpBuffer, nTotal)));
                    ThreadPool.QueueUserWorkItem(ProcessQueue);
                }
            }
            ep = new IPEndPoint(IPAddress.Any, 0);
            _listenSocket.BeginReceiveFrom(_udpBuffer, 0, _udpBuffer.Length, SocketFlags.None, ref ep, RecieveFromCallback, null);
        }

        private static void ProcessQueue(object o)
        {
            try
            {
                while (_udpReceiveQueue.Count > 0)
                {
                    IPEndPoint ep;
                    PacketReader packet;
                    var next = _udpReceiveQueue.Dequeue();
                    ep = next.First;
                    packet = next.Second;

                    if (packet.GetOpcode() == Operation.BridgeRequest)
                    {
                        var uid = packet.ReadMuid();
                        var ip = packet.ReadBytes(4);
                        var port = packet.ReadInt32();

                        Client client = TcpServer.GetClientFromUid(uid);
                        if (client == null)
                        {
                            Log.Write("Invalid Client: {0} {1} {2}", uid.HighId, BitConverter.ToString(ip), port);
                            return;
                        }

                        client.PeerEnd = ep;
                        Match.ResponseBridgePeer(client);
                    }
                }
            }
            catch
            {
                Log.Write("Error during udp queue");
            }
        }

    }
}
