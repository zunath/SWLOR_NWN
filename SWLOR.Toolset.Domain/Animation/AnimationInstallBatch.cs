namespace SWLOR.Toolset.Domain.Animation;

/// <summary>Publishes a sequence of dependent animation plans as one recoverable batch.
/// Later plans see earlier bank edits, but any failure conditionally restores the batch's
/// original files. Concurrent writers are never overwritten during recovery.</summary>
public static class AnimationInstallBatch
{
    public const int MaximumSnapshotBytes = 512 * 1024 * 1024;
    private sealed record Snapshot(byte[]? Original, byte[] Published);

    public static IReadOnlyList<string> Apply(IEnumerable<Func<AnimationInstallPlan>> preparePlans) =>
        Apply(preparePlans, MaximumSnapshotBytes);

    internal static IReadOnlyList<string> Apply(IEnumerable<Func<AnimationInstallPlan>> preparePlans, int snapshotBudget)
    {
        if (snapshotBudget < 1 || snapshotBudget > MaximumSnapshotBytes) throw new ArgumentOutOfRangeException(nameof(snapshotBudget));
        var snapshots = new Dictionary<string, Snapshot>(AnimationInstall.PathComparer);
        var backups = new List<string>();
        long retainedBytes = 0;
        try
        {
            foreach (var prepare in preparePlans)
            {
                var plan = prepare();
                var next = new Dictionary<string, Snapshot>(AnimationInstall.PathComparer);
                var nextBytes = retainedBytes;
                foreach (var change in plan.Changes)
                {
                    if (snapshots.TryGetValue(change.Path, out var previous) && !Equal(previous.Published, change.Before))
                        throw new IOException($"'{change.Path}' changed between batch steps. The external change will be preserved.");
                    if (Equal(change.Before, change.After)) continue;
                    // A file created earlier in the batch must still be removed on rollback.
                    var original = previous == null ? change.Before : previous.Original;
                    nextBytes -= previous == null ? 0 : (previous.Original?.LongLength ?? 0) + previous.Published.LongLength;
                    nextBytes += (original?.LongLength ?? 0) + change.After.LongLength;
                    if (nextBytes > snapshotBudget)
                        throw new InvalidDataException("The animation batch exceeds its rollback snapshot budget. Install a smaller batch.");
                    next.Add(change.Path, new(original?.ToArray(), change.After.ToArray()));
                }
                try { plan.Apply(); }
                finally { backups.AddRange(plan.RetainedBackups); }
                foreach (var (path, snapshot) in next) snapshots[path] = snapshot;
                retainedBytes = nextBytes;
            }
            foreach (var (path, snapshot) in snapshots)
                if (!File.Exists(path) || !AnimationSourceFile.Matches(path, snapshot.Published))
                    throw new IOException($"'{path}' changed during the animation batch. The external change will be preserved.");
            return backups.AsReadOnly();
        }
        catch (Exception failure)
        {
            var errors = new List<Exception> { failure };
            foreach (var (path, snapshot) in snapshots.Reverse())
            {
                string? temporary = null;
                try
                {
                    if (snapshot.Original != null)
                    {
                        temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                        File.WriteAllBytes(temporary, snapshot.Original);
                    }
                    var backup = AnimationProjectFile.CommitStaged(path, temporary, snapshot.Published, () => { });
                    if (backup != null) backups.Add(backup);
                }
                catch (Exception rollback) { errors.Add(rollback); }
                finally { if (temporary != null) AnimationProjectFile.DeleteStaged(temporary); }
            }
            if (backups.Count > 0)
                errors.Add(new IOException("Retained animation recovery backups: " + string.Join(", ", backups)));
            if (errors.Count > 1) throw new AggregateException("Animation batch failed; concurrent edits or recovery errors require review.", errors);
            throw;
        }
    }

    private static bool Equal(byte[]? first, byte[]? second) => first == null ? second == null :
        second != null && first.AsSpan().SequenceEqual(second);
}
