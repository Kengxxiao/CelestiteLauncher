using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Text;
using WmiLight;

namespace Celestite.Utils
{
    // Mac 和 Linux 版本的 Interop 没有被使用，不过已存在的部分 Mac 逻辑确实是从 cex 上逆向的
    public partial class LinuxInterop
    {
        [SupportedOSPlatform("linux")]
        [LibraryImport("libc", EntryPoint = "geteuid", SetLastError = true)]
        public static partial uint GetEUID();
    }
    public partial class MacHelper
    {
        [GeneratedRegex("((en|eth)[0-9]+|ethernet)$", RegexOptions.IgnoreCase)]
        public static partial Regex Ethernet();
        [GeneratedRegex("(vboxnet[0-9]+)$", RegexOptions.IgnoreCase)]
        public static partial Regex VboxNet();
        [GeneratedRegex("^(VirtualBox)+", RegexOptions.IgnoreCase)]
        public static partial Regex VirtualBox();
    }
    public static class SystemInfoUtils
    {
#pragma warning disable IDE1006 // 命名样式
        [SupportedOSPlatform("macos")]
        static class CoreFoundation
        {
            const string LibraryName = "CoreFoundation.framework/CoreFoundation";

            public enum CFStringEncoding : uint
            {
                kCFStringEncodingASCII = 0x0600
            }

