using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FuryStance2StatusEffect : StatusEffectBase
    {
        public override string Name => "Fury Stance II";
        public override EffectIconType Icon => EffectIconType.FuryStance2StatusEffect;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;

        public FuryStance2StatusEffect()
        {
            StatGroup.Stats[StatType.WeaponAndForceDamageDealtPercentAdjustment] = 12;
            StatGroup.Stats[StatType.CriticalDamagePercentAdjustment] = 15;
            StatGroup.Stats[StatType.DamageTakenPercentAdjustment] = 5;
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = -5;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = -5;
        }
    }
}
