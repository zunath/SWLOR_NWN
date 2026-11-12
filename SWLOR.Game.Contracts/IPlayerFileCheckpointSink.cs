namespace SWLOR.Game.Contracts;

/// <summary>
/// The managed body of the hooks that persist the migration checkpoint and the craft file sequence in character files.
/// The hook is installed once per process (SWLOR.Game.NativeHooks, through <see cref="IHookControl"/>); its native entry point
/// forwards here while a game build has bound an implementation (<see cref="INativeSinkBinding"/>) and calls the engine
/// function itself while none is bound. Parameters are native pointers and numbers.
/// Every method ends with <c>original</c>, the trampoline of the function the hook wraps: the body calls it where the
/// engine's own work belongs, so the engine behaves as before.
/// </summary>
public unsafe interface IPlayerFileCheckpointSink
{
    /// <summary><c>CNWSCreature::SaveCreature</c>.</summary>
    int Save(void* creature, void* resource, void* structure, int associates, int desiredArea, int exporting, int objectIds, nint original);

    /// <summary><c>CNWSCreature::LoadCreature</c>.</summary>
    int Load(void* creature, void* resource, void* structure, int saveGame, int associate, int preserveIds, int copyObject, nint original);

    /// <summary><c>CNWSCreatureStats::ReadStatsFromGff</c>.</summary>
    uint ReadStats(void* stats, void* resource, void* structure, void* appearance, int saveGame, int playerCharacter, int copyObject, int loadObjectId, nint original);
}
