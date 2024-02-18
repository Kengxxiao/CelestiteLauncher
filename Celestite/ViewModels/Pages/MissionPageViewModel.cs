using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Avalonia.Collections;
using Avalonia.Threading;
using Celestite.I18N;
using Celestite.Network;
using Celestite.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cysharp.Threading.Tasks;

namespace Celestite.ViewModels.Pages
{

    public partial class MissionDataV2ViewModel : ViewModelBase
    {
        public string Caption { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public List<MissionDataV2Type2> MissionDataV2Type2 { get; set; } = [];
        public List<MissionDataV2Type3> MissionDataV2Type3 { get; set; } = [];
        public List<MissionDataV2Type4> MissionDataV2Type4 { get; set; } = [];
    }

    public class MissionApiContext
    {
        public long UserId { get; set; }
        public long Timestamp { get; set; }
        public string Device { get; set; } = string.Empty;
        public string AuthenticationCode { get; set; } = string.Empty;
        public List<int> MissionIds { get; set; } = [];

        [JsonIgnore] public string Url { get; set; } = string.Empty;
    }

    [JsonSerializable(typeof(MissionApiContext))]
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
    public partial class MissionApiJsonContext : JsonSerializerContext
    {
    }

    public partial class MissionPageViewModel : ViewModelBase
    {
        private const string GeneralMissionWeb = "https://mission.games.dmm.com";
        private const string AdultMissionWeb = "https://mission.games.dmm.co.jp";

        public static MissionPageViewModel Instance { get; } = new();

        [ObservableProperty] private AvaloniaList<MissionDataV2ViewModel> _mission = [];
        [ObservableProperty]
        private MissionApiContext? _context;

        [ObservableProperty] private string _medalCount = "0";

        [ObservableProperty] private bool _mobileMode;
        [ObservableProperty] private bool _fanza = DmmGamePlayerApiHelper.IsAdultMode;

        private CancellationTokenSource? _cancellationTokenSource;

        partial void OnFanzaChanged(bool value) =>
            RefreshTasksCommand.ExecuteAsync(null);

        partial void OnMobileModeChanged(bool value) => RefreshTasksCommand.ExecuteAsync(null);

