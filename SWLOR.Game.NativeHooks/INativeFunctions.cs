using NWNX.NET.Native;

namespace SWLOR.Game.NativeHooks;

/// <summary>
/// The two engine calls a hook makes while it is installed: finding the engine function by its symbol and asking NWNX to
/// hook it. The server uses <see cref="NwnxNativeFunctions"/>; tests hand in a stand-in that records the requests.
/// </summary>
internal unsafe interface INativeFunctions
{
    /// <summary>The address of the engine function exported as <paramref name="symbol"/>.</summary>
    nint GetExport(string symbol);

    /// <summary>Hooks <paramref name="function"/> with <paramref name="hook"/> at <paramref name="order"/> (<c>NWNXAPI.RequestFunctionHook</c>).</summary>
    FunctionHook* RequestHook(nint function, nint hook, int order);
}
