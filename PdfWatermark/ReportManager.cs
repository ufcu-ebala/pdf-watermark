using System.Collections.Generic;

namespace PdfWatermark
{
    public static class ReportManager
    {
        private static ICollection<string> MissingFiles { get; set; }
        private static ICollection<string[]> MissingContent { get; set; }

        public static void Generate() { }

        public static void ReportMissingFile(string filename)
        {
            (MissingFiles ??= new List<string>()).Add(filename);
            Logger.Log($"{filename} Added to missing report's collection", Logger.LogLevel.Warning);
        }

        public static void ReportMissingContent(string[] orig)
        {
            (MissingContent ??= new List<string[]>()).Add(orig);
            Logger.Log($"{orig[0]} missing content needed for report", Logger.LogLevel.Warning);
        }
    }
}
