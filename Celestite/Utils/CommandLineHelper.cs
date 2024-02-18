using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using Avalonia.Platform;
using Celestite.Network;
using Celestite.Network.Models;
using Cysharp.Threading.Tasks;
using IniParser;
using Microsoft.Win32;

namespace Celestite.Utils
{
    public class CommandLineHelper
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
            var commandLine = cmd.Split(' ');
            var args = ParseArguments(commandLine);
            if (args.TryGetValue("scheme", out var scheme))
            {
                try
                {
                    var uri = new Uri(scheme);
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
                catch (Exception)
                {
                    // ignored
                }
            }
            else if (args.TryGetValue("productId", out var productId) && args.TryGetValue("type", out var type))
            {
                try
                {
                    var typeEnum = TApiGameTypeExtension.FromString(type);
                    await LaunchHelper.LaunchGame(productId, typeEnum);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
            else
            {
                WindowTrayHelper.RequestShow();
            }
        }

        private static Dictionary<string, string> ParseArguments(IReadOnlyList<string> args)
        {
            var arguments = new Dictionary<string, string>();

            for (var i = 0; i < args.Count; i++)
            {
                if (!args[i].StartsWith("--") || i + 1 >= args.Count) continue;
                var key = args[i][2..];
                var value = args[i + 1];
                arguments[key] = value;
                i++;
            }

            return arguments;
        }
    }
}
