using System.Threading.Tasks;

namespace WordCounting
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            await new Layer1().Start();
        }
    }
}