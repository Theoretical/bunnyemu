using System;
using Bunny.Channels;
using Bunny.Core;
using Bunny.Enums;
using Bunny.Stages;

namespace Bunny.Players
{
    class GameStats
    {
        public bool Loaded;
        public bool Entered;
        public bool RequestedInfo;
        public bool InGame;
        public bool Spawned;
        public bool LateJoined;
        public Int32 Kills;
        public Int32 Deaths;

        public void Reset()
        {
            Loaded = false;
            Entered = false;
            RequestedInfo = false;
            InGame = false;
            Spawned = false;
            LateJoined = false;
            Kills = 0;
            Deaths = 0;
        }
    }
    class Player
    {
        public Muid PlayerId;
        public AccountInfo PlayerAccount = new AccountInfo();
        public UGradeId PlayerAccess = UGradeId.Guest;
        public CharacterInfo PlayerCharacter = new CharacterInfo();
        public Place PlayerLocation;
        public Channel PlayerChannel;
        public Stage PlayerStage;
        public Team PlayerTeam = Team.Red;
        public ObjectStageState PlayerState = ObjectStageState.NonReady;
        public byte StageIndex;
        public int ChannelPage;
        public GameStats PlayerStats = new GameStats();
        public Int32 LastTimeSync;
        public bool ClanWar;

        public Player(Muid uid)
        {
            PlayerId = uid;
        }
    }
}
