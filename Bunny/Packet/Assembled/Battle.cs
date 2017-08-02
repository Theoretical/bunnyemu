using System;
using System.Collections.Generic;
using Bunny.Core;
using Bunny.Enums;
using Bunny.GameTypes;
using Bunny.Items;
using Bunny.Network;
using Bunny.Stages;
using Bunny.Utility;

namespace Bunny.Packet.Assembled
{
    class Battle
    {
        public static void StageLaunch(List<Client> clients, StageTraits stage)
        {
            using (var packet = new PacketWriter(Operation.StageLaunch, CryptFlags.Encrypt))
            {
                packet.Write(stage.StageId);
                packet.Write(stage.Map);

                clients.ForEach(c => c.Send(packet));
            }
        }

        public static void StageRelayLaunch(List<Client> clients, StageTraits stage)
        {
            using (var packet = new PacketWriter(Operation.StageLaunchRelay, CryptFlags.Encrypt))
            {
                packet.Write(stage.StageId);
                packet.Write(stage.Map);
                packet.Write(false);

                clients.ForEach(c => c.Send(packet));
            }
        }

        public static void GameResponseTimesync(Client client, Int32 time)
        {
            using (var packet = new PacketWriter(Operation.GameResponseTimeSync, CryptFlags.Encrypt))
            {
                packet.Write(time);

                var timesync = time;
                client.ClientPlayer.LastTimeSync = (int)timesync;
                
                packet.Write(timesync);

                client.Send(packet);
            }
        }

        public static void LoadingComplete(List<Client> clients, Muid playerId)
        {
            using (var packet = new PacketWriter(Operation.LoadingComplete, CryptFlags.Encrypt))
            {
                packet.Write(playerId);
                packet.Write(100);

                clients.ForEach(c => c.Send(packet));
            }
        }

        public static void GameEnterBattle(List<Client> clients, Client client)
        {
            var stage = client.GetStage().GetTraits();

            using (var packet = new PacketWriter(Operation.StageEnterBattle, CryptFlags.Encrypt))
            {
                if (stage.State == StageState.Standby)
                    packet.Write((byte)0);
                else
                    packet.Write((byte)1);

                //really hate constants :(.
                packet.Write(1, 166);
                packet.Write(client.GetMuid());

                if (client.PeerEnd == null)
                {
                    packet.Write(new byte[4], 0, 4);
                    packet.Write(0);
                }
                else
                {
                    packet.Write(client.PeerEnd.Address.GetAddressBytes(), 0, 4);
                    packet.Write(client.PeerEnd.Port);
                }

                packet.Write(client.GetCharacter().Name, 32);
                packet.Write(client.GetCharacter().ClanName, 16);
                packet.Write((Int32)client.GetCharacter().ClanGrade);//clan rank
                packet.Write(client.GetCharacter().ClanPoint);//clan points
                packet.Write((byte)0);//?
                packet.Write(client.GetCharacter().Level);
                packet.Write(client.GetCharacter().Sex);
                packet.Write(client.GetCharacter().Hair);
                packet.Write(client.GetCharacter().Face);
                packet.Write(client.GetCharacter().Xp);
                packet.Write(client.GetCharacter().Bp);
                packet.Write(client.GetCharacter().BonusRate);
                packet.Write(client.GetCharacter().Prize);
                packet.Write(client.GetCharacter().Hp);
                packet.Write(client.GetCharacter().Ap);
                packet.Write(client.GetCharacter().MaxWeight);
                packet.Write(client.GetCharacter().SafeFalls);
                packet.Write(client.GetCharacter().Fr);
                packet.Write(client.GetCharacter().Cr);
                packet.Write(client.GetCharacter().Er);
                packet.Write(client.GetCharacter().Wr);
                foreach (Item nItem in client.GetCharacter().EquippedItems)
                    packet.Write(nItem.ItemId);

                packet.Write((Int32)client.ClientPlayer.PlayerAccount.Access);
                packet.Write(client.GetCharacter().ClanId);
                packet.Write((byte)client.ClientPlayer.PlayerTeam);
                packet.Write((byte)0);
                packet.Write((Int16)0);
                clients.ForEach(c => c.Send(packet));

            }
        }

