using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ScrapeAAS;

namespace GemLevelProtScraper;

public sealed class SignalCompletionStorage
{
    private readonly Dictionary<Type, Entry> _tcsByType = new();

    public TaskCompletionSource<T> GetOrAdd<T>()
    {
        lock (_tcsByType)
        {
            ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(_tcsByType, typeof(T), out var exists);

            if (!exists)
            {
                entry = new()
                {
                    Completion = new TaskCompletionSource<T>(),
                };
            }
            Debug.Assert(entry is not null);
            return entry!.CompletionOf<T>()!;
        }
    }

    public bool HasInitial<T>()
    {
        lock (_tcsByType)
        {
            if (_tcsByType.TryGetValue(typeof(T), out var entry))
            {
                return entry.HasInitialCompletion;
            }

            return false;
        }
    }

    public TaskCompletionSource<T>? Replace<T>(T result)
    {
        lock (_tcsByType)
        {
            ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(_tcsByType, typeof(T), out var exists);

            if (!exists)
            {
                entry = new();
            }
            Debug.Assert(entry is not null);
            entry!.HasInitialCompletion = true;

            if (entry.CompletionOf<T>() is { } existing)
            {
                _ = existing.TrySetResult(result);
            }

            TaskCompletionSource<T> replacement = new();
            entry.Completion = replacement;
            return replacement;
        }
    }

    private sealed class Entry
    {
        public object? Completion;
        public bool HasInitialCompletion;

        public TaskCompletionSource<T>? CompletionOf<T>()
        {
            return Completion is null ? null : (TaskCompletionSource<T>)Completion!;
        }
    }
}

public sealed class DataflowSignal<T>(SignalCompletionStorage storage) : IDataflowHandler<T>, IAsyncEnumerable<T>
{
    private bool _hasInitial;

    ValueTask IDataflowHandler<T>.HandleAsync(T message, CancellationToken cancellationToken)
    {
        _ = storage.Replace(message);
        return default;
    }

    public ValueTask WaitInitialAsync(CancellationToken cancellationToken = default)
    {
        if (_hasInitial)
        {
            return default;
        }

        if (storage.HasInitial<T>())
        {
            _hasInitial = true;
            return default;
        }

        return new(WaitOneAsync(cancellationToken));
    }

    public Task<T> WaitOneAsync(CancellationToken cancellationToken = default)
    {
        return storage.GetOrAdd<T>().Task.WaitAsync(cancellationToken);
    }

    public async Task<T?> WaitOneAsync(Func<T, bool> predicate, CancellationToken cancellationToken = default)
    {
        await using var en = GetAsyncEnumerator(cancellationToken);
        while (await en.MoveNextAsync().ConfigureAwait(false))
        {
            var item = en.Current;
            if (predicate(item))
            {
                return item;
            }
        }

        return default;
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return Enumerable(this, cancellationToken).GetAsyncEnumerator(cancellationToken);

        static async IAsyncEnumerable<T> Enumerable(DataflowSignal<T> signal, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var task = signal.WaitOneAsync(cancellationToken);
            while (true)
            {
                var innerTask = Interlocked.Exchange(ref task, signal.WaitOneAsync(cancellationToken));
                var item = await innerTask.ConfigureAwait(false);
                yield return item;
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
    }

}
