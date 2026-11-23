namespace PennMUSH.Server.Engine;

using System.Collections.Concurrent;

/// <summary>
/// The one thread that mutates the world. Socket reads and writes stay on their
/// own threads and post work here. A call made from the game thread runs inline
/// so a command can reboot without waiting for itself.
/// </summary>
public sealed class GameMailbox : IDisposable
{
    private readonly BlockingCollection<Work> _queue = new();
    private readonly Thread _thread;
    private volatile bool _stop;

    public GameMailbox()
    {
        _thread = new Thread(Loop)
        {
            IsBackground = true,
            Name = "PennMUSH game"
        };
        _thread.Start();
    }

    public bool IsCurrent => ReferenceEquals(Thread.CurrentThread, _thread);

    public bool IsStopped => _stop;

    public void Run(Action action)
    {
        if (IsCurrent)
        {
            action();
            return;
        }

        RunAsync(action).GetAwaiter().GetResult();
    }

    public T Run<T>(Func<T> action)
    {
        T result = default!;
        // The body must be a statement. An expression lambda would return T and bind back to this method.
        Run(() => { result = action(); });
        return result;
    }

    public Task RunAsync(Action action)
    {
        if (IsCurrent)
        {
            try
            {
                action();
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }

        if (_stop)
            return Task.FromException(new ObjectDisposedException(nameof(GameMailbox)));

        var work = new Work(action);
        try
        {
            _queue.Add(work);
        }
        catch (InvalidOperationException)
        {
            return Task.FromException(new ObjectDisposedException(nameof(GameMailbox)));
        }

        return work.Task;
    }

    public Task<T> RunAsync<T>(Func<T> action)
    {
        var source = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var queued = RunAsync(() =>
        {
            try
            {
                source.TrySetResult(action());
            }
            catch (Exception ex)
            {
                source.TrySetException(ex);
            }
        });
        queued.ContinueWith(
            task =>
            {
                if (task.IsFaulted && task.Exception != null)
                    source.TrySetException(task.Exception.InnerExceptions);
                else if (task.IsCanceled)
                    source.TrySetCanceled();
            },
            CancellationToken.None,
            TaskContinuationOptions.NotOnRanToCompletion,
            TaskScheduler.Default);
        return source.Task;
    }

    /// <summary>
    /// Finish the job that is running, fail anything still queued, and exit the thread.
    /// </summary>
    public void StopAfterCurrent()
    {
        _stop = true;
        if (!IsCurrent)
            _queue.CompleteAdding();
    }

    public void Dispose()
    {
        StopAfterCurrent();
    }

    private void Loop()
    {
        try
        {
            foreach (var work in _queue.GetConsumingEnumerable())
            {
                if (_stop)
                {
                    work.Cancel();
                    Drain();
                    return;
                }

                work.Invoke();
                if (_stop)
                {
                    Drain();
                    return;
                }
            }
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private void Drain()
    {
        while (_queue.TryTake(out var leftover))
            leftover.Cancel();
    }

    private sealed class Work
    {
        private readonly Action _action;
        private readonly TaskCompletionSource _done = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Work(Action action) => _action = action;

        public Task Task => _done.Task;

        public void Invoke()
        {
            try
            {
                _action();
                _done.TrySetResult();
            }
            catch (Exception ex)
            {
                _done.TrySetException(ex);
            }
        }

        public void Cancel()
            => _done.TrySetException(new ObjectDisposedException(nameof(GameMailbox)));
    }
}
