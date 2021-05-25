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
        BattleArena,
        Castle,
        Crypt,
        Courtyard,
        Dojo,
        Dungeon,
        DungeonII,
        Factory,
        Garden,
        HighHaven,
        Island,
        Lodge,
        LostShrine,
        Port,
        Prison,
        PrisonII,
        Ruin,
        SnowTown,
        Stairway,
        Station,
        Town,
        ThievesDen,
        WeaponShop,
        
        // duel
        DUEL_BEGIN,
        Catacomb,
        Corridor,
        DivingRoom,
        Foyer,
        Hall,
        Jail,
        Library,
        Passage,
        ShowerRoom,
        Stockade,
        Vault
    }
}
