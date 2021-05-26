using System;
using System.IO;
using System.Xml.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Bunny
{
    public class Database
    {
        public string Host { get; set; }
        public string DatabaseName { get; set; }
        public bool WindowsAuth { get; set; }
        public string User { get; set; }
        public string Pass { get; set; }
    }

    public class Tcp
    {
        public short Port { get; set; }
        public int BackLog;
        public int ReceiveBuffer { get; set; }
        public int SendBuffer { get; set; }
    }

    public class Udp
    {
        public short Port { get; set; }
        public int Buffer { get; set; }
    }

    public class Locator
    {
        public string Ip { get; set; }
        public short Port { get; set; }
    }

    public class Agent
    {
        public string Ip { get; set; }
        public string RemoteIp { get; set; }
        public short TcpPort { get; set; }
        public short UdpPort { get; set; }
    }

    public class Server
    {
        public byte Id { get; set; }
        public short Capacity { get; set; }
        public string Mode { get; set; }
        public string Name { get; set; }
        public bool UseMd5 { get; set; }
        public bool AutoRegistration { get; set; }
    }

    public class ClientConfig
    {
        public int Version { get; set; }
        public bool UseCrc { get; set; }
        public uint FileList { get; set; }
    }

    public class Character
    {
        public int StartingBounty { get; set; }
        public int MaxItems { get; set; }
        public bool EquipSameItems { get; set; }
    }

    public class ItemsConfig
    {
        public int MaxWeight { get; set; }
        public bool UseBounty { get; set; }
    }

    public class Config
    {
        public Database Database { get; set; }
        public Tcp Tcp { get; set; }
        public Udp Udp { get; set; }
        public Locator Locator { get; set; }
        public Agent Agent { get; set; }
        public Server Server { get; set; }
        
        [YamlMember(Alias = "client", ApplyNamingConventions = false)]
        public ClientConfig Client { get; set; }
        public Character Character { get; set; }

        [YamlMember(Alias = "items", ApplyNamingConventions = false)]
        public ItemsConfig Items { get; set; }

        public static Config Load()
        {
            using (var reader = new StreamReader("config.yaml"))
            {
                var contents = reader.ReadToEnd();
                var deserializer = new DeserializerBuilder()
                  .WithNamingConvention(CamelCaseNamingConvention.Instance)
                  .Build();
                return deserializer.Deserialize<Config>(contents);
            }
        }

    }
}
