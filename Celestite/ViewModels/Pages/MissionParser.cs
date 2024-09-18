using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Html.Dom;
using Avalonia.Media.Imaging;
using Celestite.I18N;
using Celestite.Network;
using Celestite.Network.Models;
using Celestite.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using FluentAvalonia.UI.Controls;

namespace Celestite.ViewModels.Pages
{
    public class MissionDataV2(int type) : ViewModelBase
    {
        public int Type => type;
        public int Id { get; set; } = -1;
        public virtual bool Clear { get; set; }
        public virtual bool Done { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        public Symbol HeadIconSource =>
            Clear ? Symbol.QuestionCircle : (Done ? Symbol.CheckmarkCircle : Symbol.ErrorCircle);
    }

    public class MissionDataV2Type3() : MissionDataV2(3)
    {
        public string Text { get; set; } = string.Empty;
        public Dictionary<string, string> Definition { get; } = [];
        public string StatusText { get; set; } = string.Empty;
        public string ButtonText { get; set; } = string.Empty; // 任务完成的按钮
        public string LotteryNote { get; set; } = string.Empty;
    }

    public partial class MissionDataV2Type4() : MissionDataV2(4)
    {
        public string MedalText { get; set; } = string.Empty;
        public string MedalScore { get; set; } = string.Empty;
        public Dictionary<string, string> Definition { get; } = [];
        public string LotteryNote { get; set; } = string.Empty;
        [ObservableProperty] private bool _isNotStarted;
        public string ButtonText { get; set; } = string.Empty;
        public string StartPath { get; set; } = string.Empty;
        public List<MissionGameData> Games { get; set; } = [];

        [RelayCommand]
        private async Task SubmitStart()
        {
            var uri = new Uri(StartPath);
            using var result = await HttpHelper.GetAsync(ZString.Concat(
                DmmGamePlayerApiHelper.IsAdultMode
                    ? "https://mission.games.dmm.co.jp"
                    : "https://mission.games.dmm.com", uri.AbsolutePath, "/"));
            if (result.Failed) return;
            IsNotStarted = false;
        }
    }

    public partial class MissionGameData : ViewModelBase
    {
        public string Url { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public string IsPlayedText { get; set; } = string.Empty;

        public string GameType { get; set; } = "PLAYER";

        private Task<Bitmap?>? _image;

        public Task<Bitmap?> ImageData =>
            LazyInitializer.EnsureInitialized(ref _image, () => AvaImageHelper.LoadFromWeb(Image));

        [RelayCommand]
        private async Task EmuLaunch()
        {
            var uri = new Uri(Image);
            if (uri.Segments[^1] != "200.gif")
            {
                NotificationHelper.Error(Localization.GetMissionGameIdFailed);
                return;
            }

            var gameId = uri.Segments[^2].Trim('/');
            if (GameType == "PLAYER")
            {
                var targetGameType = await DmmGamePlayerApiHelper.GetGameTypeFromOldKeys(gameId, REQUEST_FLOOR.FREE, adult: MissionPageViewModel.Instance.Fanza);
                if (targetGameType.Failed) return;
                var result =
                    await DmmGamePlayerApiHelper.AddStoreProductToMyGame(gameId, targetGameType.Value.GameType, targetGameType.Value.GameOs);
                if (result.Failed) return;
                await LaunchHelper.EmulateLaunch(gameId, targetGameType.Value.GameType);
                return;
            }

            var productId = await LaunchHelper.TransformDmmAppIdToProductId(gameId, MissionPageViewModel.Instance.Fanza, GameType);
            if (productId == null)
            {
                NotificationHelper.Warn(Localization.TransformProductIdFailed);
                return;
            }

            var browserGames = await DmmGamePlayerApiHelper.MyBrowserGameList();
            if (browserGames.Failed) return;
            var myGameData = browserGames.Value.FirstOrDefault(x => x.ProductId == productId.ProductId);
            if (myGameData == null)
            {
                var addResult = await DmmBrowserGameLaunchHelper.TryAddBrowserGame(productId.ProductId, false, false, GameType == "SP");
                if (!addResult) return;
            }

            await LaunchHelper.EmulateLaunch(productId.ProductId, productId.Type, GameType);
        }
    }

