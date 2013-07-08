using Bunny.Enums;
using Bunny.Packet.Assembled;
using Bunny.Players;
using Bunny.Stages;

namespace Bunny.GameTypes
{
    partial class BaseGametype
    {
        private RoundState _roundState;
        private ObjectStageGameType _gameType;
        private int[] _teamScores = new int[2];
        private Stage _stage;
        private bool _inProgress = true;

        public RoundState State
        {
            get { return _roundState;  }
            set { _roundState = value;  }
        }

        public ObjectStageGameType GameType
        {
            get { return _gameType;  }
            set { _gameType = value; }
        }

        public int[] Scores
        {
            get { return _teamScores; }
            set { _teamScores = value; }
        }

        public Stage CurrentStage
        {
            get { return _stage; }
            set { _stage = value; }
        }

        public bool GameInProgress
        {
            get { return _inProgress;  }
            set { _inProgress = value;  }
        }

        public BaseGametype(Stage stage, ObjectStageGameType gameType)
        {
            _stage = stage;
            _gameType = gameType;
        }

        public void GameOver(Team winner = Team.Spectator)
        {
            var traits = CurrentStage.GetTraits();
            traits.CurrentRound = 0;
            traits.State = StageState.Standby;
            traits.Round = RoundState.Prepare;

            Battle.StageRoundUpdate(traits.Players, traits.StageId, 0, RoundState.Finish);
            Battle.StageRoundUpdate(traits.Players, traits.StageId, 0, RoundState.Exit);
            Battle.StageFinish(traits.Players, traits.StageId);

            traits.DuelQueue = new QueueInfo();

            foreach (var client in traits.Players)
            {
                client.GetGameStats().Reset();

                if (client != traits.Master)
                    client.ClientPlayer.PlayerState = ObjectStageState.NonReady;
            }

            Battle.StageRoundUpdate(traits.Players, traits.StageId, 0, RoundState.Prepare);

            if (_gameType == ObjectStageGameType.DeathMatch && (this as Deathmatch).ItemSpawns != null)
                (this as Deathmatch).ItemSpawns.Abort();
            else if (_gameType == ObjectStageGameType.Berserker && (this as Berserker).ItemSpawns != null)
                (this as Berserker).ItemSpawns.Abort();
            else if (_gameType == ObjectStageGameType.TeamDeathMatchExtreme && (this as TeamDeathmatchExtreme).ItemSpawns != null)
                (this as TeamDeathmatchExtreme).ItemSpawns.Abort();

        }

        public bool IsQuestDerived()
        {
            return _gameType == ObjectStageGameType.Quest || _gameType == ObjectStageGameType.Survival;
        }

        public bool IsDuel()
        {
            return _gameType == ObjectStageGameType.Duel;
        }

        public bool IsBerserker()
        {
            return _gameType == ObjectStageGameType.Berserker;
        }

        public bool IsAssassination()
        {
            return _gameType == ObjectStageGameType.Assassination;
        }

        public bool IsTeam()
        {
            return _gameType == ObjectStageGameType.TeamDeathMatch || _gameType == ObjectStageGameType.TeamGladiator ||
                   _gameType == ObjectStageGameType.TeamDeathMatchExtreme || _gameType == ObjectStageGameType.Assassination;
        }

        public bool IsExtreme()
        {
            return _gameType == ObjectStageGameType.TeamDeathMatchExtreme;
        }
    }
}
