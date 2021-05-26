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
        private string _connectionString;

        public int GetIdentity() { return 0; }
        public bool Initialize()
        {
            try
            {
                var connectionString = string.Format(
                    "server={2}; user={0}; password={1}; database={3};",
                    Globals.Config.Database.User, Globals.Config.Database.Pass, Globals.Config.Database.Host,
                    Globals.Config.Database.DatabaseName
                    );

                _connectionString = connectionString;
                using (var sqlConnection = new MySqlConnection(connectionString))
                {
                    sqlConnection.Open();
                    return true;
                }
            }
            catch (Exception e)
            {
                Log.Write("Error Initializing DB: {0}", e.Message);
                return false;
            }
        }

        public int GetIdentity(MySqlConnection conn)
        {
                using (var command = new MySqlCommand("SELECT @@identity", conn))
                {
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
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand("SELECT COUNT(AID) FROM account WHERE UserID=@user", conn))
                {
                    cmd.Parameters.AddWithValue("@user", user);
                    return Convert.ToInt32(cmd.ExecuteScalar()) == 1;
                }
            }
        }

        public void CreateAccount(string user, string password, ref AccountInfo accountInfo)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand("INSERT INTO account(userid, access, premium, name, password, email) VALUES (@user, @ugradeid, @pgradeid, @name, @password, @email)", conn))
                {
                    cmd.Parameters.AddWithValue("user", user);
                    cmd.Parameters.AddWithValue("ugradeid", UGradeId.Registered);
                    cmd.Parameters.AddWithValue("pgradeid", PGradeId.Free);
                    cmd.Parameters.AddWithValue("name", user);
                    cmd.Parameters.AddWithValue("password", password);
                    cmd.Parameters.AddWithValue("email", "Asd@asd.com");
                    cmd.ExecuteNonQuery();
                }
            }
            GetAccount(user, password, ref accountInfo);

        }
        public void GetAccount(string szUser, string szPassword, ref AccountInfo accountInfo)
        {
            if (accountInfo == null)
                accountInfo = new AccountInfo();

            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var command = new MySqlCommand("SELECT aid,access as ugradeid,premium as pgradeid from account WHERE userid=@user AND password=@pass", conn))
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
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var command = new MySqlCommand("SELECT Name,Level FROM `character` WHERE AID=@aid ORDER BY CharNum ASC LIMIT 4", conn))
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

            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var command = new MySqlCommand("insert into `character` (aid,charnum,name,sex,hair,face) values (@AID,@CharNum,@Name,@Sex,@hair,@face)", conn))
                {
                    command.Parameters.AddWithValue("@AID", aid);
                    command.Parameters.AddWithValue("@CharNum", nCharNumber);
                    command.Parameters.AddWithValue("@Name", szName);
                    command.Parameters.AddWithValue("@Sex", nSex);
                    command.Parameters.AddWithValue("@Hair", nHair);
                    command.Parameters.AddWithValue("@Face", nFace);

                    command.ExecuteNonQuery();

                    var charId = GetIdentity(conn);

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
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var command = new MySqlCommand("SELECT Name,Level FROM `character` WHERE AID=@aid ORDER BY CharNum ASC LIMIT 4", conn))
                {
                    command.Parameters.AddWithValue("@aid", aid);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader == null)
                            return;

                        var characterList = new List<int>();
                        for (var i = 0; reader.Read(); ++i)
                            characterList.Add(Convert.ToInt32(reader[0]));

                        reader.Close();

                        for (var i = 0; i < characterList.Count; ++i)
                        {
                            using (var cmd = new MySqlCommand("update `character` set charnum=@charnum where cid=@cid", conn))
                            {
                                cmd.Parameters.AddWithValue("@charnum", i);
                                cmd.Parameters.AddWithValue("@cid", characterList[i]);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
        }
        public void UpdateBp(Int32 cid, Int32 newBounty)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var command = new MySqlCommand("UPDATE `character` SET bp=@bp where cid=@cid", conn))
                {
                    command.Parameters.AddWithValue("@bp", newBounty);
                    command.Parameters.AddWithValue("@cid", cid);
                    command.ExecuteNonQuery();
                }
            }
        }
        public bool GetCharacter(Int32 aid, byte nIndex, CharacterInfo charInfo)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var command = new MySqlCommand("SELECT * FROM `character` WHERE AID=@aid AND CharNum=@index", conn))
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

                        using (var cmd = new MySqlCommand("SELECT head_slot,chest_slot,hands_slot,legs_slot,Feet_slot,fingerl_slot,fingerr_slot,melee_slot,primary_slot,secondary_slot,custom1_slot,custom2_slot FROM `character` WHERE CID=@cid", conn))
                        {
                            cmd.Parameters.AddWithValue("@cid", charInfo.CharacterId);
                            using (var itemReader = cmd.ExecuteReader())
                            {
                                while(itemReader.Read())
                                {
                                    for (int i = 0; i < itemReader.FieldCount; ++i)
                                    {
                                        charInfo.EquippedItems[i] = new Item();

                                        charInfo.EquippedItems[i].ItemCid = Convert.ToInt32(itemReader.IsDBNull(i) ? 0 : itemReader[i]);
                                        charInfo.EquippedItems[i].RentHour = 525600;
                                    }

                                }
                            }
                        }

                        using (var clanCmd = new MySqlCommand("SELECT CLID,Grade,ContPoint FROM clanmember WHERE CID=@cid", conn))
                        {
                            clanCmd.Parameters.AddWithValue("@cid", charInfo.CharacterId);
                            using (var clanReader = clanCmd.ExecuteReader())
                            {
                                while (clanReader.Read())
                                {
                                    if (clanReader.FieldCount > 0)
                                    {
                                        charInfo.ClanId = Convert.ToInt32(clanReader.IsDBNull(0) ? 0 : clanReader[0]);
                                        charInfo.ClanGrade = (ClanGrade)Convert.ToInt32(clanReader[1]);
                                        charInfo.ClanPoint = Convert.ToInt16(clanReader[2]);
                                    }

                                }
                            }
                        }

                        if (charInfo.ClanId > 0)
                        {
                            using (var nameCmd = new MySqlCommand("SELECT Name FROM clan WHERE CLID=@clid", conn))
                            {
                                nameCmd.Parameters.AddWithValue("@clid", charInfo.ClanId);
                                charInfo.ClanName = Convert.ToString(nameCmd.ExecuteScalar());
                            }
                        }

                        using (var itemCmd = new MySqlCommand("SELECT ItemID,CIID,RentHourPeriod,cnt as quantity FROM characteritem WHERE CID=@cid", conn))
                        {
                            itemCmd.Parameters.AddWithValue("@cid", charInfo.CharacterId);
                            using (var dataReader = itemCmd.ExecuteReader())
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
                            if (charInfo.EquippedItems[i] == null)
                                charInfo.EquippedItems[i] = new Item();

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
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand("SELECT CID FROM `character` WHERE CharNum=@num AND AID=@aid", conn))
                {
                    cmd.Parameters.AddWithValue("@num", marker);
                    cmd.Parameters.AddWithValue("@aid", aid);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public int GetCharacterCount(Int32 aid)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand("SELECT COUNT(AID) from `character` where AID=@aid", conn))
                {
                    cmd.Parameters.AddWithValue("@aid", aid);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public bool CharacterExists(string name)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand("SELECT COUNT(CID) from `character` where name=@name", conn))
                {
                    cmd.Parameters.AddWithValue("@name", name);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        public void DeleteCharacter(Int32 aid, Int32 cid)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand("DELETE from `character` where CID=@cid", conn))
                {
                    cmd.Parameters.AddWithValue("@cid", cid);
                    cmd.ExecuteNonQuery();
                }
            }

            UpdateIndexes(aid);
        }

        public int AddItem(Int32 cid, Int32 itemid, Int32 count = 0)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand("INSERT INTO characteritem (CID,ItemID,RegDate,cnt) VALUES (@cid, @itemid, CURDATE(), @count)", conn))
                {
                    cmd.Parameters.AddWithValue("@cid", cid);
                    cmd.Parameters.AddWithValue("@itemid", itemid);
                    cmd.Parameters.AddWithValue("@count", count);
                    cmd.ExecuteNonQuery();
                }
            }

            return GetIdentity();
        }

        public void DeleteItem(Int32 ciid)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand("DELETE from `characteritem` where ciid=@ciid", conn))
                {
                    cmd.Parameters.AddWithValue("@ciid", ciid);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpdateSlot(Int32 cid, ItemSlotType slot, Int32 itemid)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand($"UPDATE `character` SET {slot}=@itemid WHERE CID=@cid", conn))
                {
                    cmd.Parameters.AddWithValue("@itemid", itemid);
                    cmd.Parameters.AddWithValue("@cid", cid);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpdateLevel(Int32 cid, UInt32 xp, Int32 level)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand("UPDATE `character` SET XP=@xp,Level=@level WHERE CID=@cid", conn))
                {
                    cmd.Parameters.AddWithValue("@xp", xp);
                    cmd.Parameters.AddWithValue("@level", level);
                    cmd.Parameters.AddWithValue("@cid", cid);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public bool IsInClan(Client client)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand("SELECT COUNT(CID) FROM clanmember WHERE CID=@cid", conn))
                {
                    cmd.Parameters.AddWithValue("@cid", client.GetCharacter().CharacterId);
                    return Convert.ToInt32(cmd.ExecuteNonQuery()) > 0;
                }
            }
        }

        public bool ClanExists(string clanName)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand("SELECT COUNT(Name) FROM clan WHERE Name=@name", conn))
                {
                    cmd.Parameters.AddWithValue("@name", clanName);
                    return Convert.ToInt32(cmd.ExecuteNonQuery()) > 0;
                }
            }
        }

        public int CreateClan(string clanName, Client master, List<Pair<Client, bool>> members)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand("INSERT INTO clan(Name, MasterCID, added_on) VALUES(@name,@master, CURDATE())", conn))
                {
                    cmd.Parameters.AddWithValue("@name", clanName);
                    cmd.Parameters.AddWithValue("@master", master.GetCharacter().CharacterId);
                    cmd.ExecuteNonQuery();
                }

                var clanId = GetIdentity(conn);
                using (var masterCmd = new MySqlCommand("INSERT INTO clanmember(CLID, CID, Grade, added_on) VALUES (@clid, @cid, @grade, CURDATE())", conn))
                {
                    masterCmd.Parameters.AddWithValue("@clid", clanId);
                    masterCmd.Parameters.AddWithValue("@cid", master.GetCharacter().CharacterId);
                    masterCmd.Parameters.AddWithValue("@grade", ClanGrade.Master);
                    masterCmd.ExecuteNonQuery();
                }
                foreach (var member in members)
                {
                    using (var memberCmd = new MySqlCommand("INSERT INTO clanmember(CLID, CID, Grade, added_on) VALUES (@clid, @cid, @grade, NOW())", conn))
                    {
                        memberCmd.Parameters.AddWithValue("@clid", clanId);
                        memberCmd.Parameters.AddWithValue("@cid", member.First.GetCharacter().CharacterId);
                        memberCmd.Parameters.AddWithValue("@grade", ClanGrade.User);
                        memberCmd.ExecuteNonQuery();
                    }
                }

                return clanId;
            }
            
        }

        public void JoinClan(Int32 cid, Int32 clid)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand("INSERT INTO clanmember (CLID,CID,Grade,RegDate) VALUES (@clid, @cid, @grade, NOW())", conn))
                {
                    cmd.Parameters.AddWithValue("@cid", cid);
                    cmd.Parameters.AddWithValue("@clid", clid);
                    cmd.Parameters.AddWithValue("@grade", ClanGrade.User);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void ExpelMember(Int32 cid)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand("DELETE from `clanmember` where cid=@cid", conn))
                {
                    cmd.Parameters.AddWithValue("@cid", cid);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpdateMember(Int32 cid, Int32 grade)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand("UPDATE clanmember SET Grade=@grade where CID=@cid", conn))
                {
                    cmd.Parameters.AddWithValue("@grade", grade);
                    cmd.Parameters.AddWithValue("@cid", cid);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public Int32 GetClanId(string clanName)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var command = new MySqlCommand("SELECT CLID FROM clan where Name=@name", conn))
                {
                    command.Parameters.AddWithValue("@name", clanName);
                    return (Int32)command.ExecuteScalar();
                }
            }
        }
        public string GetCharacterName(Int32 cid)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var command = new MySqlCommand("SELECT name FROM `character` where cid=@cid", conn))
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
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var command = new MySqlCommand("SELECT * FROM clan WHERE CLID=@clid", conn))
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
                        clanInfo.Ranking = (Int32)reader["totalranking"];
                        clanInfo.EmblemChecksum = reader["emblemurl"] == null ? 0 : 1;
                        var cid = (Int32)reader["mastercid"];

                        reader.Close();

                        clanInfo.Master = GetCharacterName(cid);
                        using (var clanCmd = new MySqlCommand("SELECT COUNT(CID) FROM clanmember WHERE CLID=@clid", conn))
                        {
                            clanCmd.Parameters.AddWithValue("@clid", clanId);
                            clanInfo.MemberCount = Convert.ToInt16(clanCmd.ExecuteScalar());
                        }
                    }

                }
            }
        }
        #endregion
    }
}
