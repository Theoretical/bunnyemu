using System;
using System.Threading;
using System.Timers;
using Bunny.Core;
using Bunny.Enums;
using Bunny.Packet.Assembled;
using Bunny.Stages;
using Timer = System.Timers.Timer;

namespace Bunny.GameTypes
{
    class Deathmatch : BaseGametype
    {
        public Thread ItemSpawns;
        public Timer GameTimer;

        public void EndGameByTime(object source, ElapsedEventArgs e)
        {
            if (!GameInProgress)
                return;

            GameTimer.Enabled = false;
            ItemSpawns.Abort();
            GameOver();           
        }

        private void CheckSpawns()
        {
            var traits = CurrentStage.GetTraits();
            var map = traits.CurrentMap;

            lock (map.DeathMatchItems)
            {
                foreach (var i in map.DeathMatchItems)
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
            while (true)
            {
                if (!GameInProgress)
                    return;

                CheckSpawns();

                Thread.Sleep(1);
            }
        }

        private void SpawnItems(Client client)
        {
            var traits = CurrentStage.GetTraits();

            lock (traits.WorldItems)
                foreach (var item in traits.WorldItems)
                {
                    Battle.SpawnWorldItem(client, item);
                }
        }

        public override void GameLateJoinCallback(Client client)
        {
            var player = client.ClientPlayer;
            var traits = CurrentStage.GetTraits();

            player.PlayerStats.Spawned = false;
            player.PlayerStats.Entered = true;
            player.PlayerStats.InGame = true;
            player.PlayerStats.Loaded = true;
            player.PlayerStats.LateJoined = true;
            SpawnItems(client);

            Battle.StageRoundUpdate(client, traits.StageId, traits.CurrentRound, traits.Round);
            ProcessRoundState();
        }


        public override void OnGameKill(Client killer, Client victim)
        {

            if (killer.ClientPlayer.PlayerStats.Kills == CurrentStage.GetTraits().RoundCount)
            {
                GameInProgress = false;
                ItemSpawns.Abort();
                GameOver();
            }
            else
            {
                Spawn(victim, 5);
            }
        }

        public override void OnInitialStart()
        {
            if (CurrentStage.GetTraits().Time > 0)
            {
                GameTimer = new Timer();
                GameTimer.Elapsed += EndGameByTime;
                GameTimer.Interval = TimeSpan.FromMinutes(CurrentStage.GetTraits().Time).TotalMilliseconds;
                GameTimer.Enabled = true;
            }
            ItemSpawns = new Thread(ItemThread);
            ItemSpawns.Start();
            Log.Write("Deathmatch has started at: {0}", DateTime.Now);
        }

        public Deathmatch(Stage stage)
            : base(stage, ObjectStageGameType.DeathMatch)
        {
            
        }
    }
}
