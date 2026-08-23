namespace SWLOR.Toolset.Services
{
    /// <summary>
    /// Whether a module-wide operation that reads or rewrites the whole module - packing, validation,
    /// Build All Scripts, or the modal ERF archive workflow - is in flight, and a notification for
    /// when that changes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The operations this guards all walk the module folder from the outside: the packer copies every
    /// resource, the compiler traverses every script, validation opens every file. Anything that writes
    /// a resource while one of them runs can be copied half-written, or copied twice in two states.
    /// </para>
    /// <para>
    /// This exists as an object rather than as another <c>Func&lt;bool&gt;</c> because a predicate only
    /// answers "is it locked now"; a control also has to be told when the answer changed, or it stays
    /// greyed after the pack finishes. Panels take the lock, read <see cref="IsLocked"/> from their
    /// can-execute, and re-raise their own can-execute from <see cref="Changed"/>.
    /// </para>
    /// <para>
    /// The shell publishes its own operations through <see cref="Set"/>. Module Contents also owns
    /// a scoped resource-deletion state through <see cref="BeginResourceDeletion"/>, because that
    /// background operation must block shared editor routes and application shutdown too.
    /// </para>
    /// </remarks>
    public sealed class ModuleMutationLock
    {
        private static readonly AsyncLocal<int> ModuleWriteAllowance = new();
        private readonly object _stateGate = new();
        private bool _shellOperationActive;
        private int _resourceDeletionCount;

        /// <summary>True while a module-wide operation is running.</summary>
        public bool IsLocked
        {
            get
            {
                lock (_stateGate)
                    return _shellOperationActive || _resourceDeletionCount > 0;
            }
        }

        /// <summary>
        /// True while Module Contents is deleting a logical resource, including any wait for its
        /// cross-process lease and the UI-side catalog cleanup after the filesystem commit.
        /// </summary>
        public bool IsResourceDeletionActive
        {
            get
            {
                lock (_stateGate)
                    return _resourceDeletionCount > 0;
            }
        }

        /// <summary>Raised when <see cref="IsLocked"/> flips, on whichever thread set it.</summary>
        public event Action? Changed;

        /// <summary>
        /// Records whether a shell-owned module-wide operation is running. Idempotent, so repeating
        /// the current state raises nothing; scoped resource deletions are counted independently.
        /// </summary>
        public void Set(bool isLocked)
        {
            bool changed;
            lock (_stateGate)
            {
                var wasLocked = _shellOperationActive || _resourceDeletionCount > 0;
                _shellOperationActive = isLocked;
                changed = wasLocked != (_shellOperationActive || _resourceDeletionCount > 0);
            }

            if (changed)
                Changed?.Invoke();
        }

        /// <summary>
        /// Publishes a resource deletion as a module-wide operation until the returned scope is
        /// disposed. This blocks every editor-opening and write route and lets the shell refuse
        /// shutdown while the background filesystem transaction is still in flight.
        /// </summary>
        public IDisposable BeginResourceDeletion()
        {
            var changed = false;
            lock (_stateGate)
            {
                var wasLocked = _shellOperationActive || _resourceDeletionCount > 0;
                _resourceDeletionCount++;
                changed = wasLocked != (_shellOperationActive || _resourceDeletionCount > 0);
            }

            if (changed)
                Changed?.Invoke();

            return new ResourceDeletionScope(this);
        }

        /// <summary>
        /// Atomically starts an exclusive resource deletion when no module-wide operation or other
        /// deletion already owns the workspace. This closes the gap between a caller checking
        /// <see cref="IsLocked"/> and publishing its own deletion state.
        /// </summary>
        public IDisposable? TryBeginResourceDeletion()
        {
            lock (_stateGate)
            {
                if (_shellOperationActive || _resourceDeletionCount > 0)
                    return null;

                _resourceDeletionCount = 1;
            }

            Changed?.Invoke();
            return new ResourceDeletionScope(this);
        }

        private void EndResourceDeletion()
        {
            var changed = false;
            lock (_stateGate)
            {
                if (_resourceDeletionCount == 0)
                    return;

                var wasLocked = _shellOperationActive || _resourceDeletionCount > 0;
                _resourceDeletionCount--;
                changed = wasLocked != (_shellOperationActive || _resourceDeletionCount > 0);
            }

            if (changed)
                Changed?.Invoke();
        }

        /// <summary>
        /// The lock every module write is checked against, or null when nothing is enforcing one.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Process-wide on purpose, and the one piece of shared state here that is. Greying a
        /// command is a courtesy; this is the guarantee. Eight editor tabs each own a Save button
        /// that goes straight to their own <c>TrySaveAsync</c>, and every one of them was a way to
        /// replace an ARE/GIT/GIC triplet while the packer was walking past it — so the module
        /// being built could contain two generations of the same area. Asking each editor to
        /// remember the check is asking the ninth one to forget it.
        /// </para>
        /// <para>
        /// Left null by tests and by any host that has no shell, where there is no packer to race.
        /// </para>
        /// </remarks>
        public static ModuleMutationLock? ModuleWrites { get; set; }

        /// <summary>
        /// Allows the module-wide operation that owns the lock to perform its prerequisite saves.
        /// </summary>
        /// <remarks>
        /// The allowance follows only the current async operation. Other UI commands and background
        /// work still see the lock, so reserving a pack before awaiting Save All closes the race
        /// without making the pack's own requested saves fail.
        /// </remarks>
        public static IDisposable AllowModuleWrites()
        {
            ModuleWriteAllowance.Value++;
            return new ModuleWriteAllowanceScope();
        }

        /// <summary>
        /// Throws when a module-wide operation is in flight. Called from the write paths in
        /// <c>SaveService</c>, so a refused save surfaces through the same "Save failed" reporting
        /// every other write failure already uses.
        /// </summary>
        public static void ThrowIfModuleLocked()
        {
            if (ModuleWrites?.IsLocked == true && ModuleWriteAllowance.Value == 0)
                throw new ModuleLockedException();
        }

        private sealed class ModuleWriteAllowanceScope : IDisposable
        {
            private bool _disposed;

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                ModuleWriteAllowance.Value = Math.Max(0, ModuleWriteAllowance.Value - 1);
            }
        }

        private sealed class ResourceDeletionScope(ModuleMutationLock owner) : IDisposable
        {
            private ModuleMutationLock? _owner = owner;

            public void Dispose()
            {
                Interlocked.Exchange(ref _owner, null)?.EndResourceDeletion();
            }
        }
    }

    /// <summary>Raised when a write is attempted while a module-wide operation owns the module.</summary>
    public sealed class ModuleLockedException()
        : InvalidOperationException(
            "A module-wide operation is running. Try again when it finishes.");
}
