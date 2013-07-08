using System.Collections.Generic;
using System.Linq;
using Bunny.Core;

namespace Bunny.Stages
{
    class QueueInfo
    {
        public Client Champion; 
        public Client Challenger;
        public Queue<Client> WaitQueue = new Queue<Client>();
        public byte QueueLength;
        public byte Victories;
        public bool RoundEnd;
        private readonly object  _objectLock = new object();

        public void Create(List<Client> players)
        {
            if (players.Count > 0)
            {
                Champion = players[0];
            }

            if (players.Count > 1)
            {
                Challenger = players[1];
            }

            for (var i = 2; i < players.Count; ++i)
            {
                Log.Write("Player: {0} is in the wait queue", players[i].GetCharacter().Name);
                WaitQueue.Enqueue(players[i]);
            }
        }

        public void NewChampion()
        {
            Victories = 1;

            lock (_objectLock)
            {
                if (WaitQueue.Count > 0)
                {
                    var client = WaitQueue.Dequeue();

                    if (client != null)
                    {
                        WaitQueue.Enqueue(Champion);
                        Champion = Challenger;
                        Challenger = client;
                    }
                    else
                    {
                        client = Champion;
                        Champion = Challenger;
                        Challenger = client;
                    }
                }
                else
                {
                    var client = Champion;
                    Champion = Challenger;
                    Challenger = client;
                }
            }

        }

        public void NewChallenger()
        {
            Victories++;
            lock (_objectLock)
            {
                if (WaitQueue.Count > 0)
                {
                    var client = WaitQueue.Dequeue();

                    if (client != null)
                    {
                        if (Challenger != null)
                            WaitQueue.Enqueue(Challenger);

                        Challenger = client;
                    }
                }
            }
        }

        public void AddToQueue(Client client)
        {
            lock (_objectLock)
            {
                WaitQueue.Enqueue(client);
            }
        }

        public void RemovePlayer(Client client)
        {
            lock (_objectLock)
            {
                if (!WaitQueue.Contains(client))
                    return;

                var queue = WaitQueue.ToList();
                queue.Remove(client);
                WaitQueue = new Queue<Client>(queue);
            }
        }

        public List<Client> Clone()
        {
            lock (_objectLock)
            {
                var clients = new List<Client>();

                clients.Add(Champion);
                clients.Add(Challenger);

                foreach (var client in WaitQueue)
                {
                    Log.Write("Player: {0} is in the wait queue", client.GetCharacter().Name);
                    clients.Add(client);
                }

                for (var i = WaitQueue.Count; i < 14; ++i)
                {
                    clients.Add(null);
                }

                return clients;
            }
        }
    }
}
