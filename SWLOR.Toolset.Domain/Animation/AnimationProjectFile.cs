namespace SWLOR.Toolset.Domain.Animation;

/// <summary>Publishes a saved project without replacing a concurrent writer's file.</summary>
public static class AnimationProjectFile
{
    public static async Task<string?> SaveAsync(string path, byte[] data, byte[]? expected, Action ensureCanCommit)
    {
        var id = Guid.NewGuid().ToString("N");
        var temporary = path + "." + id + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(temporary, data);
            return CommitStaged(path, temporary, expected, ensureCanCommit);
        }
        finally { DeleteStaged(temporary); }
    }

    // A null staged path removes only the captured, verified version (used by install rollback).
    internal static string? CommitStaged(string path, string? temporary, byte[]? expected, Action ensureCanCommit,
        Action<string>? deleteBackup = null)
    {
        if (temporary == null && expected == null) throw new ArgumentException("Removing a file requires its expected contents.");
        var id = Guid.NewGuid().ToString("N");
        var backup = path + "." + id + ".bak";
        var captured = false;
        var committed = false;
        try
        {
            if (expected != null)
            {
                // Capture whichever version is present at the atomic rename, then validate that
                // captured file while excluding writers. Publishing uses create-only semantics:
                // a writer that creates the original path during this interval always wins.
                File.Move(path, backup, overwrite: false);
                captured = true;
                using var lease = new FileStream(backup, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                if (!AnimationSourceFile.Matches(lease, expected)) throw Changed();
                ensureCanCommit();
                if (temporary != null) File.Move(temporary, path, overwrite: false);
                committed = true;
            }
            else
            {
                ensureCanCommit();
                File.Move(temporary!, path, overwrite: false);
                committed = true;
            }
        }
        catch (Exception failure)
        {
            if (captured && !committed)
            {
                try { File.Move(backup, path, overwrite: false); }
                catch (Exception recovery) when (recovery is IOException or UnauthorizedAccessException)
                {
                    throw new IOException($"The file changed while saving. The file at '{path}' was preserved; " +
                        $"the captured version is retained at '{backup}'.", new AggregateException(failure, recovery));
                }
            }
            if (failure is IOException) throw Changed(failure);
            throw;
        }
        if (captured)
        {
            try { (deleteBackup ?? File.Delete)(backup); }
            catch (IOException) { return backup; }
            catch (UnauthorizedAccessException) { return backup; }
        }
        return null;
    }

    internal static void DeleteStaged(string temporary)
    {
        try { if (File.Exists(temporary)) File.Delete(temporary); }
        catch (IOException) { /* Keep the original failure and any recovery-path information. */ }
        catch (UnauthorizedAccessException) { /* Remove the staged file after access is restored. */ }
    }

    private static IOException Changed(Exception? cause = null) =>
        new("The project changed while saving or is locked. Save again to review the external change.", cause);
}
