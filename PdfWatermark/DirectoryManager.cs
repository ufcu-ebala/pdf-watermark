using System;
using System.Collections.Generic;
using System.IO;

namespace PdfWatermark
{
    /// <summary>
    /// <see cref="Setup" />
    /// </summary>
    public static class DirectoryManager
    {
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
                OriginalDirectory = Path.Join(BaseDirectory, "Originals");
                ArchiveDirectory = Path.Join(BaseDirectory, "Archive");
                FailureDirectory = Path.Join(BaseDirectory, "Failures");
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
