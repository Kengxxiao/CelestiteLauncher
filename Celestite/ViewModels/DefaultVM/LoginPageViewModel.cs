using System;
using System.Buffers;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Celestite.Configs;
using Celestite.Dialogs;
using Celestite.I18N;
using Celestite.Network;
using Celestite.Network.Models;
using Celestite.Utils;
using Celestite.ViewModels.Dialogs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using FluentAvalonia.UI.Controls;
using ZeroLog;

namespace Celestite.ViewModels.DefaultVM
{
    public partial class LoginPageViewModel : ViewModelBase
    {
        [ObservableProperty] private string _infoBarMessage = string.Empty;
        [ObservableProperty] private InfoBarSeverity _severity = InfoBarSeverity.Warning;
        [ObservableProperty] private bool _canUseLoginButton;

        [ObservableProperty] private bool _isDmmLoginEnabled = ConfigUtils.IsDgp5Installed && OperatingSystem.IsWindows();

        private readonly Log _logger = LogManager.GetLogger("Login");

        private static ContentDialog? _registerFormDialog = null;
        private static ContentDialog? _defaultLoginFormDialog = null;

        public static ContentDialog RegisterFormDialog => LazyInitializer.EnsureInitialized(ref _registerFormDialog,
            () => Dispatcher.UIThread.Invoke(() => new ContentDialog()
            {
                Title = Localization.RegisterSubtitle,
                CloseButtonText = Localization.FormClose,
                Content = new RegisterFormDialog()
            }));

        public static ContentDialog DefaultLoginFormDialog => LazyInitializer.EnsureInitialized(
            ref _defaultLoginFormDialog, () => Dispatcher.UIThread.Invoke(() => new ContentDialog
            {
                Title = Localization.LoginSubtitle,
                PrimaryButtonText = Localization.DefaultLoginTitle,
                CloseButtonText = Localization.FormClose,
                DefaultButton = ContentDialogButton.Primary,
                Content = new DefaultLoginFormDialog()
            }));

        public LoginPageViewModel()
        {
            if (!Design.IsDesignMode)
                UniTask.Run(() => ProcessAutoLoginCommand.ExecuteAsync(null));
        }
        private static async UniTask<DmmOpenApiResult> Request(string email, string password, bool saveEmail, bool savePassword, bool autoLogin)
        {
            HttpHelper.ClearCookies();
            var login = await DmmOpenApiHelper.Login(email, password);
            if (login.Failed)
                return login;
            var accessToken = await DmmOpenApiHelper.ExchangeAccessToken(login.Value.AccessToken);
            if (accessToken.Failed)
                return accessToken;
            if (!JwtUtils.TryParseIdToken(login.Value.IdToken, out var idToken) || string.IsNullOrEmpty(idToken?.UserId))
                return DmmOpenApiResult.Fail(Localization.InvalidIdTokenError);
            if (!ConfigUtils.TryGetGuidByAccountEmail(email, out var id) ||
                !ConfigUtils.TryGetAccountObjectByGuid(id, out var accountObject) || !ConfigUtils.TryGetAccountObjectByUserId(idToken.UserId!, out accountObject))
                accountObject = new AccountObject();
            accountObject!.SaveEmail = saveEmail;
            accountObject!.SavePassword = savePassword;
            if (saveEmail)
                accountObject!.Email = email;
            if (savePassword)
                accountObject!.Password = password;
            accountObject!.AutoLogin = autoLogin;
            if (autoLogin)
            {
                accountObject.UserId = idToken!.UserId;
                accountObject.RefreshToken = login.Value.RefreshToken;
            }
            var issueSessionId = await DmmOpenApiHelper.IssueSessionId(idToken!.UserId!);
            if (issueSessionId.Failed) return issueSessionId;
            if (!string.IsNullOrEmpty(accountObject.Email) || !string.IsNullOrEmpty(accountObject.UserId))
                ConfigUtils.PushAccountObject(accountObject);
            DmmGamePlayerApiHelper.SetUserCookies(issueSessionId.Value.SecureId, issueSessionId.Value.UniqueId);
            DmmGamePlayerApiHelper.SetAgeCheckDone();
            return issueSessionId;
        }

