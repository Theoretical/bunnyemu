using Bunny.Core;
using Bunny.Enums;
using Bunny.Network;
using Bunny.Packet.Assembled;

namespace Bunny.Packet.Disassemble
{
    class Login
    {
        [PacketHandler(Operation.MatchLogin, PacketFlags.None)]
        public static void ProcessLoginRequest(Client client, PacketReader packetReader)
        {
            var user = packetReader.ReadString();
            var pass = packetReader.ReadString();
            var version = packetReader.ReadInt32();
            var checksum = packetReader.ReadInt32();
            var totalSize = packetReader.ReadInt32();
            var blobSize = packetReader.ReadInt32();
            var blobCount = packetReader.ReadInt32();
            var md5 = new byte[blobSize*blobCount];
            
            //packetReader.Read(md5, 0, blobSize);

            if (blobSize > 32)
            {
                //client.Disconnect();
                //return;
            }

            pass = "asd";
            if (!Globals.AcceptedString.IsMatch(user) || !Globals.AcceptedString.IsMatch(pass))
            {
                Match.ResponseLogin(client, Results.LoginAuthenticationFailed, user, UGradeId.Guest, PGradeId.Free, new Muid(0,0));
                client.Disconnect();
                return;
            }

            if (Globals.Config.Server.AutoRegistration)
            {
                if (user.StartsWith("!r"))
                {
                    user = user.Substring(2);
                    if (!Globals.GunzDatabase.AccountExists(user))
                        Globals.GunzDatabase.CreateAccount(user, pass, ref client.ClientPlayer.PlayerAccount);
                    else
                    {
                        Match.ResponseNotify(client, "That account is already in use!", 1);
                        client.Disconnect();
                        return;
                    }
                }
                else
                    Globals.GunzDatabase.GetAccount(user, pass, ref client.ClientPlayer.PlayerAccount);
            }
            else
                Globals.GunzDatabase.GetAccount(user, pass, ref client.ClientPlayer.PlayerAccount);


            if (client.ClientPlayer.PlayerAccount.AccountId == 0)
            {
                Match.ResponseLogin(client, Results.LoginIncorrectPassword, user, UGradeId.Guest, PGradeId.Free, new Muid(0, 0));
                client.Disconnect();
                return;
            }
            
            if (client.ClientPlayer.PlayerAccount.Access == UGradeId.Banned || client.ClientPlayer.PlayerAccount.Access == UGradeId.Criminal)
            {
                Match.ResponseLogin(client, Results.LoginBannedId, user, UGradeId.Guest, PGradeId.Free, new Muid(0, 0));
                client.Disconnect();
                return;
            }
            
            if (version != Globals.Config.Client.Version)
            {
                //Match.ResponseLogin(client, Results.LoginInvalidVersion, user, UGradeId.Guest, PGradeId.Free, new Muid(0, 0));
               // return;
            }

            if (Globals.Config.Client.UseCrc)
            {
                if ((checksum^0) != Globals.Config.Client.FileList)
                {
                    //Match.ResponseLogin(client, Results.LoginInvalidVersion, user, UGradeId.Guest, PGradeId.Free, new Muid(0, 0));
                    //client.Disconnect();
                    return;
                }
            }

            var inuse = TcpServer.GetClientFromAid(client.ClientPlayer.PlayerAccount.AccountId);

            if (inuse != null && inuse != client)
            {
                inuse.Disconnect();
            }

            client.ClientFlags = PacketFlags.Login;
            Match.ResponseLogin(client, Results.Accepted, user, client.ClientPlayer.PlayerAccount.Access, PGradeId.Free, client.GetMuid());
        }

        [PacketHandler(Operation.MatchRequestAccountCharList, PacketFlags.Login)]
        public static void ProcessAccountCharList (Client client, PacketReader packetReader)
        {
            client.Unload();

            var characters = Globals.GunzDatabase.GetCharacterList(client.ClientPlayer.PlayerAccount.AccountId);
            Match.ResponseCharList(client, characters);
        }

        [PacketHandler(Operation.MatchRequestCreateChar, PacketFlags.Login)]
        public static void ProcessCreateChar(Client client, PacketReader packetReader)
        {
            var uid = packetReader.ReadMuid();
            var index = packetReader.ReadInt32();
            var name = packetReader.ReadString();
            var sex = packetReader.ReadInt32();
            var hair = packetReader.ReadInt32();
            var face = packetReader.ReadInt32();
            var costume = packetReader.ReadInt32();
            var result = Results.Accepted;

            if (uid != client.GetMuid() || index < 0 || index > 4 || sex < 0 || sex > 1)
            {
                client.Disconnect();
                return;
            }

            if (!Globals.AcceptedString.IsMatch(name))
                result = Results.CharacterEnterName;
            else if (Globals.GunzDatabase.GetCharacterCount(client.ClientPlayer.PlayerAccount.AccountId) >= 4)
                result = Results.CharacterNameNonExistant;
            else if (Globals.GunzDatabase.CharacterExists(name))
                result = Results.CharacterNameInUse;
            else if (!Globals.GunzDatabase.CreateCharacter(client.ClientPlayer.PlayerAccount.AccountId, (byte)index, name, sex, hair, face, costume))
                result = Results.CharacterInvalidName;

            Match.ResponseCreateChar(client, result, name);
        }

        [PacketHandler(Operation.MatchRequestAccountCharInfo, PacketFlags.Login)]
        public static void ProcessCharInfo(Client client, PacketReader packetReader)
        {
            var index = packetReader.ReadByte();

            if (index < 0 || index > 4)
            {
                client.Disconnect();
                return;
            }

            client.GetCharacter().CharNum = index;
            client.GetCharacter().UGrade = client.ClientPlayer.PlayerAccount.Access;
            Globals.GunzDatabase.GetCharacter(client.ClientPlayer.PlayerAccount.AccountId, index, client.GetCharacter());

            Match.ResponseCharInfo(client);
        }

        [PacketHandler(Operation.MatchRequestDeleteChar, PacketFlags.Login)]
        public static void ProcessDeleteChar(Client client, PacketReader packetReader)
        {
            var uid = packetReader.ReadMuid();
            var index = packetReader.ReadInt32();
            var name = packetReader.ReadString();
            var result = Results.Accepted;

            if (uid != client.GetMuid() || !Globals.AcceptedString.IsMatch(name) || index < 0 || index > 4)
            {
                client.Disconnect();
                return;
            }

            var cid = Globals.GunzDatabase.GetCid(client.ClientPlayer.PlayerAccount.AccountId, index);
            if (cid == 0)
            {
                result = Results.CharacterDeleteDisabled;
            }
            else
            {
                Globals.GunzDatabase.DeleteCharacter(client.ClientPlayer.PlayerAccount.AccountId, cid);
            }

            Match.ResponseDeleteCharacter(client, result);
        }

        [PacketHandler(Operation.MatchRequestSelectChar, PacketFlags.Login)]
        public static void ProcessSelectChar(Client client, PacketReader packetReader)
        {
            var uid = packetReader.ReadMuid();
            var index = packetReader.ReadInt32();

            if (uid != client.GetMuid() || index < 0 || index > 4)
            {
                client.Disconnect();
                return;
            }

            client.ClientFlags = PacketFlags.Character;
            Globals.GunzDatabase.GetCharacter(client.ClientPlayer.PlayerAccount.AccountId, (byte)index, client.GetCharacter());
            Match.ResponseSelectCharacter(client);
        }


    }
}
