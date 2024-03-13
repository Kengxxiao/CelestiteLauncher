using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform;
using Celestite.Network;
using Celestite.Network.Models;
using CommandLine;
using Cysharp.Threading.Tasks;
using IniParser;
using Microsoft.Win32;

namespace Celestite.Utils
{
    public partial class CommandLineHelper
    {
        private static bool _created;
        public static void RegisterUriScheme()
        {
            using var process = Process.GetCurrentProcess();
            var currentFile = process.MainModule!.FileName;

            if (OperatingSystem.IsWindows())
            {
                using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Classes\celestite");
                key.SetValue("URL Protocol", string.Empty);

                using var defaultIcon = key.CreateSubKey("DefaultIcon");
                defaultIcon.SetValue(string.Empty, currentFile);

                using var shell = key.CreateSubKey("shell");
                using var open = shell.CreateSubKey("open");
                using var command = open.CreateSubKey("command");
                command.SetValue(string.Empty, $"{currentFile} --scheme %1");
            }
            else if (OperatingSystem.IsLinux())
            {
                var iconPath = $"{ConfigUtils.GetFilePath("celestite-icon.png"u8)}.png";
                if (!File.Exists(iconPath))
                {
                    using var stream = AssetLoader.Open(new Uri("avares://Celestite/Assets/celestite-icon.png"));
                    using var iconFile = File.OpenWrite(iconPath);
                    stream.CopyTo(iconFile);
                }
                var xdgDataHomeHead = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
                if (string.IsNullOrEmpty(xdgDataHomeHead))
                    xdgDataHomeHead = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var applicationPath = Path.Combine(xdgDataHomeHead, "applications");
                var xdgDataDesktop = Path.Combine(applicationPath, "celestite.desktop");
                if (File.Exists(xdgDataDesktop)) File.Delete(xdgDataDesktop);
                File.WriteAllText(xdgDataDesktop, $"""
                                                  [Desktop Entry]
                                                  Version=1.0
                                                  Type=Application
                                                  Exec={currentFile} --scheme %u
                                                  Icon={iconPath}
                                                  StartupNotify=true
                                                  NoDisplay=true
                                                  Terminal=false
                                                  Categories=Application
                                                  MimeType=x-scheme-handler/celestite
                                                  Name=Celestite Launcher
                                                  """.Replace("\r", string.Empty));
                var parser = new FileIniDataParser();
                parser.Parser.Configuration.AssigmentSpacer = string.Empty;
                var mimeAppsListPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "mimeapps.list");
                if (!File.Exists(mimeAppsListPath))
                    File.WriteAllLines(mimeAppsListPath, ["[Default Applications]"]);
                var iniData = parser.ReadFile(mimeAppsListPath);
                if (!iniData.Sections.ContainsSection("Added Associations"))
                    iniData.Sections.AddSection("Added Associations");
                iniData.Sections["Added Associations"].RemoveKey("x-scheme-handler/celestite");
                iniData.Sections["Added Associations"].AddKey("x-scheme-handler/celestite", "celestite.desktop;");
                parser.WriteFile(mimeAppsListPath, iniData);
                Process.Start("update-desktop-database", applicationPath);
            }
        }

        private static readonly BlockingCollection<string> CommandsQueue = [];

        public static void CreatePipe()
        {
            if (_created) return;
            _created = true;
            DmmGamePlayerApiHelper.LoginSessionChangedEvent += DmmGamePlayerApiHelper_LoginSessionChangedEvent;

            UniTask.Run(async () =>
            {
                await using var pipeSever = new NamedPipeServerStream($"{Environment.UserName}_celestite", PipeDirection.In);
                do
                {
                    try
                    {
                        await pipeSever.WaitForConnectionAsync();
                        using var sw = new StreamReader(pipeSever, leaveOpen: true);
                        var commandLine = await sw.ReadToEndAsync();
                        CommandsQueue.Add(commandLine);
                        pipeSever.Disconnect();
                    }
                    catch (Exception)
                    {
                        ;
                    }
                } while (true);
            }).Forget();
        }

        private static void DmmGamePlayerApiHelper_LoginSessionChangedEvent(object? sender, EventArgs e)
        {
            DmmGamePlayerApiHelper.LoginSessionChangedEvent -= DmmGamePlayerApiHelper_LoginSessionChangedEvent;
            CommandsQueue.Add(Environment.CommandLine);
            UniTask.Run(async () =>
            {
                while (true)
                {
                    var command = CommandsQueue.Take();
                    // Dispatcher.UIThread.Post(WindowTrayHelper.ReShowWindowCore);
                    await ParseCommandLine(command);
                }
            }).Forget();
        }

        public static async UniTask ParseCommandLine(string cmd)
        {
            var commands = cmd.Split(' ');
            var parserResult = await Parser.Default.ParseArguments<LaunchCommandLine>(commands)
                .WithParsedAsync(async (c) => await LaunchCall(c));
            if (string.IsNullOrEmpty(parserResult.Value.Scheme) && string.IsNullOrEmpty(parserResult.Value.ProductId) && string.IsNullOrEmpty(parserResult.Value.GameType))
                WindowTrayHelper.RequestShow();
        }

