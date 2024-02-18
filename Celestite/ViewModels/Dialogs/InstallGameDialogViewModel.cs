using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Celestite.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Celestite.ViewModels.Dialogs
{
    public partial class InstallGameDialogViewModel(string fileSize) : ViewModelBase
    {
        [ObservableProperty] private string _fileSize = fileSize;

        [ObservableProperty] private bool _launchAfterInstall;
        [ObservableProperty] private bool _createShortcutAfterInstall;

        [ObservableProperty] private string _downloadDir = ConfigUtils.DmmGamesDefaultInstallFolder;

        partial void OnDownloadDirChanged(string value)
        {
            ConfigUtils.UpdateDmmGamesDefaultInstallFolder(value);
        }

        [RelayCommand]
        private async Task OpenFolderDialog()
        {
            var picker =
                await ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow!
                    .StorageProvider.OpenFolderPickerAsync(
                        new FolderPickerOpenOptions()
                        {
                            Title = "选择游戏安装目录",
                            AllowMultiple = false
                        }
                    );
            if (picker.Count == 1)
                DownloadDir = picker[0].Path.LocalPath;
        }
    }
}
