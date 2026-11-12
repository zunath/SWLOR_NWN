using System.Runtime.InteropServices;
using NWNX.NET;
using NWNX.NET.Native;

namespace SWLOR.Game.NativeHooks;

/// <summary>The engine's own symbols and NWNX's hook request, as the hook files of the game always called them.</summary>
internal sealed unsafe class NwnxNativeFunctions : INativeFunctions
{
    public static NwnxNativeFunctions Instance { get; } = new();

    private NwnxNativeFunctions()
    {
    }

    public nint GetExport(string symbol) => NativeLibrary.GetExport(NativeLibrary.GetMainProgramHandle(), symbol);

    public FunctionHook* RequestHook(nint function, nint hook, int order) => NWNXAPI.RequestFunctionHook(function, hook, order);
}
