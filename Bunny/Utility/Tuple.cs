namespace Bunny.Utility
{
    class Tuple<T1, T2, T3>
    {
        public T1 First;
        public T2 Second;
        public T3 Third;

        public Tuple(T1 first, T2 second, T3 third)
        {
            First = first;
            Second = second;
            Third = third;
        }
    }
}
