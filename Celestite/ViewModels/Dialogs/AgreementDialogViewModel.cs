using System.Linq;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Html.Dom;
using Celestite.Network;
using Celestite.Network.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Cysharp.Text;
using Cysharp.Threading.Tasks;

namespace Celestite.ViewModels.Dialogs
{
    public partial class AgreementDialogViewModel : ViewModelBase
    {
        [ObservableProperty] private string _agreement = string.Empty;
        [ObservableProperty] private bool _isNotification;
        [ObservableProperty] private bool _isProfileApp;

        public AgreementDialogViewModel(GameAgreementResponse response)
        {
            GetAgreement(response).Forget();
        }

        private async UniTask GetAgreement(GameAgreementResponse response)
        {
            var doc = await Task.WhenAll(response.AgreementList.Select(x =>
                HttpHelper.HtmlParserContext.OpenAsync(r => r.Content(x.Agreement))));
            Agreement = ZString.Join('\n', doc.Where(x => x.Children is [IHtmlElement]).Select(x =>
            {
                var content = x.Children[0].TextContent;
                x.Dispose();
                return content;
            }));
        }
    }
}
