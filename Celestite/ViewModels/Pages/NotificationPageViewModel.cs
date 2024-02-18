using System.Threading;
using Avalonia.Collections;
using Avalonia.Threading;
using Celestite.Network;
using Celestite.Network.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Cysharp.Threading.Tasks;

namespace Celestite.ViewModels.Pages
{

    public partial class NotificationPageViewModel : ViewModelBase
    {
        private CancellationTokenSource? _cancellationTokenSource;

        [ObservableProperty] private AvaloniaList<LogNotification> _notifications = [];
        public NotificationPageViewModel()
        {
            DmmGamePlayerApiHelper.LoginSessionChangedEvent +=
                (_, _) =>
                {
                    Dispatcher.UIThread.Invoke(() => FetchNotifications().Forget());
                };
            FetchNotifications().Forget();
        }

        protected async UniTask FetchNotifications()
        {
            if (_cancellationTokenSource != null)
            {
                await _cancellationTokenSource.CancelAsync();
                _cancellationTokenSource.Dispose();
            }
            _cancellationTokenSource = new CancellationTokenSource();
            var result = await DmmGamePlayerApiHelper.GetLogNotification(_cancellationTokenSource.Token);
            if (result.Failed) return;
            Notifications.Clear();
            Notifications.AddRange(result.Value);
        }
    }
}
