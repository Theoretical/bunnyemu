using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bunny.Enums
{
    public enum ChannelType : byte
    {
        General,
        Private,
        User,
        Clan,
        ClanWar, // Test.
        DuelTournament = 5,
        None = 255
    }
    public enum ChannelRule : byte
    {
        Novice,
        Newbie,
        Rookie,
        Mastery,
        Elite
    }

}
