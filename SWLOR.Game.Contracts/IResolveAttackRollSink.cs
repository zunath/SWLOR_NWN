namespace SWLOR.Game.Contracts;

/// <summary>
/// The managed body of the hook that replaces the engine's attack roll. The engine function is never called, so there is no original.
/// The game installs the hook itself today and its native entry point only forwards here; under the host the host
/// installs the hook once and each game build binds an implementation. Parameters are native pointers and numbers.
/// </summary>
public unsafe interface IResolveAttackRollSink
{
    /// <summary><c>CNWSCreature::ResolveAttackRoll</c>.</summary>
    void ResolveAttackRoll(void* thisPtr, void* pTarget);
}
