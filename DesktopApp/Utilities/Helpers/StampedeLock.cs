using System.Collections.Concurrent;

namespace DesktopApp.Utilities.Helpers;

public static class StampedeLock<TKey, TValue>
    where TKey : notnull
{
    private static readonly ConcurrentDictionary<TKey, Task<TValue>> _openRequests = [];

#pragma warning disable CA1000 // by design; making the class non-generic would involve another dictionary keyed on the two types
    public static async Task<(bool IsMainRequest, TValue Result)> CombineConcurrent(TKey key, Func<Task<TValue>> valueFactory)
#pragma warning restore CA1000 // Do not declare static members on generic types
    {
        var isMainRequest = !_openRequests.TryGetValue(key, out var task);
        if (isMainRequest)
        {
            task = valueFactory();
            _openRequests.TryAdd(key, task);
        }
        LogHost.Default.Debug($"StampedeLock[{typeof(TKey).Name}:{typeof(TValue).Name}] - {(isMainRequest ? "main request" : "waiting")} for {key}");

        var result = await task!;
        if (isMainRequest)
            _openRequests.TryRemove(key, out _);

        return (isMainRequest, result);
    }
}
