using SWLOR.Game.Contracts;
using NativeResolveAttackRoll = SWLOR.Game.Server.Native.ResolveAttackRoll;

namespace SWLOR.Game.Server.NativeSinks;

/// <summary>
/// Replaces the engine's attack roll. The roll rules are shared with the paired ranged attack cycle and stay in
/// <see cref="NativeResolveAttackRoll"/>; the engine function is never called.
/// </summary>
public sealed unsafe class ResolveAttackRollSink : IResolveAttackRollSink
{
    public void ResolveAttackRoll(void* thisPtr, void* pTarget)
        => NativeResolveAttackRoll.ResolveWeaponRoll(thisPtr, pTarget);
}
