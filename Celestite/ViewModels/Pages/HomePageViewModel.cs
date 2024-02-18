using System.Collections.ObjectModel;
using Avalonia.Threading;
using Celestite.Network;
using Celestite.Network.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Cysharp.Threading.Tasks;

namespace Celestite.ViewModels.Pages
{
    public partial class HomePageViewModel : ViewModelBase
    {
        [ObservableProperty] private ObservableCollection<AnnounceInfo> _announceInfos = [];
        [ObservableProperty] private ObservableCollection<ViewBannerRotationInfo> _banners = [];

        public HomePageViewModel()
        {
            DmmGamePlayerApiHelper.AdultModeChangedEvent += (_, _) => Dispatcher.UIThread.Invoke(() => _ = GetBannerRotation());
            GetAnnounceData().Forget();
            GetBannerRotation().Forget();
        }

        private async UniTask GetAnnounceData()
        {
            var announceData = await DmmGamePlayerApiHelper.AnnounceInfo();
            if (announceData.Failed) return;
            AnnounceInfos = new ObservableCollection<AnnounceInfo>(announceData.Value);
        }

        private async UniTask GetBannerRotation()
        {
            var banner = await DmmGamePlayerApiHelper.BannerRotationList();
            if (banner.Failed) return;
            Banners = new ObservableCollection<ViewBannerRotationInfo>(banner.Value);
        }
    }
}
