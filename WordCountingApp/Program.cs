using System;
using System.Threading.Tasks;
using WordCounting;

namespace WordCountingApp
{
    public class Program
    {
        static async Task Main(string[] args)
        {
#if DEBUG
            var bytesSource = args.Length == 2 ?
                new FileBytesSource(args[0], args[1], Environment.ProcessorCount) :
                new FileBytesSource(@"c:\1251.txt", @"c:\txt.txt", Environment.ProcessorCount);
#else
            if (args.Length != 2)
            {
                Console.WriteLine("wordcountingapp.exe \"input.txt\" \"output.txt\"");
                Console.ReadLine();
                return;
            }

            var bytesSource = new FileBytesSource(args[0], args[1], Environment.ProcessorCount);
#endif
            if (!bytesSource.Configure())
                return;

            AsyncWordAccumulator asyncWordAccumulator = new AsyncWordAccumulator();
            if (!asyncWordAccumulator.Configure(Environment.ProcessorCount))
                return;

            await LogicWordCounts.Start(bytesSource, asyncWordAccumulator);
            Console.ReadKey(true);
        }
    }
}