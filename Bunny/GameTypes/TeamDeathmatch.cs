using System;
using System.Collections.Generic;
using System.Threading;
using Bunny.Core;
using Bunny.Enums;
using Bunny.Packet.Assembled;
using Bunny.Stages;
using Timer = System.Timers.Timer;

namespace Bunny.GameTypes
{
    class TeamDeathmatch : BaseGametype
    {
        public Timer GameTimer;
        private volatile bool _killThread;
        
        private void OnRoundEnd()
        {
            GameInProgress = false;
            ProcessRoundFinish();
        }

        private void CheckSpawns()
        {
            var traits = CurrentStage.GetTraits();
            var map = traits.CurrentMap;

            lock (map.TeamItems)
            {
                foreach (var i in map.TeamItems)
                {
                    if (i.NextSpawn <= DateTime.Now && i.Taken)
                    {
                        i.ItemUid = traits.WorldItemUid;
                        Interlocked.Increment(ref traits.WorldItemUid);
                        Battle.SpawnWorldItem(CurrentStage.GetTraits().Players, i);
                        lock (traits.WorldItems)
                            traits.WorldItems.Add(i.Clone());
                        Log.Write("Spawning item: {0}. Next Spawn: {1}", i.ItemId,
                                  DateTime.Now.AddSeconds(i.SpawnTime));

                        i.NextSpawn = DateTime.Now.Add(TimeSpan.FromSeconds(i.SpawnTime));
                        i.Taken = false;
                    }
                }
            }
        }

        private void ItemThread()
        {
            Log.Write("Item thread created: {0}", _killThread);
            while (!_killThread)
            {
                if (!GameInProgress)
                    break;

                CheckSpawns();

                Thread.Sleep(1);
            }

            CurrentStage.GetTraits().CurrentMap.TeamItems.ForEach(delegate (ItemSpawn item)
                                                                      {
                                                                          item.Taken = true;
                                                                          item.NextSpawn = DateTime.Now;
                                                                      });
            _killThread = true;
        }


        private void ProcessRound(object o)
        {
            var traits = CurrentStage.GetTraits();

            Log.Write("Starting new round.");
            lock (CurrentStage.ObjectLock)
            {
                GameInProgress = true;
                if (traits.Time > 0)
                {
                    GameTimer = new Timer();
                    GameTimer.Interval = TimeSpan.FromMinutes(traits.Time).TotalMilliseconds;
                    GameTimer.Elapsed += (s, a) => OnRoundEnd();
                    GameTimer.Enabled = true;
                }

                traits.Round = RoundState.Countdown;
                Battle.StageRoundUpdate(traits.Players, traits.StageId, traits.CurrentRound, traits.Round);

                Thread.Sleep(TimeSpan.FromSeconds(2));
                traits.Round = RoundState.Play;
                Battle.StageRoundUpdate(traits.Players, traits.StageId, traits.CurrentRound, traits.Round);

                traits.Players.ForEach(delegate(Client c)
                                           {
                                               c.ClientPlayer.PlayerStats.Entered = false;
                                               c.ClientPlayer.PlayerStats.RequestedInfo = false;
                                               c.ClientPlayer.PlayerStats.Spawned = true;
                                               c.ClientPlayer.PlayerStats.InGame = true;
                                               c.ClientPlayer.PlayerStats.LateJoined = false;
                                           });
            }

            if (!_killThread)
            {
                _killThread = true;
                while (_killThread)
                {
                    Thread.Sleep(1);
                }
            }

            _killThread = false;
            new Thread(ItemThread).Start();

        }

        private void ProcessIdleRound (object o)
        {
            var traits = CurrentStage.GetTraits();
            lock (CurrentStage.ObjectLock)
            {
                Thread.Sleep(500);
                traits.Round = RoundState.Free;
                Battle.StageRoundUpdate(traits.Players, traits.StageId, traits.CurrentRound, traits.Round);

                traits.Players.ForEach(c => c.ClientPlayer.PlayerStats.Spawned = false);
            }
        }
        
        
        public override void ProcessRoundFinish()
        {
            var traits = CurrentStage.GetTraits();
            List<Client> clients;
            lock (CurrentStage.ObjectLock)
                clients = traits.Players.FindAll(c => c.ClientPlayer.PlayerStats.InGame && !c.ClientPlayer.PlayerStats.LateJoined);

            Log.Write("Players in-game: {0}", clients.Count);
            if (clients.Count == 0)
            {
                GameOver();
            }
            else if (clients.Count > 1)
            {
                var team = 0;
                if (clients.FindAll(c => c.ClientPlayer.PlayerTeam == Team.Blue).TrueForAll(c => !c.ClientPlayer.PlayerStats.Spawned))
                {
                    Scores[0]++;
                    team = 1;
                }
                else if (clients.FindAll(c => c.ClientPlayer.PlayerTeam == Team.Red).TrueForAll(c => !c.ClientPlayer.PlayerStats.Spawned))
                {
                    Scores[1]++;
                    team = 2;
                }
                
                Log.Write("Blue Count: {0}", clients.FindAll(c => c.ClientPlayer.PlayerTeam == Team.Blue).Count);
                Log.Write("Red Count: {0}", clients.FindAll(c => c.ClientPlayer.PlayerTeam == Team.Red).Count);
                Log.Write("Winner: {0}",team);

                if (team == 0 && GameInProgress)
                    return;

                GameInProgress = false;

                traits.Round = RoundState.Finish;
                Battle.StageRoundUpdate(traits.Players, traits.StageId, traits.CurrentRound, traits.Round, team);
                traits.CurrentRound++;

                if (traits.Players.FindAll(c => c.ClientPlayer.PlayerTeam == Team.Blue).Count == 0 || traits.Players.FindAll(c => c.ClientPlayer.PlayerTeam == Team.Red).Count == 0)
                {
                    Log.Write("Stage: {0} entering free mode.", traits.StageId.HighId);
                    ThreadPool.QueueUserWorkItem(ProcessIdleRound);
                    return;
                }

                if (traits.CurrentRound < traits.RoundCount)
                {
                    Log.Write("Stage: {0} entering next round.", traits.StageId.HighId);
                    Thread.Sleep(TimeSpan.FromSeconds(3));

                    ThreadPool.QueueUserWorkItem(ProcessRound);
                    return;
                }

                _killThread = true;
                GameOver(Scores[0] > Scores[1] ? Team.Blue : Team.Red);
            }
        }

        public override void OnGameKill(Client killer, Client victim)
        {
            ProcessRoundFinish();
        }

        public override void OnInitialStart()
        {
            new Thread(ItemThread).Start();
        }

        public TeamDeathmatch(Stage currentStage) : base(currentStage, ObjectStageGameType.TeamDeathMatch)
        {
        }
    }
}
