using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Bunny.Core;
using Bunny.Enums;
using Bunny.Network;
using Bunny.Packet.Assembled;
using Bunny.Utility;
using Bunny.Players;

namespace Bunny.Packet.Disassemble
{
    class Clan
    {
        [PacketHandler(Operation.MatchClanRequestCreateClan, PacketFlags.Character)]
        public static void ProcessCreateClan(Client client, PacketReader packetReader)
        {
            var masterId = packetReader.ReadMuid();
            var reqeuest = packetReader.ReadInt32();
            var clanName = packetReader.ReadString();
            var member1 = packetReader.ReadString();
            var member2 = packetReader.ReadString();
            var member3 = packetReader.ReadString();
            var member4 = packetReader.ReadString();

            var members = new List<Pair<Client, bool>>();
            members.Add(new Pair<Client, bool>(TcpServer.GetClientFromName(member1), false));
            members.Add(new Pair<Client, bool>(TcpServer.GetClientFromName(member2), false));
            members.Add(new Pair<Client, bool>(TcpServer.GetClientFromName(member3), false));
            members.Add(new Pair<Client, bool>(TcpServer.GetClientFromName(member4), false));


            if (!Globals.AcceptedString.IsMatch(clanName))
                return;

            if (Globals.GunzDatabase.ClanExists(clanName))
            {
                ClanPackets.ResponseCreateClan(client, Results.ClanNameInUse, reqeuest);
                return;
            }

            foreach (var member in members)
            {
                if (member == null || Globals.GunzDatabase.IsInClan(member.First))
                {
                    ClanPackets.ResponseCreateClan(client, Results.ClanUserAlreadyInAClan, reqeuest);
                    return;
                }
            }

            var pendingClan = new PendingClan();
            pendingClan.ClanMaster = client;
            pendingClan.ClanName = clanName;
            pendingClan.RequestId = reqeuest;
            foreach (var member in members)
            {
                pendingClan.Members.Add(member);
            }

            lock (Globals.PendingClans)
                Globals.PendingClans.Add(pendingClan);

            ClanPackets.ResponseCreateClan(client, Results.Accepted, reqeuest);

            var clientList = new List<Client>();
            foreach (var m in members)
                clientList.Add(m.First);

            ClanPackets.AskAgreement(clientList, reqeuest, clanName, client.GetMuid(), client.GetCharacter().Name);   
    
            var responsetimer = new Timer(30000);
            responsetimer.Elapsed += (s, o) => CancelRequest(client, pendingClan, responsetimer);
            responsetimer.Start();
        }

        public static void CancelRequest(Client client, PendingClan clan, Timer timer)
        {
            lock (Globals.PendingClans)
                try
                {
                    Globals.PendingClans.Remove(clan);
                    timer.Enabled = false;
                    
                }
                catch (ObjectDisposedException)
                {
                }

            ClanPackets.ResponseCreateClan(client, Results.ClanCannotBeCreated, clan.RequestId);
        }

        [PacketHandler(Operation.MatchClanAnswerSponsorAgreement, PacketFlags.Character)]
        public static void ProcessAnswerAgreement(Client client, PacketReader packetReader)
        {
            var request = packetReader.ReadInt32();
            var masterId = packetReader.ReadMuid();
            var name = packetReader.ReadString();
            var answer = packetReader.ReadBoolean();

            PendingClan pendingClan;

            lock (Globals.PendingClans)
                pendingClan = Globals.PendingClans.Find(c => c.ClanMaster.GetMuid() == masterId);

            if (pendingClan == null)
                return;

            if (answer == false)
            {
                lock (Globals.PendingClans)
                    Globals.PendingClans.Remove(pendingClan);

                return;
            }

            foreach(var member in pendingClan.Members)
            {
                if (member.First.GetCharacter().Name == name)
                    member.Second = answer;
            }

            var isAccepted = pendingClan.Members.All(m => m.Second == true);
            if (isAccepted)
            {
                int clanId = Globals.GunzDatabase.CreateClan(pendingClan.ClanName, pendingClan.ClanMaster, pendingClan.Members);

                ClanPackets.ResponseAagreedCreateClan(pendingClan);
                pendingClan.ClanMaster.GetCharacter().ClanId = clanId;

                foreach (var member in pendingClan.Members)
                {
                    member.First.GetCharacter().ClanId = clanId;
                    member.First.GetCharacter().ClanName = pendingClan.ClanName;
                }               

                lock (Globals.PendingClans)
                    Globals.PendingClans.Remove(pendingClan);

                ClanPackets.SendMemberList(pendingClan.ClanMaster);
                ClanPackets.UpdateClanCharInfo(pendingClan.ClanMaster);
                foreach (var member in pendingClan.Members)
                {
                    ClanPackets.SendMemberList(member.First);
                    ClanPackets.UpdateClanCharInfo(member.First);
                }

            }
        }

