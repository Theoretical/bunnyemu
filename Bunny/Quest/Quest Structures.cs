using System.Collections.Generic;
using Bunny.Enums;

namespace Bunny.Quest
{
    class QuestGameInfo
    {
        public short QuestLevel;
        public float NpcTc = 1065353216;
        public ushort NpcCount;
        public byte NpcInfoCount;
        public byte[] NpcInfo = new byte[8];
        public ushort MapSectorCount;
        public ushort[] MapSectorId = new ushort[16];
        public byte[] MapSectorLinkIndex = new byte[16];
    }

    class QuestScenarioInfo
    {
        public int Id;
        public string Title;
        public int QuestLevel;
        public float Dc; // unknown.
        public int SacItemCount;
        public uint[] SacItemIds = new uint[2];
        public int MapSet;
        public bool Special;
        public int XpReward;
        public int BpReward;
        public int RewardItemCount;
        public int[] RewardItemId = new int[3];
        public float RewardItemRate;
        public QuestScenarioInfoMaps[] ScenarioMaps = new QuestScenarioInfoMaps[6];
    }

    class QuestScenarioInfoMaps
    {
        public int KeySectorId;
        public int KeyNpcId;
        public bool KeyNpcIsBoss;
        public List<int> NpcSet;
        public int JacoCount;
        public uint JacoSpawnTickTime;
        public int JacoMinNpcCount;
        public int JacoMaxNpcCount;
        public List<QuestScenarioInfoMapJaco> JacoList;
    }

    class QuestScenarioInfoMapJaco
    {
        public QuestNpc NpcId;
        public float Rate;
    }
}
