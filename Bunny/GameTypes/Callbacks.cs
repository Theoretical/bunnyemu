using System;
using System.Collections.Generic;
using System.Threading;
using Bunny.Channels;
using Bunny.Core;
using Bunny.Enums;
using Bunny.Packet.Assembled;
using Bunny.Players;
using Bunny.Utility;

namespace Bunny.GameTypes
{
    partial class BaseGametype
    {

        protected int Initiailized;

        //This is the initial start of ANY round (called once in Deathmatch, multiple times in TDM and Duel)
        //Here is where yo initialize things such as a timer or item thread.
        public virtual void OnRoundStart()
        {
        }

        public virtual void OnClientsLoaded()
        {
        }

        public virtual void OnInitialStart()
        {
        }

        public virtual void ProcessRoundFinish()
        {
        }

        //This is how we determine what the stage round is and wether or not not spawn a player.
        public virtual void ProcessRoundState()
        {
            List<Client> clients;
            var traits = CurrentStage.GetTraits();

            lock (CurrentStage.ObjectLock)
                clients = new List<Client>(traits.Players);

            traits.State = StageState.Battle;
            Log.Write("Current state: {0}", traits.Round);
            if (traits.Round == RoundState.Prepare || traits.Round == RoundState.Countdown)
            {
                var loadedClients = clients.FindAll(c => c.ClientPlayer.PlayerStats.Loaded && c.ClientPlayer.PlayerStats.Entered);
				
                if (Initiailized == 0)
                {
                    if (loadedClients.Count == traits.Players.Count)
					{
                        Initiailized = 1;
                        //traits.Round = RoundState.Countdown;
                        //Battle.StageRoundUpdate(traits.Players, traits.StageId, traits.CurrentRound, traits.Round);

                        OnClientsLoaded();
                    }
                }
                else if (Initiailized == 1)
				{
                    var requested =
                        clients.FindAll(
                            c => c.ClientPlayer.PlayerStats.Loaded && c.ClientPlayer.PlayerStats.RequestedInfo);
							
                    if (requested.Count == traits.Players.Count)
                    {
                        if (!IsTeam())
                        {
                            /*
                            traits.Round = RoundState.Countdown;
                            traits.State = StageState.Battle;

                            Battle.StageRoundUpdate(traits.Players, traits.StageId, traits.CurrentRound, traits.Round);
                            */
                            traits.Round = RoundState.Play;
                            Battle.StageRoundUpdate(traits.Players, traits.StageId, traits.CurrentRound, traits.Round);

                            OnInitialStart();
                        }
                        else
                        {
                            var redTeam = clients.FindAll(c => c.ClientPlayer.PlayerTeam == Team.Red);
                            if (redTeam.Count != clients.Count && redTeam.Count > 0)
                            {
                                traits.Round = RoundState.Countdown;
                                Battle.StageRoundUpdate(traits.Players, traits.StageId, traits.CurrentRound,
                                                        traits.Round);


                                traits.Round = RoundState.Play;
                                traits.State = StageState.Battle;

                                Battle.StageRoundUpdate(traits.Players, traits.StageId, traits.CurrentRound,
                                                        traits.Round);

                                traits.Players.ForEach(delegate(Client c)
                                                           {
                                                               c.ClientPlayer.PlayerStats.Entered = false;
                                                               c.ClientPlayer.PlayerStats.RequestedInfo = false;
                                                               c.ClientPlayer.PlayerStats.Spawned = true;
                                                               c.ClientPlayer.PlayerStats.InGame = true;
                                                           });

                                OnInitialStart();
                            }
                            else
                            {
                                traits.Round = RoundState.Free;
                                Battle.StageRoundUpdate(traits.Players, traits.StageId, traits.CurrentRound, traits.Round);
                            }
                        }
			 		}
                    else
                    {
                        if (IsTeam())
                        {
                            traits.Round = RoundState.Free;
                            Battle.StageRoundUpdate(traits.Players, traits.StageId, traits.CurrentRound, traits.Round);

                        }
                    }

				}
		    }
            else if (traits.Round == RoundState.Play && IsTeam())
            {
                ProcessRoundFinish();
            }
            else if (traits.Round == RoundState.Free && IsTeam())
            {
                var blueTeam = clients.FindAll(c => c.ClientPlayer.PlayerTeam == Team.Blue && c.ClientPlayer.PlayerStats.RequestedInfo);
                var redTeam = clients.FindAll(c => c.ClientPlayer.PlayerTeam == Team.Red && c.ClientPlayer.PlayerStats.RequestedInfo);

                if (redTeam.Count > 0 && blueTeam.Count > 0)
                {
                    traits.Round = RoundState.Countdown;
                    Battle.StageRoundUpdate(traits.Players, traits.StageId, traits.CurrentRound,
                                            traits.Round);


                    traits.Round = RoundState.Play;
                    traits.State = StageState.Battle;

                    Battle.StageRoundUpdate(traits.Players, traits.StageId, traits.CurrentRound,
                                            traits.Round);

                    traits.Players.ForEach(delegate(Client c)
                    {
                        c.ClientPlayer.PlayerStats.Entered = false;
                        c.ClientPlayer.PlayerStats.RequestedInfo = false;
                        c.ClientPlayer.PlayerStats.Spawned = true;
                        c.ClientPlayer.PlayerStats.InGame = true;
                    });

                    OnInitialStart();
                }
            }
		}

