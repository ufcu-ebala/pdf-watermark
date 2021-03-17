using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PdfWatermark
{
    /// <summary>
    /// <see cref="Setup" />
    /// </summary>
    public static class DirectoryManager
    {
        private static object _lock = new();
        /// <summary>
        /// Files from the File Directory
        /// </summary>
        private static string[] Files { get; set; }
        /// <summary>
        /// Base Directory level
        /// </summary>
        public static string BaseDirectory { get; set; }
        /// <summary>
        /// Original Files used for processing
        /// </summary>
        public static string FileDirectory { get; set; }
        /// <summary>
        /// Archived files based off <see cref="FileDirectory" />
        /// </summary>
        public static string ArchiveDirectory { get; set; }
        /// <summary>
        /// Original PDF generated Directory off <see cref="FileDirectory" />
        /// </summary>
        public static string OriginalDirectory { get; set; }
        /// <summary>
        /// Failed Files for whatever reason
        /// </summary>
        public static string FailureDirectory { get; set; }
        /// <summary>
        /// Data file used for parsing <see cref="FileDirectory" />
        /// </summary>
        public static string DataFile { get; set; }

        /// <summary>
        /// Attempts to retrieve a file info object based on partial information provided
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static FileInfo GetFile(string filename)
        {
            if (string.IsNullOrEmpty(FileDirectory))
                throw new ArgumentException("File Directory Must be Setup First!");
            lock (_lock)
                if (Files == null || !Files.Any())
                    Files = Directory.GetFiles(FileDirectory);
            if (Files.Contains(filename)) return new FileInfo(Path.Join(FileDirectory, filename));
            // we might have a mis-match
            var file = Files.FirstOrDefault(f => f.Contains(filename));
            if (file != null) return new FileInfo(file);

            file = Files.FirstOrDefault(f => f.Contains(filename.Split('_')[0]));
            if (file == null)
            {
                ReportManager.ReportMissingFile(filename);
                return null;
            }

            ReportManager.ReportModifiedFile(filename, file);
            return new FileInfo(file);
        }

        /// <summary>
        /// Attempts to setup the appropriate directories
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> Setup()
        {
            // SetupBaseDirectory Content
            try
            {
                Console.Write("Working Directory: ");
                if (string.IsNullOrEmpty(BaseDirectory))
                   BaseDirectory = Console.ReadLine() ?? Directory.GetCurrentDirectory();
                else
                    Console.Write($"{BaseDirectory}\n");
                Console.Write("Original Documents Directory: ");
                if (string.IsNullOrEmpty(FileDirectory))
                    FileDirectory = Console.ReadLine() ?? throw new ArgumentException("PDFBaseDirectory cannot be null!");
                else
                    Console.Write($"{FileDirectory}\n");
                OriginalDirectory = Path.Join(BaseDirectory, "Original");
                ArchiveDirectory = Path.Join(BaseDirectory, "Copy");
                FailureDirectory = Path.Join(BaseDirectory, "Failure");
                if (!Directory.Exists(BaseDirectory))
                   Directory.CreateDirectory(BaseDirectory);
                if (!Directory.Exists(OriginalDirectory))
                   Directory.CreateDirectory(OriginalDirectory);
                if (!Directory.Exists(ArchiveDirectory))
                   Directory.CreateDirectory(ArchiveDirectory);
                if (!Directory.Exists(FailureDirectory))
                   Directory.CreateDirectory(FailureDirectory);
                Console.Write("DataFile File (full filepath): ");
                if (string.IsNullOrEmpty(DataFile))
                    DataFile = Console.ReadLine() ?? throw new ArgumentException("DataFile File cannot be null");
                else
                    Console.Write($"{DataFile}\n");
                return File.ReadAllLines(DataFile);
            }
            catch (Exception e)
            {
                Logger.Log("Unable to setupBaseDirectory", Logger.LogLevel.Error);
                Logger.Log(e.ToString(), Logger.LogLevel.Warning);
                return null;
            }
        }
    }
}
