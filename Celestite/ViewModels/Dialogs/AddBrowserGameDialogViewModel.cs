using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Celestite.Network.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Celestite.ViewModels.Dialogs
{
    public partial class AddBrowserGameDialogViewModel : ViewModelBase
    {
        [ObservableProperty] private string _gameId = string.Empty;
        [ObservableProperty] private bool _isNotificationAllowed;
        [ObservableProperty] private bool _isDisplayAllowed;


        [ObservableProperty] private List<SearchGameData> _games = [];

        public static AutoCompleteFilterPredicate<object> AutoCompleteFilter =>
            (search, item) =>
            {
                if (string.IsNullOrEmpty(search) || item is not SearchGameData x) return false;
                return x.Title.Contains(search, StringComparison.OrdinalIgnoreCase) || x.TransformGameId.StartsWith(search, StringComparison.OrdinalIgnoreCase);
            };
    }
}