        private async UniTask ProcessUser()
        {
            var userInfo = await DmmGamePlayerApiHelper.GetUserInfo();
            if (userInfo.Failed)
            {
                SetLoginErrorStatus(userInfo.ErrorMessage!);
                return;
            }

            NavigationFactory.OuterInstance.Navigate(!userInfo.Value.IsProfileRegistered
                ? NavigationFactory.DmmProfileReg
                : NavigationFactory.MainNavigation);
        }

        [RelayCommand]
        private async Task ProcessAutoLogin()
        {
            if (ConfigUtils.TryGetLastLogin(out var accountObject) && accountObject!.AutoLogin)
            {
                SetLoginStatus();
                HttpHelper.ClearCookies();
                DmmOpenApiHelper.UpdateRefreshToken(accountObject!.RefreshToken);
                var login = await DmmOpenApiHelper.RefreshToken();
                if (login.Failed)
                {
                    SetLoginErrorStatus(login.Error!);
                    return;
                }
                accountObject.RefreshToken = login.Value.RefreshToken;
                ConfigUtils.PushAccountObject(accountObject);
                var issueSessionId = await DmmOpenApiHelper.IssueSessionId(accountObject.UserId!);
                if (issueSessionId.Failed)
                {
                    SetLoginErrorStatus(issueSessionId.Error!);
                    return;
                }
                DmmGamePlayerApiHelper.SetUserCookies(issueSessionId.Value.SecureId, issueSessionId.Value.UniqueId);
                DmmGamePlayerApiHelper.SetAgeCheckDone();
                await ProcessUser();
            }
            SetDefaultStatus();
        }

        [RelayCommand]
        private async Task ShowRegisterButton()
        {
            await RegisterFormDialog.ShowAsync();
        }

        [RelayCommand]
        private async Task ShowDefaultLoginButton()
        {
            var viewModel = Dispatcher.UIThread.Invoke(() => ((UserControl)DefaultLoginFormDialog.Content!).DataContext as DefaultLoginFormDialogViewModel)!;
            viewModel.Reset(lockSave: false);
            var result = await DefaultLoginFormDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                SetLoginStatus();

                var requestOp = await Request(viewModel.Email, viewModel.Password, viewModel.SaveEmail, viewModel.SavePassword, viewModel.AutoLogin);
                if (requestOp.Failed)
                {
                    SetLoginErrorStatus(requestOp.Error!);
                }
                else
                {
                    await ProcessUser();
                    SetDefaultStatus();
                }
            }
        }

        [RelayCommand]
        private void ShowDmmLoginButton()
        {
            if (!OperatingSystem.IsWindows())
            {
                SetLoginErrorStatus(Localization.UnsupportedSystemError);
                return;
            }

            SetLoginStatus();

            var localState = ConfigUtils.ParseLocalState();
            if (localState == null)
            {
                SetLoginErrorStatus(Localization.LoadLocalStateError);
                return;
            }

            // TODO: 多系统
            if (localState.OsCrypt == null || !ConvertEncryptedKeyBase64(localState.OsCrypt.EncryptedKey, out var masterKey))
            {
                SetLoginErrorStatus(Localization.LoadLocalStateError);
                return;
            }
            masterKey = ProtectedData.Unprotect(masterKey, null, DataProtectionScope.CurrentUser);
            UniTask.Run(async () =>
            {
                try
                {
                    await using var sqlConnection = ConfigUtils.OpenDgpCookiesConnection();
                    var altHash = ElectronCookie.GetCookieByName(sqlConnection, "althash");
                    var hasAltHash = ElectronCookie.GetCookieByName(sqlConnection, "has_althash");
                    if (altHash == null || hasAltHash == null)
                    {
                        SetLoginErrorStatus(Localization.LoggedUserNotFoundError);
                        return;
                    }

                    var decryptedAltHash = DecryptCookie(altHash.EncryptedValue.AsSpan(), masterKey);
                    var decryptedHasAltHash = DecryptCookie(hasAltHash.EncryptedValue.AsSpan(), masterKey);

                    var loginResult =
                        await DmmAltHashLoginHelper.LoginFromAltHash(decryptedAltHash, decryptedHasAltHash);
                    if (loginResult.Failed)
                    {
                        SetLoginErrorStatus(loginResult.Error!);
                        return;
                    }

                    altHash.EncryptedValue = EncryptCookie(loginResult.Value.AltHash.Value, masterKey);
                    altHash.ExpiresUtc =
                        (((DateTimeOffset)loginResult.Value.AltHash.Expires).ToUnixTimeSeconds() + 11644473600) *
                        1000000;
                    hasAltHash.EncryptedValue = EncryptCookie(loginResult.Value.HasAltHash.Value, masterKey);
                    hasAltHash.ExpiresUtc =
                        (((DateTimeOffset)loginResult.Value.HasAltHash.Expires).ToUnixTimeSeconds() + 11644473600) *
                        1000000;
                    ElectronCookie.UpdateCookie(sqlConnection, altHash);
                    ElectronCookie.UpdateCookie(sqlConnection, hasAltHash);

                    DmmGamePlayerApiHelper.SetUserCookies(loginResult.Value.LoginSecureId, loginResult.Value.LoginSessionId);
                    DmmGamePlayerApiHelper.SetAgeCheckDone();

                    ConfigUtils.ClearLastLogin();
                    await ProcessUser();
                    SetDefaultStatus();
                }
                catch (Exception ex)
                {
                    SetLoginErrorStatus(ex.Message);
                    _logger.Error(string.Empty, ex);
                }
            }).Forget();
        }

