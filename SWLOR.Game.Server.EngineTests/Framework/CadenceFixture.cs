using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.EngineTests.Framework
{
    /// <summary>
    /// Seeds Cadence through the production landed-hit adapter: one committed weapon ability whose
    /// first hit grants the requested charges, capped by the actor's Cadence maximum.
    /// </summary>
    public static class CadenceFixture
    {
        public static void Grant(uint actor, int charges)
        {
            var activation = new WeaponAbilityTraitActivation(PairedRangedTraits.BeginAbility(actor, false, false), 0)
            {
                CadenceOnHit = charges,
                IsCommitted = true,
            };
            PairedRangedTraits.RecordWeaponAbilityHit(actor, activation);
        }
    }
}
