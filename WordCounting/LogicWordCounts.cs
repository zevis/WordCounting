using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace WordCounting
{
    /// <summary>
    /// Занимается связью источника байт с логикой подсчета количества слов.
    /// </summary>
    public class LogicWordCounts
    {
        /// <summary>
        /// Запустить задачу подсчета слов.
        /// </summary>
        public static async Task Start(IBytesSource bytesSource, AsyncWordAccumulator asyncWordAccumulator)
        {
#if DEBUG
            Stopwatch sw = Stopwatch.StartNew();
#endif
            foreach (var part in bytesSource.GetParts())
            {
                await asyncWordAccumulator.EnqueueAsync(part);
            }

            var wordCountPairs = await asyncWordAccumulator.GetWordCountPairsAsync();            
#if DEBUG
            Console.WriteLine(sw.Elapsed);
#endif
            bytesSource.WriteWordCountPairs(wordCountPairs);            
#if DEBUG
            Console.WriteLine(sw.Elapsed);
            Console.WriteLine(wordCountPairs.Count() + " " + wordCountPairs.Select(x => x.Value.Value).Sum());
#endif            
        }
    }
}