    public partial class MissionDataV2Type2() : MissionDataV2(2)
    {
        public string MedalText { get; set; } = string.Empty;
        public string MedalScore { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string CompletedText { get; set; } = string.Empty;
        public List<MissionGameData> Games { get; set; } = [];

        public bool IsExpandable => Games.Count != 0;

        [RelayCommand]
        private async Task OpenExternalUrl()
        {
            if (Title.StartsWith("マイゲームを閲覧しよう"))
            {
                var tasks = new UniTask<bool>[4]
                {
                    DmmHermesHelper.CompleteMyGameAccessMission(false, false),
                    DmmHermesHelper.CompleteMyGameAccessMission(true, false),
                    DmmHermesHelper.CompleteMyGameAccessMission(false, true),
                    DmmHermesHelper.CompleteMyGameAccessMission(true, true),
                };
                var result = await UniTask.WhenAll(tasks);
                if (result == null || result.Any(x => !x))
                {
                    NotificationHelper.Warn(Localization.ClearMyGameAccessMissionFailed);
                    return;
                }
                NotificationHelper.Success(Localization.ClearMyGameAccessMissionSuccess);
                return;
            }

            if (!string.IsNullOrEmpty(Url))
                ProcessUtils.OpenExternalLink(Url);
        }
    }