        public static void BattleResponseInfo(Client client, int red, int blue, List<Client> players)
        {
            using (var packet = new PacketWriter(Operation.BattleResponseInfo, CryptFlags.Encrypt))
            {
                var traits = client.GetStage().GetTraits();

                packet.Write(traits.StageId);
                packet.Write(1, 6);
                packet.Write((byte)red);
                packet.Write((byte)blue);
                packet.Write(0);

                //TODO: Add rulesets here.
                if (!traits.Ruleset.IsBerserker() || !traits.Ruleset.IsAssassination() || ((Berserker)traits.Ruleset).CurrentBerserker == null)
                {
                    packet.Write(0, 0);
                }
                else if (traits.Ruleset.IsBerserker())
                {
                    var berserker = traits.Ruleset as Berserker;

                    if (berserker != null && berserker.CurrentBerserker != null)
                    {
                        packet.Write(1, 9);
                        packet.Write((byte)8);
                        packet.Write(berserker.CurrentBerserker.GetMuid());
                    }
                }
                else if (traits.Ruleset.IsAssassination())
                {
                    var rule = traits.Ruleset as Assassination;

                    if (rule.RedVip != null && rule.BlueVip != null)
                    {
                        packet.Write(1, 17);
                        packet.Write((byte) ObjectStageGameType.Assassination);
                        packet.Write(rule.RedVip.GetMuid());
                        packet.Write(rule.BlueVip.GetMuid());
                    }
                    else
                    {
                        packet.Write(0,0);
                    }
                }

                packet.Write(players.Count, 17);
                foreach (var c in players)
                {
                    packet.Write(c.GetMuid());
                    packet.Write(c.ClientPlayer.PlayerStats.Spawned);
                    packet.Write(c.ClientPlayer.PlayerStats.Kills);
                    packet.Write(c.ClientPlayer.PlayerStats.Deaths);
                }

                client.Send(packet);
            }
        }

        public static void GameSpawn(List<Client> clients, Muid playerId, Position position, Direction direction)
        {
            using (var packet = new PacketWriter(Operation.GameResponseSpawn, CryptFlags.Encrypt))
            {
                packet.Write(playerId);
                packet.Write((UInt16)position.X);
                packet.Write((UInt16)position.Y);
                packet.Write((UInt16)position.Z);
                packet.Write((UInt16)(direction.X * 32000.0));
                packet.Write((UInt16)(direction.Y * 32000.0));
                packet.Write((UInt16)(direction.Z * 32000.0));

                clients.ForEach(c => c.Send(packet));
            }
        }

        public static void StageRoundUpdate(List<Client> clients, Muid stageId, Int32 curRound, RoundState roundState, Int32 winner = 0)
        {
            using (var packet = new PacketWriter(Operation.StageRoundState, CryptFlags.Encrypt))
            {
                packet.Write(stageId);
                packet.Write(curRound);
                packet.Write((Int32)roundState);
                packet.Write(winner);

                clients.ForEach(c => c.Send(packet));
            }
        }

        public static void StageRoundUpdate(Client client, Muid stageId, Int32 curRound, RoundState roundState, Int32 winner = 0)
        {
            using (var packet = new PacketWriter(Operation.StageRoundState, CryptFlags.Encrypt))
            {
                packet.Write(stageId);
                packet.Write(curRound);
                packet.Write((Int32)roundState);
                packet.Write(winner);

                client.Send(packet);
            }
        }

        public static void StageFinish(List<Client> clients, Muid stageId, bool relayFinished = true)
        {
            using (var packet = new PacketWriter(Operation.StageFinish, CryptFlags.Encrypt))
            {
                packet.Write(stageId);
                packet.Write(relayFinished);

                clients.ForEach(c => c.Send(packet));
            }
        }

