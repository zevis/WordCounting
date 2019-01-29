using System;
using System.Collections.Generic;
using Dictionary;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace WordCounting
{
    /// <summary>
    /// Класс для параллельного подсчета частот слов.
    /// </summary>
    public class AsyncWordAccumulator
    {
        private WordAccumulator _wordAccumulator;
        private BufferBlock<(int, int, byte[])> _buffer;
        private TransformBlock<(int, int, byte[]), FastDictionary<(int, int, byte[]), BoxedInt>> _counter;
        private ActionBlock<FastDictionary<(int, int, byte[]), BoxedInt>> _adder;

        public bool Configure(int cores)
        {
            try
            {
                _wordAccumulator = new WordAccumulator();
                _buffer = new BufferBlock<(int, int, byte[])>(new DataflowBlockOptions());
                _counter = new TransformBlock<(int, int, byte[]), FastDictionary<(int, int, byte[]), BoxedInt>>(
                    WordAccumulator.GetWordCountPairs,
                    new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = cores});
                _adder = new ActionBlock<FastDictionary<(int, int, byte[]), BoxedInt>>(_wordAccumulator.Accumulate,
                    new ExecutionDataflowBlockOptions {SingleProducerConstrained = true});
                _buffer.LinkTo(_counter);
                _counter.LinkTo(_adder);
                _buffer.Completion.ContinueWith(task => _counter.Complete());
                _counter.Completion.ContinueWith(task => _adder.Complete());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                return false;
            }

            return true;
        }

        /// <summary>
        /// Добавить часть для обработки.
        /// </summary>
        public async Task<bool> EnqueueAsync((int, int, byte[]) item)
        {
            return await _buffer.SendAsync(item);
        }

        /// <summary>
        /// Получить конечный результат.
        /// </summary>
        public async Task<IEnumerable<KeyValuePair<(int, int, byte[]), BoxedInt>>> GetWordCountPairsAsync()
        {
            _buffer.Complete();
            await _adder.Completion;
            return _wordAccumulator.WordCountPairs;
        }
    }
}