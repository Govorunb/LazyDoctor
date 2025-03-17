using System.Collections.Concurrent;

namespace DesktopApp.Utilities.Helpers;

/// <summary>
/// A helper to prevent duplicate concurrent requests for the same resource.
/// </summary>
/// <typeparam name="TKey">Resource key type, e.g. <c>string</c> for a URL.</typeparam>
/// <typeparam name="TValue">Any type that is returned from the request.</typeparam>
public static class StampedeLock<TKey, TValue>
    where TKey : notnull
{
#pragma warning disable CA1000 // by design; making the class non-generic would involve another dictionary keyed on the two types
    private static readonly ConcurrentDictionary<TKey, Task<TValue>> _openRequests = [];

    /// <summary>
    /// Ensures only one request for a specific resource is executed at a time.
    /// If multiple calls with the same key arrive concurrently, only the first one executes
    /// the <paramref name="valueFactory"/> while others wait for and share its result.
    /// </summary>
    /// <param name="key">A unique identifier for this resource.</param>
    /// <param name="valueFactory">The async operation to execute for the resource.</param>
    /// <returns>
    /// A tuple containing:
    /// <list type="number">
    ///     <item>IsMainRequest: whether this call initiated the request.</item>
    ///     <item>Result: The value returned by the task.</item>
    /// </list>
    /// </returns>
    public static async Task<(bool IsMainRequest, TValue Result)> CombineConcurrent(TKey key, Func<Task<TValue>> valueFactory)
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

    /// <summary>
    /// Forces a new request for a specific resource, replacing any existing request in progress.<br/>
    /// Future calls to <see cref="CombineConcurrent"/> will use this new request as the "main request".
    /// </summary>
    /// <remarks>
    /// Note that this does not cancel the original request (if any).<br/>
    /// Additionally, this will <b>not</b> change the main request for any existing calls to <see cref="CombineConcurrent"/> and they will continue waiting on the original main request.
    /// </remarks>
    /// <param name="key">A unique identifier for this resource.</param>
    /// <param name="task">The async operation to execute for the resource.</param>
    public static void SetMainRequest(TKey key, Task<TValue> task)
    {
        _openRequests[key] = task;
        LogHost.Default.Debug($"StampedeLock[{typeof(TKey).Name}:{typeof(TValue).Name}] - overriding {key}");
    }
}