        public static void GameDie(List<Client> clients, Pair<Muid, Muid> uids, Pair<UInt32, UInt32> args)
        {
            using (var packet = new PacketWriter(Operation.GameDie, CryptFlags.Encrypt))
            {
                packet.Write(uids.First);
                packet.Write(args.First);
                packet.Write(uids.Second);
                packet.Write(args.Second);

                clients.ForEach(c => c.Send(packet));
            }
        }

        public static void GameLevelUp(List<Client> clients, Muid uid, Int32 level)
        {
            using (var packet = new PacketWriter(Operation.GameLevelUp, CryptFlags.Encrypt))
            {
                packet.Write(uid);
                packet.Write(level);

                clients.ForEach(c => c.Send(packet));
            }
        }

        public static void LeaveBattle(List<Client> clients, Muid playerId, bool relayMap = false)
        {
            using (var packet = new PacketWriter(Operation.StageResponseLeaveBattle, CryptFlags.Encrypt))
            {
                packet.Write(playerId);
                packet.Write(relayMap);

                clients.ForEach(c => c.Send(packet));
            }
        }

        public static void ResponseLateJoin (Client client)
        {
            using (var packet = new PacketWriter(Operation.StageResponseForcedEntry, CryptFlags.Encrypt))
            {
                packet.Write(0);

                client.Send(packet);
            }
        }

        public static void ResponsePeerList (Client client, List<Client> clients)
        {
            using (var packet = new PacketWriter(Operation.StageResponsePeerList, CryptFlags.Encrypt))
            {
                packet.Write(client.GetStage().GetTraits().StageId);

                packet.Write(clients.Count, 166);
                foreach (var c in clients)
                {
                    packet.Write(c.GetMuid());

                    if (c.PeerEnd == null)
                    {
                        packet.Write(new byte[4], 0, 4);
                        packet.Write(0);
                    }
                    else
                    {
                        packet.Write(c.PeerEnd.Address.GetAddressBytes(), 0, 4);
                        packet.Write(c.PeerEnd.Port);
                    }

                    packet.Write(c.GetCharacter().Name, 32);
                    packet.Write(c.GetCharacter().ClanName, 16);
                    packet.Write((Int32)c.GetCharacter().ClanGrade);//clan rank
                    packet.Write(c.GetCharacter().ClanPoint);//clan points
                    packet.Write((byte)0);//?
                    packet.Write(c.GetCharacter().Level);
                    packet.Write(c.GetCharacter().Sex);
                    packet.Write(c.GetCharacter().Hair);
                    packet.Write(c.GetCharacter().Face);
                    packet.Write(c.GetCharacter().Xp);
                    packet.Write(c.GetCharacter().Bp);
                    packet.Write(c.GetCharacter().BonusRate);
                    packet.Write(c.GetCharacter().Prize);
                    packet.Write(c.GetCharacter().Hp);
                    packet.Write(c.GetCharacter().Ap);
                    packet.Write(c.GetCharacter().MaxWeight);
                    packet.Write(c.GetCharacter().SafeFalls);
                    packet.Write(c.GetCharacter().Fr);
                    packet.Write(c.GetCharacter().Cr);
                    packet.Write(c.GetCharacter().Er);
                    packet.Write(c.GetCharacter().Wr);
                    foreach (var nItem in c.GetCharacter().EquippedItems)
                        packet.Write(nItem.ItemId);

                    packet.Write((Int32)c.ClientPlayer.PlayerAccount.Access);
                    packet.Write(c.GetCharacter().ClanId);

                    packet.Write((byte)c.ClientPlayer.PlayerTeam);
                    packet.Write((byte)0);
                    packet.Write((Int16)0);
                }

                client.Send(packet);
            }
        }

