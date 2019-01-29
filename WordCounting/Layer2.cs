using System.Collections.Generic;
using Dictionary;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace WordCounting
{
    class Layer2
    {
        private WordAccumulator _wordAccumulator;
        private BufferBlock<(int, int, byte[])> _buffer;
        private TransformBlock<(int, int, byte[]), FastDictionary<(int, int, byte[]), BoxedInt>> _counter;
        private ActionBlock<FastDictionary<(int, int, byte[]), BoxedInt>> _adder;

        public void Configure(int cores)
        {
            _wordAccumulator = new WordAccumulator();
            _buffer = new BufferBlock<(int, int, byte[])>(new DataflowBlockOptions());
            _counter = new TransformBlock<(int, int, byte[]), FastDictionary<(int, int, byte[]), BoxedInt>>(
                WordAccumulator.GetWordCountPairs, new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = cores});
            _adder = new ActionBlock<FastDictionary<(int, int, byte[]), BoxedInt>>(_wordAccumulator.Accumulate,
                new ExecutionDataflowBlockOptions {SingleProducerConstrained = true});
            _buffer.LinkTo(_counter);
            _counter.LinkTo(_adder);
            _buffer.Completion.ContinueWith(task => _counter.Complete());
            _counter.Completion.ContinueWith(task => _adder.Complete());
        }

        public async Task<bool> EnqueueAsync((int, int, byte[]) item)
        {
            return await _buffer.SendAsync(item);
        }

        public async Task<IEnumerable<KeyValuePair<(int, int, byte[]), BoxedInt>>> GetResAsync()
        {
            _buffer.Complete();
            await _adder.Completion;
            return _wordAccumulator.WordCountPairs;
        }
    }
}