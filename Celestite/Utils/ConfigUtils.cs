using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Text;
using System.Text.Json;
using Celestite.Configs;
using Celestite.Network;
using Celestite.Network.Models;
using Cysharp.Text;
using Microsoft.Data.Sqlite;
using ZeroLog;

#if !DEBUG
using MemoryPack;
#endif

namespace Celestite.Utils
{
    public static class ConfigUtils
    {
        private static LauncherConfig _config = new();
        private static DmmGameCnf _dmmGameCnf = new();
        private static string _dmmConfigFile = string.Empty;
        private static bool _initialized = false;
#if DEBUG
        private const bool DebugMode = true;
#else
        private const bool DebugMode = false;
#endif
        private static readonly Log Logger = LogManager.GetLogger("Config");

        private static readonly string Dgp5ConfigFilePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "dmmgameplayer5");
        public static readonly bool IsDgp5Installed;

        public static string CelestiteAppConfigFolder { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "celestite");
        public static string DmmGamesDefaultInstallFolder => _dmmGameCnf.DefaultInstallDir;

        public static event EventHandler<EventArgs>? OnGameContentChanged;

        static ConfigUtils()
        {
            IsDgp5Installed = File.Exists(Path.Combine(Dgp5ConfigFilePath, "Local State")) && File.Exists(Path.Combine(Dgp5ConfigFilePath, "Network", "Cookies"));
            DmmGamePlayerApiHelper.UserInfoChangedEvent += (currentUserInfo, _) =>
            {
                if (currentUserInfo is not UserInfo userInfo || !TryGetLastLogin(out var accountObject) ||
                    accountObject == null || userInfo == null) return;
                var userName = userInfo?.Profile?.Nickname ?? string.Empty;
                if (accountObject.NickName != userName)
                {
                    Logger.Info($"UserInfo changed {accountObject.Id}, new username: {userName}");
                    accountObject.NickName = userName;
                    // accountObject.AvatarImage = userInfo.Profile.AvatarImage;
                    Save();
                }
            };
        }

        public static void UpdateDmmGamesDefaultInstallFolder(string folder)
        {
            ReadDmmConfig();
            _dmmGameCnf.DefaultInstallDir = folder;
            Logger.Info("dmmgame.cnf save triggered by UpdateDmmGamesDefaultInstallFolder");
            SaveDmmCnf();
        }

        public static LocalState? ParseLocalState()
        {
            var localStatePath = ZString.Concat(Dgp5ConfigFilePath, "/Local State");
            if (!File.Exists(localStatePath)) return null;
            using var fs = File.OpenRead(localStatePath);
            return JsonSerializer.Deserialize(fs, ElectronDataJsonSerializerContext.Default.LocalState);
        }

