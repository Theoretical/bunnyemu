using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bunny.Core;
using Bunny.Enums;

namespace Bunny.Packet.Assembled
{
    class AgentPackets
    {
        public static void ResponseLiveCheckk (Client client, Int32 timeStamp)
        {
            using (var packet = new PacketWriter(Operation.MatchAgentResponseLiveCheck, CryptFlags.Encrypt))
            {
                packet.Write(timeStamp);

                client.Send(packet);
            }
        }

        public static void RelayPeer (Client client, System.Tuple<Muid,Muid,Muid> uids)
        {
            using (var packet = new PacketWriter(Operation.AgentRelayPeer, CryptFlags.Encrypt))
            {
                packet.Write(uids.Item1);
                packet.Write(uids.Item2);
                packet.Write(uids.Item3);

                client.Send(packet);
            }
        }

        public static void RoutePeer(Client client, Muid sender, Int32 size, Int32 count, byte[] blob)
        {
            using (var packet = new PacketWriter(Operation.P2PRoute, CryptFlags.Decrypt))
            {
                packet.Write(sender);
                packet.Write(size);
                packet.Write(blob);
                client.Send(packet);
            }
        }

        public static void AgentError (Client client, Int32 error)
        {
            using (var packet = new PacketWriter(Operation.AgentError, CryptFlags.Encrypt))
            {
                packet.Write(error);
                client.Send(packet);
            }
        }

        public static void AgentLocateToClient(Client client, Muid agentId)
        {
            using (var packet = new PacketWriter(Operation.AgentLocateToClient, CryptFlags.Encrypt))
            {
                packet.Write(agentId);
                packet.Write(Globals.Config.Agent.RemoteIp);
                packet.Write((Int32)Globals.Config.Agent.TcpPort);                
                packet.Write((Int32)Globals.Config.Agent.UdpPort);

                Log.Write("Telling client to locate to: {0}:{1}:{2}", Globals.Config.Agent.RemoteIp,
                          Globals.Config.Agent.TcpPort, Globals.Config.Agent.UdpPort);
                client.Send(packet);
            }
        }

        public static void ResponsePeerRelay(Client client, Muid playerId)
        {
            using (var packet = new PacketWriter(Operation.MatchResponsePeerRelay, CryptFlags.Encrypt))
            {
                packet.Write(playerId);
                client.Send(packet);
            }
        }

        public static void ReserveStageToAgent(Client client, Muid stageId)
        {
            using (var packet = new PacketWriter(Operation.AgentStageReserve, CryptFlags.Encrypt))
            {
                packet.Write(stageId);
                client.Send(packet);
            }
        }

        public static void ReleaseStageToAgent(Client client, Muid stageId)
        {
            using (var packet = new PacketWriter(Operation.AgentStageRelease, CryptFlags.Encrypt))
            {
                packet.Write(stageId);
                client.Send(packet);
            }
        }

        public static void UnbindPeer(Client client, Muid playerId)
        {
            using (var packet = new PacketWriter(Operation.AgentPeerUnbind, CryptFlags.Encrypt))
            {
                packet.Write(playerId);
                client.Send(packet);
            }
        }
    }
}
