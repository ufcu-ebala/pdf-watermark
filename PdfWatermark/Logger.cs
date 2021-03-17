using System;

namespace PdfWatermark
{
    public static class Logger
    {
        public static LogLevel DefaultLevel = LogLevel.Info;

        public static void Log(string message, LogLevel level = LogLevel.Info)
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
