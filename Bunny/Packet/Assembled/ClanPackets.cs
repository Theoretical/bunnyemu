using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bunny.Core;
using Bunny.Enums;
using Bunny.Network;
using Bunny.Players;

namespace Bunny.Packet.Assembled
{
    class ClanPackets
    {
        public static void ResponseCreateClan(Client client, Results result, Int32 request)
        {
            using (var packet = new PacketWriter(Operation.MatchClanResponseCreateclan, CryptFlags.Encrypt))
            {
                packet.Write((Int32)result);
                packet.Write(request);
                client.Send(packet);
            }
        }

        public static void UpdateClanCharInfo(Client client)
        {
            using (var packet = new PacketWriter(Operation.MatchClanUpdateClanInfo, CryptFlags.Encrypt))
            {
                packet.Write(client.GetCharacter().ClanName, 16);
                packet.Write((Int32)client.GetCharacter().ClanGrade);
                client.Send(packet);
            }
        }

        public static void AskAgreement(List<Client> members, int request, string name, Muid master, string masterName)
        {
            using (var packet = new PacketWriter(Operation.MatchClanAskSponsorAgreement, CryptFlags.Encrypt))
            {
                packet.Write(request);
                packet.Write(name);
                packet.Write(master);
                packet.Write(masterName);

                members.ForEach(c => c.Send(packet));
            }
        }

        public static void ResponseAagreedCreateClan (PendingClan pendingClan)
        {
            using (var packet = new PacketWriter(Operation.MatchResponseAgreedCreateClan, CryptFlags.Encrypt))
            {
                packet.Write(0);

                pendingClan.ClanMaster.Send(packet);
                pendingClan.Member1.First.Send(packet);
                pendingClan.Member2.First.Send(packet);
                pendingClan.Member3.First.Send(packet);
                pendingClan.Member4.First.Send(packet);
            }
        }

        public static void SendMemberList(Client client)
        {
            var members = TcpServer.GetClanMembers(client.GetCharacter().ClanId);

            using (var packet = new PacketWriter(Operation.MatchClanResponseClanMemberList, CryptFlags.Encrypt))
            {
                packet.Write(members.Count, 49);
                foreach (var member in members)
                {
                    packet.Write(member.GetMuid());
                    packet.Write(member.GetCharacter().Name, 32);
                    packet.Write((byte)member.GetCharacter().Level);
                    packet.Write((Int32)member.GetCharacter().ClanGrade);
                    packet.Write((Int32)member.ClientPlayer.PlayerLocation);
                }

                client.Send(packet);
            }
        }

        public static void Message(Client client, string msg)
        {
            var members = TcpServer.GetClanMembers(client.GetCharacter().ClanId);

            using (var packet = new PacketWriter(Operation.MatchClanMsg, CryptFlags.Encrypt))
            {
                packet.Write(client.GetCharacter().Name);
                packet.Write(msg);
                members.ForEach(m => m.Send(packet));
            }
        }

        public static void ExpelMemberNotAllowed (Client client)
        {
            using (var expelMember = new PacketWriter(Operation.MatchClanAdminResponseLeaveMember, CryptFlags.Encrypt))
            {
                expelMember.Write((Int32)Results.ClanNotAuthorized);
                client.Send(expelMember);
            }
        }

        public static void ResponseChangeGrade (Client client)
        {
            using (var packet = new PacketWriter(Operation.MatchClanMasterResponseChangeGrade, CryptFlags.Encrypt))
            {
                packet.Write(0);
                client.Send(packet);
            }
        }

        public static void ResponseJoin (Client client, Results results)
        {
            using (var packet = new PacketWriter(Operation.MatchClanResponseJoinClan, CryptFlags.Encrypt))
            {
                packet.Write((Int32)results);
                client.Send(packet);
            }
        }

        public static void ResponseAgreedJoin(Client client)
        {
            using (var packet = new PacketWriter(Operation.MatchClanResponseAgreedJoinClan, CryptFlags.Encrypt))
            {
                packet.Write(0);
                client.Send(packet);
            }
        }

        public static void RequestJoin (Client client, Client target, string clanName)
        {
            using (var packet = new PacketWriter(Operation.MatchClanAskJoinAgreement, CryptFlags.Encrypt))
            {
                packet.Write(clanName);
                packet.Write(client.GetMuid());
                packet.Write(client.GetCharacter().Name);

                target.Send(packet);
            }
        }

        public static void MemberConnected(Client client, string name)
        {
            var members = TcpServer.GetClanMembers(client.GetCharacter().ClanId);

            using (var packet = new PacketWriter(Operation.MatchClanMemberConnected, CryptFlags.Encrypt))
            {
                packet.Write(name);
                members.ForEach(m => m.Send(packet));
            }
        }

        public static void ClanInfo (Client client, string clanName)
        {
            var player = client.ClientPlayer;
            var info = new ClanInfo();
            var clanId = Globals.GunzDatabase.GetClanId(clanName);
            
            Globals.GunzDatabase.GetClanInfo(clanId, ref info);

            if (info == null)
                return;

            info.ConnectedMembers = Convert.ToInt16(TcpServer.GetClanMembers(clanId).Count);

            using (var packet = new PacketWriter(Operation.MatchClanResponseClanInfo, CryptFlags.Encrypt))
            {
                packet.Write(1, 78);

                packet.Write(info.Name, 16);
                packet.Write(info.Level);    
                packet.Write(info.Points);
                packet.Write(info.TotalPoints);
                packet.Write(info.Ranking);
                packet.Write(info.Master, 32);
                packet.Write(info.Wins);
                packet.Write(info.Losses);
                packet.Write(info.MemberCount);
                packet.Write(info.ConnectedMembers);
                packet.Write(info.ClanId);
                packet.Write(info.EmblemChecksum);

                client.Send(packet);
            }
        }

        public static void AskAgreement(List<Client> clients, Client proposer, int mode, int request)
        {
            using (var packet = new PacketWriter(Operation.MatchAskAgreement, CryptFlags.Encrypt))
            {
                packet.Write(proposer.GetMuid());

                packet.Write(clients.Count+1, 32);
                clients.ForEach(c => packet.Write(c.GetCharacter().Name, 32));
                packet.Write(proposer.GetCharacter().Name, 32);
                packet.Write(mode);
                packet.Write(request);

                clients.ForEach(c => c.Send(packet));
            }
        }
        public static void ResponseProposal(Client client, Int32 result, Int32 mode, Int32 request)
        {
            using (var packet = new PacketWriter(Operation.MatchLadderResponseChallenge, CryptFlags.Encrypt))
            {
                packet.Write(result);
                packet.Write(mode);
                packet.Write(request);

                client.Send(packet);
            }
        }

        public static void SearchRival(List<Client> clients)
        {
            using (var packet = new PacketWriter(Operation.MatchLadderSearchRival, CryptFlags.Encrypt))
            {
                clients.ForEach(c => c.Send(packet));
            }
        }
    }
}
