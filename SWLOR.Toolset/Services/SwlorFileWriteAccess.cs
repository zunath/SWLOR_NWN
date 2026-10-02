using Nwn.Authoring.Editing;

namespace SWLOR.Toolset.Services;

/// <summary>Applies SWLOR module-packing policy and cross-process resource locks to shared writes.</summary>
public sealed class SwlorFileWriteAccess : IFileWriteAccess
{
    public static SwlorFileWriteAccess WriteAccess { get; } = new();
    public static AtomicFileGroupWriter Writer { get; } = new(WriteAccess);

    public void EnsureAllowed() => ModuleMutationLock.ThrowIfModuleLocked();

    public IDisposable Acquire(string path, TimeSpan? timeout = null) =>
        ModuleWriteLock.AcquireForResourcePath(path, timeout);
}
