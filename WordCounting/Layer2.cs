using Dictionary;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace WordCounting
{
    class Layer2
    {
        Layer3 l3;
        BufferBlock<(int, int, byte[])> buffer;
        TransformBlock<(int, int, byte[]), FastDictionary<(int, int, byte[]), BoxedInt>> writer1;
        ActionBlock<FastDictionary<(int, int, byte[]), BoxedInt>> writer2;

        public void Configure(int cores)
        {
            l3 = new Layer3();
            buffer = new BufferBlock<(int, int, byte[])>(new DataflowBlockOptions() { BoundedCapacity = cores * 3 });
            writer1 = new TransformBlock<(int, int, byte[]), FastDictionary<(int, int, byte[]), BoxedInt>>(Layer3.GetWordCountPairs, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = cores,
                BoundedCapacity = cores * 3
            });
            writer2 = new ActionBlock<FastDictionary<(int, int, byte[]), BoxedInt>>(l3.AddToRes, new ExecutionDataflowBlockOptions
            {
                SingleProducerConstrained = true
            });
            buffer.LinkTo(writer1);
            writer1.LinkTo(writer2);
            buffer.Completion.ContinueWith(task => writer1.Complete());
            writer1.Completion.ContinueWith(task => writer2.Complete());
        }

        public async Task<bool> Enqueue((int, int, byte[]) item)
        {
            return await buffer.SendAsync(item);
        }

        public async Task<FastDictionary<(int, int, byte[]), BoxedInt>> GetRes()
        {
            buffer.Complete();
            await writer2.Completion;
            return l3.res;
        }
    }
}