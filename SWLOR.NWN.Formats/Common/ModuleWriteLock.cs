// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace SWLOR.NWN.Formats.Common;

/// <summary>
/// An exclusive, cross-process lease for mutations beneath one unpacked module root.
/// </summary>
/// <remarks>
/// The desktop toolset and SWLOR.CLI are separate processes, so an in-memory semaphore cannot
/// prevent a pack or unpack from walking the module while an editor replaces resources. The lease
/// is a deny-sharing file handle in the system temporary directory, keyed by the normalized module
/// path. A persistent empty lock file avoids repository debris; exclusivity comes from the open
/// handle, not the file's existence.
/// </remarks>
public sealed class ModuleWriteLock : IDisposable
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(25);
    private static readonly AsyncLocal<Dictionary<string, HeldLease>?> AmbientLeases = new();
    private static readonly HashSet<string> ResourceDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "are", "dlg", "fac", "gic", "git", "ifo", "itp", "jrl", "ncs", "nss",
        "utc", "utd", "ute", "uti", "utm", "utp", "uts", "utt", "utw"
    };

    private readonly string _moduleKey;
    private HeldLease? _held;

    private ModuleWriteLock(string moduleKey, HeldLease held)
    {
        _moduleKey = moduleKey;
        _held = held;
    }

    /// <summary>
    /// Acquires the module exclusively, waiting for the current pack, unpack, or writer to finish.
    /// </summary>
    public static ModuleWriteLock Acquire(string moduleRoot, TimeSpan? timeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleRoot);
        var moduleKey = NormalizeModuleRoot(moduleRoot);
        var ambient = AmbientLeases.Value;
        if (ambient != null &&
            ambient.TryGetValue(moduleKey, out var existing) &&
            existing.TryRetain())
        {
            return new ModuleWriteLock(moduleKey, existing);
        }

        var lockDirectory = Path.Combine(Path.GetTempPath(), "SWLOR.ModuleLocks");
        Directory.CreateDirectory(lockDirectory);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(moduleKey)));
        var lockPath = Path.Combine(lockDirectory, hash + ".lock");

        var wait = timeout ?? DefaultTimeout;
        var stopwatch = Stopwatch.StartNew();
        Exception? lastFailure = null;
        FileStream? stream = null;
        do
        {
            try
            {
                stream = new FileStream(
                    lockPath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    bufferSize: 1,
                    FileOptions.None);
                break;
            }
            catch (IOException ex)
            {
                lastFailure = ex;
            }
            catch (UnauthorizedAccessException ex)
            {
                lastFailure = ex;
            }

            if (stopwatch.Elapsed < wait)
                Thread.Sleep(RetryDelay);
        } while (stopwatch.Elapsed < wait);

        if (stream == null)
            throw new ModuleWriteLockException(moduleKey, lastFailure!);

        var held = new HeldLease(stream);
        // AsyncLocal values flow into child ExecutionContexts. Never mutate a dictionary inherited
        // from the caller: an async operation can yield after acquiring this lease while the caller
        // starts unrelated UI work. A private copy keeps that sibling operation from mistaking this
        // lease for one of its own nested acquisitions.
        var updated = ambient == null
            ? new Dictionary<string, HeldLease>(PathComparer)
            : new Dictionary<string, HeldLease>(ambient, PathComparer);
        updated[moduleKey] = held;
        AmbientLeases.Value = updated;
        return new ModuleWriteLock(moduleKey, held);
    }

    /// <summary>
    /// Acquires the module containing a loose resource path. Resource files live one directory
    /// below the module root; root-level transaction markers use their containing directory.
    /// </summary>
    public static ModuleWriteLock AcquireForResourcePath(
        string resourcePath,
        TimeSpan? timeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourcePath);
        var fullPath = Path.GetFullPath(resourcePath);
        var directory = Directory.Exists(fullPath)
            ? fullPath
            : Path.GetDirectoryName(fullPath)
              ?? throw new InvalidOperationException(
                  $"Could not determine the containing directory of '{resourcePath}'.");

        var directoryName = Path.GetFileName(directory.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar));
        var moduleRoot = ResourceDirectories.Contains(directoryName)
            ? Directory.GetParent(directory)?.FullName
              ?? throw new InvalidOperationException(
                  $"Could not determine the module root containing '{resourcePath}'.")
            : directory;

        return Acquire(moduleRoot, timeout);
    }

    public void Dispose()
    {
        var held = Interlocked.Exchange(ref _held, null);
        if (held == null)
            return;

        if (!held.Release())
            return;

        var ambient = AmbientLeases.Value;
        if (ambient != null &&
            ambient.TryGetValue(_moduleKey, out var current) &&
            ReferenceEquals(current, held))
        {
            var updated = new Dictionary<string, HeldLease>(ambient, PathComparer);
            updated.Remove(_moduleKey);
            AmbientLeases.Value = updated.Count == 0 ? null : updated;
        }

        held.Stream.Dispose();
    }

    private static string NormalizeModuleRoot(string moduleRoot)
    {
        var normalized = Path.GetFullPath(moduleRoot).TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar);
        return OperatingSystem.IsWindows() ? normalized.ToUpperInvariant() : normalized;
    }

    private static StringComparer PathComparer =>
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    private sealed class HeldLease(FileStream stream)
    {
        private int _depth = 1;

        public FileStream Stream { get; } = stream;

        public bool TryRetain()
        {
            while (true)
            {
                var depth = Volatile.Read(ref _depth);
                if (depth <= 0)
                    return false;

                if (Interlocked.CompareExchange(ref _depth, depth + 1, depth) == depth)
                    return true;
            }
        }

        public bool Release() => Interlocked.Decrement(ref _depth) == 0;
    }
}

/// <summary>Raised when another process is already mutating or walking the same module.</summary>
public sealed class ModuleWriteLockException(string moduleRoot, Exception innerException)
    : IOException(
        $"Timed out waiting for the pack, unpack, or module writer using '{moduleRoot}' to finish.",
        innerException);