        public static SqliteConnection OpenDgpCookiesConnection()
        {
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = ZString.Concat(Dgp5ConfigFilePath, '/', "Network", '/', "Cookies")
            };
            var connection = new SqliteConnection(builder.ToString());
            return connection;
        }

        public static string GetDgpCookiesFilePath()
        {
            return ZString.Concat("Data Source = ", Dgp5ConfigFilePath, '/', "Network", '/', "Cookies", ';');
        }

        public static string GetWinePath() => _config.NonWindowsConfig.WinePath;

        public static void SetWinePath(string path)
        {
            _config.NonWindowsConfig.WinePath = path;
            Save();
        }

        public static string GetFilePath(ReadOnlySpan<byte> fileName) => Path.Combine(CelestiteAppConfigFolder, Convert.ToHexString(XxHash3.Hash(fileName)));

        public static string GetFilePath(string fileName) => Path.Combine(CelestiteAppConfigFolder,
            Convert.ToHexString(XxHash3.Hash(Encoding.UTF8.GetBytes(fileName))));

        public static bool TryGetContent(string productId, TApiGameType type, out DmmGameCnfContent? content)
        {
            ReadDmmConfig();
            content = _dmmGameCnf.Contents.FirstOrDefault(x =>
                x.ProductId == productId && x.GameType == type);
            return content != null;
        }

        public static IEnumerable<DmmGameCnfContent> GetAllInstalledContent()
        {
            ReadDmmConfig();
            return _dmmGameCnf.Contents.Where(x => x.Detail.Installed);
        }
        public static bool IsContentInstalled(string productId, TApiGameType type, out DmmGameCnfContent? config) =>
            TryGetContent(productId, type, out config) && config!.Detail.Installed;

        public static DmmGameCnfContent UpdateLocalContent(string productId, TApiGameType type, DmmGameCnfContentDetail detail)
        {
            if (TryGetContent(productId, type, out var content))
                content!.Detail = detail;
            else
            {
                content = new DmmGameCnfContent
                {
                    ProductId = productId,
                    GameType = type,
                    Detail = detail
                };
                _dmmGameCnf.Contents.Add(content);
            }
            Logger.Info("dmmgame.cnf save triggered by UpdateLocalContent");
            foreach (var p in _dmmGameCnf.Contents)
            {
                if (p.Detail != null)
                    Logger.Info($"gameId {p.ProductId}, version {p.Detail.Version}, path {p.Detail.Path}");
            }
            SaveDmmCnf();
            OnGameContentChanged?.Invoke(null, EventArgs.Empty);
            return content;
        }

        public static void RemoveLocalContent(string productId, TApiGameType type)
        {
            if (TryGetContent(productId, type, out var content))
                _dmmGameCnf.Contents.Remove(content!);
            SaveDmmCnf();
            OnGameContentChanged?.Invoke(null, EventArgs.Empty);
        }

        private static void ReadDmmConfig()
        {
            if (string.IsNullOrEmpty(_dmmConfigFile))
                return;
            using var dmmConfigFileStream = File.OpenRead(_dmmConfigFile);
            _dmmGameCnf = JsonSerializer.Deserialize(dmmConfigFileStream, DmmGameCnfJsonSerializerContext.Default.DmmGameCnf) ?? throw new InvalidDataException("error in reading dmmgame.cnf");
        }

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;

            if (!Directory.Exists(CelestiteAppConfigFolder))
                Directory.CreateDirectory(CelestiteAppConfigFolder);
            if (!Directory.Exists(Dgp5ConfigFilePath))
                Directory.CreateDirectory(Dgp5ConfigFilePath);

            _dmmConfigFile = Path.Combine(Dgp5ConfigFilePath, "dmmgame.cnf");
            if (!File.Exists(_dmmConfigFile))
            {
                using var dmmConfigFileStream = File.OpenWrite(_dmmConfigFile);
                _dmmGameCnf.DefaultInstallDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Games");
                if (!Directory.Exists(_dmmGameCnf.DefaultInstallDir))
                    Directory.CreateDirectory(_dmmGameCnf.DefaultInstallDir);
                JsonSerializer.Serialize(dmmConfigFileStream, _dmmGameCnf,
                    DmmGameCnfJsonSerializerContext.Default.DmmGameCnf);
            }
            else
                ReadDmmConfig();

            var configFile = GetFilePath(DebugMode ? "config.json"u8 : "config.dat"u8);
            if (File.Exists(configFile))
            {
                using var readStream = File.OpenRead(configFile);
#if DEBUG
                _config = JsonSerializer.Deserialize(readStream, LauncherConfigJsonSerializerContext.Default.LauncherConfig!)!;
#else
                {
                    using var memoryStream = new MemoryStream();
                    readStream.CopyTo(memoryStream);
                    MemoryPackSerializer.Deserialize(memoryStream.ToArray(), ref _config!);
                }
#endif
            }
            else
            {
                _config = new LauncherConfig();
                Save();
            }

            // 问题修复
            if (_config.BaseSection.Locale.Contains('_'))
                _config.BaseSection.Locale = _config.BaseSection.Locale.Replace('_', '-');
        }

        public static string GetLocale() => _config.BaseSection.Locale;

        public static bool TryGetGuidByAccountEmail(string email, out Guid guid) =>
            _config.FastAccountsIndex.TryGetValue(email, out guid);
        public static bool TryGetAccountObjectByGuid(Guid guid, out AccountObject? accountObject) =>
            _config.Accounts.TryGetValue(guid, out accountObject);

        public static Dictionary<Guid, AccountObject> GetAllAccountObjects() => _config.Accounts;

        public static bool TryGetAccountObjectByUserId(string userId, out AccountObject? accountObject)
        {
            accountObject = _config.Accounts.Values.FirstOrDefault(x => x.UserId == userId);
            return accountObject != null;
        }

        public static void PushAccountObject(AccountObject accountObject, bool notPushLastLogin = false)
        {
            if (accountObject.SaveEmail)
            {
                if (_config.FastAccountsIndex.TryGetValue(accountObject.Email, out var accountObjectOldGuid))
                    _config.Accounts.Remove(accountObjectOldGuid);
                _config.FastAccountsIndex[accountObject.Email] = accountObject.Id;
            }
            _config.Accounts[accountObject.Id] = accountObject;
            if (!notPushLastLogin)
                _config.BaseSection.LastLoginId = accountObject.Id;
            Save();
        }

        public static bool IsLastLogin(Guid guid) => _config.BaseSection.LastLoginId == guid;

        public static void ClearLastLogin()
        {
            _config.BaseSection.LastLoginId = null;
            Save();
        }

        public static void RemoveAccountObjectByGuid(Guid innerGuid)
        {
            if (_config.Accounts.Remove(innerGuid, out var accountObject))
                _config.FastAccountsIndex.Remove(accountObject.Email);
            Save();
        }

        public static bool TryGetLastLogin(out AccountObject? accountObject)
        {
            accountObject = null;
            return _config.BaseSection.LastLoginId != null &&
                   TryGetAccountObjectByGuid(_config.BaseSection.LastLoginId.Value, out accountObject);
        }

        public static string[] GetAllSavedEmails()
        {
            return [.. _config.FastAccountsIndex.Keys];
        }

        // TODO: 异常处理
        public static void Save()
        {
            var configFile = GetFilePath(DebugMode ? "config.json"u8 : "config.dat"u8);
            using var writeStream = File.OpenWrite(configFile);

#if DEBUG
            writeStream.SetLength(0);
            JsonSerializer.Serialize(writeStream, _config, LauncherConfigJsonSerializerContext.Default.LauncherConfig!);
#else
            {
                var bytes = MemoryPackSerializer.Serialize(_config);
                writeStream.Write(bytes);
            }
#endif

            Logger.Info("config.dat saved");
        }

        private static void SaveDmmCnf()
        {
            using var writeStream = File.OpenWrite(Path.Combine(Dgp5ConfigFilePath, "dmmgame.cnf"));
            writeStream.SetLength(0);
            JsonSerializer.Serialize(writeStream, _dmmGameCnf, DmmGameCnfJsonSerializerContext.Default.DmmGameCnf);
        }

        public static bool UseMica() => _config.BaseSection.UseMica;
        public static bool UseAcrylic() => _config.BaseSection.UseAcrylic;
        public static bool GetMinimizeToTray() => _config.BaseSection.MinimizeToTray;
        public static bool GetHimariProxyStatus() => _config.BaseSection.EnableHimariProxy;
        public static bool GetBypassSystemProxy() => _config.BaseSection.BypassSystemProxy;
        public static string GetLanguageCode() => _config.BaseSection.Locale;
        public static bool GetEnableEmbeddedWebView() => _config.BaseSection.EnableEmbeddedWebView;
        public static bool GetDisableIFrameEx() => _config.BaseSection.DisableIFrameEx;

        public static void SetMica(bool value)
        {
            _config.BaseSection.UseMica = value;
            Save();
        }
        public static void SetAcrylic(bool value)
        {
            _config.BaseSection.UseAcrylic = value;
            Save();
        }

        public static void SetMinimizeToTray(bool value)
        {
            _config.BaseSection.MinimizeToTray = value;
            Save();
        }

        public static void SetHimariProxyStatus(bool value)
        {
            _config.BaseSection.EnableHimariProxy = value;
            Save();
        }
        public static void SetBypassSystemProxy(bool value)
        {
            _config.BaseSection.BypassSystemProxy = value;
            Save();
        }
        public static void SetLanguageCode(string value)
        {
            _config.BaseSection.Locale = value;
            Save();
        }
        public static void SetEnableEmbeddedWebView(bool value)
        {
            _config.BaseSection.EnableEmbeddedWebView = value;
            Save();
        }
        public static void SetDisableIFrameEx(bool value)
        {
            _config.BaseSection.DisableIFrameEx = value;
            Save();
        }

        public static GameSettingsForLauncher GetGameSettings(string productId)
        {
            if (_config.GameSettings.TryGetValue(productId, out var settings)) return settings;
            settings = new GameSettingsForLauncher();
            _config.GameSettings[productId] = settings;
            Save();

            return settings;
        }

        public static string GetGameSettingsCommandLine(string productId) => _config.GameSettings.TryGetValue(productId, out var settings) ? settings.ExtraCommandLine : string.Empty;
        public static bool GetGameSettingsForceSkipFileCheck(string productId) => _config.GameSettings.TryGetValue(productId, out var settings) && settings.ForceSkipFileCheck;
        public static bool GetGameSettingsForcePin(string productId) => _config.GameSettings.TryGetValue(productId, out var settings) && settings.ForcePin;
        public static void UpdateGameSettingsCommandLine(string productId, string commandLine)
        {
            if (commandLine.Length > 128)
                commandLine = commandLine[..128];
            if (_config.GameSettings.TryGetValue(productId, out var settings))
                settings.ExtraCommandLine = commandLine;
            else
            {
                settings = new GameSettingsForLauncher
                {
                    ExtraCommandLine = commandLine
                };
                _config.GameSettings.Add(productId, settings);
            }
            Save();
        }

        public static void UpdateGameSettingsForceSkipCheck(string productId, bool value)
        {
            if (_config.GameSettings.TryGetValue(productId, out var settings))
                settings.ForceSkipFileCheck = value;
            else
            {
                settings = new GameSettingsForLauncher
                {
                    ForceSkipFileCheck = value
                };
                _config.GameSettings.Add(productId, settings);
            }
            Save();
        }

        public static void UpdateGameSettingsForcePin(string productId, bool value)
        {
            if (_config.GameSettings.TryGetValue(productId, out var settings))
                settings.ForcePin = value;
            else
            {
                settings = new GameSettingsForLauncher
                {
                    ForcePin = value
                };
                _config.GameSettings.Add(productId, settings);
            }
            Save();
        }
    }
}
