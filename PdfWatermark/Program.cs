using System;
//using System.IO;
using System.Linq;
using System.Threading;

namespace PdfWatermark
{
    public class Program
    {
        /// <summary>
        /// main entry point for watermarking PDFs
        /// </summary>
        /// <param name="args">args[0] - Log Level: Defaults to LogLevel.Info</param>
        static void Main(string[] args)
        {
            try
            {
                //var lines = File.ReadAllLines(@"E:\reports\success.csv");
                //var modified = File.ReadAllLines(@"E:\reports\modified-files.csv").Skip(1).ToList();
                //// var files = Directory.GetFiles(@"\\appshares\DigDevLoanSec\Results\Copy\");

                //var any = lines.Select(l => l.Split(',')[0].Split('_')[0]).GroupBy(l => l).Where(l => l.Count() > 1).ToList();
                //var filename = @"E:\reports\groupings.csv";
                //foreach (var temp in any)
                //{
                //    File.AppendAllText(filename, $"{temp.Key}");
                //    foreach (var item in lines.Where(l => l.Contains(temp.Key)))
                //        File.AppendAllText(filename, $",{item.Split(',')[0]}");
                //    var mods = modified.Where(l => l.Split(',')[0].Split('_')[0] == temp.Key).ToList();
                //    if (mods.Any())
                //        File.AppendAllText(filename, $",{mods.Aggregate((a, b) => $"{a},{b}")}");
                //    File.AppendAllText(filename, Environment.NewLine);
                //}

                //Console.WriteLine($"Results: {any.Count}, Max: {any.Max(i => i.Count())}");
                //Console.WriteLine("Finished");
                //Console.ReadLine();
                //return;
                Logger.Log($"Starting Application: {args?.Length}", Logger.LogLevel.Verbose);
                if (args?.Length > 0)
                    Logger.DefaultLevel = Enum.Parse<Logger.LogLevel>(args[0]);
                if (args?.Length > 1)
                    DirectoryManager.BaseDirectory = args[1];
                if (args?.Length > 2)
                    DirectoryManager.FileDirectory = args[2];
                if (args?.Length > 3)
                    DirectoryManager.DataFile = args[3];
                // setup directories
                var content = DirectoryManager.Setup()?.ToList() ?? throw new ArgumentException("Content is Empty!");
                var progress = new ProgressBar(content.Count);
                using var countdown = new CountdownEvent(content.Count);
                {
                    foreach (var line in content)
                        ThreadPool.QueueUserWorkItem(
                            _ => TransactionWorker.DoWork(line.Split(','), countdown, progress));
                    countdown.Wait();
                    progress.Report(1);
                }
                progress.Dispose();
                Logger.Log("Generating After-Action Report...");
                ReportManager.Generate();

                Console.WriteLine("Finished Execution, press any key to continue...");
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Logger.Log("Unable execute transactions");
                Logger.Log(e.ToString(), Logger.LogLevel.Error);
                Console.WriteLine("Press any key to continue...");
                Console.ReadLine();
            }
        }
    }
}
