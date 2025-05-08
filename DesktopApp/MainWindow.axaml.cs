using Avalonia;
using Avalonia.ReactiveUI;
using FluentAvalonia.UI.Controls;

namespace DesktopApp;

public sealed partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
        this.WhenActivated(Activated);
        return;

        // context/intellisense sucks when inside a lambda
        void Activated(object _)
        {
            this.WhenAnyValue(t => t.ViewModel!.SelectedPage)
                .WhereNotNull()
                .Subscribe(p => Nav.SelectedItem = Nav.MenuItems.Cast<NavigationViewItem>().FirstOrDefault(nvi => nvi.DataContext == p));
            Nav.GetBindingObservable(NavigationView.SelectedItemProperty, o => (NavigationViewItem)o)
                .Select(viewitem => viewitem.Value.DataContext)
                .Where(_ => ViewModel is { })
                .Subscribe(p => ViewModel!.SelectedPage = p as PageBase);
        }
    }
}
