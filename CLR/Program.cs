using System;

namespace CLR
{
    class Program
    {
        static void Main(string[] args)
        {
            int i = 10;
            Console.WriteLine(SubtractBeforeResult(ref i));
            Console.WriteLine(i);
            Console.WriteLine(SubtractAfterResult(ref i));
            Console.ReadKey();
        }
        static int SubtractBeforeResult(ref int i)
        {
            return i--;
        }
        static int SubtractAfterResult(ref int i)
        {
            return --i;
        }
    }
}
