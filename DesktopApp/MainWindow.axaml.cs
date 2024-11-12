using System.ComponentModel;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.ReactiveUI;

namespace DesktopApp;

public sealed partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public event EventHandler<HandledEventArgs>? ClipboardUpdated;
    public MainWindow()
    {
        InitializeComponent();
        var listeningToClipboard = AddClipboardFormatListener(TryGetPlatformHandle()!.Handle);
        if (listeningToClipboard)
            Win32Properties.AddWndProcHookCallback(this, WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wparam, IntPtr lparam, ref bool handled)
    {
        const uint WM_CLIPBOARDUPDATE = 0x031D;
        if (msg != WM_CLIPBOARDUPDATE)
            return 0;

        if (ClipboardUpdated is null)
            return 0;

        var args = new HandledEventArgs(false);
        ClipboardUpdated.Invoke(this, args);
        handled = args.Handled;

        return 0;
    }

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AddClipboardFormatListener(IntPtr hwnd);
}