        private static async Task LaunchCall(LaunchCommandLine launchCommandLine)
        {
            if (!string.IsNullOrEmpty(launchCommandLine.Scheme))
            {
                try
                {
                    var uri = new Uri(launchCommandLine.Scheme);
                    if (uri.Authority is "launch" or "emulaunch")
                    {
                        var param = uri.AbsolutePath.Split('/');
                        if (param.Length < 3 || !string.IsNullOrEmpty(param[0]))
                            return;
                        var typeEnum = TApiGameTypeExtension.FromString(param[2].ToUpper());
                        switch (uri.Authority)
                        {
                            case "launch":
                                await LaunchHelper.LaunchGame(param[1], typeEnum);
                                break;
                            case "emulaunch":
                                await LaunchHelper.EmulateLaunch(param[1], DmmTypeApiGameHelper.GetGameTypeDataFromApiGameType(typeEnum));
                                break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to parse scheme. {e.Message}");
                }
                return;
            }

            if (!string.IsNullOrEmpty(launchCommandLine.ProductId) && !string.IsNullOrEmpty(launchCommandLine.GameType))
            {
                try
                {
                    var typeEnum = TApiGameTypeExtension.FromString(launchCommandLine.GameType);
                    await LaunchHelper.LaunchGame(launchCommandLine.ProductId, typeEnum);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to parse productId and gameType. {e.Message}");
                }
            }
        }

        private static async Task<bool> NoGuiLogin(LaunchCommandLine launchCommandLine)
        {
            string? userId;
            if (!string.IsNullOrEmpty(launchCommandLine.Username) && !string.IsNullOrEmpty(launchCommandLine.Password))
            {
                var login = await DmmOpenApiHelper.Login(launchCommandLine.Username, launchCommandLine.Password);
                if (login.Failed)
                    return false;
                var accessToken = await DmmOpenApiHelper.ExchangeAccessToken(login.Value.AccessToken);
                if (accessToken.Failed)
                    return false;
                if (!JwtUtils.TryParseIdToken(login.Value.IdToken, out var idToken) || string.IsNullOrEmpty(idToken?.UserId))
                    return false;
                userId = idToken.UserId;
            }
            else
            {
                if (ConfigUtils.TryGetLastLogin(out var accountObject) && accountObject!.AutoLogin)
                {
                    DmmOpenApiHelper.UpdateRefreshToken(accountObject!.RefreshToken);
                    var login = await DmmOpenApiHelper.RefreshToken();
                    if (login.Failed)
                        return false;
                    accountObject.RefreshToken = login.Value.RefreshToken;
                    ConfigUtils.PushAccountObject(accountObject);
                }
                else
                {
                    Console.WriteLine("Cannot find valid login session on Celestite save state, use --username and --password to login or do that in GUI mode.");
                    return false;
                }
                userId = accountObject.UserId;
            }

            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("Invalid userId.");
                return false;
            }
            var loginSession = await DmmOpenApiHelper.IssueSessionId(userId);
            if (loginSession.Failed)
                return false;
            DmmGamePlayerApiHelper.SetUserCookies(loginSession.Value.SecureId, loginSession.Value.UniqueId);
            DmmGamePlayerApiHelper.SetAgeCheckDone();
            return true;
        }

        [LibraryImport("kernel32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool AttachConsole(int dwProcessId);

        public static async Task<bool> ParseLaunchCommand(params string[] args)
        {
            ConfigUtils.Init();
            var parserResult = await Parser.Default.ParseArguments<LaunchCommandLine>(args)
                .WithParsedAsync(async (cmd) =>
                {
                    if (cmd.NoGui)
                    {
                        if (OperatingSystem.IsWindows()) AttachConsole(-1);
                        if (string.IsNullOrEmpty(cmd.Scheme) && (string.IsNullOrEmpty(cmd.ProductId) || string.IsNullOrEmpty(cmd.GameType)))
                        {
                            Console.WriteLine("Invalid game launch arguments.");
                            return;
                        }
                        if (await NoGuiLogin(cmd))
                            await LaunchCall(cmd);
                    }
                    else if (SingletonInstanceHelper.IsRunning())
                    {
                        using var pipeClient = new NamedPipeClientStream(".", $"{Environment.UserName}_celestite", PipeDirection.Out);
                        pipeClient.Connect();
                        var bytes = Encoding.UTF8.GetBytes(Environment.CommandLine);
                        pipeClient.Write(bytes, 0, bytes.Length);
                        Environment.Exit(0);
                        return;
                    }
                });
            if (parserResult.Errors.Any()) return false;
            return !parserResult.Value.NoGui;
        }
    }

    public class LaunchCommandLine
    {
        [Option('s', "scheme", Required = false)]
        public string Scheme { get; set; } = string.Empty;
        [Option('u', "username", Required = false)]
        public string Username { get; set; } = string.Empty;
        [Option('p', "password", Required = false)]
        public string Password { get; set; } = string.Empty;
        [Option('i', "productId", Required = false)]
        public string ProductId { get; set; } = string.Empty;
        [Option('t', "type", Required = false)]
        public string GameType { get; set; } = string.Empty;
        [Option('n', "nogui", Required = false)]
        public bool NoGui { get; set; }
    }
}
