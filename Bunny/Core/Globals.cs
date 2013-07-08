using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Bunny.Stages;
using Bunny.Utility;

namespace Bunny.Core
{
    class PendingClan
    {
        public string ClanName;
        public Client ClanMaster;
        public Int32 RequestId;
        public Pair<Client, bool> Member1;
        public Pair<Client, bool> Member2;
        public Pair<Client, bool> Member3;
        public Pair<Client, bool> Member4;
    }

    class PendingClanWarRequest
    {
        public Client Requester;
        public string ClanName;
        public Int32 RequestId;
        public Int32 RequestMode;
        public List<Pair<Client, bool>> Players = new List<Pair<Client, bool>>();
    }

    class Globals
    {
        public static Configuration Config;
        public static Regex AcceptedString = new Regex("[a-zA-Z0-9]{3,16}");
        public static MuidWrapper StageCounter = new MuidWrapper();
        public static MapManager Maps;
        public static Client NatAgent;
        public static List<PendingClan> PendingClans = new List<PendingClan>();
        public static List<PendingClanWarRequest> PendingClanWar = new List<PendingClanWarRequest>();
        public static IDatabase GunzDatabase;

    }
}
