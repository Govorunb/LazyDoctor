using System.Drawing.Imaging;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using DesktopApp.Utilities.Helpers;
using WClipboard = System.Windows.Forms.Clipboard;

namespace DesktopApp.Recruitment.Views;

public sealed partial class RecruitTabView : ReactiveUserControl<RecruitTab>
{
    public static readonly FilterType[] FilterTypes = Enum.GetValues<FilterType>();

    public RecruitTabView()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, HandlePaste, RoutingStrategies.Tunnel);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        Focus();
    }

    // ReSharper disable once AsyncVoidMethod
    private async void HandlePaste(object? sender, KeyEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this)!;
        if (!topLevel.PlatformSettings!.HotkeyConfiguration.Paste.Any(kg => kg.Matches(e))) return;
        if (ViewModel is null)
        {
            LogHost.Default.Error("Can't handle paste without viewmodel");
            return;
        }

        e.Handled = true;

        var clipboard = topLevel.Clipboard!;
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
        // this is a very special edge case that most people will probably never encounter
        // Windows Clipboard removes all other formats when re-selecting from history
        // you can see this if you:
        //  1. copy an image with transparency (it'll be in the DIB format)
        //  2. copy some other text
        //  3. press Win+V and click on the image to paste it (it only keeps CF_BITMAP)
        //  4. the image loses its transparency
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
