namespace SWLOR.Toolset.Domain.Animation;

/// <summary>Publishes a saved project without replacing a concurrent writer's file.</summary>
public static class AnimationProjectFile
{
    public static async Task<string?> SaveAsync(string path, byte[] data, byte[]? expected, Action ensureCanCommit)
    {
        var id = Guid.NewGuid().ToString("N");
        var temporary = path + "." + id + ".tmp";
        var backup = path + "." + id + ".bak";
        var captured = false;
        var committed = false;
        var staged = false;
        try
        {
            await File.WriteAllBytesAsync(temporary, data);
            staged = true;
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
                File.Move(temporary, path, overwrite: false);
                committed = true;
            }
            else
            {
                ensureCanCommit();
                File.Move(temporary, path, overwrite: false);
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
                    throw new IOException($"The project changed while saving. The file at '{path}' was preserved; " +
                        $"the captured version is retained at '{backup}'.", new AggregateException(failure, recovery));
                }
            }
            if (staged && failure is IOException) throw Changed(failure);
            throw;
        }
        finally
        {
            try { if (File.Exists(temporary)) File.Delete(temporary); }
            catch (IOException) { /* Keep the original failure and any recovery-path information. */ }
            catch (UnauthorizedAccessException) { /* The staged file can be removed after access is restored. */ }
        }
        if (captured)
        {
            try { File.Delete(backup); }
            catch (IOException) { return backup; }
            catch (UnauthorizedAccessException) { return backup; }
        }
        return null;
    }

    private static IOException Changed(Exception? cause = null) =>
        new("The project changed while saving or is locked. Save again to review the external change.", cause);
}
