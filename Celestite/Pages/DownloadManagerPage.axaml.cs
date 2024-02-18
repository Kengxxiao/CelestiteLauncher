using Avalonia.Controls;
using Celestite.ViewModels.Pages;

namespace Celestite.Pages;

public partial class DownloadManagerPage : UserControl
{
    public DownloadManagerPage()
    {
        InitializeComponent();
        DataContext = DownloadManagerPageViewModel.Instance;
    }
}