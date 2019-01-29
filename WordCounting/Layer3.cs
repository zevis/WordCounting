using Dictionary;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace WordCounting
{
    class Layer3
    {
        public FastDictionary<(int, int, byte[]), BoxedInt> res { get; private set; } = new FastDictionary<(int, int, byte[]), BoxedInt>(new Comp());

        private class Comp : IEqualityComparer<(int, int, byte[])>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe bool Equals((int, int, byte[]) val1, (int, int, byte[]) val2)
            {
                var Filee1 = val1.Item3;
                var Filee2 = val2.Item3;
                fixed (byte* file1 = Filee1, file2 = Filee2)
                {
                    byte* x1 = file1 + val1.Item1;
                    byte* x2 = file2 + val2.Item1;
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
            public int GetHashCode((int, int, byte[]) obj)
            {
                var Filee = obj.Item3;
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

        public void AddToRes(FastDictionary<(int, int, byte[]), BoxedInt> dic)
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
        }

        public static FastDictionary<(int, int, byte[]), BoxedInt> GetWordCountPairs((int, int, byte[]) part)
        {
            var comp = new Comp();
            FastDictionary<(int, int, byte[]), BoxedInt> dict = new FastDictionary<(int, int, byte[]), BoxedInt>(20000, comp);

            int current_word_start = part.Item1;
            int end = part.Item2;
            var Filee = part.Item3;

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

                (int, int, byte[]) word = (current_word_start, len, Filee);
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
    }
}
