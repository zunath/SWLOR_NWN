using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CalibratedField1StatusEffect : StatusEffectBase
    {
        public override string Name => "Calibrated Field I";
        public override EffectIconType Icon => EffectIconType.CalibratedField1StatusEffect;
        public override List<Type> MorePowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(CalibratedField2StatusEffect),
        };

        public CalibratedField1StatusEffect()
        {
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = 6;
            StatGroup.Stats[StatType.DefensePercentAdjustment] = 6;
        }
    }
}
