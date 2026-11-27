namespace SWLOR.Game.Contracts;

/// <summary>
/// The managed body of the hooks that keep a keyboard drive path from pulling a creature back after a teleport.
/// The hook is installed once per process (SWLOR.Game.NativeHooks, through <see cref="IHookControl"/>); its native entry point
/// forwards here while a game build has bound an implementation (<see cref="INativeSinkBinding"/>) and calls the engine
/// function itself while none is bound. Parameters are native pointers and numbers.
/// Every method ends with <c>original</c>, the trampoline of the function the hook wraps: the body calls it where the
/// engine's own work belongs, so the engine behaves as before.
/// </summary>
public unsafe interface IStaleDriveAfterJumpSink
{
    /// <summary><c>CNWSCreature::AIActionJumpToPoint</c>.</summary>
    uint JumpToPoint(void* creature, void* node, nint original);

    /// <summary><c>CNWSCreature::AddDriveAction</c>.</summary>
    int AddDriveAction(void* creature, ushort groupId, void* pathStart, int bearing, int clientPathNumber, int driveFlags, int wayPointsToGenerate, nint original);
}