    public class MissionParser
    {
        private static MissionDataV2 ParseType4(IHtmlElement listMissionItem)
        {
            var missionData = new MissionDataV2Type4();

            /*
             * <h4 class="missionFrame_title listMission_title">
                   <span>今月のおすすめゲームの中から1ゲームを10日間プレイ！</span>
               </h4>
             */
            if (listMissionItem.GetElementsByClassName("missionFrame_title") is
                [IHtmlHeadingElement { Children: [IHtmlSpanElement h4Span] }])
                missionData.Title = h4Span.InnerHtml;

            /*
             * <div class="missionFrame_status listMission_status c-button is_in-progress">
                   対象ゲームを遊ぼう！
               </div>
               <div class="missionFrame_status listMission_status c-button is_not-started">
                   抽選ミッションに参加する
               </div>
             */
            if (listMissionItem.GetElementsByClassName("missionFrame_status") is [IHtmlDivElement div] &&
                !string.IsNullOrEmpty(div.ClassName))
            {
                missionData.IsNotStarted = div.ClassName.Contains("is_not-started");
                if (missionData.IsNotStarted && div.ParentElement is IHtmlAnchorElement a)
                    missionData.StartPath = a.Href;
                missionData.ButtonText = div.InnerHtml.Trim();
            }

            /*
             * <div class="listMission_lotteryMedal missionFrame_medal">
                   <p class="listMission_lotteryMedal_text">獲得Mメダル枚数</p>
                   <p class="listMission_lotteryMedal_score"> 20,000枚</p>
               </div>
             */
            if (listMissionItem.GetElementsByClassName("listMission_lotteryMedal") is [IHtmlDivElement missionFrameMedalDiv])
            {
                if (missionFrameMedalDiv.GetElementsByClassName("listMission_lotteryMedal_text") is
                    [IHtmlParagraphElement medalText])
                    missionData.MedalText = medalText.InnerHtml;
                if (missionFrameMedalDiv.GetElementsByClassName("listMission_lotteryMedal_score") is
                    [IHtmlParagraphElement medalScore])
                    missionData.MedalScore = medalScore.InnerHtml;
            }

            /*
             * <dl class="missionFrame_definition listMission_definition">
                   <div>
                       <dt><span class="c-label">当選者数</span></dt>
                       <dd>毎月20名</dd>
                   </div>
                   <div>
                       <dt><span class="c-label">開催期間</span></dt>
                       <dd>毎月1日0:00〜月末23:59まで</dd>
                   </div>
               </dl>
             */

            if (listMissionItem.GetElementsByClassName("listMission_definition") is [IHtmlElement definition])
            {
                foreach (var row in definition.Children)
                {
                    if (row.Children is
                        [
                            IHtmlElement
                        {
                            Children: [IHtmlSpanElement cLabel]
                        },
                            IHtmlElement dd
                        ])
                        missionData.Definition.Add(cLabel.InnerHtml, dd.InnerHtml);
                }
            }

            /*
             * <p class="missionFrame_note listMission_note">
                   ※対象期間内に
                   <span class="note-part1">「抽選ミッションに参加する」ボタンのクリックを忘れると、</span>
                   <span class="note-part2"> クリア条件を達成しても抽選対象外</span>
                   となるのでご注意ください。
               </p>
             */
            if (listMissionItem.GetElementsByClassName("listMission_note") is [IHtmlParagraphElement p])
                missionData.LotteryNote = p.TextContent.Replace("\n", string.Empty).Replace(" ", string.Empty);

            /*
             * <div class="missionFrame_target listMission_targetLotteryGame">
                   <div class="listMission_targetGame">抽選ミッション参加中</div>
                   <ul class="listMission_targetList">
                       <li>
                           <div class="listMission_targetImage"><img
                                   src="https://media.games.dmm.com/freegame/app/649077/60.gif" alt=""></div>
                           <div class="listMission_targetText">
                               <a class="listMission_targetLink fn-actionLabel" data-actionlabel="monthly_mission_click"
                                   href="https://games.dmm.com/detail/meshiya-girls/">救世少女 メシアガール おかわり</a>
                               <div class="listMission_targetStatus">
                                   クリアまで<br>
                                   あと<span class="remaining-days">9日</span>ゲームプレイが必要
                               </div>
                           </div>
                       </li>
                   </ul>
               </div>
             */
            if (listMissionItem.GetElementsByClassName("listMission_targetList") is [IHtmlUnorderedListElement ul])
            {
                foreach (var e in ul.Children)
                {
                    if (e is not IHtmlListItemElement li)
                        continue;
                    var missionGame = new MissionGameData();
                    if (li.GetElementsByClassName("listMission_targetImage") is
                        [IHtmlDivElement { Children: [IHtmlImageElement image] }])
                        missionGame.Image = image.Source!.Replace("60.gif", "200.gif");
                    if (li.GetElementsByClassName("listMission_targetLink") is [IHtmlAnchorElement targetLink])
                    {
                        missionGame.Name = targetLink.InnerHtml;
                        missionGame.Url = targetLink.Href;
                    }

                    if (li.GetElementsByClassName("listMission_targetStatus") is [IHtmlDivElement targetStatus])
                        missionGame.IsPlayedText = targetStatus.TextContent.Replace("\n", string.Empty).Replace(" ", string.Empty);
                    missionGame.GameType = "PC";
                    missionData.Games.Add(missionGame);
                }
            }

            if (listMissionItem.GetElementsByClassName("listMission_targetGame") is [IHtmlDivElement div2])
                missionData.Status = div2.InnerHtml.Trim();

            return missionData;
        }

