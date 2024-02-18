using Celestite.Utils;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Celestite.ViewModels.Dialogs
{
    public partial class DefaultLoginFormDialogViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _email = string.Empty;
        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty] private bool _saveEmail;
        [ObservableProperty] private bool _savePassword;
        [ObservableProperty] private bool _autoLogin;

        [ObservableProperty] private string[] _autoCompleteItems = ConfigUtils.GetAllSavedEmails();

        [ObservableProperty] private bool _lockSaveEmail;
        [ObservableProperty] private bool _lockSavePassword;

        partial void OnEmailChanged(string value)
        {
            if (!ConfigUtils.TryGetGuidByAccountEmail(value, out var guid) ||
                !ConfigUtils.TryGetAccountObjectByGuid(guid, out var accountObject)) return;
            if (!accountObject.SaveEmail) return;
            Email = accountObject.Email;
            if (accountObject.SavePassword)
                Password = accountObject.Password;
            SaveEmail = accountObject.SaveEmail;
            SavePassword = accountObject.SavePassword;
            AutoLogin = accountObject.AutoLogin;
        }

        public void Reset(bool lockSave = false)
        {
            LockSaveEmail = lockSave;
            LockSavePassword = lockSave;
            SaveEmail = lockSave;
            SavePassword = lockSave;
            Email = string.Empty;
            Password = string.Empty;
            AutoLogin = false;
            if (lockSave)
                AutoCompleteItems = [];
        }
    }
}
