using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Bunny.Players;
using Bunny.Utility;
using Bunny.Enums;

namespace Bunny.Core
{
    interface IDatabase
    {
        bool Initialize();
        int GetIdentity();

        #region Modules
        bool AccountExists(string user);
        void CreateAccount(string user, string password, ref AccountInfo accountInfo);
        void GetAccount(string user, string password, ref AccountInfo accountInfo);
        List<Pair<string, byte>> GetCharacterList(Int32 aid);
        bool CreateCharacter(Int32 aid, byte nCharNumber, string name, Int32 nSex, Int32 nHair, Int32 nFace, Int32 nCostume);
        void UpdateIndexes(Int32 aid);
        void UpdateBp(Int32 cid, Int32 newBounty);
        bool GetCharacter(Int32 aid, byte nIndex, CharacterInfo charInfo);
        int GetCid(Int32 aid, int marker);
        int GetCharacterCount(Int32 aid);
        bool CharacterExists(string name);
        void DeleteCharacter(Int32 aid, Int32 cid);
        int AddItem(Int32 cid, Int32 itemid, Int32 count = 0);
        void DeleteItem(Int32 ciid);
        void UpdateSlot(Int32 cid, ItemSlotType slot, Int32 itemid);
        void UpdateLevel(Int32 cid, UInt32 xp, Int32 level);
        bool IsInClan(Client client);
        bool ClanExists(string clanName);
        int CreateClan(string clanName, Client master, List<Pair<Client, bool>> members);
        void JoinClan(Int32 cid, Int32 clid);
        void ExpelMember(Int32 cid);
        void UpdateMember(Int32 cid, Int32 rank);
        Int32 GetClanId (string clanName);
        string GetCharacterName (Int32 cid);
        void GetClanInfo (Int32 clanId, ref ClanInfo clanInfo);
        #endregion
    }
}
