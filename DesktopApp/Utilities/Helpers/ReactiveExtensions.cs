using System.Collections;
using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using JetBrains.Annotations;

namespace DesktopApp.Utilities.Helpers;

[PublicAPI]
public static class ReactiveExtensions
{
    /// <summary>
    /// Observes when the target is disposed (or on subscription if target is already disposed by then).
    /// </summary>
    /// <param name="target">Object to observe.</param>
    /// <returns>An <see cref="IObservable{T}"/> that produces one <see cref="Unit.Default"/> value and completes when the given object notifies of being disposed.</returns>
    public static IObservable<Unit> WhenDisposed(this ReactiveObjectBase target)
    {
        return target.WhenAnyValue(t => t.IsDisposed).WhenIs(true).Take(1);
    }

    /// <summary>
    /// Disposes the <paramref name="target"/> when the given <paramref name="disposable"/> notifies of being disposed.
    /// </summary>
    /// <remarks>
    /// If the <paramref name="disposable"/> is already disposed on subscription, this will dispose immediately as well.
    /// </remarks>
    /// <returns>An <see cref="IDisposable"/> to represent the subscription. Dispose it to cancel the hook.</returns>
    public static IDisposable DisposeWith<TTarget, TDisposable>(this TTarget target, TDisposable disposable)
        where TTarget : IDisposable
        where TDisposable : ReactiveObjectBase
    {
        return disposable.DisposeWithMe(target);
    }

    /// <returns>A <see cref="Unit"/> observable that fires on each item in the <paramref name="source"/> observable equal to the given <paramref name="item"/>.</returns>
    public static IObservable<Unit> WhenIs<T>(this IObservable<T> source, T item, IEqualityComparer<T>? comparer = null)
        where T : IEquatable<T>
    {
        comparer ??= EqualityComparer<T>.Default;
        return source.Where(i => comparer.Equals(i, item)).Select(_ => Unit.Default);
    }
    public static IObservable<Unit> WhenNot<T>(this IObservable<T> source, T item, IEqualityComparer<T>? comparer = null)
        where T : IEquatable<T>
    {
        comparer ??= EqualityComparer<T>.Default;
        return source.Where(i => !comparer.Equals(i, item)).Select(_ => Unit.Default);
    }

    internal static IObservable<T> LogDebug<T>(this IObservable<T> source, string name, Func<T, string>? formatFunc = null)
    {
        var logger = LogHost.Default;
        if (logger.Level > LogLevel.Debug)
            return source;

        formatFunc ??= x => x?.ToString() ?? "<NULL>";
        return source.Do(
            onNext: item => logger.Debug($"{name} --> {formatFunc(item)}"),
            onError: ex => logger.Debug($"{name} --x {ex}"),
            onCompleted: () => logger.Debug($"{name} --o")
        );
    }

    public static IObservable<T> OnMainThread<T>(this IObservable<T> source)
    {
        return source.ObserveOn(RxApp.MainThreadScheduler);
    }

    public static IObservable<IChangeSet<TItem>> SortBy<TItem, TComparable>(this IObservable<IChangeSet<TItem>> source, Func<TItem, TComparable> selector)
        where TItem : notnull
        where TComparable : IComparable<TComparable>
    {
        return source.Sort(Comparer<TItem>.Create((i1, i2) => selector(i1).CompareTo(selector(i2))));
    }

    public static IObservable<IChangeSet<TItem>> SortByDescending<TItem, TComparable>(this IObservable<IChangeSet<TItem>> source, Func<TItem, TComparable> selector)
        where TItem : notnull
        where TComparable : IComparable<TComparable>
    {
        return source.Sort(new ReverseComparer<TItem>((i1, i2) => selector(i1).CompareTo(selector(i2))));
    }

    public static IObservable<int> WhenCountChanged<TCollection>(this TCollection collection, bool sendInitialValue = true)
        where TCollection : ICollection, INotifyCollectionChanged
    {
        var obs = collection.ObserveCollectionChanges()
            .Select(_ => collection.Count)
            .DistinctUntilChanged();
        return sendInitialValue ? obs.Prepend(collection.Count) : obs;
    }
}