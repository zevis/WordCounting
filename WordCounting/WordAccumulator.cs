using Dictionary;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace WordCounting
{
    public class WordAccumulator
    {
        public FastDictionary<(int, int, byte[]), BoxedInt> WordCountPairs { get; } =
            new FastDictionary<(int, int, byte[]), BoxedInt>(new WordsComparer());

        private class WordsComparer : IEqualityComparer<(int, int, byte[])>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe bool Equals((int, int, byte[]) val1, (int, int, byte[]) val2)
            {
                fixed (byte* bytes1 = val1.Item3, bytes2 = val2.Item3)
                {
                    byte* x1 = bytes1 + val1.Item1;
                    byte* x2 = bytes2 + val2.Item1;
                    int l = val1.Item2;
                    for (int i = 0; i < l / 8; i++, x1 += 8, x2 += 8)
                        if (*(long*) x1 != *(long*) x2)
                            return false;

                    if ((l & 4) != 0)
                    {
                        if (*(int*) x1 != *(int*) x2)
                            return false;
                        x1 += 4;
                        x2 += 4;
                    }

                    if ((l & 2) != 0)
                    {
                        if (*(short*) x1 != *(short*) x2)
                            return false;
                        x1 += 2;
                        x2 += 2;
                    }

                    if ((l & 1) == 0)
                        return true;

                    return *(x1) == *(x2);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetHashCode((int, int, byte[]) obj)
            {
                byte[] bytes = obj.Item3;
                unchecked
                {
                    const int p = 16777619;
                    int hash = (int) 2166136261;
                    for (int i = obj.Item1; i < obj.Item1 + obj.Item2; i++)
                        hash = (hash ^ bytes[i]) * p;

                    hash += hash << 13;
                    hash ^= hash >> 7;
                    hash += hash << 3;
                    hash ^= hash >> 17;
                    hash += hash << 5;
                    return hash;
                }
            }
        }

        public void Accumulate(FastDictionary<(int, int, byte[]), BoxedInt> word_count_pairs)
        {
            foreach (var word_count_pair in word_count_pairs)
            {
                if (WordCountPairs.TryGetValue(word_count_pair.Key, out BoxedInt val))
                {
                    val.Value += word_count_pair.Value.Value;
                }
                else
                {
                    WordCountPairs[word_count_pair.Key] = new BoxedInt {Value = word_count_pair.Value.Value};
                }
            }
        }

        public static FastDictionary<(int, int, byte[]), BoxedInt> GetWordCountPairs((int, int, byte[]) part)
        {
            WordsComparer comp = new WordsComparer();
            FastDictionary<(int, int, byte[]), BoxedInt> dict =
                new FastDictionary<(int, int, byte[]), BoxedInt>(20000, comp);

            int current_word_start = part.Item1;
            int end = part.Item2;
            byte[] bytes = part.Item3;

            for (int i = part.Item1; i < end; i++)
            {
                if (bytes[i] >= 97 && bytes[i] <= 122 || bytes[i] >= 224)
                {
                    continue;
                }

                if (bytes[i] >= 65 && bytes[i] <= 90 ||
                    bytes[i] >= 192 && bytes[i] <= 223)
                {
                    bytes[i] += 32;
                    continue;
                }

                if (bytes[i] == 184)
                {
                    continue;
                }

                if (bytes[i] == 168)
                {
                    bytes[i] = 184;
                    continue;
                }

                var len = i - current_word_start;

                if (len == 0)
                {
                    current_word_start = i + 1;
                    continue;
                }

                (int, int, byte[]) word = (current_word_start, len, bytes);
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