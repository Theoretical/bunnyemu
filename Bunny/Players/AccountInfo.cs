using System;
using Bunny.Enums;

namespace Bunny.Players
{
    class AccountInfo
    {
        public Int32 AccountId;
        public string UserId = "";
        public UGradeId Access = UGradeId.Guest;
        public PGradeId Premium = PGradeId.Free;
    }
}
