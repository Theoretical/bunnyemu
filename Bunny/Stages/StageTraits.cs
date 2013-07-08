using System;
using System.Collections.Generic;
using Bunny.Core;
using Bunny.Enums;
using Bunny.GameTypes;

namespace Bunny.Stages
{
    class StageTraits
    {
        public Muid StageId;
        public Client Master;
        public string Name;
        public string Password;
        public string Map = "Mansion";
        public StageState State = StageState.Standby;
        public RoundState Round = RoundState.Prepare;
        public ObjectStageGameType Gametype = ObjectStageGameType.DeathMatch;
        public byte MaxPlayers = 8;
        public bool TeamKill;
        public bool WinThePoint;
        public bool ForcedEntry;
        public bool TeamBalanced;
        public bool RelayEnabled;
        public bool Locked;
        public Int32 RoundCount = 50;
        public byte Time = 30;
        public byte Level;
        public byte CurrentRound;
        public int Type;
        public List<Client> Players = new List<Client>();
        public int StageIndex;
        public BaseGametype Ruleset;
        public Map CurrentMap = Globals.Maps.GetMap("Mansion");
        public Int32 WorldItemUid = 1;
        public List<ItemSpawn> WorldItems = new List<ItemSpawn>();
        public QueueInfo DuelQueue = new QueueInfo();
        public RelayMapInfo RelayMaps;
    }
}
