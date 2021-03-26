using System;
using System.IO;
using System.Linq;
using System.Threading;
using iText.Kernel.Pdf;

namespace PdfWatermark
{
    public class Program
    {
        private static void ParseLargeFiles(string[] args)
        {
            // var source_test = @"\\appshares\DigDevLoanSec\Test Runs\Results Folder\Original";
            var source_live = @"\\appshares\DigDevLoanSec\Results\Original\";
            var dest_live = @"\\appshares\DigDevLoanSec\Results\Extended Files\";
            // var dest_test = @"\\appshares\DigDevLoanSec\Test Runs\Results Folder\Extended Files\";
            Console.WriteLine("Grabbing Files");
            var files = Directory.GetFiles(source_live);
            Console.WriteLine($"Files to Parse: {files.Length}");
            using (var progress = new ProgressBar(files.Length))
            {
                foreach (var file in files)
                {
                    var info = new FileInfo(file);
                    if (info.Length < 50000000)
                    {
                        progress.Report();
                        continue;
                    }
                    // we need to parse out each page
                    using var reader = new PdfReader(info);
                    using var source = new PdfDocument(reader);
                    var pages = source.GetNumberOfPages();
                    for (var i = 1; i <= pages; i++)
                    {
                        Console.Title = $"Parsing File {Path.GetFileName(file)} Page {i} of {pages}";
                        var destination = $@"{dest_live}{Path.GetFileNameWithoutExtension(info.Name)}_{i}of{pages}.pdf";
                        using var writer = new PdfWriter(destination);
                        using var pdf = new PdfDocument(writer);
                        source.CopyPagesTo(i, i, pdf);
                    }

                    progress.Report();
                }
            }

            Console.WriteLine("Finished Parsing Directory");
            Console.ReadLine();
        }

        private static void DoAdditionalReporting(string[] args)
        {
            var lines = File.ReadAllLines(@"E:\list_of_loans.csv").OrderBy(f => f).Take(1000);
            var modified = File.ReadAllLines(@"E:\reports\modified-files.csv").Skip(1).ToList();
            // var files = Directory.GetFiles(@"\\appshares\DigDevLoanSec\Results\Copy\");

            var any = lines.Select(l => l.Split(',')[0].Split('_')[0]).GroupBy(l => l).Where(l => l.Count() > 1).ToList();
            var filename = @"E:\reports\groupings.csv";
            foreach (var temp in any)
            {
                File.AppendAllText(filename, $"{temp.Key}");
                foreach (var item in lines.Where(l => l.Contains(temp.Key)))
                    File.AppendAllText(filename, $",{item.Split(',')[0]}");
                var mods = modified.Where(l => l.Split(',')[0].Split('_')[0] == temp.Key).ToList();
                if (mods.Any())
                    File.AppendAllText(filename, $",{mods.Aggregate((a, b) => $"{a},{b}")}");
                File.AppendAllText(filename, Environment.NewLine);
            }

            Console.WriteLine($"Results: {any.Count}, Max: {any.Max(i => i.Count())}");
            Console.WriteLine("Finished");
            Console.ReadLine();
        }

        public static void OldMain(string[] args)
        {
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
            var chunksize = 1000;
            var collection = DirectoryManager.Setup()?.ToList() ?? throw new ArgumentException("Content is Empty!");
            var chunks = collection.Select((s, i) => new { Value = s, Index = i })
                .GroupBy(x => x.Index / chunksize)
                .Select(grp => grp.Select(x => x.Value).ToArray())
                .ToArray();

            Logger.Log($"Chunking Size: {chunksize}, Total # of Chunks: {chunks.Length}");
            for (var i = 0; i < chunks.Length; i++)
            {
                Console.Title = $"Chunking Files: {i + 1} of {chunks.Length}";
                Logger.Log($"Chunking Started: {i}");
                var content = chunks[i].ToList();
                var progress = new ProgressBar(content.Count);
                using (var countdown = new CountdownEvent(content.Count))
                {
                    foreach (var line in content)
                        ThreadPool.QueueUserWorkItem(
                            _ => TransactionWorker.DoWork(line.Split(','), countdown, progress));
                    countdown.Wait();
                    progress.Report(1);
                }
                progress.Dispose();
            }

            Logger.Log("Generating After-Action Report...");
            ReportManager.Generate();

            Console.WriteLine("Finished Execution, press any key to continue...");
            Console.ReadLine();
        }

        /// <summary>
        /// main entry point for watermarking PDFs
        /// </summary>
        /// <param name="args">args[0] - Log Level: Defaults to LogLevel.Info</param>
        public static void Main(string[] args)
        {
            try
            {
                Logger.DefaultLevel = Logger.LogLevel.Verbose;
                Console.Write("Original's Directory: ");
                var directory = Console.ReadLine();
                Console.Write("Destination Directory (will be created if it doesn't exist: ");
                var destination = Console.ReadLine();

                if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(destination))
                    throw new ArgumentException("Locations cannot be Null nor Empty!");

                if (!Directory.Exists(directory))
                    throw new ArgumentException("Directory Doesn't Exist!");
                if (!Directory.Exists(destination))
                    Directory.CreateDirectory(destination);
                var files = Directory.GetFiles(directory);
                using var progress = new ProgressBar(files.Length);
                foreach (var file in Directory.GetFiles(directory))
                {
                    PdfManager.WatermarkPdf(file, Path.Join(destination, Path.GetFileName(file)));
                    progress.Report();
                }
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
