using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using Bunny.Enums;
using Bunny.Items;
using Bunny.Players;
using Bunny.Utility;


namespace Bunny.Core
{
    class MySQLDatabase : IDatabase
    {
        private MySqlConnection _sqlConnection;

        public bool Initialize()
        {
            try
            {
                var connectionString = string.Format(
                    "server={2}; user={0}; password={1}; database={3};",
                    Globals.Config.Database.User, Globals.Config.Database.Pass, Globals.Config.Database.Host,
                    Globals.Config.Database.DatabaseName
                    );

                _sqlConnection = new MySqlConnection(connectionString);
                _sqlConnection.Open();
                return true;
            }
            catch (Exception e)
            {
                Log.Write("Error Initializing DB: {0}", e.Message);
                return false;
            }
        }
        public void Execute(string szQuery)
        {
            lock (_sqlConnection)
            {
                using (var command = new MySqlCommand(szQuery, _sqlConnection))
                    command.ExecuteNonQuery();
            }
        }
        public void Execute(string szQuery, ArrayList pArray)
        {
            lock (_sqlConnection)
            {
                using (var command = new MySqlCommand(szQuery, _sqlConnection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader == null)
                            return;

                        while (reader.Read())
                            for (int i = 0; i < reader.FieldCount; ++i)
                                pArray.Add(reader.IsDBNull(i) ? 0 : reader[i]);
                    }
                }
            }
        }
        public int GetQuery(string szQuery)
        {
            lock (_sqlConnection)
            {
                using (var command = new MySqlCommand(szQuery, _sqlConnection))
                    return Convert.ToInt32(command.ExecuteScalar());
            }
        }
        public object GetQueryScalar(string szQuery)
        {
            lock (_sqlConnection)
            {
                using (var command = new MySqlCommand(szQuery, _sqlConnection))
                    return command.ExecuteScalar();
            }
        }
        public int GetIdentity(string szQuery)
        {
            lock (_sqlConnection)
            {
                using (var command = new MySqlCommand(szQuery, _sqlConnection))
                using (var reader = command.ExecuteReader())
                {
                    if (reader == null)
                        return 0;

                    reader.Read();
                    return Convert.ToInt32(reader[0]);
                }
            }
        }


