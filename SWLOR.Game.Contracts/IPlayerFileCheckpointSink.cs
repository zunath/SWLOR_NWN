namespace SWLOR.Game.Contracts;

/// <summary>
/// The managed body of the hooks that persist the migration checkpoint and the craft file sequence in character files.
/// The game installs the hook itself today and its native entry point only forwards here; under the host the host
/// installs the hook once and each game build binds an implementation. Parameters are native pointers and numbers.
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
