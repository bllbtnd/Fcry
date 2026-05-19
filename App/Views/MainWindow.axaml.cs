using Avalonia.Input;
using Avalonia.Controls;
using Fcry.App.ViewModels;

namespace Fcry.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        (DataContext as MainWindowViewModel)?.ResetInactivity();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        (DataContext as MainWindowViewModel)?.ResetInactivity();
    }
}