        private static bool ConvertEncryptedKeyBase64(string base64, out byte[] result)
        {
            var bytes = ArrayPool<byte>.Shared.Rent(CyBase64.GetMaxBase64DecodeLength(base64.Length));
            try
            {
                if (CyBase64.TryFromBase64String(base64, bytes, out var bytesWritten) && bytesWritten >= 5)
                {
                    result = bytes[5..bytesWritten];
                    return true;
                }
                result = [];
                return false;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }
        }

        private static string DecryptCookie(Span<byte> encryptedData, byte[] masterKey)
        {
            if (encryptedData.Length < 3 + 12 + 16)
                return string.Empty;
            var nonce = encryptedData[3..15];
            var cipherText = encryptedData[15..^16];
            var tag = encryptedData[^16..(encryptedData.Length)];

            Span<byte> resultBytes = stackalloc byte[cipherText.Length];
            using var aesGcm = new AesGcm(masterKey, 16);
            aesGcm.Decrypt(nonce, cipherText, tag, resultBytes);
            return Encoding.UTF8.GetString(resultBytes);
        }

        private static byte[] EncryptCookie(string data, byte[] masterKey)
        {
            using var stream = new MemoryStream();
            stream.Write("v10"u8);
            var nonce = RandomNumberGenerator.GetBytes(12);
            using var aesGcm = new AesGcm(masterKey, 16);
            Span<byte> tag = stackalloc byte[16];
            var cipherText = Encoding.UTF8.GetBytes(data);
            var encryptedCipherText = new byte[cipherText.Length];
            aesGcm.Encrypt(nonce, cipherText, encryptedCipherText, tag);
            stream.Write(nonce);
            stream.Write(encryptedCipherText);
            stream.Write(tag);
            return stream.ToArray();
        }

        private void SetLoginStatus()
        {
            InfoBarMessage = Localization.LoginStatusInfoBarMessage;
            Severity = InfoBarSeverity.Informational;
            CanUseLoginButton = false;
        }
        private void SetLoginErrorStatus(string errorMessage)
        {
            InfoBarMessage = errorMessage;
            Severity = InfoBarSeverity.Error;
            CanUseLoginButton = true;
        }

        private void SetLoginErrorStatus(DmmOpenApiErrorBody error)
        {
            var errorMessage = ZString.Format("[{0}] {1}", error.Code, error.Reason);
            InfoBarMessage = errorMessage;
            Severity = InfoBarSeverity.Error;
            CanUseLoginButton = true;
        }
        private void SetDefaultStatus()
        {
            InfoBarMessage = Localization.DGPLoginInfoBarMessage;
            Severity = InfoBarSeverity.Warning;
            CanUseLoginButton = true;
        }
    }
}
