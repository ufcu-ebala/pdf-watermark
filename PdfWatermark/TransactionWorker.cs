using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace PdfWatermark
{
    public static class TransactionWorker
    {
        /// <summary>
        /// Process work
        /// </summary>
        /// <param name="entry"></param>
        public static void DoWork(string[] entry, CountdownEvent evt, ProgressBar progress)
        {
            var session = Guid.NewGuid().ToString("N").Substring(0, 8);
            // check original files, error'ing on files missing
            Logger.Log($"[{session}] Attempting to check for file: {entry[0]}.tif", Logger.LogLevel.Verbose);
            if (!File.Exists(Path.Join(DirectoryManager.FileDirectory, $"{entry[0]}.tif")))
            {
                ReportManager.ReportMissingFile($"{entry[0]}.tif");
                progress.Report();
                evt.Signal();
                return;
            }

            Logger.Log($"[{session}] File Found, moving to 'Original' Folder: {entry[0]}.tif", Logger.LogLevel.Verbose);
            // convert to PDF and move to "original" folder
            PdfManager.TiffToPdf(Path.Join(DirectoryManager.FileDirectory, $"{entry[0]}.tif"),
                Path.Join(DirectoryManager.OriginalDirectory, $"{entry[0]}.pdf"));
            Logger.Log($"[{session}] File Successfully migrated: {entry[0]}.tif - {entry[0]}.pdf", Logger.LogLevel.Verbose);
            // create a failure drop location if not enough information to generate index
            if (entry.Length < 3 || entry.Any(string.IsNullOrEmpty))
            {
                ReportManager.ReportMissingContent(entry);
                File.Copy(Path.Join(DirectoryManager.OriginalDirectory, $"{entry[0]}.pdf"),
                    Path.Join(DirectoryManager.FailureDirectory, $"{entry[0]}.pdf"));
                Logger.Log($"[{session}] File Successfully migrated to failures directory: {entry[0]}.pdf", Logger.LogLevel.Verbose);
                progress.Report();
                evt.Signal();
                return;
            }

            Logger.Log($"[{session}] Creating Watermark PDF: {entry[0]}.pdf", Logger.LogLevel.Verbose);
            // create watermarked pdf
            PdfManager.WatermarkPdf(Path.Join(DirectoryManager.OriginalDirectory, $"{entry[0]}.pdf"),
                Path.Join(DirectoryManager.ArchiveDirectory, $"{entry[0]}.pdf"));
            Logger.Log($"[{session}] File Successfully created: {entry[0]}.pdf", Logger.LogLevel.Verbose);
            // create associated IDX
            using (var writer = File.CreateText(Path.Join(DirectoryManager.ArchiveDirectory, $"{entry[0]}.csv")))
                writer.WriteLine($"{entry[1]},{entry[2]}");
            Logger.Log($"[{session}] Associated IDX File Created: {entry[0]}.csv", Logger.LogLevel.Verbose);

            evt.Signal();
            progress.Report();
        }
    }
}
