using System.Collections.Generic;

namespace WordCounting
{
    public interface IBytesSource
    {
        /// <summary>
        /// Получить части байт от полного набора байт.
        /// </summary>
        IEnumerable<(int, int, byte[])> GetParts();

        /// <summary>
        /// Записать результат подсчета в файл.
        /// </summary>
        void WriteWordCountPairs(IEnumerable<KeyValuePair<(int, int, byte[]), BoxedInt>> wordCountPairs);
    }
}
