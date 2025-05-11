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
                .Select(p => Nav.MenuItems.Cast<NavigationViewItem>()
                    .Append(Nav.SettingsItem)
                    .FirstOrDefault(page => page.DataContext == p))
                .Subscribe(nvi => Nav.SelectedItem = nvi);
            Nav.GetBindingObservable(NavigationView.SelectedItemProperty, o => (NavigationViewItem)o)
                .Select(bv => bv.Value)
                .WhereNotNull()
                .Select(viewitem => viewitem.DataContext)
                .Where(_ => ViewModel is { })
                .Subscribe(p => ViewModel!.SelectedPage = p as PageBase);
        }
    }
}
