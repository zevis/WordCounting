using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordCounting
{
    class Layer1
    {
        public static bool GetEncoding(string src_file)
        {
            // *** Detect byte order mark if any - otherwise assume default
            byte[] buffer = new byte[10];
            FileStream bytes = new FileStream(src_file, FileMode.Open);
            bytes.Read(buffer, 0, 10);
            bytes.Close();
            return (buffer[0] != 0xef || buffer[1] != 0xbb || buffer[2] != 0xbf) &&
                   ((buffer[0] != 0xfe || buffer[1] != 0xff) &&
                    ((buffer[0] != 0 || buffer[1] != 0 || buffer[2] != 0xfe || buffer[3] != 0xff) &&
                     ((buffer[0] != 0x2b || buffer[1] != 0x2f || buffer[2] != 0x76) &&
                      ((buffer[0] != 0xFE || buffer[1] != 0xFF) && (buffer[0] != 0xFF || buffer[1] != 0xFE)))));
        }

        public static bool CanRewrite(string output)
        {
            try
            {
                using (File.Create(Path.Combine(output), 1, FileOptions.DeleteOnClose))
                    return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool CanRead(string output)
        {
            try
            {
                using (File.Open(output, FileMode.Open, FileAccess.Read, FileShare.Read))
                    return true; 
            }
            catch (IOException)
            {
                return false;
            }
        }

        public static bool CheckFiles(string input, string output)
        {
            if (!CanRead(input))
            {
                Console.WriteLine("Bad input bytes.");
                return false;
            }

            if (!GetEncoding(input))
            {
                Console.WriteLine("Assert! Probably bad encoding.");
                return true;
            }

            if (!CanRewrite(output))
            {
                Console.WriteLine("Bad output bytes.");
                return false;
            }

            return true;
        }

        public static async Task Start(string input, string output, int cores)
        {
            if (!CheckFiles(input, output))
                return;

#if DEBUG
            Stopwatch sw = Stopwatch.StartNew();
#endif
            byte[] bytes = File.ReadAllBytes(input);
#if DEBUG
            Console.WriteLine(sw.Elapsed);
#endif
            Layer2 l2 = new Layer2();
            l2.Configure(cores);
            int part_size = bytes.Length / (cores);
            int current_start = 0;
            for (int i = 1; i <= (cores) - 1; i++)
            {
                int current_end = part_size * i;
                while (true)
                {
                    if (bytes[current_end] >= 97 && bytes[current_end] <= 122 ||
                        bytes[current_end] >= 65 && bytes[current_end] <= 90 ||
                        bytes[current_end] >= 192 ||
                        bytes[current_end] == 184 ||
                        bytes[current_end] == 168)
                    {
                        current_end--;
                        continue;
                    }

                    break;
                }

                await l2.EnqueueAsync((current_start, current_end + 1, bytes));
                current_start = current_end;
            }

            await l2.EnqueueAsync((current_start, bytes.Length, bytes));

            var res = await l2.GetResAsync();

#if DEBUG
            Console.WriteLine(sw.Elapsed);
#endif
            File.WriteAllText(output, string.Join("\n",
                res.OrderByDescending(x => x.Value.Value).Select(x =>
                    $"{Encoding.GetEncoding(1251).GetString(x.Key.Item3, Math.Max(0, x.Key.Item1), x.Key.Item2)},{x.Value.Value}")));
#if DEBUG
            Console.WriteLine(sw.Elapsed);
            Console.WriteLine(res.Count() + " " + res.Select(x => x.Value.Value).Sum());
#endif
            Console.ReadKey(true);
        }
    }
}