        public virtual void OnGameKill(Client killer, Client victim)
        { 
        }
        public virtual void GameStartCallback(Client client, bool clanWar = false)
        {
            
            if (Globals.NatAgent != null)
            {
                AgentPackets.ReserveStageToAgent(Globals.NatAgent, CurrentStage.GetTraits().StageId);
                Log.Write("Reserved stage: {0} to NAT Server", CurrentStage.GetTraits().Name);
            }

            State = RoundState.Prepare;
            _stage.GetTraits().State = StageState.Battle;
            client.ClientPlayer.PlayerLocation = Place.Battle;
            GameInProgress = true; 

            lock (_stage.ObjectLock)
            {
                if (!clanWar)
                    Battle.StageLaunch(CurrentStage.GetTraits().Players, _stage.GetTraits());
                else
                {
                    StagePackets.LadderLaunch(_stage.GetTraits().Players, _stage.GetTraits());
                }
            }

            Channel.Refresh(client);
        }
        public virtual void GameStartRelayCallback(Client client)
        {
            State = RoundState.Prepare;
            _stage.GetTraits().State = StageState.Battle;
            client.ClientPlayer.PlayerLocation = Place.Battle;

            lock (_stage.ObjectLock)
            {
                Battle.StageRelayLaunch(CurrentStage.GetTraits().Players, _stage.GetTraits());
            }

            Channel.Refresh(client);
        }
        public virtual void GameLoadedCallback(Client client)
        {
            lock (_stage.ObjectLock)
            {
                Battle.LoadingComplete(CurrentStage.GetTraits().Players, client.GetMuid());
            }

            if (_stage.GetTraits().Master != client)
                client.ClientPlayer.PlayerState = ObjectStageState.NonReady;

            client.ClientPlayer.PlayerStats.InGame = true;
            client.ClientPlayer.PlayerStats.Loaded = true;
            client.ClientPlayer.PlayerLocation = Place.Battle;
            ProcessRoundState();
        }
        public virtual void GameLateJoinCallback(Client client)
        {
            var player = client.ClientPlayer;
            var traits = _stage.GetTraits();

            player.PlayerStats.Spawned = false;
            player.PlayerStats.Entered = true;
            player.PlayerStats.InGame = true;
            player.PlayerStats.Loaded = true;

            Battle.StageRoundUpdate(client, traits.StageId, traits.CurrentRound, traits.Round);
            ProcessRoundState();
        }
        public virtual void GameLeaveBattle(Client client)
        {
            lock (_stage.ObjectLock)
            {
                Globals.GunzDatabase.UpdateLevel(client.GetCharacter().CharacterId, client.GetCharacter().Xp, client.GetCharacter().Level);
                client.ClientPlayer.PlayerStats.Reset();

                Battle.LeaveBattle(CurrentStage.GetTraits().Players, client.GetMuid());

                var traits = _stage.GetTraits();
                if (traits.Players.FindAll(c => !c.GetGameStats().InGame).Count == traits.Players.Count)
                {
                    traits.State = StageState.Standby;

                    foreach (var c in _stage.GetTraits().Players)
                    {
                        c.ClientPlayer.PlayerStats = new GameStats();

                        if (c != traits.Master)
                            client.ClientPlayer.PlayerState = ObjectStageState.NonReady;
                    }

                    if (_gameType == ObjectStageGameType.DeathMatch && (this as Deathmatch).ItemSpawns != null)
                        (this as Deathmatch).ItemSpawns.Abort();
                    else if (_gameType == ObjectStageGameType.Berserker && (this as Berserker).ItemSpawns != null)
                        (this as Berserker).ItemSpawns.Abort();
                    else if (_gameType == ObjectStageGameType.TeamDeathMatchExtreme && (this as TeamDeathmatchExtreme).ItemSpawns != null)
                        (this as TeamDeathmatchExtreme).ItemSpawns.Abort();

                    _stage.GetTraits().Players.ForEach(c => StagePackets.ResponseSettings(_stage.GetTraits().Players, _stage.GetTraits()));
                }
            }
        }
        public virtual void GameEnterCallback(Client client)
        {
            lock (_stage.ObjectLock)
            {
                Battle.GameEnterBattle(CurrentStage.GetTraits().Players, client);
                Battle.StageRoundUpdate(client, CurrentStage.GetTraits().StageId, CurrentStage.GetTraits().CurrentRound, State);
            }

            client.ClientPlayer.PlayerStats.Entered = true;
            client.ClientPlayer.PlayerStats.Spawned = false;
            client.ClientPlayer.PlayerStats.InGame = false;

            ProcessRoundState();
        }
        public virtual void GameInfoCallback (Client client)
        {
            var traits = _stage.GetTraits();
            lock (_stage.ObjectLock)
            { 
                Battle.BattleResponseInfo(client, _teamScores[0], _teamScores[1], traits.Players);
            }

            client.ClientPlayer.PlayerStats.Spawned = true;
            client.ClientPlayer.PlayerStats.RequestedInfo = true;
            client.ClientPlayer.PlayerStats.Loaded = true;
            ProcessRoundState();

            if ((!IsTeam() && !IsQuestDerived() && !IsDuel()) || IsExtreme())
            {

                Battle.StageRoundUpdate(client, traits.StageId, traits.CurrentRound, traits.Round);
                Spawn(client);
            }
            else if (IsTeam())
            {
                if (_roundState != RoundState.Play)
                {
                    client.ClientPlayer.PlayerStats.Entered = true;
                }
                else
                {
                    client.ClientPlayer.PlayerStats.Entered = false;
                    client.ClientPlayer.PlayerStats.Spawned = false;
                    Battle.StageRoundUpdate(client, traits.StageId, traits.CurrentRound, traits.Round);
                }
            }
        }

