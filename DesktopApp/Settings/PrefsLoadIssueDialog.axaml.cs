using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.ReactiveUI;
using Avalonia.VisualTree;

namespace DesktopApp.Settings;

public sealed partial class PrefsLoadIssueDialog : ReactiveUserControl<PrefsLoadIssue>, IEnableLogger
{
    public PrefsLoadIssueDialog()
    {
        InitializeComponent();
    }

    public async Task<(PrefsLoadIssueAction res, bool ReadOnly)> ShowAsync(TopLevel root, bool showHosted = false)
    {
        Dialog.XamlRoot = root;

        // need to attach to visual tree to actually "activate" the dialog
        // (otherwise it doesn't call OnApplyTemplate and ends up with uninitialized important variables)
        if (root.FindDescendantOfType<ContentPresenter>()?.Child is not Panel hostPanel)
        {
            throw new InvalidOperationException("Window is empty? No ContentPresenter (or MainWindow's child is not a Panel)");
        }
        hostPanel.Children.Add(this);

        UpdateLayout(); // need to force an update

        // the dialog relocates itself in the visual tree and autoinherits the new parent's data context, we want it to keep ours
        // resolved in xaml, honestly no idea what's uglier
        //Dialog.DataContext = DataContext;

        var resRaw = await Dialog.ShowAsync(showHosted);
        hostPanel.Children.Remove(this);

        var visualCheckbox = Dialog.GetVisualDescendants().OfType<CheckBox>().First();
        var res = resRaw switch
        {
            "Retry" => PrefsLoadIssueAction.TryAgain,
            "Yes" => PrefsLoadIssueAction.Continue,
            "No" => PrefsLoadIssueAction.RevertToDefaults,
            // closed by pressing esc - pretend the user pressed the default button
            null => ViewModel!.IsWarning ? PrefsLoadIssueAction.Continue : PrefsLoadIssueAction.RevertToDefaults,
            _ => throw new InvalidOperationException($"Unexpected result from dialog: {resRaw}"),
        };

        return (res, visualCheckbox.IsChecked ?? true);
    }
}
