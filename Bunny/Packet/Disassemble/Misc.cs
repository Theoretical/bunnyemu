using Bunny.Core;
using Bunny.Enums;
using Bunny.Network;
using Bunny.Packet.Assembled;

namespace Bunny.Packet.Disassemble
{
    class Misc
    {
        [PacketHandler(Operation.MatchRequestMySimpleCharInfo, PacketFlags.None)]
        public static void ProcessMySimpleCharInfo (Client client, PacketReader packetReader)
        {
            Match.ResponseMySimpleCharInfo(client);
        }

        [PacketHandler(Operation.MatchWhisper, PacketFlags.None)]
        public static void ProcessWhisper(Client client, PacketReader packetReader)
        {
            var senderName = packetReader.ReadString();
            var targetName = packetReader.ReadString();
            var message = packetReader.ReadString();

            var target = TcpServer.GetClientFromName(targetName);

            if (target != null)
            {
                Match.Whisper(target, targetName, senderName, message);
            }
            else
            {
                Match.Notify(client, 51);
            }
        }

        [PacketHandler(Operation.AdminAnnounce, PacketFlags.None)]
        public static void ProcessAdminAnnounce (Client client, PacketReader packetReader)
        {
            var adminId = packetReader.ReadMuid();
            var message = packetReader.ReadString();

            if (client.ClientPlayer.PlayerAccount.Access == UGradeId.Administrator
                || client.ClientPlayer.PlayerAccount.Access == UGradeId.Developer
                || client.ClientPlayer.PlayerAccount.Access == UGradeId.EventMaster)
            {
                Match.Announce(client, message);
                return;
            }

            client.Disconnect();
            return;
        }
    }
}
