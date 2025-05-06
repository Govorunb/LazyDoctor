using System.ComponentModel;
using System.Reactive.Subjects;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace DesktopApp.Utilities;

// does not get called in Rider :(
internal static class HotReloadHandler
{
    private static readonly Subject<Type[]?> _update = new();
    private static readonly Subject<Type[]?> _clear = new();

    static HotReloadHandler()
    {
        _update.LogDebug(nameof(UpdateApplication)).Subscribe(_ =>
        {
            // invalidate all bindings
            // FIXME: hot reload seems to work just fine on rider (i.e. without this), remove later
            Dispatcher.UIThread.Post(() =>
            {
                if (App.Current?.MainWindow is not { DataContext: { } vm } mainWindow)
                    return;

                mainWindow.DataContext = null;
                mainWindow.DataContext = vm;
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
