using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;

namespace Celestite.Network
{
    public class AvaImageHelper
    {
        public static async Task<Bitmap?> LoadFromWeb(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;
            if (url.StartsWith("http://"))
                url = url.Replace("http://", "https://");
            var compressedStream = await HttpHelper.GetByteArrayWithCacheFromWebAsync(url);
            return compressedStream.Failed ? null : compressedStream.Value;
        }

        public static async Task<IconSource?> LoadFromWebForIconSource(string url)
        {
            var bitmap = await LoadFromWeb(url);
            return bitmap == null ? null : Dispatcher.UIThread.Invoke(() => new ImageIconSource { Source = bitmap });
        }
    }
}
