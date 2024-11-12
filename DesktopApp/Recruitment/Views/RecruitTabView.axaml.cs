using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.ReactiveUI;
using DesktopApp.Utilities.Helpers;
using WClipboard = System.Windows.Forms.Clipboard;

namespace DesktopApp.Recruitment.Views;

public sealed partial class RecruitTabView : ReactiveUserControl<RecruitTab>
{
    public static readonly FilterType[] FilterTypes = Enum.GetValues<FilterType>();

    private MainWindow? Window => TopLevel.GetTopLevel(this) as MainWindow;

    public RecruitTabView()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, HandlePasteHotkey, RoutingStrategies.Tunnel);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Debug.Assert(Window is { });
        Window!.ClipboardUpdated += OnClipboardUpdated;
        PasteTextBox.Focus();
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        Debug.Assert(Window is { });
        Window!.ClipboardUpdated -= OnClipboardUpdated;
        base.OnDetachedFromLogicalTree(e);
    }

    // ReSharper disable AsyncVoidMethod
    private async void HandlePasteHotkey(object? sender, KeyEventArgs e)
    {
        if (ViewModel is null)
            return;

        if (Window?.PlatformSettings?.HotkeyConfiguration.Paste is not { } paste)
            return;
        if (!paste.Any(kg => kg.Matches(e)))
            return;

        e.Handled = true;

        await OnPaste();
    }

    private async void OnClipboardUpdated(object? sender, HandledEventArgs e)
    {
        e.Handled = true;
        await OnPaste();
    }

    private async Task OnPaste()
    {
        Debug.Assert(ViewModel is { });
        if (Window?.Clipboard is not { } clipboard)
            return;
        var formats = await clipboard.GetFormatsAsync();

        if (formats.Contains(DataFormats.Text) && await clipboard.GetTextAsync() is { } text)
        {
            ViewModel.OnPaste(text);
            return;
        }

        if (formats.Contains("PNG") && await clipboard.GetDataAsync("PNG") is byte[] pngData)
        {
            ViewModel.OnPaste(pngData);
            return;
        }

        // Avalonia doesn't (yet?) handle image formats like CF_BITMAP ("Unknown_format_2")
        // and because of their very funny format name handling code, we literally cannot obtain data for those formats
        if (WClipboard.ContainsImage() && WClipboard.GetImage() is { } image)
        {
            using var stream = new MemoryStream();
            using (image)
            {
                image.Save(stream, ImageFormat.Png);
            }

            ViewModel.OnPaste(stream.AsSpan());
            return;
        }

        ViewModel!.PasteError = "Could not find image or text in clipboard";
    }
}
