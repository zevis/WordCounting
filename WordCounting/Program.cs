using Dictionary;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace WordCounting
{
    class Program
    {
        public class BoxedInt
        {
            public int Value;
        }

        public static ConcurrentQueue<FastDictionary<(int, int), BoxedInt>> _dics = new ConcurrentQueue<FastDictionary<(int, int), BoxedInt>>();
        public static ConcurrentQueue<(int, int)[]> _lines = new ConcurrentQueue<(int, int)[]>();
        public static byte[] Filee;
        public static volatile bool end = false;

        class Comp : IEqualityComparer<(int, int)>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe bool Equals((int, int) val1, (int, int) val2)
            {
                fixed (byte* p1 = Filee)
                {
                    byte* x1 = p1 + val1.Item1;
                    byte* x2 = p1 + val2.Item1;
                    int l = val1.Item2;
                    for (int i = 0; i < l / 8; i++, x1 += 8, x2 += 8)
                        if (*((long*)x1) != *((long*)x2))
                            return false;

                    if ((l & 4) != 0)
                    {
                        if (*((int*)x1) != *((int*)x2))
                            return false; x1 += 4; x2 += 4;
                    }
                    if ((l & 2) != 0)
                    {
                        if (*((short*)x1) != *((short*)x2))
                            return false; x1 += 2; x2 += 2;
                    }
                    if ((l & 1) != 0) if (*((byte*)x1) != *((byte*)x2))
                            return false;
                    return true;
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public unsafe int GetHashCode((int, int) obj)
            {
                unchecked
                {
                    const int p = 16777619;
                    int hash = (int)2166136261;
                    fixed (byte* p1 = Filee)
                    {
                        for (int i = obj.Item1; i < obj.Item1 + obj.Item2; i++)
                            hash = (hash ^ p1[i]) * p;
                    }

                    hash += hash << 13;
                    hash ^= hash >> 7;
                    hash += hash << 3;
                    hash ^= hash >> 17;
                    hash += hash << 5;
                    return hash;
                }
            }

        }

        public static void qq()
        {
            FastDictionary<(int, int), BoxedInt> dict;
            _dics.TryDequeue(out dict);
            while (!end)
            {
                while (_lines.TryDequeue(out (int, int)[] texts))
                {
                    for (int i1 = 0; i1 < texts.Length; i1++)
                    {
                        (int, int) text = texts[i1];
                        if (dict.TryGetValue(text, out BoxedInt val))
                        {
                            val.Value++;
                        }
                        else
                        {
                            dict[text] = new BoxedInt();
                        }
                    }
                }

                Thread.Sleep(10);
            }
            _dics.Enqueue(dict);
        }
        static unsafe void Main(string[] args)
        {
            var sw = Stopwatch.StartNew();
            var thds = new List<Thread>();
            
            Filee = File.ReadAllBytes(@"c:\1251.txt");
            Console.WriteLine(sw.Elapsed);
            var comp = new Comp();
            var res = new FastDictionary<(int, int), int>(comp);
            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                _dics.Enqueue(new FastDictionary<(int, int), BoxedInt>(20000, comp));
                thds.Add(new Thread(qq) { IsBackground = true });
            }

            foreach (var thd in thds)
            {
                thd.Start();
            }

            int start = 0;
            int j = 0;
            int count = 5000;
            int len = Filee.Length;
            unsafe
            {
                (int, int)[] textsls = new (int, int)[count];
                fixed (byte* file = Filee)
                {
                    for (int i = 0; i < len; i++)
                    {
                        if (file[i] >= 97 && file[i] <= 122)
                            continue;

                        if (file[i] >= 65 && file[i] <= 90)
                        {
                            file[i] += 32;
                            continue;
                        }                                            

                        if (file[i] >= 224 && file[i] <= 255)
                        {
                            continue;
                        }

                        if (file[i] >= 192 && file[i] <= 224)
                        {
                            file[i] += 32;
                            continue;
                        }

                        if (file[i] >= 168 && file[i] <= 168)
                        {
                            file[i] = 184;
                            continue;
                        }

                        if (file[i] >= 184 && file[i] <= 184)
                            continue;

                        if (i == start)
                        {
                            i++;
                            start = i;
                            continue;
                        }

                        textsls[j] = (start, i - start);

                        if (j > count - 2)
                        {
                            while (_lines.Count > 200)
                                Thread.Sleep(10);

                            _lines.Enqueue(textsls);
                            textsls = new (int, int)[count];
                            j = 0;
                        }

                        i++;
                        j++;
                        start = i;
                    }
                }

                _lines.Enqueue(textsls);
            }
            
            end = true;
            foreach (var thd in thds)
            {
                thd.Join();
            }

            foreach (var dic in _dics)
                foreach (var word in dic)
                {
                    if (!res.ContainsKey(word.Key)) res[word.Key] = word.Value.Value;
                    else res[word.Key] += word.Value.Value;
                }

            Console.WriteLine(sw.Elapsed);
            Console.WriteLine(res.Count + " " + res.Values.Sum());
            Console.ReadKey(true);
        }
    }
}