using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Celestite.Configs;
using Celestite.I18N;
using Celestite.Network;
using Celestite.Utils;
using Celestite.ViewModels.DefaultVM;
using Celestite.ViewModels.Dialogs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;

namespace Celestite.ViewModels.Pages
{
    public partial class AccountObjectForRenderer(string email, string nickName, Guid guid, bool autoLogin, bool isCurrent) : ViewModelBase
    {
        public string Email => email;
        [ObservableProperty] private string _nickName = string.IsNullOrEmpty(nickName) ? Localization.UnknownUsername : nickName;
        public Guid InnerGuid => guid;

        [ObservableProperty] private bool _autoLogin = autoLogin;
        [ObservableProperty] private bool _isCurrent = isCurrent;

        partial void OnAutoLoginChanged(bool value)
        {
            if (!ConfigUtils.TryGetAccountObjectByGuid(guid, out var accountObject)) return;
            accountObject!.AutoLogin = value;
            ConfigUtils.Save();
        }
    }

    public partial class AccountPageViewModel : ViewModelBase
    {
        [ObservableProperty] private bool _isAccountBusy;
        [ObservableProperty] private ObservableCollection<AccountObjectForRenderer> _accountObjects = [];

        private void SetAllRenderAccountObjects()
        {
            var accountsObject = ConfigUtils.GetAllAccountObjects();
            foreach (var v in accountsObject.Values)
            {
                if (v is { SaveEmail: true, SavePassword: true })
                    AccountObjects.Add(new AccountObjectForRenderer(v.Email, v.NickName, v.Id, v.AutoLogin, ConfigUtils.IsLastLogin(v.Id)));
            }
        }

        public AccountPageViewModel()
        {
            SetAllRenderAccountObjects();
        }

        [RelayCommand]
        private async Task AddAccount()
        {
            var viewModel = Dispatcher.UIThread.Invoke(() => ((UserControl)LoginPageViewModel.DefaultLoginFormDialog.Content!).DataContext as DefaultLoginFormDialogViewModel)!;
            viewModel.Reset(lockSave: true);

            var result = await LoginPageViewModel.DefaultLoginFormDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                if (AccountObjects.Any(x => x.Email == viewModel.Email))
                {
                    NotificationHelper.Warn(Localization.AccountIsExistsError);
                    return;
                }

                if (string.IsNullOrEmpty(viewModel.Email))
                {
                    NotificationHelper.Warn(Localization.EmailRequiredErrorMessage);
                    return;
                }
                if (string.IsNullOrEmpty(viewModel.Password))
                {
                    NotificationHelper.Warn(Localization.PasswordRequiredErrorMessage);
                    return;
                }

                IsAccountBusy = true;

                try
                {
                    var login = await DmmOpenApiHelper.Login(viewModel.Email, viewModel.Password);
                    if (login.Failed) return;
                    var acc = new AccountObject
                    {
                        AutoLogin = viewModel.AutoLogin,
                        Email = viewModel.Email,
                        Password = viewModel.Password,
                        RefreshToken = login.Value.RefreshToken,
                        SaveEmail = true,
                        SavePassword = true,
                        UserId = string.Empty
                    };
                    ConfigUtils.PushAccountObject(acc, notPushLastLogin: true);
                    AccountObjects.Add(new AccountObjectForRenderer(acc.Email, string.Empty, acc.Id, viewModel.AutoLogin, false));
                    NotificationHelper.Success(Localization.AddAccountSuccess);
                }
                finally
                {
                    IsAccountBusy = false;
                }
            }
        }

        [RelayCommand]
        private void DeleteAccount(Guid innerGuid)
        {
            if (ConfigUtils.IsLastLogin(innerGuid))
            {
                NotificationHelper.Warn(Localization.AccountIsUsingError);
                return;
            }
            if (IsAccountBusy)
            {
                NotificationHelper.Warn(Localization.AccountBusyError);
                return;
            }

            ConfigUtils.RemoveAccountObjectByGuid(innerGuid);
            NotificationHelper.Success(Localization.DeleteAccountSuccess);

            var account = AccountObjects.FirstOrDefault(x => x.InnerGuid == innerGuid);
            if (account != null)
                AccountObjects.Remove(account);
        }

        [RelayCommand]
        private async Task ChangeAccount(Guid innerGuid)
        {
            if (ConfigUtils.IsLastLogin(innerGuid))
            {
                NotificationHelper.Warn(Localization.AccountIsUsingError);
                return;
            }
            if (IsAccountBusy)
            {
                NotificationHelper.Warn(Localization.AccountBusyError);
                return;
            }

            IsAccountBusy = true;
            try
            {
                if (!ConfigUtils.TryGetAccountObjectByGuid(innerGuid, out var accountObject))
                {
                    NotificationHelper.Warn(Localization.AccountNotExistsError);
                    return;
                }

                if (!string.IsNullOrEmpty(accountObject!.RefreshToken) && !string.IsNullOrEmpty(accountObject.UserId))
                {
                    DmmOpenApiHelper.UpdateRefreshToken(accountObject!.RefreshToken);
                    var refresh = await DmmOpenApiHelper.RefreshToken();
                    if (refresh.Failed)
                    {
                        accountObject.RefreshToken = string.Empty;
                        ConfigUtils.Save();
                        return;
                    }
                    accountObject.RefreshToken = refresh.Value.RefreshToken;
                }
                else
                {
                    var login = await DmmOpenApiHelper.Login(accountObject.Email, accountObject.Password);
                    if (login.Failed) return;

                    accountObject.RefreshToken = login.Value.RefreshToken;

                    var exchangeAccessToken = await DmmOpenApiHelper.ExchangeAccessToken(login.Value.AccessToken);
                    if (exchangeAccessToken.Failed) return;

                    if (string.IsNullOrEmpty(accountObject.UserId))
                    {
                        if (!JwtUtils.TryParseIdToken(login.Value.IdToken, out var idToken) ||
                            string.IsNullOrEmpty(idToken?.UserId))
                        {
                            NotificationHelper.Warn(Localization.InvalidIdTokenError);
                            return;
                        }

                        accountObject.UserId = idToken.UserId;
                    }
                }

                var issueSessionId = await DmmOpenApiHelper.IssueSessionId(accountObject.UserId);
                if (issueSessionId.Failed) return;

                DmmGamePlayerApiHelper.SetUserCookies(issueSessionId.Value.SecureId, issueSessionId.Value.UniqueId);
                DmmGamePlayerApiHelper.SetAgeCheckDone();
                ConfigUtils.PushAccountObject(accountObject);

                var userInfo = await DmmGamePlayerApiHelper.GetUserInfo();
                if (userInfo.Failed)
                {
                    NotificationHelper.Warn(userInfo.ErrorMessage!);
                    return;
                }

                foreach (var acc in AccountObjects)
                {
                    if (acc.InnerGuid == innerGuid)
                    {
                        acc.NickName = userInfo.Value.Profile.Nickname;
                        acc.IsCurrent = true;
                    }
                    else
                        acc.IsCurrent = false;
                }

                NotificationHelper.Success(Localization.SwitchAccountSuccess);
            }
            finally
            {
                IsAccountBusy = false;
            }
        }
    }
}