        #region Modules
        public bool AccountExists(string user)
        {
            return GetQuery("SELECT COUNT(AID) FROM account WHERE UserID='" + user + "'") == 1;
        }
        public void CreateAccount(string user, string password, ref AccountInfo accountInfo)
        {
            Execute(
                string.Format("INSERT INTO account (userid, ugradeid, pgradeid, name, password) VALUES ('{0}', 0, 0, 'Bob', '{1}')",
                              user, password));
            GetAccount(user, password, ref accountInfo);

        }
        public void GetAccount(string szUser, string szPassword, ref AccountInfo accountInfo)
        {
            if (accountInfo == null)
                accountInfo = new AccountInfo();

            lock (_sqlConnection)
            {
                using (var command = new MySqlCommand("SELECT aid,ugradeid,pgradeid from account WHERE userid=@user AND password=@pass", _sqlConnection))
                {
                    command.Parameters.AddWithValue("@user", szUser);
                    command.Parameters.AddWithValue("@pass", szPassword);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader == null || !reader.Read())
                        {
                            accountInfo.UserId = "INVALID";
                            accountInfo.Access = UGradeId.Guest;
                            accountInfo.Premium = 0;
                            return;
                        }

                        accountInfo.AccountId = Convert.ToInt32(reader["AID"]);
                        accountInfo.Access = (UGradeId)Convert.ToByte(reader["UGradeID"]);
                        accountInfo.Premium = (PGradeId)Convert.ToByte(reader["PGradeID"]);
                        accountInfo.UserId = szUser;

                    }
                }
            }
        }
        public List<Pair<string, byte>> GetCharacterList(Int32 aid)
        {
            lock (_sqlConnection)
            {
                using (var command = new MySqlCommand("SELECT Name,Level FROM `character` WHERE AID=@aid ORDER BY CharNum ASC LIMIT 4", _sqlConnection))
                {
                    command.Parameters.AddWithValue("@aid", aid);

                    var characters = new List<Pair<string, byte>>();
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader == null)
                            return characters;

                        for (byte b = 0; reader.Read(); b++)
                        {
                            var name = Convert.ToString(reader["Name"]);
                            var level = Convert.ToByte(reader["Level"]);
                            characters.Add(new Pair<string, byte>(name, level));
                        }
                    }

                    return characters;
                }
            }
        }
        public bool CreateCharacter(Int32 aid, byte nCharNumber, string szName, Int32 nSex, Int32 nHair, Int32 nFace, Int32 nCostume)
        {
            var melee = new int[] { 1, 2, 1, 2, 2, 1 };
            var primary = new int[] { 5001, 5002, 4005, 4001, 4002, 4006 };
            var secondary = new int[] { 4001, 0, 5001, 4006, 0, 4006 };
            var custom1 = new int[] { 30301, 30301, 30401, 30401, 30401, 30101 };
            var custom2 = new int[] { 0, 0, 0, 0, 30001, 30001 };

            var malechest = new int[] { 21001, 21001, 21001, 21001, 21001, 21001 };
            var malelegs = new int[] { 23001, 23001, 23001, 23001, 23001, 23001 };
            var femalechest = new int[] { 21501, 21501, 21501, 21501, 21501, 21501 };
            var femalelegs = new int[] { 23501, 23501, 23501, 23501, 23501, 23501 };

            lock (_sqlConnection)
            {
                using (var command = new MySqlCommand("insert into `character` (aid,charnum,name,sex,hair,face) values (@AID,@CharNum,@Name,@Sex,@hair,@face)", _sqlConnection))
                {
                    command.Parameters.AddWithValue("@AID", aid);
                    command.Parameters.AddWithValue("@CharNum", nCharNumber);
                    command.Parameters.AddWithValue("@Name", szName);
                    command.Parameters.AddWithValue("@Sex", nSex);
                    command.Parameters.AddWithValue("@Hair", nHair);
                    command.Parameters.AddWithValue("@Face", nFace);

                    command.ExecuteNonQuery();

                    var charId = GetIdentity("select @@identity");
                    var id = AddItem(charId, melee[nCostume]);
                    UpdateSlot(charId, ItemSlotType.melee_slot, id);
                    id = AddItem(charId, primary[nCostume]);
                    UpdateSlot(charId, ItemSlotType.primary_slot, id);
                    id = AddItem(charId, secondary[nCostume]);
                    UpdateSlot(charId, ItemSlotType.secondary_slot, id);
                    id = AddItem(charId, custom1[nCostume]);
                    UpdateSlot(charId, ItemSlotType.custom1_slot, id);
                    id = AddItem(charId, custom2[nCostume]);
                    UpdateSlot(charId, ItemSlotType.custom2_slot, id);

                    if (nSex == 0)
                    {
                        id = AddItem(charId, malechest[nCostume]);
                        UpdateSlot(charId, ItemSlotType.chest_slot, id);
                        id = AddItem(charId, malelegs[nCostume]);
                        UpdateSlot(charId, ItemSlotType.legs_slot, id);
                    }
                    else
                    {
                        id = AddItem(charId, femalechest[nCostume]);
                        UpdateSlot(charId, ItemSlotType.chest_slot, id);
                        id = AddItem(charId, femalelegs[nCostume]);
                        UpdateSlot(charId, ItemSlotType.legs_slot, id);
                    }
                    return true;
                }
            }
        }
        public void UpdateIndexes(Int32 aid)
        {
            lock (_sqlConnection)
            {
                using (var command = new MySqlCommand("SELECT Name,Level FROM cCharacter` WHERE AID=@aid ORDER BY CharNum ASC LIMIT 4", _sqlConnection))
                {
                    command.Parameters.AddWithValue("@aid", aid);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader == null)
                            return;

                        var ii = new List<int>();
                        for (var i = 0; reader.Read(); ++i)
                            ii.Add(Convert.ToInt32(reader[0]));

                        reader.Close();

                        for (var i = 0; i < ii.Count; ++i)
                            Execute(string.Format("update `character` set CharNum={0} where cid={1}", i, ii[i]));
                    }
                }
            }
        }
        public void UpdateBp(Int32 cid, Int32 newBounty)
        {
            Execute(string.Format("UPDATE `character` SET BP={0} WHERE CID={1}", newBounty, cid));
        }
        public bool GetCharacter(Int32 aid, byte nIndex, CharacterInfo charInfo)
        {
            lock (_sqlConnection)
            {
                using (var command = new MySqlCommand("SELECT * FROM `character` WHERE AID=@aid AND CharNum=@index", _sqlConnection))
                {
                    command.Parameters.AddWithValue("@aid", aid);
                    command.Parameters.AddWithValue("@index", nIndex);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader == null || !reader.Read())
                            return false;

                        charInfo.CharacterId = Convert.ToInt32(reader["CID"]);
                        charInfo.ClanId = 0;
                        charInfo.Name = Convert.ToString(reader["Name"]);
                        charInfo.Level = Convert.ToByte(reader["Level"]);
                        charInfo.Sex = Convert.ToByte(reader["Sex"]);
                        charInfo.Hair = Convert.ToByte(reader["Hair"]);
                        charInfo.Face = Convert.ToByte(reader["Face"]);
                        charInfo.Xp = Convert.ToUInt32(reader["XP"]);
                        charInfo.Bp = Convert.ToInt32(reader["BP"]);
                        charInfo.BonusRate = 0.0f;
                        charInfo.Prize = 0;
                        charInfo.Fr = 0;
                        charInfo.Er = 0;
                        charInfo.Cr = 0;
                        charInfo.Wr = 0;
                        charInfo.SafeFalls = 0;
                        charInfo.Kills = Convert.ToInt32(reader["KillCount"]);
                        charInfo.Deaths = Convert.ToInt32(reader["DeathCount"]);
                        reader.Close();

                        var items = new ArrayList();

                        Execute("SELECT head_slot,chest_slot,hands_slot,legs_slot,Feet_slot,fingerl_slot,fingerr_slot,melee_slot,primary_slot,secondary_slot,custom1_slot,custom2_slot,avatar_slot,community1_slot,community2_slot,longbuff1_slot,longbuff2_slot FROM `character` WHERE CID=" + charInfo.CharacterId, items);
                        for (int i = 0; i < 17; i++)
                        {
                            charInfo.EquippedItems[i] = new Item();
                            charInfo.EquippedItems[i].ItemCid = Convert.ToInt32(items[i]);
                            charInfo.EquippedItems[i].RentHour = 525600;
                        }


                        var clanInfo = new ArrayList();
                        Execute("SELECT CLID,Grade,ContPoint FROM clanmember WHERE CID=" + charInfo.CharacterId, clanInfo);
                        if (clanInfo.Count > 0)
                        {
                            charInfo.ClanId = (Int32)clanInfo[0] < 0 ? 0 : (Int32)clanInfo[0];
                            charInfo.ClanGrade = (ClanGrade)Convert.ToInt32(clanInfo[1]);
                            charInfo.ClanPoint = Convert.ToInt16(clanInfo[2]);


                            charInfo.ClanName = Convert.ToString(GetQueryScalar("SELECT Name FROM clan WHERE CLID=" + charInfo.ClanId));
                        }

                        command.CommandText = "SELECT ItemID,CIID,RentHourPeriod,quantity FROM characteritem WHERE CID=" + charInfo.CharacterId;
                        reader.Close();
                        using (var dataReader = command.ExecuteReader())
                        {
                            if (dataReader != null)
                            {
                                while (dataReader.Read())
                                {
                                    Item nItem = new Item();
                                    nItem.ItemId = Convert.ToInt32(dataReader["itemid"]);
                                    nItem.ItemCid = Convert.ToInt32(dataReader["CIID"]);
                                    nItem.RentHour =
                                        Convert.ToInt32(dataReader.IsDBNull(2) ? 0 : dataReader["RentHourPeriod"]);
                                    nItem.Quantity = Convert.ToInt32(dataReader.IsDBNull(3) ? 0 : dataReader["quantity"]);
                                    charInfo.Items.Add(nItem);
                                }
                            }
                        }
                        for (var i = 0; i < 12; i++)
                        {
                            var item = charInfo.Items.Find(ii => ii.ItemCid == charInfo.EquippedItems[i].ItemCid);
                            charInfo.EquippedItems[i].ItemId = item == null ? 0 : item.ItemId;
                        }
                        return true;
                    }
                }
            }
        }
        public int GetCid(Int32 aid, int marker)
        {
            return GetQuery(string.Format("SELECT CID FROM `character` WHERE CharNum={0} AND AID={1}", marker, aid));
        }
        public int GetCharacterCount(Int32 aid)
        {
            return GetQuery(string.Format("SELECT COUNT(AID) FROM `character` WHERE AID={0}", aid));
        }
        public bool CharacterExists(string name)
        {
            return GetQuery("SELECT COUNT(Name) FROM `character` WHERE Name='" + name + "'") > 0;
        }
        public void DeleteCharacter(Int32 aid, Int32 cid)
        {
            Execute("DELETE FROM `character` WHERE CID=" + cid);
            UpdateIndexes(aid);
        }
        public int AddItem(Int32 cid, Int32 itemid, Int32 count = 0)
        {
            Execute(string.Format("INSERT INTO characteritem (CID,ItemID,RegDate,Quantity) VALUES ({0},{1},CURDATE(), {2})", cid, itemid, count));
            return GetIdentity(string.Format("select @@identity"));
        }
        public void Deletetem(Int32 ciid)
        {
            Execute(string.Format("DELETE FROM characteritem WHERE CIID={0}", ciid));
        }
        public void UpdateSlot(Int32 cid, ItemSlotType slot, Int32 itemid)
        {
            Execute(string.Format("UPDATE `character` SET {0}={1} WHERE CID={2}", slot, itemid, cid));
        }
        public void UpdateLevel(Int32 cid, UInt32 xp, Int32 level)
        {
            Execute(string.Format("UPDATE `character` SET XP={0},Level={1} WHERE CID={2}", xp, level, cid));
        }
        public bool IsInClan(Client client)
        {
            return GetQuery("SELECT COUNT(CID) FROM clanmember WHERE CID=" + client.GetCharacter().CharacterId) > 0;
        }
        public bool ClanExists(string clanName)
        {
            return (GetQuery("SELECT COUNT(Name) FROM clan WHERE Name='" + clanName + "'") > 0);
        }
        public int CreateClan(string clanName, Client master, Client member1, Client member2, Client member3, Client member4)
        {

            Execute(string.Format("INSERT INTO clan (Name,MasterCID,RegDate) VALUES ('{0}',{1},GetDate())", clanName, master.GetCharacter().CharacterId));
            var clanId = GetIdentity(string.Format("select @@identity"));

            Execute(string.Format("INSERT INTO clanmember (CLID,CID,Grade,RegDate) VALUES ({0},{1}, 1, GetDate())", clanId, master.GetCharacter().CharacterId));
            Execute(string.Format("INSERT INTO clanmember (CLID,CID,Grade,RegDate) VALUES ({0},{1}, 9, GetDate())", clanId, member1.GetCharacter().CharacterId));
            Execute(string.Format("INSERT INTO clanmember (CLID,CID,Grade,RegDate) VALUES ({0},{1}, 9, GetDate())", clanId, member2.GetCharacter().CharacterId));
            Execute(string.Format("INSERT INTO clanmember (CLID,CID,Grade,RegDate) VALUES ({0},{1}, 9, GetDate())", clanId, member3.GetCharacter().CharacterId));
            Execute(string.Format("INSERT INTO clanmember (CLID,CID,Grade,RegDate) VALUES ({0},{1}, 9, GetDate())", clanId, member4.GetCharacter().CharacterId));

            return clanId;
        }
        public void JoinClan(Int32 cid, Int32 clid)
        {
            Execute(string.Format("INSERT INTO clanmember (CLID,CID,Grade,RegDate) VALUES ({0},{1}, 9, GetDate())", clid, cid));
        }
        public void ExpelMember(Int32 cid)
        {
            Execute("DELETE FROM clanmember WHERE CID=" + cid);
        }
        public void UpdateMember(Int32 cid, Int32 rank)
        {
            Execute("UPDATE clanmember SET Grade=" + rank + " WHERE CID=" + cid);
        }
        public Int32 GetClanId(string clanName)
        {
            lock (_sqlConnection)
            {
                using (var command = new MySqlCommand("SELECT CLID FROM clan where Name=@name", _sqlConnection))
                {
                    command.Parameters.AddWithValue("@name", clanName);

                    return (Int32)command.ExecuteScalar();
                }
            }
        }
        public string GetCharacterName(Int32 cid)
        {
            lock (_sqlConnection)
            {
                using (var command = new MySqlCommand("SELECT name FROM `character` where cid=@cid", _sqlConnection))
                {
                    command.Parameters.AddWithValue("@cid", cid);
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                            return string.Empty;

                        return Convert.ToString(reader["name"]);
                    }
                }
            }
        }
        public void GetClanInfo(Int32 clanId, ref ClanInfo clanInfo)
        {
            lock (_sqlConnection)
            {
                using (
                    var command = new MySqlCommand("SELECT * FROM clan WHERE CLID=@clid",
                                                   _sqlConnection))
                {
                    command.Parameters.AddWithValue("@clid", clanId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader == null || !reader.Read())
                        {
                            clanInfo = null;
                            return;
                        }

                        clanInfo.ClanId = clanId;
                        clanInfo.Name = Convert.ToString(reader["name"]);
                        clanInfo.Points = (Int32)reader["exp"];
                        clanInfo.Level = Convert.ToInt16(reader["level"]);
                        clanInfo.TotalPoints = (Int32)reader["point"];
                        clanInfo.Wins = Convert.ToInt16(reader["wins"]);
                        clanInfo.Losses = Convert.ToInt16(reader["losses"]);
                        clanInfo.Ranking = (Int32)reader["ranking"];
                        clanInfo.EmblemChecksum = reader["emblemurl"] == null ? 0 : 1;
                        var cid = (Int32)reader["mastercid"];

                        reader.Close();

                        clanInfo.Master = GetCharacterName(cid);
                        clanInfo.MemberCount = Convert.ToInt16(GetQuery("SELECT COUNT(CID) FROM clanmember WHERE CLID=" + clanId));
                    }

                }
            }
        }
        #endregion
    }
}
