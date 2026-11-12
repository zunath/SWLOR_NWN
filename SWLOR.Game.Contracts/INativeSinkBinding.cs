namespace SWLOR.Game.Contracts;

/// <summary>
/// Points the process's native hooks at a game build's sinks. A build binds its sinks when it starts (the host entry when
/// it activates the generation, the native entry when it boots) and unbinds them when it is let go of, both on the engine
/// thread, so after that no hook references a type of the build. A hook with no sink bound calls the engine function
/// itself. Binding is separate from installing (<see cref="IHookControl"/>): the hooks stay installed for the process.
/// </summary>
public interface INativeSinkBinding
{
    /// <summary>Makes every hook forward to its sink in <paramref name="sinks"/>.</summary>
    void Bind(NativeSinkSet sinks);

    /// <summary>Stops each hook forwarding to its sink in <paramref name="sinks"/>, when that sink is still the bound one.</summary>
    void Unbind(NativeSinkSet sinks);
}
