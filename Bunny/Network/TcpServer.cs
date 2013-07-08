using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Bunny.Core;
using Bunny.Packet;
using Bunny.Utility;

namespace Bunny.Network
{
    class TcpServer
    {
        public static List<Client> Clients = new List<Client>();
        private static Socket _listener;
        private static MuidWrapper Muids = new MuidWrapper();
        private static object _objectLock = new object();

        public static void Remove(Client client)
        {
            lock (_objectLock)
            {
                Clients.Remove(client);
            }
        }
        private static void HandleAccept(IAsyncResult pResult)
        {
            try
            {
                lock (_objectLock)
                {
                    Clients.Add(new Client(_listener.EndAccept(pResult), Muids.GetNext()));
                }

            }
            catch (Exception e)
            {
                Log.Write("Error: {0}", e.Message);
            }
            _listener.BeginAccept(new AsyncCallback(HandleAccept), null);
        }

        public static void  GlobalPacket (PacketWriter packet)
        {
            lock (Clients)
                Clients.ForEach(c => c.Send(packet));
        }

        public static Client GetClientFromUid(Muid uidClient)
        {
            lock (Clients)
                return Clients.Find(c => c.GetMuid() == uidClient);
        }

        public static Client GetClientFromAid(int aid)
        {
            lock (Clients)
                return Clients.Find(c => c.ClientPlayer.PlayerAccount.AccountId == aid);
        }

        public static Client GetClientFromName(string name)
        {
            lock (Clients)
                return Clients.Find(c => c.GetCharacter() != null && c.GetCharacter().Name.ToLower().Equals(name.ToLower()));
        }

        public static List<Client> GetClanMembers (Int32 clanId)
        {
            lock (Clients)
                return Clients.FindAll(c => c.GetCharacter().ClanId == clanId && c.ClientPlayer.PlayerAccount.AccountId != 0).ToList();
        }

        public static bool Initialize()
        {
            try
            {
                _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _listener.Bind(new IPEndPoint(IPAddress.Any, Globals.Config.Tcp.Port));
                _listener.Listen(Globals.Config.Tcp.BackLog);
                _listener.BeginAccept(new AsyncCallback(HandleAccept), null);
                
                Clients = new List<Client>();
                Log.Write("TCP Server Iniitialized.");
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static bool IsRunning()
        {
            return _listener.IsBound;
        }
    }
}
