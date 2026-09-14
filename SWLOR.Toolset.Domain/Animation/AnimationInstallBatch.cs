namespace SWLOR.Toolset.Domain.Animation;

/// <summary>Publishes a sequence of dependent animation plans as one recoverable batch.
/// Later plans see earlier bank edits, but any failure conditionally restores the batch's
/// original files. Concurrent writers are never overwritten during recovery.</summary>
public static class AnimationInstallBatch
{
    public const int MaximumSnapshotBytes = 512 * 1024 * 1024;
    private sealed record Snapshot(byte[]? Original, byte[] Published, bool Modified)
    {
        public long ByteCount => Published.LongLength + (ReferenceEquals(Original, Published) ? 0 : Original?.LongLength ?? 0);
    }

    public static IReadOnlyList<string> Apply(IEnumerable<Func<AnimationInstallPlan>> preparePlans) =>
        Apply(preparePlans, MaximumSnapshotBytes);

    internal static IReadOnlyList<string> Apply(IEnumerable<Func<AnimationInstallPlan>> preparePlans, int snapshotBudget)
    {
        if (snapshotBudget < 1 || snapshotBudget > MaximumSnapshotBytes) throw new ArgumentOutOfRangeException(nameof(snapshotBudget));
        var snapshots = new Dictionary<string, Snapshot>(AnimationInstall.PathComparer);
        var readOnly = new Dictionary<string, AnimationInputFingerprint>(AnimationInstall.PathComparer);
        var absent = new HashSet<string>(AnimationInstall.PathComparer);
        var backups = new List<string>();
        long retainedBytes = 0;
        try
        {
            foreach (var prepare in preparePlans)
            {
                var plan = prepare();
                AnimationInstallPlan.VerifyModelResolutions(snapshots.Keys.Concat(readOnly.Keys), absent, null);
                foreach (var dependency in readOnly) dependency.Value.Verify(dependency.Key);
                var next = new Dictionary<string, Snapshot>(AnimationInstall.PathComparer);
                var nextReadOnly = new Dictionary<string, AnimationInputFingerprint>(readOnly, AnimationInstall.PathComparer);
                var nextBytes = retainedBytes;
                foreach (var (path, fingerprint) in plan.ReadOnlyInputs)
                {
                    if (absent.Contains(path) || snapshots.TryGetValue(path, out var snapshot) && !fingerprint.Matches(snapshot.Published) ||
                        readOnly.TryGetValue(path, out var prior) && prior != fingerprint)
                        throw new IOException($"'{path}' changed between batch steps. The external change will be preserved.");
                    if (!snapshots.ContainsKey(path)) nextReadOnly[path] = fingerprint;
                }
                if (nextReadOnly.Count > AnimationInstall.MaximumModelChainDepth * 32)
                    throw new InvalidDataException("The animation batch has too many read-only dependencies.");
                foreach (var (path, bytes) in plan.Inputs)
                {
                    if (absent.Contains(path) || snapshots.TryGetValue(path, out var dependency) && !Equal(dependency.Published, bytes) ||
                        nextReadOnly.TryGetValue(path, out var fingerprint) && !fingerprint.Matches(bytes))
                        throw new IOException($"'{path}' changed between batch steps. The external change will be preserved.");
                    nextReadOnly.Remove(path);
                    if (snapshots.ContainsKey(path)) continue;
                    nextBytes += bytes.LongLength;
                    if (nextBytes > snapshotBudget)
                        throw new InvalidDataException("The animation batch exceeds its rollback snapshot budget. Install a smaller batch.");
                    var owned = bytes.ToArray();
                    next.Add(path, new(owned, owned, false));
                }
                foreach (var path in plan.AbsentInputs)
                    if (snapshots.ContainsKey(path) || nextReadOnly.ContainsKey(path))
                        throw new IOException($"'{path}' disappeared between batch steps. The external change will be preserved.");
                foreach (var change in plan.Changes)
                {
                    var previous = next.GetValueOrDefault(change.Path) ?? snapshots.GetValueOrDefault(change.Path);
                    if (previous != null && !Equal(previous.Published, change.Before) || absent.Contains(change.Path) && change.Before != null ||
                        nextReadOnly.TryGetValue(change.Path, out var fingerprint) && (change.Before == null || !fingerprint.Matches(change.Before)))
                        throw new IOException($"'{change.Path}' changed between batch steps. The external change will be preserved.");
                    nextReadOnly.Remove(change.Path);
                    // Unchanged project sources still certify the generated bank contents.
                    // Keep verifying them until the whole batch finishes, but never restore
                    // a verification-only file over an animator's concurrent edit.
                    var modified = previous?.Modified == true || !Equal(change.Before, change.After);
                    // A file created earlier in the batch must still be removed on rollback.
                    var original = previous == null ? change.Before : previous.Original;
                    nextBytes -= previous?.ByteCount ?? 0;
                    nextBytes += change.After.LongLength + (modified ? original?.LongLength ?? 0 : 0);
                    if (nextBytes > snapshotBudget)
                        throw new InvalidDataException("The animation batch exceeds its rollback snapshot budget. Install a smaller batch.");
                    var published = change.After.ToArray();
                    next[change.Path] = new(modified ? original?.ToArray() : published, published, modified);
                }
                if (absent.Concat(plan.AbsentInputs).Distinct(AnimationInstall.PathComparer).Count() > AnimationInstall.MaximumAbsentReservations)
                    throw new InvalidDataException("The animation batch has too many absent dependencies. Install a smaller batch.");
                try { plan.Apply(); }
                finally { backups.AddRange(plan.RetainedBackups); }
                foreach (var (path, snapshot) in next) snapshots[path] = snapshot;
                readOnly = nextReadOnly;
                absent.UnionWith(plan.AbsentInputs);
                foreach (var change in plan.Changes) absent.Remove(change.Path);
                retainedBytes = nextBytes;
            }
            AnimationInstallPlan.VerifyModelResolutions(snapshots.Keys.Concat(readOnly.Keys), absent, null);
            foreach (var dependency in readOnly) dependency.Value.Verify(dependency.Key);
            foreach (var (path, snapshot) in snapshots)
                if (!File.Exists(path) || !AnimationSourceFile.Matches(path, snapshot.Published))
                    throw new IOException($"'{path}' changed during the animation batch. The external change will be preserved.");
            foreach (var path in absent)
                if (File.Exists(path) || Directory.Exists(path))
                    throw new IOException($"'{path}' appeared during the animation batch. The external change will be preserved.");
            return backups.AsReadOnly();
        }
        catch (Exception failure)
        {
            var errors = new List<Exception> { failure };
            foreach (var (path, snapshot) in snapshots.Reverse())
            {
                if (!snapshot.Modified) continue;
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
