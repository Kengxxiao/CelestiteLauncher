using System;
using System.IO;
using Cysharp.Text;

namespace Celestite.Utils
{
    public static class FileUtils
    {
        public static string LocaleFolder { get; }
        public static string LogFolder { get; }
        public static string HttpCacheFolder { get; }

        static FileUtils()
        {
            LocaleFolder = EnsureAppDirectory("Locale");
            LogFolder = EnsureAppDirectory("Log");
            HttpCacheFolder = EnsureAppDirectory("HttpCache");
        }

        private static string EnsureAppDirectory(string folder)
        {
            var path = Path.Combine(GetCurrentDirectory(), folder);
            if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
            return path;
        }

        public static string GetCurrentDirectory()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        public static string CalcFileSizeString(long bytes)
        {
            const long unit = 1024;
            if (bytes < unit) return $"{bytes} B";

            var exp = (int)(Math.Log(bytes) / Math.Log(unit));
            var sizeUnit = ZString.Concat("KMGTPE"[exp - 1], 'B');
            var adjustedSize = bytes / Math.Pow(unit, exp);
            return $"{adjustedSize:0.##} {sizeUnit}";
        }
    }
}