            [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
            public static extern void CFRelease(IntPtr cf);

            [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern bool CFDictionaryGetValueIfPresent(CFTypeRef theDict, CFTypeRef key, out IntPtr value);

            [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
            public static extern CFTypeRef CFStringCreateWithCString(CFTypeRef allocator, string cStr, CFStringEncoding encoding);

            [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
            [return: MarshalAs(UnmanagedType.U1)]
            public static extern bool CFStringGetCString(CFTypeRef theString, StringBuilder buffer, long bufferSize, CFStringEncoding encoding);

            [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
            public static extern CFTypeRef CFUUIDCreateString(CFTypeRef allocator, IntPtr uuid);
        }
        [SupportedOSPlatform("macos")]
        class CFTypeRef : SafeHandle
        {
            public CFTypeRef()
                : base(IntPtr.Zero, ownsHandle: true)
            {
            }

            protected override bool ReleaseHandle()
            {
                if (IsInvalid)
                {
                    return false;
                }

                CoreFoundation.CFRelease(handle);
                return true;
            }

            public override bool IsInvalid => handle == IntPtr.Zero;

            public static CFTypeRef None => new();
        }
        [SupportedOSPlatform("macos")]
        static class IOKit
        {
            const string LibraryName = "IOKit.framework/IOKit";

            public const uint kIOMasterPortDefault = 0;

            public const string kIOPlatformSerialNumberKey = "IOPlatformSerialNumber";

            [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
            public static extern CFTypeRef IORegistryEntryCreateCFProperty(uint entry, CFTypeRef key, CFTypeRef allocator, uint options);

            [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
            public static extern uint IOServiceGetMatchingService(uint masterPort, IntPtr matching);

            [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr IOServiceMatching(string name);

            [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int IOObjectRelease(uint @object);
        }
#pragma warning restore IDE1006 // 命名样式

        private const string ProcessElectronVersion = "33.3.1";

        public const string AppConfigProtocolVersion = "5.3.20";
        public const string Architecture = "amd64";

        private static string? _userOs;
        public static string UserOs => LazyInitializer.EnsureInitialized(ref _userOs, () =>
        {
            if (OperatingSystem.IsWindows())
                return "win";
            else if (OperatingSystem.IsMacOS())
                return "mac";
            else
                return "win";
        });

        private static string? _userAgent;
        public static string RequestHeaderUserAgentName => LazyInitializer.EnsureInitialized(ref _userAgent, () =>
        {
            using var sb = ZString.CreateUtf8StringBuilder();
            sb.Append("DMMGamePlayer5-");
            sb.Append(UserOs == "win" ? "Win" : "Mac");
            sb.Append('/');
            sb.Append(AppConfigProtocolVersion); // TODO: appConfig.version 和协议有关 过低可能会invalid client
            sb.Append(" Electron/");
            sb.Append(ProcessElectronVersion);
            return sb.ToString();
        });

        /*
         * 通过对 cex.dll 的 Windows 版本逆向可以得知
         * 计算 hdd_serial 时最终被hash的字符串一定是 null
         * e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 是 SHA256(string.Empty)的结果
         */
        public const string HddSerial = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        private record NetworkInterfaceDmmCExtend(
            NetworkInterface NetworkInterface,
            int Flag,
            int Score);

        private static string? _macAddress;

        public static string MacAddress => LazyInitializer.EnsureInitialized(ref _macAddress, () =>
        {
            string macAddress;
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            var ethernetInterface = networkInterfaces.Select(x =>
            {
                // 摘抄自 cex.dll 模拟golang的net interface
                int score = 0, golangFlag = 0;
                if (x.OperationalStatus == OperationalStatus.Up)
                {
                    golangFlag |= 1 << 0; // FlagUp           
                    golangFlag |= 1 << 5; // FlagRunning                        
                }

                switch (x.NetworkInterfaceType)
                {
                    case NetworkInterfaceType.Ethernet:
                    case NetworkInterfaceType.TokenRing:
                    case NetworkInterfaceType.Wireless80211:
                    case NetworkInterfaceType.HighPerformanceSerialBus:
                        golangFlag |= 1 << 1 | 1 << 4; // FlagBroadcast | FlagMulticast
                        break;
                    case NetworkInterfaceType.Ppp:
                    case NetworkInterfaceType.Tunnel:
                        golangFlag |= 1 << 3 | 1 << 4; // FlagPointToPoint | FlagMulticast
                        break;
                    case NetworkInterfaceType.Loopback:
                        golangFlag |= 1 << 2 | 1 << 4; // FlagLoopback | FlagMulticast
                        break;
                    case NetworkInterfaceType.Atm:
                        golangFlag |= 1 << 1 | 1 << 3 | 1 << 4; // FlagBroadcast | FlagPointToPoint | FlagMulticast
                        break;
                    default:
                        break;
                }

                if ((golangFlag & 1 << 2) != 0) // FlagLoopback
                    ++score;
                if ((golangFlag & 1 << 0) != 0) // FlagUp
                    ++score;

                if (MacHelper.Ethernet().IsMatch(x.Name))
                    score += 2;
                if (MacHelper.VboxNet().IsMatch(x.Name))
                    score -= 3;
                if (MacHelper.VirtualBox().IsMatch(x.Name))
                    score -= 3;

                return new NetworkInterfaceDmmCExtend(x, golangFlag, score);
            }).Where(x => x.NetworkInterface.GetPhysicalAddress().GetAddressBytes().Length != 0).MaxBy(x => x.Score)?.NetworkInterface;

            if (ethernetInterface == null)
                macAddress = "noNetworktInterface"; // 我没打错字，cex 里就是这个
            else
            {
                var physicalAddress = ZString.Join(':',
                    ethernetInterface.GetPhysicalAddress().GetAddressBytes().Select(b => b.ToString("x2")));
                macAddress = physicalAddress;
            }

            return macAddress;
        });


        private static string? _motherboard;
        public static string Motherboard => LazyInitializer.EnsureInitialized(ref _motherboard, () =>
        {
            var motherboardCache = string.Empty;
            if (OperatingSystem.IsWindows())
            {
                using var wmiConn = new WmiConnection();
                var queries = wmiConn.CreateQuery("SELECT * FROM Win32_BIOS").FirstOrDefault();
                if (queries != null)
                    motherboardCache = queries["SerialNumber"].ToString();
            }
            else if (OperatingSystem.IsMacOS()) // ioreg -l | grep IOPlatformSerialNumber
            {
                var platformExpert = IOKit.IOServiceGetMatchingService(IOKit.kIOMasterPortDefault,
                    IOKit.IOServiceMatching("IOPlatformExpertDevice"));
                if (platformExpert == 0)
                    return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(motherboardCache!)))
                        .ToLower();
                try
                {
                    using var serialNumberKey = CoreFoundation.CFStringCreateWithCString(CFTypeRef.None,
                        IOKit.kIOPlatformSerialNumberKey,
                        CoreFoundation.CFStringEncoding.kCFStringEncodingASCII);
                    var serialNumberAsString =
                        IOKit.IORegistryEntryCreateCFProperty(platformExpert, serialNumberKey, CFTypeRef.None,
                            0);
                    var sb = new StringBuilder(64);
                    if (CoreFoundation.CFStringGetCString(serialNumberAsString, sb, sb.Capacity,
                            CoreFoundation.CFStringEncoding.kCFStringEncodingASCII))
                        motherboardCache = sb.ToString().Trim();
                }
                finally
                {
                    _ = IOKit.IOObjectRelease(platformExpert);
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                motherboardCache = Environment.UserDomainName;
            }
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(motherboardCache!))).ToLower();
        });
    }
}
