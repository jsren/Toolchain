
namespace System
{
    public class Pair<T1, T2>
    {
        public T1 Item1 { get; set; }
        public T2 Item2 { get; set; }

        public Pair() { }
        public Pair(T1 Item1, T2 Item2)
        {
            this.Item1 = Item1;
            this.Item2 = Item2;
        }
    }
}
