using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Celestite.I18N;
using Celestite.Network;
using Celestite.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using FluentAvalonia.UI.Controls;
using ZeroLog;

namespace Celestite.ViewModels.Dialogs
{
    public partial class RegisterFormDialogViewModel : ObservableValidator
    {
        [ObservableProperty] private string _email = string.Empty;
        [ObservableProperty] private string _password = string.Empty;
        [ObservableProperty] private string _passwordConfirmation = string.Empty;

        [ObservableProperty] private string _emailVerificationCode = string.Empty;

        [ObservableProperty] private bool _receiveEmailsFromDmm;

        [ObservableProperty] private string _infoBarMessage = string.Empty;
        [ObservableProperty] private InfoBarSeverity _severity = InfoBarSeverity.Warning;

        [ObservableProperty] private bool _isVCodeButtonEnabled;

        // DMM
        [ObservableProperty] private string _token = string.Empty;
        [ObservableProperty] private string _registrationId = string.Empty;

        private const string ApiBaseUrl = "https://accounts.dmm.com";

        private static readonly Log Logger = LogManager.GetLogger("Register");

        public RegisterFormDialogViewModel()
        {
            SetDefaultStatus();
        }

        private static bool EmailVerification(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            var index = value.IndexOf('@');
            return
                index > 0 &&
                index != value.Length - 1 &&
                index == value.LastIndexOf('@');
        }

        private bool ValidatePropertyForVerificationCodeButton()
        {
            var emailVerified = !string.IsNullOrEmpty(Email) && EmailVerification(Email);
            var passwordConfirmVerified = !string.IsNullOrEmpty(PasswordConfirmation) &&
                                          PasswordConfirmation.SequenceEqual(Password);
            return emailVerified && passwordConfirmVerified;
        }

        partial void OnEmailChanged(string value)
        {
            IsVCodeButtonEnabled = ValidatePropertyForVerificationCodeButton();
        }

        partial void OnPasswordChanged(string value)
        {
            IsVCodeButtonEnabled = ValidatePropertyForVerificationCodeButton();
        }

        partial void OnPasswordConfirmationChanged(string value)
        {
            IsVCodeButtonEnabled = ValidatePropertyForVerificationCodeButton();
        }

        private void InvalidRedirect(string locationStr)
        {
            Logger.Warn(ZString.Format("invalid redirect: {0}", locationStr));
            SetErrorMessage(Localization.RegisterParseError);
        }

        private void InvalidLocationInFoundStatusCode()
        {
            Logger.Warn("confirm response got 302 but no location");
            SetErrorMessage(Localization.RegisterParseError);
        }

        private async UniTask<DmmNextDataPageProps?> GenerateNextData(string path)
        {
            var defaultRegisterPage = await HttpHelper.GetStringAsync(ApiBaseUrl, path);
            if (defaultRegisterPage.Failed)
            {
                SetErrorMessage(defaultRegisterPage.Exception!.Message);
                return null;
            }
            var parsedData = await NextDataParserUtils.GetDmmNextData(defaultRegisterPage.Value);
            if (parsedData != null && !string.IsNullOrEmpty(parsedData.Token))
            {
                Token = parsedData.Token;
                return parsedData;
            }
            Logger.Warn(ZString.Format("Cannot find token: {0} {1}", path, parsedData == null ? null : JsonSerializer.Serialize(parsedData, NextDataJsonSerializerContext.Default.DmmNextDataPageProps)));
            SetErrorMessage(Localization.RegisterParseError);
            return null;
        }

        private async UniTask<bool> GenerateRegIdFromConfirm(string url)
        {
            EmailVerificationCode = string.Empty;
            var nextData = await GenerateNextData(url);
            if (nextData == null) return false;
            if (!string.IsNullOrEmpty(nextData.RegistrationId))
            {
                RegistrationId = nextData.RegistrationId;
                return true;
            }
            Logger.Warn("confirm response success but no registrationId in next.js data");
            SetErrorMessage(Localization.RegisterParseError);
            return false;
        }

