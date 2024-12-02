using System.Reactive.Linq;
using Avalonia;
using Avalonia.ReactiveUI;
using FluentAvalonia.UI.Controls;

namespace DesktopApp;

public sealed partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
        this.WhenActivated(_ =>
        {
            Nav.SelectedItem = Nav.MenuItems[0];
            Nav.GetBindingObservable(NavigationView.SelectedItemProperty, o => (NavigationViewItem)o)
                .Select(viewitem => viewitem.Value.DataContext)
                .BindTo(this, t => t.ViewModel!.SelectedPage);
        });
    }
}
