namespace SWLOR.Toolset.Services
{
    /// <summary>
    /// Whether a module-wide operation that reads or rewrites the whole module - packing, validation,
    /// Build All Scripts - is in flight, and a notification for when that changes.
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
    /// The shell is the only writer. It is the thing that knows a pack started, and routing every
    /// panel through one writer is what keeps "is the module locked" from being three different
    /// answers depending on which panel is asked.
    /// </para>
    /// </remarks>
    public sealed class ModuleMutationLock
    {
        private bool _isLocked;

        /// <summary>True while a module-wide operation is running.</summary>
        public bool IsLocked => _isLocked;

        /// <summary>Raised when <see cref="IsLocked"/> flips, on whichever thread set it.</summary>
        public event Action? Changed;

        /// <summary>
        /// Records whether a module-wide operation is running. Called by the shell; idempotent, so
        /// repeating the current state raises nothing.
        /// </summary>
        public void Set(bool isLocked)
        {
            if (_isLocked == isLocked)
                return;

            _isLocked = isLocked;
            Changed?.Invoke();
        }
    }
}
