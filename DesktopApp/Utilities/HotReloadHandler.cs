using System.ComponentModel;
using System.Reactive.Subjects;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using DesktopApp.Utilities.Helpers;

namespace DesktopApp.Utilities;

// does not get called in Rider
internal static class HotReloadHandler
{
    private static readonly Subject<Type[]?> _update = new();
    private static readonly Subject<Type[]?> _clear = new();

    static HotReloadHandler()
    {
        _update.LogDebug(nameof(UpdateApplication)).Subscribe(_ =>
        {
            // invalidate all bindings
            Dispatcher.UIThread.Post(() =>
            {
                if (Application.Current is not { ApplicationLifetime: IClassicDesktopStyleApplicationLifetime { MainWindow.DataContext: { } vm } lifetime })
                    return;

                lifetime.MainWindow.DataContext = null;
                lifetime.MainWindow.DataContext = vm;
            });
        });
        _clear.LogDebug(nameof(ClearCache)).Subscribe();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void ClearCache(Type[]? updatedTypes)
    {
        _clear.OnNext(updatedTypes);
    }
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void UpdateApplication(Type[]? updatedTypes)
    {
        _update.OnNext(updatedTypes);
    }

    public static IObservable<Type[]?> OnClear() => _clear;
    public static IObservable<Type[]?> OnUpdate() => _update;
}
