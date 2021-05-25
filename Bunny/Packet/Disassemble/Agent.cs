using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bunny.Core;
using Bunny.Enums;
using Bunny.Network;
using Bunny.Packet.Assembled;
using Bunny.Utility;

namespace Bunny.Packet.Disassemble
{
    class Agent
    {
        [PacketHandler(Operation.MatchRegisterAgent, PacketFlags.None)]
        public static void ProcessRegisterAgent(Client client, PacketReader packetReader)
        {
            //read our shit brah.
            var address = packetReader.ReadString();
            var tcpPort = packetReader.ReadInt32();
            var udpPort = packetReader.ReadInt32();

            if (address == Globals.Config.Agent.Ip && Globals.NatAgent == null)
            {
                client.IsAgent = true;
                Globals.NatAgent = client;
                Log.Write("[{0}] Agent Registered. Info: {1} - {2} - {3}", client.ClientIp, address, tcpPort, udpPort);
            }
        }

        [PacketHandler(Operation.MatchAgentRequestLiveCheck, PacketFlags.None)]
        public static void ProcessLiveCheck(Client client, PacketReader packetReader)
        {
            var timeStamp = packetReader.ReadInt32();
            AgentPackets.ResponseLiveCheckk(client, timeStamp);
        }

        [PacketHandler(Operation.MatchRequestPeerRelay, PacketFlags.None)]
        public static void ProcessPeerRelay(Client client, PacketReader packetReader)
        {
            if (client.GetStage() == null)
                return;

            var charId = packetReader.ReadMuid();
            var peerId = packetReader.ReadMuid();

            //Now attempt to bind them!
            if (Globals.NatAgent != null)
            {
                AgentPackets.RelayPeer(Globals.NatAgent, new System.Tuple<Muid, Muid, Muid>(charId, peerId, client.GetStage().GetTraits().StageId));
                Log.Write("Binding player to NAT");
            }
            else
            {
                AgentPackets.AgentError(client, 10001);
            }
        }


        [PacketHandler(Operation.P2PRoute, PacketFlags.None)]
        public static void ProcessRoute(Client client, PacketReader packetReader)
        {
            if (client.GetStage() == null)
                return;

            var peerId = packetReader.ReadMuid();
            var totalSize = packetReader.ReadInt32();

            var blob = new byte[totalSize];

            packetReader.Read(blob, 0, totalSize);

            var peer = TcpServer.GetClientFromUid(peerId);

            AgentPackets.RoutePeer(peer, client.GetMuid(), totalSize, 1, blob);
        }

        [PacketHandler(Operation.AgentPeerReady, PacketFlags.None)]
        public static void ProcessPeerReady(Client client, PacketReader packetReader)
        {
            if (Globals.NatAgent == null)
                return;

            var charId = packetReader.ReadMuid();
            var peerId = packetReader.ReadMuid();
            var agentId = Globals.NatAgent.GetMuid();

            var peer1 = TcpServer.GetClientFromUid(charId);
            var peer2 = TcpServer.GetClientFromUid(peerId);


            if (peer1 != null)
            {
                AgentPackets.AgentLocateToClient(peer1, agentId);
                AgentPackets.ResponsePeerRelay(peer1, peerId);
            }

            if (peer2 != null)
            {
                AgentPackets.AgentLocateToClient(peer2, Globals.NatAgent.GetMuid());
                AgentPackets.ResponsePeerRelay(peer2, charId);
            }
        }

    }   
}