        [RelayCommand]
        private async Task ConfirmVerificationCodeButton()
        {
            if (!string.IsNullOrEmpty(RegistrationId))
            {
                using var confirmResponse = await HttpHelper.PostFormDataAsync(ApiBaseUrl, "/welcome/signup/email/complete", new Dictionary<string, string>
                {
                    {"authenticationCode", EmailVerificationCode},
                    {"token", Token},
                    {"registrationId", RegistrationId}
                });
                if (confirmResponse.Failed)
                {
                    SetErrorMessage(confirmResponse.Exception!.Message);
                    return;
                }
                switch (confirmResponse.Value.StatusCode)
                {
                    // 成功
                    case HttpStatusCode.OK:
                        SetSuccessfulMessage(Localization.RegisterSuccess);
                        return;
                    case HttpStatusCode.Found:
                        {
                            var location = confirmResponse.Value.Headers.Location;
                            if (location == null)
                            {
                                InvalidLocationInFoundStatusCode();
                                return;
                            }
                            var locationStr = location.ToString();
                            if (locationStr.Contains("/welcome/signup/email/confirm?registration_id="))
                            {
                                if (!await GenerateRegIdFromConfirm(locationStr)) return;
                                Severity = InfoBarSeverity.Error;
                                InfoBarMessage = Localization.RegisterCodeError;
                                return;
                            }

                            InvalidRedirect(locationStr);
                            break;
                        }
                    default:
                        SetErrorMessage(Localization.RegisterParseError);
                        return;
                }
            }
            else SetDefaultStatus();
        }

        [RelayCommand]
        private async Task SendVerificationCodeButton()
        {
            HttpHelper.ClearCookies();
            SetDefaultStatus();

            if (await GenerateNextData("/welcome/signup/email") == null)
                return;

            using var applyResponse = await HttpHelper.PostFormDataAsync(ApiBaseUrl, "/welcome/signup/email/apply", new Dictionary<string, string>
            {
                {"email", Email},
                {"password", Password},
                {"mail_magazine_com", ReceiveEmailsFromDmm ? string.Intern("1") : string.Intern("0")},
                {"token", Token}
            });
            if (applyResponse.Failed)
            {
                SetErrorMessage(applyResponse.Exception!.Message);
                return;
            }
            switch (applyResponse.Value.StatusCode)
            {
                case HttpStatusCode.OK:
                    // 200 一般是直接错误
                    Logger.Warn("apply response got 200");
                    SetErrorMessage(Localization.RegisterError);
                    return;
                case HttpStatusCode.Found:
                    {
                        var location = applyResponse.Value.Headers.Location;
                        if (location == null)
                        {
                            InvalidLocationInFoundStatusCode();
                            return;
                        }

                        var locationStr = location.ToString();
                        if (locationStr.EndsWith("/welcome/signup/email")) // 需要读取错误信息
                        {
                            var nextData = await GenerateNextData("/welcome/signup/email");
                            if (nextData == null) return;
                            SetErrorMessage(nextData.Error.Length == 0
                                ? Localization.RegisterError
                                : ZString.Join('\n', nextData!.Error));
                        }
                        else if (locationStr.Contains("/welcome/signup/email/confirm?registration_id="))
                            await GenerateRegIdFromConfirm(locationStr);
                        else InvalidRedirect(locationStr);
                        return;
                    }
                default:
                    SetErrorMessage(Localization.RegisterParseError);
                    return;
            }
        }

        private void SetDefaultStatus()
        {
            Token = string.Empty;
            RegistrationId = string.Empty;
            Severity = InfoBarSeverity.Warning;
            InfoBarMessage = Localization.RegisterWarningPhase;
        }

        private void SetErrorMessage(string message)
        {
            Token = string.Empty;
            RegistrationId = string.Empty;
            Severity = InfoBarSeverity.Error;
            InfoBarMessage = message;
        }

        private void SetSuccessfulMessage(string message)
        {
            Token = string.Empty;
            RegistrationId = string.Empty;
            Severity = InfoBarSeverity.Success;
            InfoBarMessage = message;
        }
    }
}
