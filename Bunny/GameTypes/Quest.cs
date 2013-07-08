using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bunny.Enums;
using Bunny.Packet;
using Bunny.Quest;
using Bunny.Stages;

namespace Bunny.GameTypes
{
    class Quest : BaseGametype
    {
        public override void GameStartCallback(Core.Client client, bool clanWar = false)
        {
            var info = new QuestGameInfo();
 
            using (var packet = new PacketWriter(Operation.QuestGameInfo, CryptFlags.Encrypt))
            {
                packet.Write(1, 67);
            }
            base.GameStartCallback(client, clanWar);
        }
        public Quest(Stage stage) : base(stage, ObjectStageGameType.Quest)
        {
            
        }
    }
}