        public virtual void GameKillCallback (Client killer, Client victim)
        {
            victim.ClientPlayer.PlayerStats.Spawned = false;
            if (_gameType == ObjectStageGameType.Training)
            {
                var uids = new Pair<Muid, Muid>(killer.GetMuid(), victim.GetMuid());
                var args = new Pair<UInt32, UInt32>(0, 0);
                lock (CurrentStage.ObjectLock)
                {
                    Battle.GameDie(CurrentStage.GetTraits().Players, uids, args);
                }
            }
            else
            {
                if (killer != victim)
                {
                    var exp = ExpManager.GetExpFromKill(killer.GetCharacter().Level,
                                                        victim.GetCharacter().Level);

                    killer.GetCharacter().Xp += exp;

                    var uids = new Pair<Muid, Muid>(killer.GetMuid(), victim.GetMuid());
                    var args = new Pair<UInt32, UInt32>((exp << 16), 0);
                    lock (CurrentStage.ObjectLock)
                    {
                        Battle.GameDie(CurrentStage.GetTraits().Players, uids, args);

                        //killer.GetCharacter().Level =
                          //  (Int16) ExpManager.GetLevel((Int32) killer.GetCharacter().Xp);
                        Log.Write("Exp Gained: {0} | Exp To Next Level: {1} | Current Exp: {2}", exp, ExpManager.GetExp(killer.GetCharacter().Level + 1), killer.GetCharacter().Xp);
                        if (ExpManager.Level(killer.GetCharacter().Level,
                                             killer.GetCharacter().Xp))
                        {
                            killer.GetCharacter().Level++;
                            Battle.GameLevelUp(CurrentStage.GetTraits().Players, killer.GetMuid(),
                                               killer.GetCharacter().Level);
                            EventManager.AddCallback(
                                () =>
                                Globals.GunzDatabase.UpdateLevel(killer.GetCharacter().CharacterId,
                                                     killer.GetCharacter().Xp,
                                                     killer.GetCharacter().Level));
                        }
                    }

                    killer.ClientPlayer.PlayerStats.Kills++;
                    victim.ClientPlayer.PlayerStats.Deaths++;

                    if (killer.ClientPlayer.PlayerStats.Kills%20 == 0)
                    {
                        EventManager.AddCallback(
                            () =>
                            Globals.GunzDatabase.UpdateLevel(killer.GetCharacter().CharacterId,
                                                 killer.GetCharacter().Xp,
                                                 killer.GetCharacter().Level));
                    }
                }
            }
            OnGameKill(killer, victim);
        }
        protected void Spawn(Client client, int sleep = 0)
        {
            if (sleep > 0)
                Thread.Sleep(sleep * 1000);
            var map = _stage.GetTraits().CurrentMap;
            var r = new Random();
            var index = r.Next(1, map.Deathmatch.Count);

            var coords = map.Deathmatch[index];

           
            lock (_stage.ObjectLock)
            {
                Battle.GameSpawn(CurrentStage.GetTraits().Players, client.GetMuid(), coords.First, coords.Second);
            }
        }
    }
}
