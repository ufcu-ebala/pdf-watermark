using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PdfWatermark
{
    public static class ReportManager
    {
        private static readonly object _lock = new();
        private static ICollection<string[]> GeneralBucket { get; set; }
        private static ICollection<string> MissingFiles { get; set; }
        private static ICollection<string[]> MissingContent { get; set; }
        private static ICollection<(string, string)> ModifiedFiles { get; set; }
        private static ICollection<string> UnknownFiles { get; set; }
        private static ICollection<string> PasswordLockedFiles { get; set; }

        public static void Generate()
        {
            // generate individual reports for each category
            Logger.Log($"Generating Missing Content Report: {MissingContent?.Count}");
            File.AppendAllText(Path.Join(DirectoryManager.BaseDirectory, "missing-content.csv"), $"Name,CUDL_ID,APP_DATE,Description{Environment.NewLine}");
            File.AppendAllText(Path.Join(DirectoryManager.BaseDirectory, "missing-files.csv"), $"File Name Missing{Environment.NewLine}");
            File.AppendAllText(Path.Join(DirectoryManager.BaseDirectory, "modified-files.csv"), $"Original,New{Environment.NewLine}");
            
            if (MissingContent?.Any() ?? false)
                foreach (var content in MissingContent)
                    File.AppendAllText(Path.Join(DirectoryManager.BaseDirectory, "missing-content.csv"),
                        $"{content.Aggregate((a, b) => $"{a},{b}")}{Environment.NewLine}");
            Logger.Log($"Generating Password Locked Files Report: {PasswordLockedFiles?.Count}");
            if (PasswordLockedFiles?.Any() ?? false)
                foreach (var file in PasswordLockedFiles)
                    File.AppendAllText(Path.Join(DirectoryManager.BaseDirectory, "password-locked.csv"), $"{file}{Environment.NewLine}");
            Logger.Log($"Generating Missing Files Report: {MissingFiles?.Count}");
            if (MissingFiles?.Any() ?? false)
                foreach (var file in MissingFiles ?? Enumerable.Empty<string>())
                    File.AppendAllText(Path.Join(DirectoryManager.BaseDirectory, "missing-files.csv"), $"{file}{Environment.NewLine}");
            Logger.Log($"Generating Modified Files Report: {ModifiedFiles?.Count}");
            if (ModifiedFiles?.Any() ?? false)
                foreach (var tuple in ModifiedFiles ?? Enumerable.Empty<(string, string)>())
                    File.AppendAllText(Path.Join(DirectoryManager.BaseDirectory, "modified-files.csv"),
                        $"{tuple.Item1},{tuple.Item2}{Environment.NewLine}");
            Logger.Log($"Generating Succeses Report: {GeneralBucket?.Count}");
            if (GeneralBucket?.Any() ?? false)
                foreach (var item in GeneralBucket ?? Enumerable.Empty<string[]>())
                    File.AppendAllText(Path.Join(DirectoryManager.BaseDirectory, "success.csv"),
                        $"{item.Aggregate((a, b) => $"{a},{b}")}{Environment.NewLine}");
            Logger.Log("Finished Generating Reports");
        }

        public static void ReportPasswordLockedFile(string filename)
        {
            lock (_lock)
                (PasswordLockedFiles ??= new List<string>()).Add(filename);
        }

        public static void Report(string[] entry, params string[] values)
        {
            lock (_lock)
                (GeneralBucket ??= new List<string[]>()).Add(entry.Concat(values).ToArray());
        }

        public static void ReportUnknownFile(string filename)
        {
            lock (_lock)
                (UnknownFiles ??= new List<string>()).Add(filename);
            Logger.Log($"{filename} Added to unknown file report collection", Logger.LogLevel.Warning);
        }

        public static void ReportMissingFile(string filename)
        {
            lock (_lock)
                (MissingFiles ??= new List<string>()).Add(filename);
            Logger.Log($"{filename} Added to missing report's collection", Logger.LogLevel.Warning);
        }

        public static void ReportMissingContent(string[] orig)
        {
            lock (_lock)
                (MissingContent ??= new List<string[]>()).Add(orig);
            Logger.Log($"{orig[0]} missing content needed for report", Logger.LogLevel.Warning);
        }

        public static void ReportModifiedFile(string original, string filename)
        {
            lock (_lock)
                (ModifiedFiles ??= new List<(string, string)>()).Add((original, filename));
            Logger.Log($"Modified File Found: {original} {filename}");
        }
    }
}
