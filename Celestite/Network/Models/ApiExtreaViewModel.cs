using System;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Celestite.Utils;
using Celestite.ViewModels;
using CommunityToolkit.Mvvm.Input;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using FluentAvalonia.UI.Controls;
using WanaKanaShaapu;

namespace Celestite.Network.Models
{
    public partial class LogNotification : ViewModelBase
    {
        private Task<IconSource?>? _iconSource;
        [JsonIgnore]
        public Task<IconSource?> Image => LazyInitializer.EnsureInitialized(ref _iconSource, () => AvaImageHelper.LoadFromWebForIconSource(ImageUrl));
        [RelayCommand]
        private void Click()
        {
            ProcessUtils.OpenExternalLink(HtmlLink);
        }
    }

    public partial class AnnounceInfo
    {
        public string FormattedTitle => ZString.Format("{0:D4}-{1:D2}-{2:D2} {3}", DateBegin!.Value.Year, DateBegin!.Value.Month,
            DateBegin!.Value.Day, Title);
    }

    public partial class ViewBannerRotationInfo
    {
        private Task<Bitmap?>? _image;
        [JsonIgnore]
        public Task<Bitmap?> Image => LazyInitializer.EnsureInitialized(ref _image, () => AvaImageHelper.LoadFromWeb(Banner));
    }

    public partial class MyGame
    {
        // TODO: BUG ItemsRepeater会初始化Image两次，所以用Lazy
        private Task<Bitmap?>? _image;
        [JsonIgnore]
        public Task<Bitmap?> Image => LazyInitializer.EnsureInitialized(ref _image, () => AvaImageHelper.LoadFromWeb(PackageImageUrl));

        private string? _romaji;
        [JsonIgnore] public string Romaji => LazyInitializer.EnsureInitialized(ref _romaji, () => WanaKana.ToRomaji(TitleRuby));

        public DmmTypeApiGame ApiType => DmmTypeApiGameHelper.GetGameTypeDataFromApiGameType(Type);
        private string ApiSuffix => DmmTypeApiGameHelper.GetSuffix(ApiType);
        public bool EnableDmmGameOptions => !ApiType.IsBrowser;

        public static EventHandler<EventArgs>? GameStatusChangedEvent;

        public void UpdateForcePinCelestite(bool value)
        {
            ConfigUtils.UpdateGameSettingsForcePin(ProductId, value);
            GameStatusChangedEvent?.Invoke(null, EventArgs.Empty);
        }

        public async UniTask<DmmGamePlayerApiResult> UpdateMyGameFavorite()
        {
            var response = await HttpHelper.DgpPostJsonAsync("/v5/mygame/favorite", new MyGameFavoriteRequest
            {
                GameType = Type,
                IsFavorite = IsFavorite,
                ProductId = ProductId
            },
                DmmGamePlayerApiRequestBaseContext.Default.MyGameFavoriteRequest, DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponse);
            if (response.Failed)
                return DmmGamePlayerApiResult.Fail(response.Exception);
            var result = DmmGamePlayerApiResult.Ok(response.Value);
            GameStatusChangedEvent?.Invoke(null, EventArgs.Empty);
            return result;
        }

        public async UniTask<DmmGamePlayerApiResult> UpdateMyGameViewFlag()
        {
            var response = await HttpHelper.DgpPostJsonAsync("/v5/mygame/viewflag", new MyGameViewFlagRequest
            {
                GameType = Type,
                IsView = IsView,
                ProductId = ProductId
            },
                DmmGamePlayerApiRequestBaseContext.Default.MyGameViewFlagRequest, DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponse);
            if (response.Failed)
                return DmmGamePlayerApiResult.Fail(response.Exception);
            var result = DmmGamePlayerApiResult.Ok(response.Value);
            GameStatusChangedEvent?.Invoke(null, EventArgs.Empty);
            return result;
        }

        public async UniTask<DmmGamePlayerApiResult<GameDetailSetting>> GetDetailSettingList()
        {
            var response = await HttpHelper.DgpPostJsonAsync("/v5/detail/setting", new ProductBaseWithGameOsRequest
            {
                GameType = Type,
                ProductId = ProductId
            },
                DmmGamePlayerApiRequestBaseContext.Default.ProductBaseWithGameOsRequest, DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponseGameDetailSetting);
            return response.Failed ? DmmGamePlayerApiResult.Fail<GameDetailSetting>(response.Exception) : DmmGamePlayerApiResult.Ok(response.Value);
        }

        public async UniTask<DmmGamePlayerApiResult> UpdateAllowReceiveNotification(bool isNotification)
        {
            var response = await HttpHelper.DgpPostJsonAsync(ZString.Concat("/v5/notification/", ApiSuffix), new UpdateNotificationRequest()
            {
                ProductId = ProductId,
                IsAdult = ApiType.IsAdult,
                IsNotification = isNotification
            },
                DmmGamePlayerApiRequestBaseContext.Default.UpdateNotificationRequest, DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponse);
            return response.Failed ? DmmGamePlayerApiResult.Fail(response.Exception) : DmmGamePlayerApiResult.Ok(response.Value);
        }

        public async UniTask<DmmGamePlayerApiResult> UpdateIsDisplayProfile(bool isDisplayProfile)
        {
            var response = await HttpHelper.DgpPostJsonAsync(ZString.Concat("/v5/displayprofile/", ApiSuffix), new UpdateDisplayProfileRequest()
            {
                ProductId = ProductId,
                IsAdult = ApiType.IsAdult,
                IsDisplay = isDisplayProfile
            },
                DmmGamePlayerApiRequestBaseContext.Default.UpdateDisplayProfileRequest, DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponse);
            return response.Failed ? DmmGamePlayerApiResult.Fail(response.Exception) : DmmGamePlayerApiResult.Ok(response.Value);
        }
    }
}
