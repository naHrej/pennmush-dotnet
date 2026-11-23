namespace PennMUSH.Core.Database;

using PennMUSH.Core.Types;

/// <summary>
/// Dbrefs and side tables that have changed since the last flush.
/// The game thread is the only writer, so the sets do not lock.
/// </summary>
public sealed class ChangeTracker
{
    private readonly HashSet<int> _objects = new();
    private readonly HashSet<int> _removed = new();
    private bool _cron;
    private bool _runtime;
    private int _suspend;

    public bool Pending => _objects.Count > 0 || _removed.Count > 0 || _cron || _runtime;

    public void MarkObject(Dbref id)
    {
        if (_suspend > 0 || !id.IsValid)
            return;
        _removed.Remove(id.Value);
        _objects.Add(id.Value);
    }

    public void MarkRemoved(Dbref id)
    {
        if (_suspend > 0 || !id.IsValid)
            return;
        _objects.Remove(id.Value);
        _removed.Add(id.Value);
    }

    public void MarkCron()
    {
        if (_suspend == 0)
            _cron = true;
    }

    public void MarkRuntime()
    {
        if (_suspend == 0)
            _runtime = true;
    }

    public void Suspend() => _suspend++;

    public void Resume() => _suspend = Math.Max(0, _suspend - 1);

    public void Clear()
    {
        _objects.Clear();
        _removed.Clear();
        _cron = false;
        _runtime = false;
    }

    public DirtyBatch Take()
    {
        var batch = new DirtyBatch(
            _objects.ToArray(),
            _removed.ToArray(),
            _cron,
            _runtime);
        Clear();
        return batch;
    }

    public void Restore(DirtyBatch batch)
    {
        foreach (int id in batch.Objects)
            _objects.Add(id);
        foreach (int id in batch.Removed)
        {
            _objects.Remove(id);
            _removed.Add(id);
        }
        if (batch.Cron)
            _cron = true;
        if (batch.Runtime)
            _runtime = true;
    }
}

public readonly struct DirtyBatch
{
    public DirtyBatch(int[] objects, int[] removed, bool cron, bool runtime)
    {
        Objects = objects;
        Removed = removed;
        Cron = cron;
        Runtime = runtime;
    }

    public int[] Objects { get; }
    public int[] Removed { get; }
    public bool Cron { get; }
    public bool Runtime { get; }

    public bool Any => Objects.Length > 0 || Removed.Length > 0 || Cron || Runtime;
}
