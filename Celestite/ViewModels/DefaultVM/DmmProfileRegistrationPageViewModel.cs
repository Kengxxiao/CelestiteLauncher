using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Html.Dom;
using Celestite.I18N;
using Celestite.Network;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using ZeroLog;

namespace Celestite.ViewModels.DefaultVM
{
    public partial class DmmProfileRegistrationPageViewModel : ViewModelBase
    {
        [ObservableProperty] private string _nickName = string.Empty;
        [ObservableProperty] private bool _isMaleChecked = true;
        [ObservableProperty] private DateTime _birthday = DateTime.UtcNow.AddYears(-30);

        [ObservableProperty] private string _errorMessage = string.Empty;

        public DmmProfileRegistrationPageViewModel()
        {
            GenerateRandomName();
        }

        private static readonly string[] DmmNames = [
            "提督",
            "審神者",
            "親方",
            "社長",
            "王子",
            "城主",
            "プロデューサー",
            "首領",
            "店長",
            "千雪"
            ,
            "結城",
            "山田",
            "田中",
            "紅雪",
            "小春凪",
            "なつひめ",
            "みさか白鳳",
            "てまり姫",
            "早生あかつき"
            ,
            "ゆかりん",
            "かおりん",
            "あーみん",
            "まりあ",
            "ことり",
            "ゆっきー",
            "みいにゃん",
            "あやきち",
            "つぐつぐ",
            "みゆ"
            ,
            "十六夜",
            "琥珀",
            "月輝夜姫",
            "都椿姫",
            "百日紅",
            "花螺李",
            "濡烏",
            "王鈴",
            "秋茜",
            "姫星紅"
            ,
            "龍眼",
            "黒の剣士",
            "自宅警備員",
            "星の王子様",
            "マルク",
            "カメバズーカ",
            "アルパカ",
            "ペンギン",
            "ロキ",
            "ムスカ"
            ,
            "シルキー",
            "月の住人",
            "ホワイトライオン",
            "覇王",
            "仙人掌",
            "しらゆき",
            "なぎ",
            "はづき",
            "ハラショー",
            "ハーゲンティ"
            ,
            "ムルムル",
            "黒い天使",
            "レリエル",
            "ルベライト",
            "赤虎眼石",
            "きたかみ",
            "さしゃ",
            "ななみつき",
            "はやて",
            "北斗"
            ,
            "クレド",
            "カノン",
            "ねむのき"
        ];

        [RelayCommand]
        private void GenerateRandomName()
        {
            NickName = RandomNumberGenerator.GetItems<string>(DmmNames.AsSpan(), 1)[0];
        }

        private void SetErrorMessage(string message)
        {
            ErrorMessage = message;
            _token = string.Empty;
        }

        private async UniTask ProcessUserInfo()
        {
            var userInfo = await DmmGamePlayerApiHelper.GetUserInfo();
            if (userInfo.Success)
                NavigationFactory.OuterInstance.Navigate(NavigationFactory.MainNavigation);
            else
            {
                _isCausedByUserinfoNetwork = true;
                SetErrorMessage(userInfo.ErrorMessage!);
            }
        }

        [RelayCommand]
        private async Task StartRegister()
        {
            if (_isCausedByUserinfoNetwork)
            {
                await ProcessUserInfo();
                return;
            }
            if (string.IsNullOrEmpty(_token))
                await GetToken("/profile/regist/");
            using var profileRegisterMessage = await HttpHelper.PostFormDataAsync(ApiBaseUrl, "/profile/regist/registration",
                new Dictionary<string, string>
                {
                    {"_token", _token },
                    {"nickname", NickName },
                    {"gender", IsMaleChecked ? string.Intern("male") : string.Intern("female") },
                    {"year", ZString.Format("{0:D4}", Birthday.Year) },
                    {"month", ZString.Format("{0:D2}", Birthday.Month) },
                    {"day", ZString.Format("{0:D2}", Birthday.Day) },
                    {"isGeneralChecked", "on" },
                    {"back_url", string.Empty },
                    {"lp_param", "0" },
                    {"app_id", string.Empty },
                    {"redirect_url", string.Empty },
                    {"isGeneralRegistered", string.Empty },
                    {"isAdultRegistered", string.Empty }
                });
            if (profileRegisterMessage.Failed)
            {
                SetErrorMessage(profileRegisterMessage.Exception.Message);
                return;
            }

            if (profileRegisterMessage.Value.StatusCode != HttpStatusCode.Found || profileRegisterMessage.Value.Headers.Location == null)
            {
                Logger.Warn(ZString.Format("profile register got {0}", profileRegisterMessage.Value.StatusCode));
                SetErrorMessage(Localization.ProfileRegisterParseError);
                return;
            }

            if (profileRegisterMessage.Value.Headers.Location.AbsolutePath.EndsWith("/regist/commit"))
                await ProcessUserInfo();
            else if (profileRegisterMessage.Value.Headers.Location.AbsolutePath.EndsWith("profile/regist"))
                await GetToken("/profile/regist/");
            else
            {
                Logger.Warn(ZString.Format("profile register got invalid redirection: {0}", profileRegisterMessage.Value.Headers.Location.AbsolutePath));
                SetErrorMessage(Localization.ProfileRegisterParseError);
            }
        }

        private const string ApiBaseUrl = "https://personal.games.dmm.com";
        private static readonly Log Logger = LogManager.GetLogger("ProfileReg");
        private string _token = string.Empty;
        private bool _isCausedByUserinfoNetwork;

        private async UniTask GetToken(string path)
        {
            _token = string.Empty;
            var defaultRegisterPage = await HttpHelper.GetStringAsync(ApiBaseUrl, path);
            if (defaultRegisterPage.Failed)
            {
                SetErrorMessage(defaultRegisterPage.Exception.Message);
                return;
            }
            try
            {
                using var content = await HttpHelper.HtmlParserContext.OpenAsync(req => req.Content(defaultRegisterPage.Value))
                    .ConfigureAwait(false);

                var nextDataElement = content.GetElementsByName("_token");
                if (nextDataElement is not [IHtmlInputElement input])
                {
                    Logger.Warn(ZString.Format("_token not found: {0}", path));
                    SetErrorMessage(Localization.ProfileRegisterParseError);
                    return;
                }

                var alertText = content.GetElementsByClassName("alert_txt");
                if (alertText.Length != 0)
                {
                    var errors = new List<string>();
                    foreach (var element in alertText)
                    {
                        if (element is IHtmlParagraphElement el)
                            errors.Add(el.TextContent);
                    }

                    if (errors.Count != 0)
                        ErrorMessage = ZString.Join('\n', errors);
                    else
                    {
                        Logger.Warn(ZString.Format("cannot find alert_txt: {0}", path));
                        ErrorMessage = Localization.ProfileRegisterInvalidRequest;
                    }
                }

                _token = input.DefaultValue;
            }
            catch (Exception ex)
            {
                Logger.Warn(ZString.Format("parse _token got exception: {0}", ex.Message));
                SetErrorMessage(ex.Message);
            }
        }
    }
}
