using SWLOR.Game.Contracts;

namespace SWLOR.Game.NativeHooks.Tests.Support;

/// <summary>
/// One hooked engine function as the game always hooked it: its hook file, symbol and hook order, the sink method its
/// trampoline forwards to, and a call through the trampoline with fixed arguments.
/// </summary>
/// <param name="EntryPoint">Its index in the order the hooks are requested when every file is enabled in id order.</param>
/// <param name="Hook">Its hook file.</param>
/// <param name="Symbol">The engine function's symbol.</param>
/// <param name="Order">The NWNX hook order.</param>
/// <param name="SinkMethod">The sink method the trampoline forwards to.</param>
/// <param name="SinkGetsOriginal">False for the three hooks whose sink replaces the engine function.</param>
/// <param name="Returns">False for a function that returns nothing.</param>
/// <param name="Args">The arguments <see cref="Invoke"/> passes, widened to long.</param>
/// <param name="Invoke">Calls the trampoline (the hook pointer handed to NWNX) with <see cref="Args"/>, returning its result widened to long.</param>
public sealed record HookedFunction(
    int EntryPoint,
    NativeHookId Hook,
    string Symbol,
    int Order,
    string SinkMethod,
    bool SinkGetsOriginal,
    bool Returns,
    long[] Args,
    Func<nint, long> Invoke)
{
    /// <summary>What the stand-in engine function returns (see <see cref="EngineStandIns"/>).</summary>
    public long EngineResult => Returns ? EntryPoint + 1 : 0;

    /// <summary>What the recording sink returns (see <see cref="RecordingSinks"/>).</summary>
    public long SinkResult => !Returns ? 0 : SinkMethod == "CalculateModifier" ? RecordingSinks.ModifierResult : RecordingSinks.Result;

    public override string ToString() => $"{Hook}.{SinkMethod}#{EntryPoint}";
}
