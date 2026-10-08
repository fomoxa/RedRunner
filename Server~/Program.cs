using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;

namespace RedRunner.Server
{
    public static class Program
    {
        private const ushort DefaultPort = 7777;

        public static int Main(string[] args)
        {
            ushort port = ushort.TryParse(Argument(args, "-port"), NumberStyles.None, CultureInfo.InvariantCulture, out ushort parsed) ? parsed : DefaultPort;
            string logFile = Argument(args, "-logFile");
            bool logPaths = Array.IndexOf(args, "-log-paths") >= 0;
            string folder = AppContext.BaseDirectory;
            using (var output = logFile != null ? new StreamWriter(logFile, false) { AutoFlush = true } : null)
            {
                Action<string> write = line =>
                {
                    Console.WriteLine(line);
                    output?.WriteLine(line);
                };
                var clock = Stopwatch.StartNew();
                using (var game = new ServerGame(File.ReadAllBytes(Path.Combine(folder, "LevelCatalogue.bytes")), Path.Combine(folder, "Scenes"), () => clock.Elapsed, write, logPaths))
                {
                    game.Start(port);
                    TimeSpan last = clock.Elapsed;
                    while (true)
                    {
                        TimeSpan now = clock.Elapsed;
                        game.Frame(now, (now - last).TotalSeconds);
                        last = now;
                        Thread.Sleep(1);
                    }
                }
            }
        }

        private static string Argument(string[] args, string name)
        {
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
