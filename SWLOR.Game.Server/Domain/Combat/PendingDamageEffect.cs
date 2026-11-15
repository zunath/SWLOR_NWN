using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>A damage effect an ability impact queued for its end (<see cref="AbilityImpactTracker"/>).</summary>
    public sealed class PendingDamageEffect
    {
        public PendingDamageEffect(
            uint target,
            int damage,
            DamageType damageType,
            CombatDamageType combatDamageType,
            CombatRecordAttribution recorderAttribution)
        {
            Target = target;
            Damage = damage;
            DamageType = damageType;
            CombatDamageType = combatDamageType;
            RecorderAttribution = recorderAttribution;
        }

        public uint Target { get; }
        public int Damage { get; }
        public DamageType DamageType { get; }
        public CombatDamageType CombatDamageType { get; }
        public CombatRecordAttribution RecorderAttribution { get; }
    }
}