        public static void SpawnWorldItem (List<Client> clients, ItemSpawn item)
        {
            using (var packet = new PacketWriter(Operation.MatchWorldItemSpawn, CryptFlags.Encrypt))
            {
                packet.Write(1, 12);
                packet.Write((UInt16)item.ItemUid);
                packet.Write((UInt16)item.ItemId);
                packet.Write((UInt16)1);
                packet.Write((UInt16)item.Position.X);
                packet.Write((UInt16)item.Position.Y);
                packet.Write((UInt16)item.Position.Z);

                clients.ForEach(c => c.Send(packet));
            }
        }

        public static void SpawnWorldItem(Client client, ItemSpawn item)
        {
            using (var packet = new PacketWriter(Operation.MatchWorldItemSpawn, CryptFlags.Encrypt))
            {
                packet.Write(1, 12);
                packet.Write((UInt16)item.ItemUid);
                packet.Write((UInt16)item.ItemId);
                packet.Write((UInt16)1);
                packet.Write((UInt16)item.Position.X);
                packet.Write((UInt16)item.Position.Y);
                packet.Write((UInt16)item.Position.Z);

                client.Send(packet);
            }
        }

        public static void ObtainWorldItem(List<Client> clients, Muid player, int item)
        {
            using (var packet = new PacketWriter(Operation.MatchWorldItemObtain, CryptFlags.Encrypt))
            {
                packet.Write(player);
                packet.Write(item);

                clients.ForEach(c=> c.Send(packet));
            }
        }

        public static void AssignBerserker(List<Client> clients, Muid berserkerId)
        {
            using (var packet = new PacketWriter(Operation.MatchAssignBerserker, CryptFlags.Encrypt))
            {
                packet.Write(berserkerId);

                clients.ForEach(c => c.Send(packet));
            }
        }

        public static void DuelQueue (List<Client> clients, List<Client> players, byte queueLength, byte streak, bool roundEnd = false)
        {
            using (var packet = new PacketWriter(Operation.MatchDuelQueueInfo, CryptFlags.Encrypt))
            {
                packet.Write(131);
                foreach (var player in players)
                {
                    packet.Write(player == null ? new Muid(0, 0) : player.GetMuid());
                }

                packet.Write(queueLength);
                packet.Write(streak);
                packet.Write(roundEnd);

                clients.ForEach(c => c.Send(packet));
            }
        }

        public static void DuelQueue(Client client, List<Client> players, byte queueLength, byte streak, bool roundEnd = false)
        {
            using (var packet = new PacketWriter(Operation.MatchDuelQueueInfo, CryptFlags.Encrypt))
            {
                packet.Write(131);
                foreach (var player in players)
                {
                    packet.Write(player == null ? new Muid(0, 0) : player.GetMuid());
                }

                packet.Write(queueLength);
                packet.Write(streak);
                packet.Write(roundEnd);

                client.Send(packet);
            }
        }

        public static void SetObserver (List<Client> clients, Muid playerId)
        {
            using (var packet = new PacketWriter(Operation.MatchSetObserver, CryptFlags.Encrypt))
            {
                packet.Write(playerId);

                clients.ForEach(c => c.Send(packet));
            }
        }

        public static void AssignVips (List<Client> clients, Muid red, Muid blue)
        {
            using (var packet = new PacketWriter(Operation.MatchAssignCommander, CryptFlags.Encrypt))
            {
                packet.Write(red);
                packet.Write(blue);

                clients.ForEach(c => c.Send(packet));
            }
        }

        public static void UpdateDuelStreak(string character, string channel, int room, int victories)
        {
            using (var packet = new PacketWriter(Operation.MatchBroadcastDuelRenewVictories, CryptFlags.Encrypt))
            {
                packet.Write(character);
                packet.Write(channel);
                packet.Write(room);
                packet.Write(victories);

                TcpServer.GlobalPacket(packet);
            }
        }

        public static void EndDuelStreak(string character, string inturupt, int victories)
        {
            using (var packet = new PacketWriter(Operation.MatchBroadcastDuelInterruptVictories, CryptFlags.Encrypt))
            {
                packet.Write(character);
                packet.Write(inturupt);
                packet.Write(victories);

                TcpServer.GlobalPacket(packet);
            }
        }
    }
}
