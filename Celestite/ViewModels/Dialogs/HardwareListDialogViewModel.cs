using System.Collections.Generic;
using Celestite.Network;
using Celestite.Network.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Cysharp.Threading.Tasks;

namespace Celestite.ViewModels.Dialogs
{
    public partial class HardwareListDialogViewModel : ViewModelBase
    {
        [ObservableProperty] private List<Hardware> _hardware = [];
        [ObservableProperty] private int _maxAuthCount = 5;

        public HardwareListDialogViewModel()
        {
            GetHardwareList().Forget();
        }

        private async UniTask GetHardwareList()
        {
            var response = await DmmGamePlayerApiHelper.GetHardwareList();
            if (response.Failed) return;

            Hardware = response.Value.Hardwares ?? [];
            MaxAuthCount = response.Value.DeviceAuthLimitNum;
        }
    }
}
