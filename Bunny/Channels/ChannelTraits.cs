using System;
using System.Collections.Generic;
using Bunny.Core;
using Bunny.Enums;

namespace Bunny.Channels
{
    class ChannelTraits
    {
        public Muid ChannelId;
        public string ChannelName = "";
        public Int32 MinLevel;
        public Int32 MaxLevel;
        public Int32 MaxUsers;
        public ChannelRule Rule = ChannelRule.Novice;
        public ChannelType Type = ChannelType.General;
        public List<Client> Playerlist = new List<Client>();

        public ChannelTraits(Muid uid)
        {
            ChannelId = uid;
        }

        public ChannelTraits()
        {
            
        }
    }
}
