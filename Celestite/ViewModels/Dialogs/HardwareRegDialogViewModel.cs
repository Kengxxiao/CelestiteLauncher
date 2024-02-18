using System.Collections.Generic;
using System.Net;
using Celestite.Network;
using Celestite.Network.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Cysharp.Threading.Tasks;

namespace Celestite.ViewModels.Dialogs
{
    public partial class HardwareRegDialogViewModel : ViewModelBase
    {
        [ObservableProperty] private string _userEmail = DmmGamePlayerApiHelper.CurrentUserInfo!.Email;
        [ObservableProperty] private string _hostname = Dns.GetHostName();
        [ObservableProperty] private string _verificationCode = string.Empty;

        [ObservableProperty] private int _maxDeviceCount = 5;

        [ObservableProperty] private List<Hardware> _hardware = [];

        public HardwareRegDialogViewModel()
        {
            GetHardwareList().Forget();
            PublishHardwareCode().Forget();
        }

        private async UniTask GetHardwareList()
        {
            var hardware = await DmmGamePlayerApiHelper.GetHardwareList();
            if (hardware.Failed) return;
            Hardware = hardware.Value.Hardwares ?? [];
            MaxDeviceCount = hardware.Value.DeviceAuthLimitNum;
        }

        private static async UniTask PublishHardwareCode()
        {
            await DmmGamePlayerApiHelper.PublishHardwareAuthCode();
        }
    }
}
