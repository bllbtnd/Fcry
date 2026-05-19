using Avalonia.Controls;
using Avalonia.Input;

namespace Fcry.App.Views;

public partial class LockScreenView : UserControl
{
    public LockScreenView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);
        PassphraseBox?.Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Enter)
            (DataContext as ViewModels.LockScreenViewModel)?.UnlockCommand.Execute(null);
    }
}
