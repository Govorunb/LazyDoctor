using Avalonia;
using Avalonia.ReactiveUI;
using DesktopApp.Data;
using DesktopApp.Settings;
using FluentAvalonia.UI.Controls;

namespace DesktopApp;

public sealed partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
        Title = $"{AssemblyInfo.Product} v{App.Version}";
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

            ViewModel!.ShowPrefsLoadIssueDialog.RegisterHandler(async err =>
            {
                var dialog = new PrefsLoadIssueDialog { ViewModel = err };
                var openPrefsFileCommand = dialog.Dialog.Commands[0];
                openPrefsFileCommand.Command = AvaloniaHelpers.OpenFileCommand;
                openPrefsFileCommand.CommandParameter = AppData.GetFullPath(Constants.PrefsAppDataPath);
                var (res, readOnly) = await dialog.ShowAsync(this, true);

                return new(res, readOnly);
            });
            ViewModel.OnPrefsLoadIssueDialogHandlerRegistered();
        }
    }
}
