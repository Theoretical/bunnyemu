namespace Bunny.Enums
{
    enum SpawnType
    {
        Solo,
        Red,
        Blue,
        Wait
    }
    public enum ObjectStageState
    {
        NonReady,
        Ready,
        Shop,
        Equipment,
        End
    }
    public enum StageState : byte
    {
        Standby,
        Coutdown,
        Battle,
        End
    }
    public enum RoundState : byte
    {
        Prepare,
        Countdown,
        Play,
        Finish,
        Exit,
        Free,
        Failed
    }
    public enum ObjectStageGameType : byte
    {
        DeathMatch,
        TeamDeathMatch,
        Gladiator,
        TeamGladiator,
        Assassination,
        Training,
        Survival,
        Quest,
        Berserker,
        TeamDeathMatchExtreme,
        Duel
    }
    public enum Team : int
    {
        All,
        Spectator,
        Red,
        Blue,
        End
    }

    public enum StageType
    {
        None,
        Regular,
        Locked,
        LockedUnknown,
        LevelRestricted,
    }

    public enum ObjectCache : byte
    {
        Keep,
        New,
        Expire
    }

    public enum RelayMaps : int
    {
        Mansion,
        Prison,
        Station,
        PrisonII,
        BattleArena,
        Town,
        Dungeon,
        Ruin,
        Island,
        Garden,
        Castle,
        Factory,
        Port,
        LostShrine,
        Stairway,
        Snow_Town,
        Hall,
        Catacomb,
        Jail,
        Shower_Room,
        High_Haven,
        Citadel,
        WeaponShop,
    }
}
