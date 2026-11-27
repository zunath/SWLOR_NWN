using System.Runtime.InteropServices;
using NWNX.NET.Native;

namespace SWLOR.Game.NativeHooks.Tests.Support;

/// <summary>
/// Stands in for the engine and NWNX: an export is a made-up address per symbol, and a hook request records the
/// symbol, the trampoline handed over and the order, and returns a <see cref="FunctionHook"/> whose
/// <c>m_trampoline</c> is the symbol's stand-in engine function (<see cref="EngineStandIns"/>).
/// </summary>
internal sealed unsafe class RecordingNativeFunctions : INativeFunctions
{
    private static readonly int TrampolineOffset = (int)Marshal.OffsetOf<FunctionHook>(nameof(FunctionHook.m_trampoline));

    private readonly Dictionary<string, int> _entryPoints;
    private readonly nint[] _standIns = EngineStandIns.Pointers();
    private readonly Dictionary<nint, string> _symbolsByAddress = new();

    public RecordingNativeFunctions()
    {
        _entryPoints = HookedFunctions.All.ToDictionary(function => function.Symbol, function => function.EntryPoint);
    }

    /// <summary>Every symbol looked up, in order.</summary>
    public List<string> Exports { get; } = [];

    /// <summary>Every hook requested, in order.</summary>
    public List<(string Symbol, nint Hook, int Order)> Requests { get; } = [];

    /// <summary>The hook pointer the trampoline of each entry point was installed with.</summary>
    public nint[] Hooks { get; } = new nint[HookedFunctions.All.Length];

    /// <summary>The <see cref="FunctionHook"/> handed back for each entry point.</summary>
    public nint[] FunctionHooks { get; } = new nint[HookedFunctions.All.Length];

    /// <summary>The stand-in engine function of an entry point.</summary>
    public nint StandIn(int entryPoint) => _standIns[entryPoint];

    public nint GetExport(string symbol)
    {
        if (!_entryPoints.TryGetValue(symbol, out var entryPoint))
            throw new EntryPointNotFoundException(symbol);

        Exports.Add(symbol);
        var address = (nint)(0x7000_0000 + entryPoint * 0x10);
        _symbolsByAddress[address] = symbol;
        return address;
    }

    public FunctionHook* RequestHook(nint function, nint hook, int order)
    {
        var symbol = _symbolsByAddress[function];
        var entryPoint = _entryPoints[symbol];
        Requests.Add((symbol, hook, order));
        Hooks[entryPoint] = hook;

        // Lives for the test process, as NWNX's hooks live for the server's.
        var functionHook = (FunctionHook*)NativeMemory.AllocZeroed((nuint)sizeof(FunctionHook));
        // m_trampoline is read-only to managed code; NWNX writes it, so this stand-in does the same.
        *(nint*)((byte*)functionHook + TrampolineOffset) = _standIns[entryPoint];
        FunctionHooks[entryPoint] = (nint)functionHook;
        return functionHook;
    }
}
