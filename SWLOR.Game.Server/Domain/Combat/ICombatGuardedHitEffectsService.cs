using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The effects a guarded hit grants the guard: the remembered hit, the next attack and next skill ability counters and
    /// the Guard Reversal exposed rider.
    /// </summary>
    public interface ICombatGuardedHitEffectsService
    {
        void TrackGuardedHit(uint creature);

        void ApplyGuardedHitNextAttackEffects(uint creature);

        int ConsumeGuardedHitNextSkillAbilityExposedDamageBonus(uint creature, SkillType skillType);
    }
}