        [PacketHandler(Operation.MatchClanRequestClanMemberList, PacketFlags.Character)]
        public static void ProcessClanMemberList(Client client, PacketReader packetReader)
        {
            if (client.GetCharacter().ClanId < 1)
                return;

            ClanPackets.SendMemberList(client);
        }

        [PacketHandler(Operation.MatchClanRequestMsg, PacketFlags.Character)]
        public static void ProcessClanChat(Client client, PacketReader packetReader)
        {
            if (client.GetCharacter().ClanId < 1)
                return;

            var sender = packetReader.ReadMuid();
            var msg = packetReader.ReadString();

            ClanPackets.Message(client, msg);
        }

        [PacketHandler(Operation.MatchClanAdminRequestExpelMember, PacketFlags.Character)]
        public static void ProcessExpelMember(Client client, PacketReader packetReader)
        {
            if (client.GetCharacter().ClanId < 1 || client.GetCharacter().ClanGrade == ClanGrade.User)
                return;

            var sender = packetReader.ReadMuid();
            var member = packetReader.ReadString();

            Client memberClient = TcpServer.GetClientFromName(member);

            if (memberClient != null)
            {
                if (memberClient.GetCharacter().ClanGrade == ClanGrade.Master || (memberClient.GetCharacter().ClanGrade == ClanGrade.Admin && client.GetCharacter().ClanGrade == ClanGrade.Admin))
                {
                    ClanPackets.ExpelMemberNotAllowed(client);
                    return;
                }

                memberClient.GetCharacter().ClanName = string.Empty;
                memberClient.GetCharacter().ClanId = 0;
                memberClient.GetCharacter().ClanGrade = ClanGrade.None;
                ClanPackets.SendMemberList(memberClient);
                ClanPackets.UpdateClanCharInfo(memberClient);
                Globals.GunzDatabase.ExpelMember(memberClient.GetCharacter().CharacterId);
            }

            using (var expelMember = new PacketWriter(Operation.MatchClanAdminResponseLeaveMember, CryptFlags.Encrypt))
            {
                expelMember.Write(0);
                client.Send(expelMember);
            }
        }

        [PacketHandler(Operation.MatchClanMasterRequestChangeGrade, PacketFlags.Character)]
        public static void ProcessPromoteMember(Client client, PacketReader packetReader)
        {
            if (client.GetCharacter().ClanId < 1 || client.GetCharacter().ClanGrade != ClanGrade.Master)
                return;

            var sender = packetReader.ReadMuid();
            var member = packetReader.ReadString();
            var rank = packetReader.ReadInt32();

            if (!Enum.IsDefined(typeof(ClanGrade), rank))
            {
                client.Disconnect();
                return;
            }

            Client memberClient = TcpServer.GetClientFromName(member);

            if (memberClient != null)
            {
                memberClient.GetCharacter().ClanGrade = (ClanGrade)rank;
                Globals.GunzDatabase.UpdateMember(memberClient.GetCharacter().CharacterId, rank);
            }

            ClanPackets.ResponseChangeGrade(client);
            ClanPackets.UpdateClanCharInfo(memberClient);
            ClanPackets.SendMemberList(client);
        }

        [PacketHandler(Operation.MatchClanRequestJoinClan, PacketFlags.Character)]
        public static void ProcessJoinClan(Client client, PacketReader packetReader)
        {
            var uidChar = packetReader.ReadMuid();
            var clanName = packetReader.ReadString();
            var memberName = packetReader.ReadString();

            var target = TcpServer.GetClientFromName(memberName);

            if (target == null)
            {
                ClanPackets.ResponseJoin(client, Results.FriendNotOnline);
                return;
            }

            if (!Globals.AcceptedString.IsMatch(memberName))
            {
                Log.Write("Clan regex fail.");
                ClanPackets.ResponseJoin(client, Results.FriendNotOnline);
                return;
            }

            if (Globals.GunzDatabase.IsInClan(target))
            {
                ClanPackets.ResponseJoin(client, Results.ClanUserAlreadyInAClan);
                return;
            }

            ClanPackets.ResponseJoin(client, Results.Accepted);
            ClanPackets.RequestJoin(client, target, clanName);
        }

