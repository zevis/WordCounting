﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordCounting
{
    class Layer1
    {
        public async Task Start(string input, string output, int cores)
        {
            var sw = Stopwatch.StartNew();

            var Filee = File.ReadAllBytes(input);
            Console.WriteLine(sw.Elapsed);
            var l2 = new Layer2();
            l2.Configure(cores);
            var part_size = Filee.Length / (cores * 4);
            var current_start = 0;
            for (int i = 1; i <= (cores * 4) - 1; i++)
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

                await l2.Enqueue((current_start, current_end + 1, Filee));
                current_start = current_end;
            }

            await l2.Enqueue((current_start, Filee.Length, Filee));
            var res = await l2.GetRes();

            Console.WriteLine(sw.Elapsed);
            File.WriteAllText(output, string.Join("\r\n",
                res.OrderByDescending(x => x.Value.Value).Select(x =>
                    Encoding.GetEncoding(1251).GetString(Filee, Math.Max(0, x.Key.Item1), x.Key.Item2) + " " +
                    x.Value.Value)));
            Console.WriteLine(sw.Elapsed);
            Console.WriteLine(res.Count + " " + res.Values.Select(x => x.Value).Sum());
            Console.ReadKey(true);
        }
    }
}