        private static MissionDataV2 ParseType2(IHtmlElement listMissionItem)
        {
            var missionData = new MissionDataV2Type2
            {
                Clear = listMissionItem.ClassName?.Contains("is-clear") ?? false,
                Done = listMissionItem.ClassName?.Contains("is-done") ?? false
            };

            /*
             * <label class="missionFrame_checkbox listMission_checkbox fn-checkboxLabel">
                   <input type="checkbox" value="4" disabled="">
                   <span></span>
               </label>
             */
            if (listMissionItem.GetElementsByClassName("missionFrame_checkbox") is [IHtmlLabelElement label] &&
                label.Children is [IHtmlInputElement checkbox, ..])
                missionData.Id = int.Parse(checkbox.DefaultValue);

            /*
             * <h4 class="missionFrame_title listMission_title">
                   <span>クレジットカードを登録しよう</span>
               </h4>
             */
            if (listMissionItem.GetElementsByClassName("missionFrame_title") is [IHtmlHeadingElement h4])
                missionData.Title = h4.TextContent.Trim();

            /*
             * <div class="listMission_medal missionFrame_medal">
                   <p class="listMission_medal_text">獲得Mメダル枚数</p>
                   <p class="listMission_medal_score"> 150枚</p>
               </div>
             */
            if (listMissionItem.GetElementsByClassName("missionFrame_medal") is [IHtmlDivElement missionFrameMedalDiv])
            {
                if (missionFrameMedalDiv.GetElementsByClassName("listMission_medal_text") is
                    [IHtmlParagraphElement medalText])
                    missionData.MedalText = medalText.InnerHtml;
                if (missionFrameMedalDiv.GetElementsByClassName("listMission_medal_score") is
                    [IHtmlParagraphElement medalScore])
                    missionData.MedalScore = medalScore.InnerHtml;
            }

            /*
             *  <div class="listMission_btn_wrap">
                   <div class="listMission_btn missionFrame_btn">
                       <div class="missionFrame_status listMission_status">ミッションをクリアしにいく</div>
                       <a class="c-btnLink " data-actionlabel=""
                           href="https://api-mission.games.dmm.com/redirect/v1?mission_id=4&amp;domain_type=1&amp;return_url=DRVESRUMTh1VDEJLWlkIGQYEW1NDFwBUD0xUCV9K&amp;device=pc"
                           target="_self" data-mid="4">
                           クレジットカード登録へ
                       </a>
                   </div>
               </div>

               <div class="listMission_btn_wrap">
                   <div class="missionFrame_status listMission_status">ミッションクリア！</div>
                   <div class="listMission_btn missionFrame_btn">
                       <button class="c-btnAction fn-getMedalSingle" type="button" disabled="">受け取り済</button>
                   </div>
               </div>
             */

            if (listMissionItem.GetElementsByClassName("missionFrame_status") is [IHtmlDivElement status])
                missionData.Status = status.InnerHtml.Trim();
            if (listMissionItem.GetElementsByClassName("listMission_gauge_text") is [IHtmlParagraphElement gaugeText])
                missionData.Status = gaugeText.TextContent.Trim();

            if (listMissionItem.GetElementsByClassName("c-btnLink") is [IHtmlAnchorElement cBtnLink])
                missionData.Url = cBtnLink.Href;
            if (listMissionItem.GetElementsByClassName("c-btnAction") is [IHtmlButtonElement cBtnAction])
                missionData.CompletedText = cBtnAction.InnerHtml;

            if (listMissionItem.GetElementsByClassName("listMission_targetGameList") is [IHtmlUnorderedListElement games])
            {
                foreach (var targetGameItem in games.Children.Where(x => x is IHtmlListItemElement).Cast<IHtmlListItemElement>())
                {
                    var gameData = new MissionGameData();
                    if (targetGameItem.GetElementsByClassName("listMission_targetGameItem_inner") is
                        [IHtmlAnchorElement gameAnchor])
                    {
                        gameData.Url = gameAnchor.Href;
                        if (gameAnchor.GetElementsByClassName("targetGameItem_image") is [IHtmlDivElement
                            {
                                Children: [IHtmlImageElement image]
                            }])
                            gameData.Image = image.Source!;
                        if (gameAnchor.GetElementsByClassName("listMission_targetGameItem_title") is
                            [IHtmlParagraphElement name])
                            gameData.Name = name.InnerHtml;
                        if (gameAnchor.GetElementsByClassName("listMission_targetGameItem_genre") is [IHtmlParagraphElement genre])
                            gameData.Genre = genre.InnerHtml;
                        if (gameAnchor.GetElementsByClassName("listMission_targetGameItem_isPlayed") is
                            [IHtmlParagraphElement
                            {
                                Children: [IHtmlSpanElement isPlayed]
                            }])
                            gameData.IsPlayedText = isPlayed.TextContent.Trim();
                        gameData.GameType = gameData.Image.Split('/') switch
                        {
                        [.., "client", _, _] => "PLAYER",
                        [.., "app", _, _] => "PC",
                            _ => gameData.GameType
                        };
                    }
                    missionData.Games.Add(gameData);
                }
            }

            return missionData;
        }
        private static MissionDataV2 ParseType3(IHtmlElement listMissionItem)
        {
            var missionData = new MissionDataV2Type3();

            // <p class="missionLottery_catch">毎週抽選で<b>100</b>名様に<span><b>500</b>円分</span>のMメダルプレゼント！</p>
            if (listMissionItem.GetElementsByClassName("missionLottery_catch") is [IHtmlParagraphElement catchText])
                missionData.Title = catchText.TextContent.Trim();

            // <p class="missionLottery_text">Mメダルを1週間で5回受け取ると5,000Mメダルが当たるチャンス！</p>
            if (listMissionItem.GetElementsByClassName("missionLottery_text") is [IHtmlParagraphElement lotteryText])
                missionData.Text = lotteryText.InnerHtml;

            /*
             * <dl class="missionLottery_definition">
                   <div class="missionLottery_definition_row">
                       <dt><span class="c-label">開催期間</span></dt>
                       <dd>毎週月曜 0:00〜日曜 23:59まで</dd>
                   </div>
                   <div class="missionLottery_definition_row">
                       <dt><span class="c-label">獲得Mメダル数</span></dt>
                       <dd>5,000枚</dd>
                   </div>
                   <div class="missionLottery_definition_row">
                       <dt><span class="c-label">当選者数</span></dt>
                       <dd>毎週100名</dd>
                   </div>
               </dl>
             */

            if (listMissionItem.GetElementsByClassName("missionLottery_definition") is [IHtmlElement definition])
            {
                foreach (var row in definition.GetElementsByClassName("missionLottery_definition_row"))
                {
                    if (row.Children is
                        [
                            IHtmlElement
                        {
                            Children: [IHtmlSpanElement cLabel]
                        },
                            IHtmlElement dd
                        ])
                        missionData.Definition.Add(cLabel.InnerHtml, dd.InnerHtml);
                }
            }

            // <p class="missionLottery_note">※当選者への通知は、Mメダル通帳への記載をもってかえさせていただきます。</p>
            if (listMissionItem.GetElementsByClassName("missionLottery_note") is [IHtmlParagraphElement note])
                missionData.LotteryNote = note.InnerHtml;

            /*
             * <div class="missionLottery_statusWrapper in_progress ">
                   <p class="missionLottery_statusText">〜まずは1週間で5つクリアを目指そう！〜</p>
                   <p class="missionLottery_status in_progress fn-lotteryStatus" data-status="1">クリアまであと1回メダルGETが必要</p>
                   <div class="listLotteryMission_gauge missionFrame_gauge">
                       <div class="listLotteryMission_gauge_flame">
                           <div class="listLotteryMission_gauge_inner" style="width:80%">&nbsp;</div>
                       </div>
                   </div>
               </div>
             */
            /*
             * <div class="missionLottery_statusWrapper is_completed ">
                   <p class="missionLottery_statusText">〜まずは1週間で5つクリアを目指そう！〜</p>
                   <p class="missionLottery_status is_completed fn-lotteryStatus" data-status="0">ミッションクリア！</p>
                   <div class="c-button is_completed">
                       抽選に参加しました
                   </div>
               </div>
             */

            if (listMissionItem.GetElementsByClassName("missionLottery_statusText") is [IHtmlParagraphElement statusText])
                missionData.StatusText = statusText.InnerHtml;
            if (listMissionItem.GetElementsByClassName("missionLottery_status") is [IHtmlParagraphElement status])
            {
                missionData.Status = status.InnerHtml;
                if (!string.IsNullOrEmpty(status.ClassName))
                {
                    var completed = status.ClassName.Contains("is_completed");
                    missionData.Done = completed; // TODO: 能否检查status attribute？
                    if (completed && status.GetElementsByClassName("c-button") is [IHtmlDivElement cButton])
                        missionData.ButtonText = cButton.InnerHtml;
                }
            }

            return missionData;
        }

