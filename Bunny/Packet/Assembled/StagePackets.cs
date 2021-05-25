using System;
using System.Collections.Generic;
using System.Linq;
using Bunny.Core;
using Bunny.Enums;
using Bunny.Network;
using Bunny.Stages;

namespace Bunny.Packet.Assembled
{
    class StagePackets
    {
        public static void ResponseStageJoin(Client client, Results result)
        {
            using (var packet = new PacketWriter(Operation.StageJoin, CryptFlags.Encrypt))
            {
                packet.Write(client.GetMuid());
                packet.Write(client.GetStage().GetTraits().StageId);
                packet.Write(client.GetStage().GetTraits().StageIndex);
                packet.Write(client.GetStage().GetTraits().Name);
                packet.Write((bool)false);

                client.Send(packet);
            }
        }
        public static void ResponseObjectCache (List<Client> sendTo, ObjectCache cache, List<Client> clients)
        {
            using (var packet = new PacketWriter(Operation.MatchObjectCache, CryptFlags.Encrypt))
            {
                packet.Write((byte)cache);
                packet.Write(clients.Count, 176);

                foreach (var c in clients)
                {
                    packet.Write(0);
                    packet.Write(c.GetMuid());
                    packet.Write(c.GetCharacter().Name, 32);
                    packet.Write(c.GetCharacter().ClanName, 16);
                    packet.Write((Int32)c.GetCharacter().Level);
                    packet.Write((Int32)c.ClientPlayer.PlayerAccount.Access);
                    packet.Write(0);
                    packet.Write(0);
                    packet.Write(c.GetCharacter().ClanId);
                    packet.Write(0);
                    packet.Write("", 32);
                    packet.Write(0);
                    packet.Write((Int32)c.GetCharacter().Sex);
                    packet.Write(c.GetCharacter().Hair);
                    packet.Write(c.GetCharacter().Face);
                    packet.Write((Int16)0);

                    foreach (var item in c.GetCharacter().EquippedItems)
                    {
                        packet.Write(item.ItemId);
                    }
                }

                sendTo.ForEach(c => c.Send(packet));
            }
        }

        public static void ResponseObjectCache(Client client, ObjectCache cache, List<Client> clients)
        {
            using (var packet = new PacketWriter(Operation.MatchObjectCache, CryptFlags.Encrypt))
            {
                packet.Write((byte)cache);
                packet.Write(clients.Count, 176);

                foreach (var c in clients)
                {
                    packet.Write(0);
                    packet.Write(c.GetMuid());
                    packet.Write(c.GetCharacter().Name, 32);
                    packet.Write(c.GetCharacter().ClanName, 16);
                    packet.Write((Int32)c.GetCharacter().Level);
                    packet.Write((Int32)c.ClientPlayer.PlayerAccount.Access);
                    packet.Write(0);
                    packet.Write(0);
                    packet.Write(c.GetCharacter().ClanId);
                    packet.Write(0);
                    packet.Write("", 32);
                    packet.Write(0);
                    packet.Write((Int32)c.GetCharacter().Sex);
                    packet.Write(c.GetCharacter().Hair);
                    packet.Write(c.GetCharacter().Face);
                    packet.Write((Int16)0);

                    foreach (var item in c.GetCharacter().EquippedItems)
                    {
                        packet.Write(item.ItemId);
                    }
                }

                client.Send(packet);
            }
        }

        public static void ResponseObjectCacheExclusive(List<Client> clients, ObjectCache cache, Client player)
        {
            using (var packet = new PacketWriter(Operation.MatchObjectCache, CryptFlags.Encrypt))
            {
                packet.Write((byte) cache);
                packet.Write(1, 176);

                packet.Write(0);
                packet.Write(player.GetMuid());
                packet.Write(player.GetCharacter().Name, 32);
                packet.Write(player.GetCharacter().ClanName, 16);
                packet.Write((Int32) player.GetCharacter().Level);
                packet.Write((Int32) player.ClientPlayer.PlayerAccount.Access);
                packet.Write(0);
                packet.Write(0);
                packet.Write(player.GetCharacter().ClanId);
                packet.Write(0);
                packet.Write("", 32);
                packet.Write(0);
                packet.Write((Int32) player.GetCharacter().Sex);
                packet.Write(player.GetCharacter().Hair);
                packet.Write(player.GetCharacter().Face);
                packet.Write((Int16) 0);

                foreach (var item in player.GetCharacter().EquippedItems)
                {
                    packet.Write(item.ItemId);
                }

                clients.ForEach(c => c.Send(packet));


            }
        }
        public static void ResponseStageMaster(List<Client> clients, Stage stage)
        {
            using (var packet = new PacketWriter(Operation.StageMaster, CryptFlags.Encrypt))
            {
                packet.Write(stage.GetTraits().StageId);
                packet.Write(stage.GetTraits().Master.GetMuid());

                clients.ForEach(c => c.Send(packet));
            }
        }
        public static void ResponseStageLeave (List<Client> clients, Stage stage, Muid player)
        {
            using (var packet = new PacketWriter(Operation.StageLeave, CryptFlags.Encrypt))
            {
                packet.Write(player);
                packet.Write(stage.GetTraits().StageId);

                clients.ForEach(c => c.Send(packet));
            }
        }

