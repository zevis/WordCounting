using System;
using System.Threading.Tasks;

namespace WordCounting
{
    public class Program
    {
        static async Task Main(string[] args)
        {
#if DEBUG
            await Layer1.Start(@"c:\1251.txt", @"f:\txt.txt", Environment.ProcessorCount);
#else
            if (args.Length == 2)
            {
                await Layer1.Start(args[0], args[1], Environment.ProcessorCount);
            }
#endif
        }
    }
}