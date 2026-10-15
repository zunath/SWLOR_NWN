using SWLOR.Game.Server.Service.AbilityService;

namespace SWLOR.Game.Server.Service.CombatRecorderService
{
    /// <summary>
    /// Measures the FP and STM an ability's activation requirements actually took from the
    /// caster and attributes resource changes inside it to the ability's cost. Inactive by default.
    /// </summary>
    public readonly struct CombatRecordAbilityCostScope : IDisposable
    {
        internal readonly bool IsActive;
        internal readonly uint Activator;
        internal readonly AbilityDetail Ability;
        internal readonly int Feat;
        internal readonly int FPBefore;
        internal readonly int StaminaBefore;
        internal readonly CombatRecordAttributionScope Attribution;

        internal CombatRecordAbilityCostScope(
            uint activator, AbilityDetail ability, int feat, int fpBefore, int staminaBefore,
            CombatRecordAttributionScope attribution)
        {
            IsActive = true;
            Activator = activator;
            Ability = ability;
            Feat = feat;
            FPBefore = fpBefore;
            StaminaBefore = staminaBefore;
            Attribution = attribution;
        }

        public void Dispose()
        {
            if (IsActive)
                CombatRecorder.CompleteAbilityCost(this);
        }
    }
}
