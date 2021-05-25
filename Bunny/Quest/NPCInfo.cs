using System;
using System.Collections.Generic;
using System.Xml;
using Bunny.Core;

namespace Bunny.Quest
{
    class NpcInfoElement
    {
        public string Name;
        public string Desc;
        public byte Id = 11;
        public short Hp = 100;
        public short Ap = 100;
        public byte Intelligence = 3;
        public byte Agility = 3;
        public float ViewDistance = 1145569280;
        public float DyingTime = 1084227584;
        public float CollRadius = 1108082688;
        public float CollHeight = 1127481344;
        public byte NpcAttackTypes = 1;
        public float AttackRangeAngle = 1070141400;
        public int WeaponItemId = 300000;
        public float Speed = 1133903872;
    }

    class NpcList
    {
        private static readonly object Locker = new object();
        private static List<NpcInfoElement> _npcs = new List<NpcInfoElement>();

        public static void Load(string xml)
        {
            lock (Locker)
            {
                var npcInfo = new NpcInfoElement();

                using (var reader = new XmlTextReader(xml))
                {
                    while (reader.Read())
                    {
                        switch (reader.Name)
                        {
                            case "NPC":
                                npcInfo = new NpcInfoElement();
                                if ((reader.GetAttribute("id")) == null)
                                    break;

                                npcInfo.Id = Byte.Parse(reader.GetAttribute("id"));
                                npcInfo.Name = reader.GetAttribute("name");
                                npcInfo.Desc = reader.GetAttribute("desc");
                                npcInfo.Hp = Int16.Parse(reader.GetAttribute("max_hp"));
                                npcInfo.Ap = Int16.Parse(reader.GetAttribute("max_ap"));
                                npcInfo.Intelligence = reader.GetAttribute("int") != null ? Byte.Parse(reader.GetAttribute("int")) : npcInfo.Intelligence;
                                npcInfo.Agility = reader.GetAttribute("agility") != null ? Byte.Parse(reader.GetAttribute("agility")) : npcInfo.Agility;
                                npcInfo.DyingTime = reader.GetAttribute("dyingtime") != null
                                                        ? float.Parse(reader.GetAttribute("dyingtime"))
                                                        : npcInfo.DyingTime;
                                break;
                            case "COLLISION":
                                npcInfo.CollRadius = float.Parse(reader.GetAttribute("radius"));
                                npcInfo.CollHeight = float.Parse(reader.GetAttribute("height"));
                                break;

                            case "SPEED":
                                npcInfo.Speed = float.Parse(reader.GetAttribute("default"));
                                break;
                            case "DROP":
                                Log.Write("Adding NPC: {0}", npcInfo.Name);
                                _npcs.Add(npcInfo);
                                break;
                        }
                    }
                }
            }
        }
    }
}
