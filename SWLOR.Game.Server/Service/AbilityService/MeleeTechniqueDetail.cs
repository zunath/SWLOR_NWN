using System;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Service.AbilityService
{
    /// <summary>Declares an attack's sequence behavior independently of purchased perks.</summary>
    public sealed class MeleeTechniqueDetail
    {
        public MeleeTechniqueLine Line { get; init; }
        public MeleeTechniqueStage CreatedStage { get; init; }
        public MeleeTechniqueStage ConsumedStages { get; init; }
        public bool CreatesStageOnlyWhenSequenced { get; init; }
        public int SetupDurationSeconds { get; init; }
        public int OrdinaryAccuracyBonus { get; init; }
        public int OpeningPower { get; init; }
        public int FinishPower { get; init; }
        public int DistractionPower { get; init; }
        public int ImpactCount { get; init; } = 1;
        public int WeaponDamagePercent { get; init; } = 100;
        public bool RefundEligible { get; init; }
        public bool RefundRequiresSetup { get; init; } = true;
        public bool UsesAbilityPower { get; init; }
        public bool ConsumesRetaliation { get; init; }
        public bool PreservesMatchingSetup { get; init; }
        public bool ClearSetup { get; init; }
        public Func<IStatusEffect> SourceOwnedSetupStatusEffectFactory { get; init; }

        public int GetStagePower(MeleeTechniqueStage stage) => stage switch
        {
            MeleeTechniqueStage.Opening => OpeningPower,
            MeleeTechniqueStage.Finish => FinishPower,
            MeleeTechniqueStage.Distraction => DistractionPower,
            _ => 0
        };
    }
}
