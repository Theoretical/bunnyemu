using System;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using Bunny.Core;
using Bunny.Enums;
using Bunny.Packet.Assembled;
using Bunny.Players;
using Bunny.Stages;
using Timer = System.Timers.Timer;

namespace Bunny.GameTypes
{
    class Duel : BaseGametype
    {
        public Timer GameTimer;

        public void EndGameByTime(object source, ElapsedEventArgs e)
        {
            var traits = CurrentStage.GetTraits();
            if (traits.Players.Find(p => p.ClientPlayer.PlayerStats.Kills >= traits.RoundCount) != null && GameInProgress)
            {
                GameTimer.Enabled = false;
                GameOver();
            }
            else
            {
                ThreadPool.QueueUserWorkItem(StartNextRound);
            }
        }
        private void BuldQueue()
        {
            var traits = CurrentStage.GetTraits();

            lock (CurrentStage.ObjectLock)
            {
                traits.DuelQueue.Create(traits.Players);
            }
        }
        private void StartNextRound(object o)
        {
            var traits = CurrentStage.GetTraits();
            
            traits.Round = RoundState.Finish;
            Battle.StageRoundUpdate(traits.Players, traits.StageId, traits.CurrentRound, traits.Round);
            
            lock (CurrentStage.ObjectLock)
            {
                List<Client> clients;
                lock (traits.Players) clients = new List<Client>(traits.Players);

                foreach (var client in clients)
                {
                    client.ClientPlayer.PlayerStats.Spawned = false;
                    client.ClientPlayer.PlayerStats.Entered = false;
                    client.ClientPlayer.PlayerStats.RequestedInfo = false;
                }
                
                traits.Round = RoundState.Countdown;
                //Battle.StageRoundUpdate(traits.Players, traits.StageId, traits.CurrentRound, traits.Round);

                Thread.Sleep(TimeSpan.FromSeconds(3));

                traits.Round = RoundState.Play;
                Battle.StageRoundUpdate(traits.Players, traits.StageId, traits.CurrentRound, traits.Round);

                var players = traits.DuelQueue.Clone();
                Battle.DuelQueue(traits.Players, players, (byte)traits.DuelQueue.WaitQueue.Count,
                                 traits.DuelQueue.Victories, true);

                for (var i = 2; i < players.Count; ++i)
                {
                    if (players[i] == null)
                        break;

                    Log.Write("Player: {0} is an observer.", players[i].GetCharacter().Name);
                    Battle.SetObserver(traits.Players, players[i].GetMuid());
                }

                if (CurrentStage.GetTraits().Time > 0)
                {
                    if (GameTimer != null)
                        GameTimer.Enabled = false;

                    GameTimer = new Timer();
                    GameTimer.Elapsed += EndGameByTime;
                    GameTimer.Interval = TimeSpan.FromMinutes(CurrentStage.GetTraits().Time).TotalMilliseconds;
                    GameTimer.Enabled = true;
                }
            }
        }
        public override void GameLeaveBattle(Client client)
        {
            lock (CurrentStage.ObjectLock)
            {
                client.ClientPlayer.PlayerStats = new GameStats();
                Battle.LeaveBattle(CurrentStage.GetTraits().Players, client.GetMuid());

                if (client == CurrentStage.GetTraits().DuelQueue.Champion || client == CurrentStage.GetTraits().DuelQueue.Challenger)
                {
                    var traits = CurrentStage.GetTraits();

                    if (traits.DuelQueue.Challenger == client)
                    {
                        if (traits.DuelQueue.WaitQueue.Count > 0)
                        {
                            traits.DuelQueue.NewChallenger();
                        }
                        else
                        {
                            traits.DuelQueue.Challenger = null;
                        }
                        if (traits.DuelQueue.Victories % 10 == 0 && traits.DuelQueue.Victories > 0)
                            Battle.UpdateDuelStreak(traits.DuelQueue.Champion.GetCharacter().Name, traits.DuelQueue.Champion.ClientPlayer.PlayerChannel.GetTraits().ChannelName, traits.StageIndex, traits.DuelQueue.Victories);
                    }
                    else
                    {
                        if (traits.DuelQueue.Victories % 10 == 0 && traits.DuelQueue.Victories > 0)
                            Battle.EndDuelStreak(traits.DuelQueue.Champion.GetCharacter().Name, traits.DuelQueue.Challenger.GetCharacter().Name, traits.DuelQueue.Victories);
                        traits.DuelQueue.NewChampion();
                    }

                    ThreadPool.QueueUserWorkItem(StartNextRound);
                    return;
                }

                CurrentStage.GetTraits().DuelQueue.RemovePlayer(client);
                var players = CurrentStage.GetTraits().DuelQueue.Clone();
                Battle.DuelQueue(CurrentStage.GetTraits().Players, players, (byte)CurrentStage.GetTraits().DuelQueue.WaitQueue.Count,
                                 CurrentStage.GetTraits().DuelQueue.Victories);
            }

            base.GameLeaveBattle(client);
        }
        public override void OnGameKill(Client killer, Client victim)
        {
            if (killer.ClientPlayer.PlayerStats.Kills == CurrentStage.GetTraits().RoundCount)
            {
                GameInProgress = false;
                GameOver();
            }
            else
            {
                var traits = CurrentStage.GetTraits();

                if (traits.DuelQueue.Champion == killer)
                {
                    traits.DuelQueue.NewChallenger();
                    if (traits.DuelQueue.Victories % 10 == 0 && traits.DuelQueue.Victories > 0)
                        Battle.UpdateDuelStreak(traits.DuelQueue.Champion.GetCharacter().Name, traits.DuelQueue.Champion.ClientPlayer.PlayerChannel.GetTraits().ChannelName, traits.StageIndex, traits.DuelQueue.Victories);
                }
                else
                {
                    if (traits.DuelQueue.Victories % 10 == 0 && traits.DuelQueue.Victories > 0)
                        Battle.EndDuelStreak(traits.DuelQueue.Champion.GetCharacter().Name, traits.DuelQueue.Challenger.GetCharacter().Name, traits.DuelQueue.Victories);
                    traits.DuelQueue.NewChampion();
                }

                ThreadPool.QueueUserWorkItem(StartNextRound);
            }
        }
        public override void GameInfoCallback(Client client)
        {
            var traits = CurrentStage.GetTraits();

            lock (CurrentStage.ObjectLock)
            { 
                Battle.BattleResponseInfo(client, Scores[0], Scores[1], traits.Players);
            }

            client.ClientPlayer.PlayerStats.Spawned = true;
            client.ClientPlayer.PlayerStats.RequestedInfo = true;

            if (client.ClientPlayer.PlayerStats.LateJoined)
            {
                lock (CurrentStage.ObjectLock)
                {
                    if (traits.DuelQueue.Challenger != null)
                    {
                        traits.DuelQueue.AddToQueue(client);
                        var players = traits.DuelQueue.Clone();
                        Battle.DuelQueue(client, players, (byte)traits.DuelQueue.WaitQueue.Count,
                            traits.DuelQueue.Victories, true);
                        Battle.SetObserver(traits.Players, client.GetMuid());
                    }
                    else
                    {

                        var players = traits.DuelQueue.Clone();
                        players.Add(client);
                        Battle.DuelQueue(client, players, (byte)(traits.DuelQueue.WaitQueue.Count+1), traits.DuelQueue.Victories);

                        traits.DuelQueue.Challenger = client;
                        players = traits.DuelQueue.Clone();
                        Battle.DuelQueue(traits.Players, players, traits.DuelQueue.QueueLength,
                            traits.DuelQueue.Victories, true);
                        ThreadPool.QueueUserWorkItem(StartNextRound);
                    }
                }
            }
            ProcessRoundState();
        }

