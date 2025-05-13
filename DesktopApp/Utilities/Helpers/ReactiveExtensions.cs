using System.Collections;
using System.Collections.Specialized;
using System.Reactive;
using DynamicData;
using DynamicData.Binding;

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

    public static IDisposable NotifyProperty<TTarget, TDontCare>(this TTarget target, string prop, IObservable<TDontCare> observable)
        where TTarget : ReactiveObjectBase
    {
        return observable.Subscribe(_ => target.RaisePropertyChanged(prop));
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

    public static IObservable<TResult> Switch<TSource, TResult>(this IObservable<TSource> source, Func<TSource, IObservable<TResult>> selector)
        => source
            .Select(selector)
            .Switch();

    // https://stackoverflow.com/questions/37129159/subscribing-to-observable-sequence-with-async-function
    public static IDisposable SubscribeAsync<T>(this IObservable<T> source, Func<T, Task> observer)
        => source
            .Select(item => Observable.FromAsync(() => observer(item)))
            .Concat()
            .Subscribe();

    public static IObservable<Unit> ToUnit<T>(this IObservable<T> source)
        => source.Select(_ => Unit.Default);

    /// <summary>
    /// Replays notifications like <see cref="System.Reactive.Linq.Observable.Replay{TSource}(IObservable{TSource}, int)"/>, but immediately subscribes to the underlying <paramref name="source" /> so that no notifications before the first subscription are missed.
    /// </summary>
    public static IObservable<T> ReplayCold<T>(this IObservable<T> source, int bufferSize)
        => source.Replay(bufferSize).AutoConnect(0);

    // just a rename
    /// <inheritdoc cref="System.Reactive.Linq.Observable.Throttle{T}(IObservable{T}, TimeSpan)"/>
    public static IObservable<T> Debounce<T>(this IObservable<T> source, TimeSpan dueTime)
        => source.Throttle(dueTime);

    /// <summary>
    /// Fully replace the contents of the SourceList with <paramref name="items"/>.<br/>
    /// Unlike <see cref="SourceListEditConvenienceEx.EditDiff{T}"/>, this method doesn't bother to calculate the difference.
    /// </summary>
    public static void Reset<T>(this SourceList<T> source, IEnumerable<T> items)
        where T : notnull
    {
        source.Edit(l =>
        {
            l.Clear();
            l.AddRange(items);
        });
    }

    public static IObservable<bool> And(this IObservable<bool> first, IObservable<bool> second)
        => first.CombineLatest(second).Select(p => p.First && p.Second);

    public static IDisposable RegisterHandler<TIn, TOut>(this Interaction<TIn, TOut> interaction, Func<TIn, TOut> handler)
        => interaction.RegisterHandler(ctx => ctx.SetOutput(handler(ctx.Input)));
    public static IDisposable RegisterHandler<TIn, TOut>(this Interaction<TIn, TOut> interaction, Func<TIn, Task<TOut>> handler)
        => interaction.RegisterHandler(async ctx => ctx.SetOutput(await handler(ctx.Input)));
    public static IDisposable RegisterHandler<TOut>(this Interaction<Unit, TOut> interaction, Func<TOut> handler)
        => interaction.RegisterHandler(ctx => ctx.SetOutput(handler()));
    public static IDisposable RegisterHandler<TOut>(this Interaction<Unit, TOut> interaction, Func<Task<TOut>> handler)
        => interaction.RegisterHandler(async ctx => ctx.SetOutput(await handler()));
    public static IDisposable RegisterHandler<TIn>(this Interaction<TIn, Unit> interaction, Action<TIn> handler)
        => interaction.RegisterHandler(ctx =>
        {
            handler(ctx.Input);
            ctx.SetOutput(Unit.Default);
        });
    public static IDisposable RegisterHandler<TIn>(this Interaction<TIn, Unit> interaction, Func<TIn, Task> handler)
        => interaction.RegisterHandler(async ctx =>
        {
            await handler(ctx.Input);
            ctx.SetOutput(Unit.Default);
        });
}
