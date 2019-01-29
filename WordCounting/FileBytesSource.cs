using System;
using System.Collections.Generic;
#if DEBUG
using System.Diagnostics;
#endif
using System.IO;
using System.Linq;
using System.Text;

namespace WordCounting
{
    /// <summary>
    /// Класс для работы с файлами.
    /// </summary>
    public class FileBytesSource : IBytesSource
    {
        private readonly string _input;
        private readonly string _output;
        private readonly int _parts;

        public FileBytesSource(string input, string output, int parts)
        {
            _input = input;
            _output = output;
            _parts = parts;
        }

        public virtual bool Configure()
        {
            return CheckFiles();
        }

        private bool GetEncoding()
        {
            // *** Detect byte order mark if any - otherwise assume default
            byte[] buffer = new byte[10];
            try
            {
                FileStream bytes = new FileStream(_input, FileMode.Open);
                bytes.Read(buffer, 0, 10);
                bytes.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }

            return (buffer[0] != 0xef || buffer[1] != 0xbb || buffer[2] != 0xbf) &&
                   ((buffer[0] != 0xfe || buffer[1] != 0xff) &&
                    ((buffer[0] != 0 || buffer[1] != 0 || buffer[2] != 0xfe || buffer[3] != 0xff) &&
                     ((buffer[0] != 0x2b || buffer[1] != 0x2f || buffer[2] != 0x76) &&
                      ((buffer[0] != 0xFE || buffer[1] != 0xFF) && (buffer[0] != 0xFF || buffer[1] != 0xFE)))));
        }

        private bool CanRewrite()
        {
            try
            {
                using (File.Create(Path.Combine(_output), 1, FileOptions.DeleteOnClose))
                    return true;
            }
            catch
            {
                return false;
            }
        }

        private bool CanRead()
        {
            try
            {
                using (File.Open(_input, FileMode.Open, FileAccess.Read, FileShare.Read))
                    return true;
            }
            catch (IOException)
            {
                return false;
            }
        }

        private bool CheckFiles()
        {
            if (!CanRead())
            {
                Console.WriteLine("Bad input bytes.");
                return false;
            }

            if (!GetEncoding())
            {
                Console.WriteLine("Assert! Probably bad encoding.");
                return true;
            }

            if (!CanRewrite())
            {
                Console.WriteLine("Bad output bytes.");
                return false;
            }

            return true;
        }

        public IEnumerable<(int, int, byte[])> GetParts()
        {
            byte[] bytes = GetBytes();
            if (bytes == null)
                yield break;

            int partSize = bytes.Length / (_parts);
            int currentStart = 0;
            for (int i = 1; i <= (_parts) - 1; i++)
            {
                int currentEnd = partSize * i;
                while (true)
                {
                    if (currentEnd == partSize * (i - 1))
                        break;

                    if (bytes[currentEnd] >= 97 && bytes[currentEnd] <= 122 ||
                        bytes[currentEnd] >= 65 && bytes[currentEnd] <= 90 ||
                        bytes[currentEnd] >= 192 ||
                        bytes[currentEnd] >= 48 && bytes[currentEnd] <= 57 ||
                        bytes[currentEnd] == 184 ||
                        bytes[currentEnd] == 168)
                    {
                        currentEnd--;
                        continue;
                    }

                    break;
                }

                yield return (currentStart, currentEnd, bytes);
                currentStart = currentEnd;
            }

            yield return (currentStart, bytes.Length, bytes);
        }

        protected virtual byte[] GetBytes()
        {
#if DEBUG
            Stopwatch sw = Stopwatch.StartNew();
#endif
            byte[] bytes = null;

            try
            {
                bytes = File.ReadAllBytes(_input);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
#if DEBUG
            Console.WriteLine(sw.Elapsed);
#endif
            return bytes;
        }

        public void WriteWordCountPairs(IEnumerable<KeyValuePair<(int, int, byte[]), BoxedInt>> res)
        {
            var text = string.Join("\n",
                res.OrderByDescending(x => x.Value.Value).Select(x =>
                    $"{Encoding.GetEncoding(1251).GetString(x.Key.Item3, Math.Max(0, x.Key.Item1), x.Key.Item2)},{x.Value.Value}"));

            WriteText(text);
        }

        protected virtual bool WriteText(string text)
        {
            try
            {
                File.WriteAllText(_output, text);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while writing result. " + ex);
                return false;
            }

            return true;
        }
    }
}