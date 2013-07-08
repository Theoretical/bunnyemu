using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Bunny.Enums;
using Bunny.Network;
using Bunny.Packet;
using Bunny.Packet.Assembled;
using Bunny.Utility;
using Bunny.Players;

namespace Bunny.Core
{
    class Client
    {
        public Player ClientPlayer;
        public bool IsAgent;
        public IPEndPoint PeerEnd;
        public string  ClientIp = String.Empty;
        public PacketFlags ClientFlags = PacketFlags.None;

        private readonly Socket _socket;
        private byte[] _stream = new byte[0];
        private readonly byte[] _buffer = new byte[Globals.Config.Tcp.ReceiveBuffer];
        private readonly byte[] _crypt = new byte[32];
        private byte _packetCounter;
        private bool _sending;
        private readonly SocketAsyncEventArgs _args = new SocketAsyncEventArgs();
        private readonly LockFreeQueue<PacketReader> _packetQueue = new LockFreeQueue<PacketReader>();
        private readonly LockFreeQueue<byte[]> _sendQueue = new LockFreeQueue<byte[]>();

        public Muid GetMuid() {
            return ClientPlayer.PlayerId;
        }
        
        public bool IsStaff() {
            return ClientPlayer.PlayerAccess >= UGradeId.Developer;
        }
        
        public GameStats GetGameStats() {
            return ClientPlayer.PlayerStats;
        }
        
        public CharacterInfo GetCharacter() {
            return ClientPlayer.PlayerCharacter;
        }

        public Stages.Stage GetStage() {
            return ClientPlayer.PlayerStage;
        }

        public bool InGame() {
            return GetStage() != null && GetGameStats().InGame;
        }

        public Channels.Channel GetChannel() {
            return ClientPlayer.PlayerChannel;
        }

        public void Unload()
        {
            if (GetStage() != null)
            {
                GetStage().GetTraits().Ruleset.GameLeaveBattle(this);
                GetStage().Leave(this);
            }

            if (GetChannel() != null)
                GetChannel().Leave(this);

            if (Globals.NatAgent != null && !IsAgent)
            {
                AgentPackets.UnbindPeer(Globals.NatAgent, GetMuid());
            }

            if (IsAgent)
            {
                Globals.NatAgent = null;
            }

            ClientFlags = PacketFlags.Login;
            ClientPlayer.PlayerLocation = Place.Outside;
            ClientPlayer.PlayerCharacter = new CharacterInfo();
        }
        public void Disconnect()
        {
            Unload();

            if (_socket.Connected)
                _socket.Close();

            TcpServer.Remove(this);
        }
        public void Send(PacketWriter packetReader)
        {
            var packet = packetReader.Process(++_packetCounter, _crypt);

            if (_sending)
            {
                _sendQueue.Enqueue(packet);
            }
            else Send(packet);
        }
        private void Send(byte[] pBuffer)
        {
            try
            {
                _args.Completed += HandleAsyncSend;
                _args.SetBuffer(pBuffer, 0, pBuffer.Length);
                _args.UserToken = this;
                _sending = true;
                _socket.SendAsync(_args);
            }
            catch (ObjectDisposedException)
            {
                Disconnect();
                return;
            }
            catch
            {
                Log.Write("Failed to send packet.");
                _sending = false;
            }
        }
        private void HandleAsyncSend(object pObject, SocketAsyncEventArgs pArgs)
        {
            pArgs.Completed -= HandleAsyncSend;
            if (_sendQueue.Count > 0)
                Send(_sendQueue.Dequeue());
            else _sending = false;

        }
        private void HandleReceive(IAsyncResult pResult)
        {
            int nTotalRecv;

            try
            {
                nTotalRecv = _socket.EndReceive(pResult);
            }
            catch
            {
                Disconnect();
                Log.Write("[{0}] Client Disconnected.", ClientIp );
                return;
            }


            if (nTotalRecv < 1)
            {
                Disconnect();
                Log.Write("[{0}] Client Disconnected.", ClientIp);
                return;
            }

            var bTemp = new byte[_stream.Length + nTotalRecv];
            Buffer.BlockCopy(_buffer, 0, bTemp, _stream.Length, nTotalRecv);
            if (_stream.Length > 0)
                Buffer.BlockCopy(bTemp, 0, _stream, 0, _stream.Length);

            _stream = bTemp;
            try
            {
                ProcessStream();
                ThreadPool.QueueUserWorkItem(ProcessQueue);
                Array.Clear(_buffer, 0, 4096);
                _socket.BeginReceive(_buffer, 0, 4096, SocketFlags.None, new AsyncCallback(HandleReceive), null);
            }
            catch
            {
                Log.Write("Error: Processing packet | Initializing Receieve.");
                Disconnect();
            }
        }
        private void ProcessStream()
        {
            var position = 0;
            var finished = false;

            while (!finished && position < _stream.Length)
            {
                if ((_stream.Length - position) >= 6)
                {
                    var bPacket = new byte[6];
                    Buffer.BlockCopy(_stream, position, bPacket, 0, 6);

                    if ((CryptFlags)bPacket[0] == CryptFlags.Encrypt)
                    {
                        PacketCrypt.Decrypt(bPacket, 2, 2, _crypt);
                        var size = BitConverter.ToUInt16(bPacket, 2);
                        if ((_stream.Length - position) >= size)
                        {
                            Array.Resize(ref bPacket, size);
                            Buffer.BlockCopy(_stream, position, bPacket, 0, size);
                            PacketCrypt.Decrypt(bPacket, 6, size - 6, _crypt);

                            var pReader = new PacketReader(bPacket, size);
                            _packetQueue.Enqueue(pReader);

                            position += size;
                        }
                        else finished = true;
                    }
                    else if ((CryptFlags)bPacket[0] == CryptFlags.Decrypt)
                    {
                        var size = BitConverter.ToUInt16(bPacket, 2);
                        if ((_stream.Length - position) >= size)
                        {
                            Array.Resize(ref bPacket, size);
                            Buffer.BlockCopy(_stream, position, bPacket, 0, size);

                            var pReader = new PacketReader(bPacket, size);
                            _packetQueue.Enqueue(pReader);

                            position += size;
                        }
                        else finished = true;

                    }
                }
                else finished = true;
            }

            var temp = new byte[_stream.Length - position];
            Buffer.BlockCopy(_stream, position, temp, 0, _stream.Length - position);
            _stream = temp;
        }
        private void ProcessQueue(object pObject)
        {
            while (_packetQueue.Count > 0)
            {
                PacketReader pReader;
                HandlerDelegate handler;
                try
                {
                    pReader = _packetQueue.Dequeue();
                }
                catch
                {
                    Log.Write("Could not dequeue packet?");
                    continue;
                }

                if (pReader.GetOpcode() != Operation.GameRequestTimeSync && pReader.GetOpcode() != Operation.MatchAgentRequestLiveCheck)
                    Log.Write("[{0}] Received: {1}", ClientIp, pReader.GetOpcode());

                if (Manager.Operations.TryGetValue(pReader.GetOpcode(), out handler))
                {
                    if (ClientFlags >= handler.Flags)
                    {
                        try
                        {
                            //Fuck the usage of the eventmanager.
                            handler.Processor(this, pReader);
                        }
                        catch (Exception e)
                        {
                            Log.Write("Error processing packet: {0}. Message: {1}", pReader.GetOpcode(), e);
                            Disconnect();
                            return;
                        }
                    }
                    else
                    {
                        Log.Write("[{0}] Attempted to bypass flags {1} - {2}!", ClientIp, ClientFlags, handler.Flags);
                    }
                }
                else
                {
                    Log.Write("[{0}] Received: unimplemented packet: {1} {2:X}!", ClientIp, Enum.IsDefined((typeof(Operation)), pReader.GetOpcode()) ? pReader.GetOpcode().ToString() : "", pReader.GetOpcode());
                }
            }
        }

