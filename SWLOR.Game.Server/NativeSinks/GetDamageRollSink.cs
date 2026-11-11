using SWLOR.Game.Contracts;
using NativeGetDamageRoll = SWLOR.Game.Server.Native.GetDamageRoll;

namespace SWLOR.Game.Server.NativeSinks;

/// <summary>
/// Replaces the engine's weapon damage roll. The damage rules are shared with the paired ranged attack cycle and
/// stay in <see cref="NativeGetDamageRoll"/>; the engine function is never called.
/// </summary>
public sealed unsafe class GetDamageRollSink : IGetDamageRollSink
{
    public int GetDamageRoll(void* thisPtr, void* pTarget, int bOffHand, int bCritical, int bSneakAttack, int bDeathAttack, int bForceMax)
        => NativeGetDamageRoll.ResolveWeaponDamage(thisPtr, pTarget, bOffHand, bCritical, bSneakAttack, bDeathAttack, bForceMax);
}