        public static MissionDataV2? ParseListMission(IHtmlElement listMissionItem)
        {
            // type3
            if (listMissionItem is IHtmlDivElement && listMissionItem.Children is [IHtmlParagraphElement, ..])
                return ParseType3(listMissionItem);
            if (listMissionItem.GetElementsByClassName("missionFrame_content") is [IHtmlDivElement])
                return ParseType2(listMissionItem);
            if (listMissionItem.GetElementsByClassName("missionFrame_header") is [IHtmlDivElement])
                return ParseType4(listMissionItem);
            return null;
        }

        public static MissionDataV2? ParseListMissionMobile(IHtmlElement missionItem)
        {
            // 手机都是type2

            var missionData = new MissionDataV2Type2
            {
                Clear = missionItem.ClassName?.Contains("is-clear") ?? false,
                Done = missionItem.ClassName?.Contains("is-done") ?? false
            };

            if (missionItem.GetElementsByClassName("fn-clear-input") is [IHtmlInputElement input])
                missionData.Id = int.Parse(input.DefaultValue);

            // <p class="mission-item-status-title">ゲームを３つプレイ DMM GAMES SP版</p>
            if (missionItem.GetElementsByClassName("mission-item-status-title") is [IHtmlParagraphElement title])
                missionData.Title = title.InnerHtml;

            // <p class="mission-item-m-medal-count">＋20</p>
            if (missionItem.GetElementsByClassName("mission-item-m-medal-count") is [IHtmlParagraphElement medal])
                missionData.MedalScore = medal.InnerHtml;

            // <div class="mission-item-status-gauge-text">0/3</div>
            if (missionItem.GetElementsByClassName("mission-item-status-gauge-text") is [IHtmlDivElement gaugeText])
                missionData.Status = gaugeText.InnerHtml;
            else if (missionItem.GetElementsByClassName("mission-item-status-discription") is [IHtmlParagraphElement desc])
                missionData.Status = desc.InnerHtml;

            if (missionItem.GetElementsByClassName("target-list") is [IHtmlUnorderedListElement ul])
            {
                /*
                 *<li class="target-item">
                       <a href="https://rcv.ixd.dmm.com/api/surl?urid=dAS4PEaq&amp;tuid=9d47facda85727262aeb077ebfd580661a092b6d25497940ac68835719a1c622"
                           class="target-item-inner">
                           <div class="target-item-image">
                               <img width="90" height="90" src="https://media.games.dmm.com/freegame/app/676200/200.gif"
                                   alt="神になるッ！-下剋上の修行伝">
                           </div>
                           <div class="target-item-text">
                               <div>
                                   <p class="target-item-text-title">神になるッ！-下剋上の修行伝</p>
                                   <p class="target-item-text-genre">RPG</p>
                               </div>
                           </div>
                       </a>
                   </li>
                 */
                foreach (var li in ul.Children)
                {
                    var game = new MissionGameData();
                    if (li.GetElementsByClassName("target-item-image") is
                        [IHtmlDivElement { Children: [IHtmlImageElement image] }])
                        game.Image = image.Source!;
                    if (li.GetElementsByClassName("target-item-text-title") is
                        [IHtmlParagraphElement gameTitle])
                        game.Name = gameTitle.InnerHtml;
                    if (li.GetElementsByClassName("target-item-text-genre") is
                        [IHtmlParagraphElement genre])
                        game.Genre = genre.InnerHtml;
                    if (li.GetElementsByClassName("target-item-text-played") is [IHtmlParagraphElement targetStatus])
                        game.IsPlayedText = targetStatus.InnerHtml;
                    game.GameType = "SP";
                    missionData.Games.Add(game);
                }
            }

            if (missionItem.GetElementsByClassName("mission-item-button") is
                [IHtmlDivElement { Children: [IHtmlAnchorElement a] }])
                missionData.Url = a.Href;

            return missionData;
        }
    }
}
