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
        /// <param name="evt"></param>
        /// <param name="progress"></param>
        public static void DoWork(string[] entry, CountdownEvent evt, ProgressBar progress)
        {
            var session = Guid.NewGuid().ToString("N").Substring(0, 8);
            // check original files, error'ing on files missing
            Logger.Log($"[{session}] Attempting to check for file: {entry[0]}", Logger.LogLevel.Verbose);
            var file = DirectoryManager.GetFile(entry[0]);
            if (file == null)
            {
                progress.Report();
                evt.Signal();
                return;
            }

            Logger.Log($"[{session}] File Found, moving to 'Original' Folder: {file.Name}", Logger.LogLevel.Verbose);
            // convert to PDF and move to "original" folder
            PdfManager.ConvertToPdf(file.FullName,
                Path.Join(DirectoryManager.OriginalDirectory, $"{Path.GetFileNameWithoutExtension(file.Name)}.pdf"));
            Logger.Log($"[{session}] File Successfully migrated: {file.Name} - {Path.GetFileNameWithoutExtension(file.Name)}.pdf", Logger.LogLevel.Verbose);
            // create a failure drop location if not enough information to generate index
            if (entry.Length < 3 || entry.Any(string.IsNullOrEmpty))
            {
                ReportManager.ReportMissingContent(entry);
                File.Copy(Path.Join(DirectoryManager.OriginalDirectory, $"{Path.GetFileNameWithoutExtension(file.Name)}.pdf"),
                    Path.Join(DirectoryManager.FailureDirectory, $"{file.Name}.pdf"));
                Logger.Log(
                    $"[{session}] File Successfully migrated to failures directory: {Path.GetFileNameWithoutExtension(file.Name)}.pdf",
                    Logger.LogLevel.Verbose);
                progress.Report();
                evt.Signal();
                return;
            }

            Logger.Log($"[{session}] Creating Watermark PDF: {Path.GetFileNameWithoutExtension(file.Name)}.pdf", Logger.LogLevel.Verbose);
            // create watermarked pdf
            PdfManager.WatermarkPdf(Path.Join(DirectoryManager.OriginalDirectory, $"{Path.GetFileNameWithoutExtension(file.Name)}.pdf"),
                Path.Join(DirectoryManager.ArchiveDirectory, $"{Path.GetFileNameWithoutExtension(file.Name)}.pdf"));
            Logger.Log($"[{session}] File Successfully created: {Path.GetFileNameWithoutExtension(file.Name)}.pdf", Logger.LogLevel.Verbose);
            // create associated IDX
            using (var writer = File.CreateText(Path.Join(DirectoryManager.ArchiveDirectory, $"{Path.GetFileNameWithoutExtension(file.Name)}.csv")))
                writer.WriteLine($"{entry[1]},Other,{entry[2]}");
            Logger.Log($"[{session}] Associated IDX File Created: {Path.GetFileNameWithoutExtension(file.Name)}.csv", Logger.LogLevel.Verbose);
            ReportManager.Report(entry);
            evt.Signal();
            progress.Report();
        }
    }
}
