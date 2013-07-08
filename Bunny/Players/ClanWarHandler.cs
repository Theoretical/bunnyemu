using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bunny.Core;

namespace Bunny.Players
{
    class ClanWar
    {
        public List<Client> Team;

        public ClanWar(List<Client> team) {
            Team = team; 
        }
    }

    class ClanWarHandler
    {
        private static readonly object ObjectLock = new object();
        private static List<ClanWar> _doublesLadder = new List<ClanWar>();
        private static List<ClanWar> _triplesLadder = new List<ClanWar>();
        private static List<ClanWar> _quadraLadder = new List<ClanWar>();

        public static bool FindMatch(List<Client> clan)
        {
            var clanwar = new ClanWar(clan);
            if (clan.FindAll(c => c.ClientPlayer.ClanWar).Count > 0)
            {
                //! Player is already in a clan war.
                Log.Write("Player in cw already!");
                return false;
            }

            lock (ObjectLock)
            {
                switch (clan.Count)
                {
                    case 2:
                        _doublesLadder.Add(clanwar);
                        Log.Write("2 added!");
                        break;
                    case 3:
                        _triplesLadder.Add(clanwar);
                        Log.Write("3 added!");
                        break;
                    case 4:
                        _quadraLadder.Add(clanwar);
                        Log.Write("4 added!");
                        break;
                    default:
                        return false;
                }


                if (_doublesLadder.Count > 1)
                {
                    //WE HAVE A WINNAAAA
                    Log.Write("2 added!");
                    var channel = clan[0].GetChannel();
                    channel.StageList().CreateClanwar(_doublesLadder[0].Team, _doublesLadder[1].Team);

                    _doublesLadder.RemoveAt(0);
                    _doublesLadder.RemoveAt(1);
                }

                if (_triplesLadder.Count > 1)
                {
                    Log.Write("3 added!");
                    //WE HAVE A WINNAAAA
                    var channel = clan[0].GetChannel();
                    channel.StageList().CreateClanwar(_triplesLadder[0].Team, _triplesLadder[1].Team);

                    _triplesLadder.RemoveAt(0);
                    _triplesLadder.RemoveAt(1);
                }

                if (_triplesLadder.Count > 1)
                {
                    Log.Write("4 added!");
                    //WE HAVE A WINNAAAA
                    var channel = clan[0].GetChannel();
                    channel.StageList().CreateClanwar(_quadraLadder[0].Team, _quadraLadder[1].Team);

                    _quadraLadder.RemoveAt(0);
                    _quadraLadder.RemoveAt(1);
                }

                return true;
            }
        }
    }
}
