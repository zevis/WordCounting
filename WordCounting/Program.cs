using Dictionary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace WordCounting
{
    public class Program
    {
        public class BoxedInt
        {
            public int Value = 1;
        }

        public static byte[] Filee;

        public class Comp : IEqualityComparer<(int, int)>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe bool Equals((int, int) val1, (int, int) val2)
            {
                fixed (byte* file = Filee)
                {
                    byte* x1 = file + val1.Item1;
                    byte* x2 = file + val2.Item1;
                    int l = val1.Item2;
                    for (int i = 0; i < l / 8; i++, x1 += 8, x2 += 8)
                        if (*((long*)x1) != *((long*)x2))
                            return false;

                    if ((l & 4) != 0)
                    {
                        if (*((int*)x1) != *((int*)x2))
                            return false;
                        x1 += 4;
                        x2 += 4;
                    }

                    if ((l & 2) != 0)
                    {
                        if (*((short*)x1) != *((short*)x2))
                            return false;
                        x1 += 2;
                        x2 += 2;
                    }

                    if ((l & 1) != 0)
                        if (*((byte*)x1) != *((byte*)x2))
                            return false;
                    return true;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetHashCode((int, int) obj)
            {
                unchecked
                {
                    const int p = 16777619;
                    int hash = (int)2166136261;
                    for (int i = obj.Item1; i < obj.Item1 + obj.Item2; i++)
                        hash = (hash ^ Filee[i]) * p;

                    hash += hash << 13;
                    hash ^= hash >> 7;
                    hash += hash << 3;
                    hash ^= hash >> 17;
                    hash += hash << 5;
                    return hash;
                }
            }
        }

        public static FastDictionary<(int, int), BoxedInt> GetWordCountPairs((int, int) part)
        {
            var comp = new Comp();
            FastDictionary<(int, int), BoxedInt> dict = new FastDictionary<(int, int), BoxedInt>(20000, comp);

            int current_word_start = part.Item1;
            int end = part.Item2;

            for (int i = part.Item1; i < end; i++)
            {
                if ((Filee[i] >= 97 && Filee[i] <= 122) || Filee[i] >= 224)
                {
                    continue;
                }

                if ((Filee[i] >= 65 && Filee[i] <= 90) ||
                    (Filee[i] >= 192 && Filee[i] <= 223))
                {
                    Filee[i] += 32;
                    continue;
                }

                if (Filee[i] == 184)
                {
                    continue;
                }

                if (Filee[i] == 168)
                {
                    Filee[i] = 184;
                    continue;
                }

                var len = i - current_word_start;

                if (len == 0)
                {
                    current_word_start = i + 1;
                    continue;
                }

                (int, int) word = (current_word_start, len);
                if (dict.TryGetValue(word, out BoxedInt val))
                {
                    val.Value++;
                }
                else
                {
                    dict[word] = new BoxedInt();
                }

                current_word_start = i + 1;
            }

            return dict;
        }

        static async Task Main(string[] args)
        {
            var sw = Stopwatch.StartNew();

            Filee = File.ReadAllBytes(@"c:\1251.txt");
            Console.WriteLine(sw.Elapsed);
            var comp = new Comp();
            var res = new FastDictionary<(int, int), BoxedInt>(comp);

            var buffer = new BufferBlock<(int, int)>(new DataflowBlockOptions());
            var writer1 = new TransformBlock<(int, int), FastDictionary<(int, int), BoxedInt>>(GetWordCountPairs, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            });
            var writer2 = new ActionBlock<FastDictionary<(int, int), BoxedInt>>(dic =>
            {
                foreach (var word in dic)
                {
                    if (res.TryGetValue(word.Key, out BoxedInt val))
                    {
                        val.Value += word.Value.Value;
                    }
                    else
                    {
                        res[word.Key] = new BoxedInt { Value = word.Value.Value };
                    }
                }
            }, new ExecutionDataflowBlockOptions
            {
                SingleProducerConstrained = true
            });
            buffer.LinkTo(writer1);
            writer1.LinkTo(writer2);
            buffer.Completion.ContinueWith(task => writer1.Complete());
            writer1.Completion.ContinueWith(task => writer2.Complete());

            var part_size = Filee.Length / (Environment.ProcessorCount * 4);
            var current_start = 0;
            for (int i = 1; i <= (Environment.ProcessorCount * 4) - 1; i++)
            {
                var current_end = part_size * i;
                while (true)
                {
                    if ((Filee[current_end] >= 97 && Filee[current_end] <= 122) ||
                        (Filee[current_end] >= 65 && Filee[current_end] <= 90) ||
                        Filee[current_end] >= 192 ||
                        Filee[current_end] == 184 ||
                        Filee[current_end] == 168)
                    {
                        current_end--;
                        continue;
                    }

                    break;
                }

                await buffer.SendAsync((current_start, current_end + 1));
                current_start = current_end;
            }

            await buffer.SendAsync((current_start, Filee.Length));

            buffer.Complete();
            writer2.Completion.Wait();

            Console.WriteLine(sw.Elapsed);
            File.WriteAllText(@"c:\txt.txt", string.Join("\r\n",
                res.OrderByDescending(x => x.Value.Value).Select(x =>
                    Encoding.GetEncoding(1251).GetString(Filee, Math.Max(0, x.Key.Item1), x.Key.Item2) + " " +
                    x.Value.Value)));
            Console.WriteLine(sw.Elapsed);
            Console.WriteLine(res.Count + " " + res.Values.Select(x => x.Value).Sum());
            Console.ReadKey(true);
        }
    }
}