        public override void GameLateJoinCallback(Client client)
        {
            var player = client.ClientPlayer;

            player.PlayerStats.Spawned = false;
            player.PlayerStats.Entered = true;
            player.PlayerStats.InGame = true;
            player.PlayerStats.Loaded = true;
            player.PlayerStats.LateJoined = true;

            ProcessRoundState();
        }
        public override void OnClientsLoaded()
        {
            var traits = CurrentStage.GetTraits();
            var players = new List<Client>();

            BuldQueue();
            players.Add(null);
            players.Add(null);

            foreach (var player in traits.Players)
            {
                players.Add(player);
            }

            for (var i = (players.Count + 2); i < 16; ++i)
                players.Add(null);

            lock (CurrentStage.ObjectLock)
                Battle.DuelQueue(traits.Players, players, (byte)traits.Players.Count, 0);
        }
        public override void OnInitialStart()
        {
            var traits = CurrentStage.GetTraits();
            var players = traits.DuelQueue.Clone();

            Battle.DuelQueue(traits.Players, players, (byte)traits.DuelQueue.WaitQueue.Count,
                             traits.DuelQueue.Victories, true);

            for (var i = 2; i < players.Count; ++i)
            {
                if (players[i] == null)
                    break;

                Battle.SetObserver(traits.Players, players[i].GetMuid());
            }
        }
 
        public Duel(Stage stage)
            : base(stage, ObjectStageGameType.Duel)
        {

        }
    }
}
