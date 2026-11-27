using System.Runtime.InteropServices;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;

namespace SWLOR.Game.NativeHooks;

/// <summary>
/// The hooks of the game's <c>Native/StaleDriveAfterJump</c>: <c>CNWSCreature::AIActionJumpToPoint</c> and
/// <c>AddDriveAction</c>, installed in that order; the jump hook marks the file installed.
/// </summary>
internal static unsafe class StaleDriveAfterJumpTrampolines
{
    internal const string JumpSymbol = "_ZN12CNWSCreature19AIActionJumpToPointEP20CNWSObjectActionNode";
    internal const string DriveSymbol = "_ZN12CNWSCreature14AddDriveActionEtRK6Vectoriiii";

    private static bool _resolved;
    private static nint _jumpFunction;
    private static nint _driveFunction;
    private static FunctionHook* _jumpHook;
    private static FunctionHook* _driveHook;
    private static volatile IStaleDriveAfterJumpSink? _sink;

    internal static bool IsEnabled => _jumpHook != null;

    internal static void Resolve(INativeFunctions native)
    {
        if (_resolved)
            return;

        _jumpFunction = native.GetExport(JumpSymbol);
        _driveFunction = native.GetExport(DriveSymbol);
        _resolved = true;
    }

    internal static void Enable(INativeFunctions native)
    {
        if (_jumpHook != null)
            return;

        Resolve(native);
        delegate* unmanaged<void*, void*, uint> jump = &OnAIActionJumpToPoint;
        delegate* unmanaged<void*, ushort, void*, int, int, int, int, int> drive = &OnAddDriveAction;
        _jumpHook = native.RequestHook(_jumpFunction, (nint)jump, HookOrder.Late);
        _driveHook = native.RequestHook(_driveFunction, (nint)drive, HookOrder.Late);
    }

    internal static IStaleDriveAfterJumpSink? Sink => _sink;

    internal static void Bind(IStaleDriveAfterJumpSink sink) => _sink = sink;

    internal static void Unbind(IStaleDriveAfterJumpSink sink)
    {
        if (ReferenceEquals(_sink, sink))
            _sink = null;
    }

    [UnmanagedCallersOnly]
    private static uint OnAIActionJumpToPoint(void* creature, void* node)
    {
        var sink = _sink;
        return sink is null
            ? ((delegate* unmanaged<void*, void*, uint>)_jumpHook->m_trampoline)(creature, node)
            : sink.JumpToPoint(creature, node, (nint)_jumpHook->m_trampoline);
    }

    [UnmanagedCallersOnly]
    private static int OnAddDriveAction(void* creature, ushort groupId, void* pathStart, int bearing,
        int clientPathNumber, int driveFlags, int wayPointsToGenerate)
    {
        var sink = _sink;
        return sink is null
            ? ((delegate* unmanaged<void*, ushort, void*, int, int, int, int, int>)_driveHook->m_trampoline)(creature, groupId,
                pathStart, bearing, clientPathNumber, driveFlags, wayPointsToGenerate)
            : sink.AddDriveAction(creature, groupId, pathStart, bearing, clientPathNumber, driveFlags, wayPointsToGenerate,
                (nint)_driveHook->m_trampoline);
    }
}
