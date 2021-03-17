using System;
using System.IO;
using System.Text;

namespace PdfWatermark
{
    public static class Logger
    {
        private static readonly object _lock = new();
        private const string FileName = "logs_{0}.log";

        public static LogLevel DefaultLevel = LogLevel.Info;

        public static void Log(string message, LogLevel level = LogLevel.Info)
        {
            lock (_lock)
            {
                ConsoleLog(message, level);
                FileLog(message, level);
            }
        }

        public static void FileLog(string message, LogLevel level = LogLevel.Info)
        {
            if (string.IsNullOrEmpty(DirectoryManager.BaseDirectory)) return;   // we don't have a location to log files to
            File.AppendAllText(
                Path.Join(DirectoryManager.BaseDirectory, string.Format(FileName, DateTime.Today.ToString("yyyyMMdd"))),
                $"[{DateTime.Now:s}] [{level}] {message}{Environment.NewLine}", Encoding.UTF8);
        }

        public static void ConsoleLog(string message, LogLevel level = LogLevel.Info)
        {
            Console.ResetColor();
            switch (level)
            {
                case LogLevel.Warning:
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"[{DateTime.Now:s}] [WRN] {message}");
                    Console.ResetColor();
                    break;
                case LogLevel.Error:
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[{DateTime.Now:s}] [ERR] {message}");
                    Console.ResetColor();
                    break;
                case LogLevel.Verbose when level >= DefaultLevel:
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"[{DateTime.Now:s}] [VRB] {message}");
                    Console.ResetColor();
                    break;
                case LogLevel.Info:
                default:
                    Console.WriteLine($"[{DateTime.Now:s}] [{level}] {message}");
                    break;
            }
        }

        public enum LogLevel
        {
            Verbose = 0,
            Info = 1,
            Warning = 2,
            Error = 3,
        }
    }
}
