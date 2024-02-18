using Avalonia;
using Avalonia.Controls;
using Celestite.ViewModels.Dialogs;

namespace Celestite.Dialogs;

public partial class RegisterFormDialog : UserControl
{
    public RegisterFormDialog()
    {
        InitializeComponent();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (DataContext is RegisterFormDialogViewModel vm)
            vm.RegistrationId = string.Empty;
    }
}