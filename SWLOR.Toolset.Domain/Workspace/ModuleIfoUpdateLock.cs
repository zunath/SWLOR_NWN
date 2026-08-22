using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace SWLOR.Toolset.Domain.Workspace
{
    /// <summary>
    /// Serializes module.ifo read/modify/write transactions across toolset processes.
    /// </summary>
    /// <remarks>
    /// A persistent lock file lives under the operating system's temporary directory rather than
    /// inside the module, so it can never be mistaken for module content. The file itself is harmless
    /// after a process exits; the exclusive handle is the lock, and the operating system releases it
    /// if a process terminates unexpectedly.
    /// </remarks>
    public sealed class ModuleIfoUpdateLock : IDisposable
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(25);
        private readonly FileStream _stream;
        private bool _disposed;

        private ModuleIfoUpdateLock(FileStream stream)
        {
            _stream = stream;
        }

        public static ModuleIfoUpdateLock Acquire(
            string moduleRoot,
            TimeSpan? timeout = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(moduleRoot);

            var identity = Path.GetFullPath(moduleRoot)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (OperatingSystem.IsWindows())
                identity = identity.ToUpperInvariant();

            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
            var lockDirectory = Path.Combine(Path.GetTempPath(), "swlor-toolset-locks");
            Directory.CreateDirectory(lockDirectory);
            var lockPath = Path.Combine(lockDirectory, hash + ".module-ifo.lock");

            var wait = timeout ?? DefaultTimeout;
            var stopwatch = Stopwatch.StartNew();
            Exception? lastFailure = null;
            while (stopwatch.Elapsed < wait)
            {
                try
                {
                    return new ModuleIfoUpdateLock(new FileStream(
                        lockPath,
                        FileMode.OpenOrCreate,
                        FileAccess.ReadWrite,
                        FileShare.None));
                }
                catch (IOException ex)
                {
                    lastFailure = ex;
                    Thread.Sleep(RetryDelay);
                }
                catch (UnauthorizedAccessException ex)
                {
                    lastFailure = ex;
                    Thread.Sleep(RetryDelay);
                }
            }

            throw new IOException(
                $"Timed out waiting to update module.ifo for '{moduleRoot}'.",
                lastFailure);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _stream.Dispose();
        }
    }
}
