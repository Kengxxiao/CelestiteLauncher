using CommunityToolkit.Mvvm.ComponentModel;

namespace Celestite.ViewModels.Dialogs
{
    public partial class UpdateGameDialogViewModel(long fileSize) : ViewModelBase
    {
        [ObservableProperty] private long _fileSize = fileSize;
    }
}
