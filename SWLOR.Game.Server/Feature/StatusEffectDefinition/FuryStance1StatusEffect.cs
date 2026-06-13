using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FuryStance1StatusEffect : StatusEffectBase
    {
        public override string Name => "Fury Stance I";
        public override EffectIconType Icon => EffectIconType.FuryStance1StatusEffect;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;

        public FuryStance1StatusEffect()
        {
            StatGroup.Stats[StatType.WeaponAndForceDamageDealtPercentAdjustment] = 8;
            StatGroup.Stats[StatType.CriticalDamagePercentAdjustment] = 10;
            StatGroup.Stats[StatType.DamageTakenPercentAdjustment] = 5;
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = -5;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = -5;
        }
    }
}
