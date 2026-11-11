namespace SWLOR.Game.Contracts;

/// <summary>
/// The managed body of the hooks that keep a keyboard drive path from pulling a creature back after a teleport.
/// The game installs the hook itself today and its native entry point only forwards here; under the host the host
/// installs the hook once and each game build binds an implementation. Parameters are native pointers and numbers.
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
