using System;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using Bunny.Core;
using Bunny.Enums;
using Bunny.Packet.Assembled;
using Bunny.Stages;
using Timer = System.Timers.Timer;

namespace Bunny.GameTypes
{
    class TeamDeathmatchExtreme : BaseGametype
    {
        public Thread ItemSpawns;
        public Timer GameTimer;

        public void EndGameByTime(object source, ElapsedEventArgs e)
        {
            if (GameInProgress)
            {
                GameTimer.Enabled = false;
                ItemSpawns.Abort();
                GameOver();
            }
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

            player.PlayerStats.Spawned = false;
            player.PlayerStats.Entered = true;
            player.PlayerStats.InGame = true;
            player.PlayerStats.Loaded = true;
            SpawnItems(client);

            ProcessRoundState();
        }

        public override void OnGameKill(Client killer, Client victim)
        {
            if (killer.ClientPlayer.PlayerTeam == Team.Red)
                Scores[0]++;
            else
                Scores[1]++;

            if (Scores[0] == CurrentStage.GetTraits().RoundCount || Scores[1] == CurrentStage.GetTraits().RoundCount)
            {
                GameOver(Scores[0] == CurrentStage.GetTraits().RoundCount ? Team.Red : Team.Blue);
                return;
            }

            Spawn(victim, 3);           
        }

        public override void OnInitialStart()
        {
            ItemSpawns = new Thread(ItemThread);
            ItemSpawns.Start();
        }


        public TeamDeathmatchExtreme(Stage currentStage)
            : base(currentStage, ObjectStageGameType.TeamDeathMatchExtreme)
        {
        }
    }
}
