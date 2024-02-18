using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Celestite.Network;
using Celestite.Network.Downloader;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZeroLog;

namespace Celestite.ViewModels.Pages
{
    public partial class DownloadStatistic(string productId, string title, string imageUrl) : ViewModelBase
    {
        public string ProductId => productId;
        public string Title => title;

        private Task<Bitmap?> _picture;
        public Task<Bitmap?> Picture => LazyInitializer.EnsureInitialized(ref _picture, () => AvaImageHelper.LoadFromWeb(imageUrl));

        [ObservableProperty]
        private long _downloadedBytes;
        [ObservableProperty]
        private long _totalBytes;
        [ObservableProperty]
        private long _speed;

        [ObservableProperty] private bool _isDismissEnabled = true;

        [ObservableProperty] private double _progress;

        [RelayCommand]
        private void CancelDownloadTask()
        {
            if (!KashimaDownloadManager.DownloaderInstance.TryGetValue(productId, out var target)) return;
            target.cts.Cancel();
            IsDismissEnabled = false;
        }
    }
    public partial class DownloadManagerPageViewModel : ViewModelBase
    {
        public static readonly DownloadManagerPageViewModel Instance = new();
        private readonly DispatcherTimer _timer;

        private static readonly Log Logger = LogManager.GetLogger("DownloadManagerVM");

        [ObservableProperty] private ObservableCollection<DownloadStatistic> _statistics = [];

        public DownloadManagerPageViewModel()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300),
            };
            _timer.Tick += UpdateStatistic;
            _timer.IsEnabled = true;
        }

        private void UpdateStatistic(object? sender, EventArgs e)
        {
            try
            {
                for (var i = Statistics.Count - 1; i >= 0; i--)
                {
                    if (!KashimaDownloadManager.IsProductDownloading(Statistics[i].ProductId))
                        Statistics.RemoveAt(i);
                }

                foreach (var instance in KashimaDownloadManager.DownloaderInstance)
                {
                    if (Statistics.All(x => x.ProductId != instance.Key))
                        Statistics.Add(instance.Value.downloader.DownloadStatistic);
                    else
                    {
                        var target = Statistics.FirstOrDefault(x => x.ProductId == instance.Key);
                        if (target == null) continue;
                        target.DownloadedBytes = instance.Value.downloader.GetDownloadedBytes();
                        target.TotalBytes = instance.Value.downloader.GetTotalSize();
                        target.Speed = instance.Value.downloader.GetSpeedPerSec();
                        target.Progress = target.TotalBytes == 0
                            ? 0
                            : Convert.ToDouble(target.DownloadedBytes) / Convert.ToDouble(target.TotalBytes) * 100;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(string.Empty, ex);
            }
        }
    }
}
