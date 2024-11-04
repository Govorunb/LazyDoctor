using System.Reactive.Disposables;

namespace DesktopApp.Common;

public class ReactiveObjectBase : ReactiveObject, ICancelable
{
    private CompositeDisposable? _disposables;
    protected CompositeDisposable Disposables => _disposables ??= [];
    [Reactive]
    public bool IsDisposed { get; private set; } // CompositeDisposable has IsDisposed but it does not notify

    protected virtual void DisposeCore()
    {
        _disposables?.Dispose();
        _disposables = null;
    }

    protected internal IDisposable DisposeWithMe(IDisposable disposable)
    {
        Disposables.Add(disposable);
        WeakReference<ReactiveObjectBase> meRef = new(this);
        WeakReference<IDisposable> dispRef = new(disposable);
        return Disposable.Create((meRef, dispRef), static (pair) =>
        {
            if (pair.meRef.TryGetTarget(out var me)
                && pair.dispRef.TryGetTarget(out var disp))
            {
                me._disposables?.Remove(disp);
            }
        });
    }

    public void Dispose()
    {
        if (IsDisposed)
            return;

        IsDisposed = true;
        DisposeCore();
        GC.SuppressFinalize(this);
    }
}