        [PacketHandler(Operation.MatchClanAnswerJoinAgreement, PacketFlags.Character)]
        public static void ProcessAnswerJoin(Client client, PacketReader packetReader)
        {
            var admin = packetReader.ReadMuid();
            var name = packetReader.ReadString();
            var answer = packetReader.ReadBoolean();

            Client owner = TcpServer.GetClientFromUid(admin);

            if (owner == null)
                return;

            if (!answer)
            {
                ClanPackets.ResponseJoin(owner, Results.ClanJoinRejected);
                return;
            }

            ClanPackets.ResponseAgreedJoin(owner);

            client.GetCharacter().ClanId = owner.GetCharacter().ClanId;
            client.GetCharacter().ClanName = owner.GetCharacter().ClanName;

            Globals.GunzDatabase.JoinClan(client.GetCharacter().CharacterId, owner.GetCharacter().ClanId);
            ClanPackets.UpdateClanCharInfo(client);
            ClanPackets.SendMemberList(client);
            ClanPackets.MemberConnected(client,name);
        }

        [PacketHandler(Operation.MatchClanRequestClanInfo, PacketFlags.Character)]
        public static void ProcessClanInfo (Client client, PacketReader packetReader)
        {
            var uid = packetReader.ReadMuid();
            var name = packetReader.ReadString();

            ClanPackets.ClanInfo(client, name);
        }

        [PacketHandler(Operation.MatchRequestProposal, PacketFlags.Character)]
        public static void ProcessRequestProposal(Client client, PacketReader packetReader)
        {
            var uid = packetReader.ReadMuid();
            var mode = packetReader.ReadInt32();
            var request = packetReader.ReadInt32();
            var memberCount = packetReader.ReadInt32();
            var totalSize = packetReader.ReadInt32();
            var blobSize = packetReader.ReadInt32();
            var blobCount = packetReader.ReadInt32();
            List<byte[]> blob = new List<byte[]>();

            if (memberCount == blobCount)
            {
                for (int i = 0; i < blobCount; ++i)
                {
                    var temp = new byte[blobSize];
                    packetReader.Read(temp, 0, blobSize);
                    blob.Add(temp);
                }
            }
            else
            {
                client.Disconnect();
                return;
            }

            List<Client> requests = new List<Client>();

            foreach (byte[] b in blob)
            {
                string s = System.Text.ASCIIEncoding.ASCII.GetString(b);
                s = s.Substring(0, s.IndexOf('\0'));
                requests.Add(TcpServer.GetClientFromName(s));
            }

            if (requests.FindAll(c => c.GetCharacter().ClanId == client.GetCharacter().ClanId).Count == memberCount)
            {
                List<Pair<Client, bool>> list = new List<Pair<Client, bool>>();

                foreach (Client c in requests)
                {
                    list.Add(new Pair<Client, bool>(c, false));
                }
                list.Add(new Pair<Client, bool>(client, true));

                ClanPackets.AskAgreement(requests, client, mode, request);
                ClanPackets.ResponseProposal(client, 0, mode, request);

                PendingClanWarRequest pc = new PendingClanWarRequest();
                pc.RequestId = request;
                pc.Players = list;
                pc.ClanName = client.GetCharacter().ClanName;
                pc.Requester = client;
                pc.RequestMode = mode;

                lock(Globals.PendingClanWar)
                    Globals.PendingClanWar.Add(pc);

                var responsetimer = new Timer(30000);
                responsetimer.Elapsed += (s, o) => CancelClanWarRequest(client, pc, responsetimer);
                responsetimer.Start();
            }
        }

        [PacketHandler(Operation.MatchReplyAgreement, PacketFlags.Character)]
        public static void ProcessResponseAgreement(Client client, PacketReader packetReader)
        {
            var uidProposer = packetReader.ReadMuid();
            var uidChar = packetReader.ReadMuid();
            var name = packetReader.ReadString();
            var proposal = packetReader.ReadInt32();
            var request = packetReader.ReadInt32();
            var agreement = packetReader.ReadBoolean();
            PendingClanWarRequest pc = Globals.PendingClanWar.Find(p => p.ClanName == client.GetCharacter().ClanName);

            if (agreement == true)
            {
                lock (Globals.PendingClanWar)
                {
                    pc.Players.Find(p => p.First == client).Second = true;
                }
            }

            lock (Globals.PendingClanWar)
            {
                if (pc.Players.FindAll(p => p.Second).Count == pc.Players.Count)
                {
                    List<Client> players = new List<Client>();
                    foreach (var p in pc.Players)
                        players.Add(p.First);
                    players.Add(pc.Requester);

                    ClanPackets.SearchRival(players);
                    Globals.PendingClanWar.Remove(pc);
                    ClanWarHandler.FindMatch(players);
                }
            }
        }

        public static void CancelClanWarRequest(Client client, PendingClanWarRequest clan, Timer timer)
        {
            lock (Globals.PendingClanWar)
                try
                {
                    Globals.PendingClanWar.Remove(clan);
                    timer.Enabled = false;

                }
                catch (ObjectDisposedException)
                {
                }

        }
    }
}
