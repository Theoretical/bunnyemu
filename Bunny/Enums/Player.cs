namespace Bunny.Enums
{
    public enum UGradeId : byte
    {
        Guest = 0,
        Registered = 1,
        Event = 2,
        Criminal = 100,
        Warning1 = 101,
        Warning2 = 102,
        Warning3 = 103,
        ChatDenied = 104,
        Penalty = 105,
        EventMaster = 252,
        Banned = 253,
        Developer = 254,
        Administrator = 255
    }

    public enum PGradeId : byte
    {
        Free,
        PremiumIp
    }

    public enum Place : byte
    {
        Outside,
        Lobby,
        Stage,
        Battle,
        End
    }

    public enum DuelTournamentRanks
    {
        Start,
        GoldenKadachis,
        GoldenKatanas,
        GoldenTripleSwords,
        GoldenCrossSwords,
        GoldenDualDaggers,
        GoldenDagger,
        SilverTripleSwords,
        SilverCrossSwords,
        SilverDualDaggers,
        SilverDagger,
        End
    }
}
