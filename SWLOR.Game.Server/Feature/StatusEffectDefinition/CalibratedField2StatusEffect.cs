using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CalibratedField2StatusEffect : StatusEffectBase
    {
        public override string Name => "Calibrated Field II";
        public override EffectIconType Icon => EffectIconType.CalibratedField2StatusEffect;
        public override List<Type> LessPowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(CalibratedField1StatusEffect),
        };

        public CalibratedField2StatusEffect()
        {
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = 10;
            StatGroup.Stats[StatType.DefensePercentAdjustment] = 10;
        }
    }
}
