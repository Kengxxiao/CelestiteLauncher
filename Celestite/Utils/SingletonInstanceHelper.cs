using System;
using System.IO;
using System.Threading;

namespace Celestite.Utils
{
    public class SingletonInstanceHelper
    {
        private static Mutex _mtxSingleInstance = null!;
        private static FileStream _fileStream = null!;
        public static bool IsRunning()
        {
            if (OperatingSystem.IsWindows())
            {
                _mtxSingleInstance = new Mutex(true, $"Global\\{Environment.UserName}_celestite", out var isNotRunning);
                return !isNotRunning;
            }

            if (OperatingSystem.IsLinux())
            {
                try
                {
                    _fileStream = File.Open(Path.Combine(ConfigUtils.CelestiteAppConfigFolder, "celestite.lock"),
                        FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                    _fileStream.Write(BitConverter.GetBytes(Environment.ProcessId));
                    _fileStream.Flush();
                }
                catch (IOException)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
