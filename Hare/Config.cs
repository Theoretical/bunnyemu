using System;
using System.IO;
using System.Xml.Serialization;

namespace Hare
{
    public class Configuration
    {
        public class DatabaseConfig
        {
            public string Host;
            public string DatabaseName;
            public bool WindowsAuth;
            public string User;
            public string Pass;
        }

        public class TcpConfig
        {
            public UInt16 Port;
            public int BackLog;
            public int ReceiveBuffer;
            public int SendBuffer;
        }

        public class UdpConfig
        {
            public short Port;
            public int Buffer;
        }

        public class LocatorConfig
        {
            public string Ip;
            public short Port;
        }

        public class AgentConfig
        {
            public string Ip;
            public short TcpPort;
            public short UdpPort;
        }

        public class ServerConfig
        {
            public byte Id;
            public short Capacity;
            public string Mode;
            public bool Survival;
            public bool DuelTourney;
            public string Name;
            public bool UseMd5;
        }

        public class ClientConfig
        {
            public int Version;
            public bool UseCrc;
            public uint FileList;
        }

        public class PingConfig
        {
            public int Delay;
            public int Timeout;
        }

        public class CharacterConfig
        {
            public int StartingBounty;
            public int MaxItems;
            public bool EquipSameItems;
        }

        public class ItemsConfig
        {
            public int MaxWeight;
            public bool UseBounty;
        }

        public static Configuration Load()
        {
            return (Configuration)new XmlSerializer(typeof(Configuration)).Deserialize(File.OpenText("Config.xml"));
        }

        public DatabaseConfig Database;
        public TcpConfig Tcp;
        public UdpConfig Udp;
        public LocatorConfig Locator;
        public AgentConfig Agent;
        public ServerConfig Server;
        public ClientConfig Client;
        public PingConfig Ping;
        public CharacterConfig Character;
        public ItemsConfig Items;
    }
}
