using System.Diagnostics;
using System.Reactive;
using System.Reactive.Subjects;
using Windows.ApplicationModel.DataTransfer;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.ReactiveUI;

namespace DesktopApp.Recruitment.Views;

public sealed partial class RecruitPageView : ReactiveUserControl<RecruitPage>, IDisposable
{
    private MainWindow? Window => TopLevel.GetTopLevel(this) as MainWindow;
    private IDisposable? _pasteHandlerSubscription;
    private readonly Subject<Unit> _pasted = new();

    public RecruitPageView()
    {
        InitializeComponent();
        Focusable = true;
        ParseClipboardButton.Click += delegate { _pasted.OnNext(Unit.Default); };
        _pasted
            .Debounce(TimeSpan.FromMilliseconds(60)) // clipboard contents change twice on copy
            .OnMainThread()
            .SubscribeAsync(_ => OnPaste());
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Clipboard.ContentChanged += OnClipboardUpdated;

        _pasteHandlerSubscription = Window?.AddDisposableHandler(KeyDownEvent, HandlePasteHotkey, RoutingStrategies.Tunnel);
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        Clipboard.ContentChanged -= OnClipboardUpdated;
        _pasteHandlerSubscription?.Dispose();

        base.OnDetachedFromLogicalTree(e);
    }

    private void HandlePasteHotkey(object? sender, KeyEventArgs e)
    {
        if (ViewModel is null)
            return;

        if (Window?.PlatformSettings?.HotkeyConfiguration.Paste is not { } paste)
            return;
        if (!paste.Any(hotkey => hotkey.Matches(e)))
            return;

        e.Handled = true;

        _pasted.OnNext(Unit.Default);
    }

    private void OnClipboardUpdated(object? s, object? e) // both always null
    {
        if (ViewModel?.Prefs.Recruitment.MonitorClipboard != true)
            return;

        _pasted.OnNext(Unit.Default);
    }

    private async Task OnPaste()
    {
        Debug.Assert(ViewModel is { });
        try
        {
            var contents = Clipboard.GetContent();

            if (contents.Contains(StandardDataFormats.Bitmap) && await contents.GetBitmapAsync() is { } image)
            {
                ViewModel.OnPaste(await image.OpenReadAsync());
                return;
            }

            if (contents.Contains(StandardDataFormats.Text) && await contents.GetTextAsync() is { } text)
            {
                ViewModel.OnPaste(text);
                return;
            }

            ViewModel.PasteError = "Could not find image or text in clipboard";
        }
        catch (Exception e)
        {
            ViewModel.PasteError = $"Could not read clipboard: {e.Message} ({e.GetType().Name})";
        }
    }

    public void Dispose()
    {
        _pasted.Dispose();
        _pasteHandlerSubscription?.Dispose();
    }
}
