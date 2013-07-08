using System;
using System.Threading;
using Bunny.Core;

namespace Bunny.Utility
{
    public delegate void Callback();
    class EventManager
    {
        private static readonly LockFreeQueue<Callback> Callbacks = new LockFreeQueue<Callback>();
        public static void AddCallback (Callback c) { Callbacks.Enqueue(c); }
        
        public static void Initialize()
        {
            new Thread(CallbackThread).Start();

            Log.Write("Initialized: callback threads.");
        }
        
        private static void CallbackThread()
        {
            while(true)
            {
                if (Callbacks.Count > 0)
                {
                    Callback callback;
                    if (Callbacks.TryDequeue(out callback) && callback != null)
                    {
                        try
                        {
                            callback();
                        }
                        catch (Exception e)
                        {   
                            Log.Write("WTF: {0}", e);
                        }
                    }
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }
    }
}