        public static void ResponseStageList (Client client, byte previous, byte next, List<Stage> stages)
        {
            using (var packet = new PacketWriter(Operation.StageList, CryptFlags.Encrypt))
            {
                packet.Write(previous);
                packet.Write(next);

                packet.Write(stages.Count, 91);

                var traits = from stage in stages
                             select stage.GetTraits();

                var index = client.ClientPlayer.StageIndex;

                foreach (var info in traits)
                {
                    packet.Write(info.StageId);
                    packet.Write(++index);
                    packet.Write(info.Name, 64);
                    packet.Write(Convert.ToByte(info.Players.Count));
                    packet.Write((byte)info.MaxPlayers);
                    packet.Write((Int32)info.State);
                    packet.Write((Int32)info.Gametype);

                    switch (info.Map)
                    {
                        case "Battle Arena":
                            packet.Write(Convert.ToByte(RelayMaps.BattleArena));
                            break;
                        case "Prison II":
                            packet.Write(Convert.ToByte(RelayMaps.PrisonII));
                            break;
                        case "Lost Shrine":
                            packet.Write(Convert.ToByte(RelayMaps.LostShrine));
                            break;
                        case "Shower Room":
                            packet.Write(Convert.ToByte(RelayMaps.Shower_Room));
                            break;
                        default:
                            packet.Write(Convert.ToByte(Enum.Parse(typeof(RelayMaps), info.Map)));
                            break;
                    }

                    if (!info.ForcedEntry)
                        packet.Write((Int32)StageType.Regular);

                    else if (info.Password.Length > 0)
                        packet.Write((Int32)StageType.Locked);

                    else if (info.Locked)
                        packet.Write((Int32)StageType.LevelRestricted);

                    else
                        packet.Write((Int32)StageType.None);

                    packet.Write(Convert.ToByte(info.Master.GetCharacter().Level));
                    packet.Write((byte)info.Level);
                    packet.Write((byte)0);
                }

                client.Send(packet);
            }
        }

        public static void ResponseSettings (List<Client> clients, StageTraits stageTraits)
        {
            using (var packet = new PacketWriter(Operation.StageResponseSettings, CryptFlags.Encrypt))
            {
                packet.Write(stageTraits.StageId);
                
                packet.Write(1, 143);

                packet.Write(stageTraits.StageId);
                packet.Write(stageTraits.Name, 64);
                packet.Write(stageTraits.Map, 32);

                packet.Write((byte)0);
                packet.Write((Int32)stageTraits.Gametype);
                packet.Write(stageTraits.RoundCount);
                packet.Write((Int32)stageTraits.Time);
                packet.Write((Int32)stageTraits.Level);
                packet.Write((Int32)stageTraits.MaxPlayers);
                packet.Write(stageTraits.TeamKill);
                packet.Write(stageTraits.WinThePoint);
                packet.Write(stageTraits.ForcedEntry);
                packet.Write(stageTraits.TeamBalanced);
                packet.Write(stageTraits.RelayEnabled);

                packet.Write((byte)1); // NETCODE
                packet.Write((byte)0);
                packet.Write(0); // hp
                packet.Write(0); // ap
                packet.Write((byte)0);
                packet.Write((byte)0);
                packet.Write((byte)0);

                packet.Write(stageTraits.Players.Count, 16);
                foreach (var c in stageTraits.Players)
                {
                    packet.Write(c.GetMuid());
                    packet.Write((Int32)c.ClientPlayer.PlayerTeam);
                    packet.Write((Int32)c.ClientPlayer.PlayerState);
                }

                packet.Write((Int32)stageTraits.State);
                packet.Write(stageTraits.Master.GetMuid());

                clients.ForEach(c => c.Send(packet));
            }
        }


        public static void ResponseStageChat(List<Client> clients, Muid playerId, Muid stageId, string message)
        {
            using (var packet = new PacketWriter(Operation.StageChat, CryptFlags.Encrypt))
            {
                packet.Write(playerId);
                packet.Write(stageId);
                packet.Write(message);

                clients.ForEach(c => c.Send(packet));
            }
        }

        public static void ResponseStageTeam(List<Client> clients, Muid playerId, Muid stageId, Team team)
        {
            using (var packet = new PacketWriter(Operation.StageTeam, CryptFlags.Encrypt))
            {
                packet.Write(playerId);
                packet.Write(stageId);
                packet.Write((Int32)team);

                clients.ForEach(c => c.Send(packet));
            }
        }

        public static void ResponsePlayerState(List<Client> clients, Muid playerId, Muid stageId, ObjectStageState state)
        {
            using (var packet = new PacketWriter(Operation.StageState, CryptFlags.Encrypt))
            {
                packet.Write(playerId);
                packet.Write(stageId);
                packet.Write((Int32)state);

                clients.ForEach(c => c.Send(packet));
            }
        }

        public static void QuestError(List<Client> clients, int error, Muid stageId)
        {
            using (var packet = new PacketWriter(Operation.QuestFail, CryptFlags.Encrypt))
            {
                packet.Write(error);
                packet.Write(stageId);

                clients.ForEach(c => c.Send(packet));
            }
        }
    
        public static void LadderPrepare(Client client, int teamNumber)
        {
            using (var packet = new PacketWriter(Operation.LadderPrepare, CryptFlags.Encrypt))
            {
                packet.Write(client.GetMuid());
                packet.Write(teamNumber);

                client.Send(packet);
            }
        }

        public static void LadderLaunch(List<Client> clients, StageTraits stage)
        {
            using (var packet = new PacketWriter(Operation.LadderLaunch, CryptFlags.Encrypt))
            {
                packet.Write(stage.StageId);
                packet.Write(stage.Map);

                clients.ForEach(c => c.Send(packet));
            }
        }

    }
}
