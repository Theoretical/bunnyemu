using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bunny.Core;
using Bunny.Enums;
using Bunny.GameTypes;
using Bunny.Packet.Assembled;

namespace Bunny.Stages
{
    class StageList
    {
        private readonly List<Stage> _stages = new List<Stage>();

        public void Add(StageTraits traits)
        {
            var s = new Stage(traits);
            s.GetTraits().Ruleset = new Deathmatch(s);

            _stages.Add(s);
        }
        public void CreateClanwar(List<Client> red, List<Client> blue)
        {
            var traits = new StageTraits();
            
            traits.StageId = Globals.StageCounter.GetNext();
            traits.Name = "LADDER_GAME";
            traits.MaxPlayers = (byte)(red.Count + blue.Count);
            traits.RoundCount = 4;
            traits.Locked = true;
            traits.Gametype = ObjectStageGameType.TeamDeathMatch;
            traits.Password = "ladder_game";
            traits.Master = red[0];
            traits.Map = "Town"; //default for now
            traits.CurrentMap = Globals.Maps.GetMap(traits.Map);
            traits.WinThePoint = true;

            var stage = new Stage(traits);
            traits.Ruleset = new TeamDeathmatch(stage);

            foreach (var client in red)
            {
                client.ClientPlayer.PlayerTeam = Team.Red;
                stage.Join(client);
                StagePackets.LadderPrepare(client, (int)Team.Red);
            }
            StagePackets.ResponseSettings(red, traits);
            foreach (var client in blue)
            {
                client.ClientPlayer.PlayerTeam = Team.Blue;
                stage.Join(client);
                StagePackets.LadderPrepare(client, (int)Team.Blue);
            }
            StagePackets.ResponseSettings(blue, traits);

            traits.Ruleset.GameStartCallback(red[0], true);
        }
        public void Remove(Stage stage)
        {
            _stages.Remove(stage);
        }
        public Stage Find(Muid stageId)
        {
            return _stages.Find(s => s.GetTraits().StageId == stageId);
        }
        public int Index(Stage stage)
        {
            return _stages.IndexOf(stage);
        }
        public List<Stage> GetList()
        {
            return _stages;
        }
    }
}
    