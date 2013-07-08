using System;
using System.Collections.Generic;
using Bunny.Enums;
using Bunny.Utility;

namespace Bunny.Stages
{
    class Map : ICloneable
    {
        public string MapName = "";
        public List<Pair<Position, Direction>> Deathmatch = new List<Pair<Position, Direction>>();
        public List<Pair<Position, Direction>> TeamRed = new List<Pair<Position, Direction>>();
        public List<Pair<Position, Direction>> TeamBlue = new List<Pair<Position, Direction>>();
        public List<ItemSpawn> DeathMatchItems = new List<ItemSpawn>();
        public List<ItemSpawn> TeamItems = new List<ItemSpawn>();

        object ICloneable.Clone()
        {
            return this.Clone();
        }

        public Map Clone()
        {
            return (Map)this.MemberwiseClone();
        }

    }
}