        public MissionPageViewModel()
        {
            RefreshTasksCommand.ExecuteAsync(null);
            DmmGamePlayerApiHelper.AdultModeChangedEvent += (_, _) =>
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    Fanza = DmmGamePlayerApiHelper.IsAdultMode;
                });
            };
        }

        [RelayCommand]
        private async Task RefreshTasks() => await ReadMissionData();

        [RelayCommand]
        private async Task ReceiveAllRewards()
        {
            var allClearedMission = new List<int>();
            allClearedMission.AddRange(Mission.SelectMany(x => x.MissionDataV2Type2).Where(x => x.Clear).Select(x => x.Id));
            allClearedMission.AddRange(Mission.SelectMany(x => x.MissionDataV2Type3).Where(x => x.Clear).Select(x => x.Id));
            allClearedMission.AddRange(Mission.SelectMany(x => x.MissionDataV2Type4).Where(x => x.Clear).Select(x => x.Id));
            if (allClearedMission.Count == 0)
            {
                NotificationHelper.Warn(Localization.NoClearedMissions);
                return;
            }
            Context!.MissionIds.AddRange(allClearedMission);
            var result =
                await HttpHelper.PostJsonAsync(Context.Url, Context, MissionApiJsonContext.Default.MissionApiContext);
            if (result.Failed) return;
            await Dispatcher.UIThread.InvokeAsync(async () => await RefreshTasksCommand.ExecuteAsync(null));
        }

        [RelayCommand]
        private void ExchangeMedal() => ProcessUtils.OpenExternalLink("https://mission.games.dmm.com/exchange/");

        private MissionDataV2ViewModel? ParsePcSection(IElement element)
        {
            if (string.IsNullOrEmpty(element.ClassName) || !element.ClassName.Contains("p-sectMission"))
                return null;

            if (element.Children is not [IHtmlHeadingElement h3, ..]) return null;
            var missionGroupName = h3.InnerHtml;
            if (element.Children is not [_, IHtmlUnorderedListElement listMission, ..]) return null;
            if (!string.IsNullOrEmpty(listMission.ClassName) && !listMission.ClassName.Contains("c-listMission"))
                return null;

            var missionData = new MissionDataV2ViewModel
            {
                Caption = missionGroupName
            };
            var listMissions = listMission.Children.Where(x => !string.IsNullOrEmpty(x.ClassName) && x.ClassName.Contains("listMission_item")).Cast<IHtmlElement>();
            var parsedListMissions = listMissions.AsParallel().Select(MissionParser.ParseListMission).Where(x => x is not null).Cast<MissionDataV2>().ToList();

            missionData.MissionDataV2Type2 = parsedListMissions.Where(x => x.Type == 2).Cast<MissionDataV2Type2>().ToList();
            missionData.MissionDataV2Type2.Sort((a, b) => a.Id - b.Id);
            missionData.MissionDataV2Type3 = parsedListMissions.Where(x => x.Type == 3).Cast<MissionDataV2Type3>().ToList();
            missionData.MissionDataV2Type3.Sort((a, b) => a.Id - b.Id);
            missionData.MissionDataV2Type4 = parsedListMissions.Where(x => x.Type == 4).Cast<MissionDataV2Type4>().ToList();
            missionData.MissionDataV2Type4.Sort((a, b) => a.Id - b.Id);

            if (element.Children is [_, _, IHtmlParagraphElement missionNote] &&
                !string.IsNullOrEmpty(missionNote.ClassName) && missionNote.ClassName.Contains("p-missionNote"))
                missionData.Note = missionNote.InnerHtml;

            return missionData;
        }

        private MissionDataV2ViewModel? ParseMobileSection(IElement element)
        {
            if (string.IsNullOrEmpty(element.ClassName) || !element.ClassName.Contains("mission-box"))
                return null;

            if (element.Children is not [IHtmlHeadingElement h3, ..]) return null;
            var missionGroupName = h3.InnerHtml;
            if (element.Children is not [_, IHtmlUnorderedListElement missionList, ..]) return null;
            if (!string.IsNullOrEmpty(missionList.ClassName) && !missionList.ClassName.Contains("mission-list"))
                return null;

            var missionData = new MissionDataV2ViewModel
            {
                Caption = missionGroupName
            };
            var listMissions = missionList.Children.Where(x => !string.IsNullOrEmpty(x.ClassName) && x.ClassName.Contains("mission-item")).Cast<IHtmlElement>();
            var parsedListMissions = listMissions.AsParallel().Select(MissionParser.ParseListMissionMobile).Where(x => x is not null).Cast<MissionDataV2>().ToList();

            missionData.MissionDataV2Type2 =
                parsedListMissions.Where(x => x.Type == 2).Cast<MissionDataV2Type2>().ToList();
            missionData.MissionDataV2Type2.Sort((a, b) => a.Id - b.Id);

            return missionData;
        }

        private void TakePcMissions(IDocument page)
        {
            var configData = page.QuerySelector("div.receiveAll_btnWrapper");
            if (configData?.Children is not
                [
                    _, IHtmlInputElement userId, IHtmlInputElement timestamp, IHtmlInputElement authenticationCode,
                    IHtmlInputElement rewardUrl, IHtmlInputElement device, ..
                ])
                return;
            Context = new MissionApiContext
            {
                AuthenticationCode = authenticationCode.DefaultValue,
                Device = device.DefaultValue,
                Timestamp = long.Parse(timestamp.DefaultValue),
                Url = rewardUrl.DefaultValue,
                UserId = long.Parse(userId.DefaultValue)
            };

            var receiveSection = page.GetElementsByClassName("p-sectMission");
            var missions = receiveSection.Select(ParsePcSection).OfType<MissionDataV2ViewModel>();

            Mission.AddRange(missions);
            var medalCountDom = page.QuerySelector("span.fn-naviNumMedal");
            if (medalCountDom != null)
                MedalCount = medalCountDom.InnerHtml;
        }

        private void TakeMobileMissions(IDocument page)
        {
            var configData = page.QuerySelector("div.receive-all");
            if (configData?.Children is not
                [
                    .., IHtmlInputElement userId, IHtmlInputElement timestamp, IHtmlInputElement authenticationCode,
                    IHtmlInputElement rewardUrl, IHtmlInputElement device
                ])
                return;
            Context = new MissionApiContext
            {
                AuthenticationCode = authenticationCode.DefaultValue,
                Device = device.DefaultValue,
                Timestamp = long.Parse(timestamp.DefaultValue),
                Url = rewardUrl.DefaultValue,
                UserId = long.Parse(userId.DefaultValue)
            };
            var missionBox = page.GetElementsByClassName("mission-box");
            var missions = missionBox.Select(ParseMobileSection).OfType<MissionDataV2ViewModel>();

            Mission.AddRange(missions);
            var medalCountDom = page.QuerySelector("p.m-medal-number-text-count span.fn-navi-num-medal");
            if (medalCountDom != null)
                MedalCount = medalCountDom.InnerHtml;
        }

        private async UniTask ReadMissionData()
        {
            Mission.Clear();
            GCUtils.CollectGeneration2();
            Context = null;
            var currentMobileMode = MobileMode;
            if (_cancellationTokenSource != null)
            {
                await _cancellationTokenSource.CancelAsync();
                _cancellationTokenSource.Dispose();
            }
            _cancellationTokenSource = new CancellationTokenSource();
            var missionPage =
                await HttpHelper.GetStringAsync(
                    Fanza ? AdultMissionWeb : GeneralMissionWeb, string.Empty, forceMobileUserAgent: MobileMode, cancellationToken: _cancellationTokenSource.Token);
            if (missionPage.Failed) return;
            using var page = await HttpHelper.HtmlParserContext.OpenAsync(x => x.Content(missionPage.Value));

            if (!currentMobileMode)
                TakePcMissions(page);
            else
                TakeMobileMissions(page);
        }
    }
}