        private void Handshake()
        {
            var bHandshake = new byte[26];
            var bClientKeys = new byte[32][];

            bClientKeys[0] = new byte[] {
                0x37, 0x04, 0x5D, 0x2E, 0x43, 0x3A,
                0x49, 0x53, 0x50, 0x05, 0x13, 0xC9, 
                0x28, 0xA4, 0x4D, 0x05
            };

            bClientKeys[1] = new byte[] {
                0x57, 0x02, 0x5B, 0x04, 0x34, 0x06, 0x01, 
                0x08, 0x37, 0x0A, 0x12, 0x69, 0x41, 0x38,
                0x0F, 0x78
            };

            //Client IP Address
            Buffer.BlockCopy(((IPEndPoint)_socket.RemoteEndPoint).Address.GetAddressBytes(), 0, _crypt, 0, 4);

            //Client Unknown
            Buffer.BlockCopy(BitConverter.GetBytes((UInt32)2), 0, _crypt, 4, 4);

            //Client MUID
            Buffer.BlockCopy(BitConverter.GetBytes(GetMuid().HighId), 0, _crypt, 8, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(GetMuid().LowId), 0, _crypt, 12, 4);

            //Static Client Key Part 1.
            Buffer.BlockCopy(bClientKeys[0], 0, _crypt, 16, 16);

            //Our actual packet
            Buffer.BlockCopy(BitConverter.GetBytes((Int16)10), 0, bHandshake, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(26), 0, bHandshake, 2, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(0), 0, bHandshake, 6, 4);
            Buffer.BlockCopy(_crypt, 4, bHandshake, 10, 12);
            Buffer.BlockCopy(_crypt, 0, bHandshake, 22, 4);

            for (int i = 0; i < 4; ++i)
            {
                uint a = BitConverter.ToUInt32(bClientKeys[1], i * 4);
                uint b = BitConverter.ToUInt32(_crypt, i * 4);
                Buffer.BlockCopy(BitConverter.GetBytes(a ^ b), 0, _crypt, i * 4, 4);
            }
            Send(bHandshake);
        }


        public Client(Socket pSocket, Muid pUid)
        {
            ClientIp = ((IPEndPoint)pSocket.RemoteEndPoint).Address.ToString();
            _socket = pSocket;
            ClientPlayer =  new Player(pUid);
            Log.Write("{0} Client connected", ClientIp);

            Handshake();
            _socket.BeginReceive(_buffer, 0, 4096, SocketFlags.None, new AsyncCallback(HandleReceive), null);
        }
    }
}
