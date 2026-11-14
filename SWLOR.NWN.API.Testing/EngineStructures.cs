using SWLOR.NWN.API.Engine;

namespace SWLOR.NWN.API.Testing;

/// <summary>
/// Creates engine structures (effects, locations, JSON values, ...) for tests. A real engine structure frees its native
/// handle when the garbage collector finalizes it, which crashes a process that has no engine, so the finalizer is
/// suppressed on every instance made here.
/// </summary>
public static class EngineStructures
{
    /// <summary>Creates a structure with the given handle and no finalizer, e.g. `Create&lt;Effect&gt;(7)`.</summary>
    public static T Create<T>(nint handle = 0) where T : EngineStructureBase => (T)Create(typeof(T), handle);

    internal static EngineStructureBase Create(Type type, nint handle)
    {
        var structure = (EngineStructureBase)(Activator.CreateInstance(type, handle)
                                              ?? throw new InvalidOperationException($"{type.Name} could not be created."));
        GC.SuppressFinalize(structure);
        return structure;
    }
}
