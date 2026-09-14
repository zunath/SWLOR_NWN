using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DistractingFeint2StatusEffect : StatusEffectBase
    {
        public override string Name => "Distracting Feint II";
        public override EffectIconType Icon => EffectIconType.DistractingFeint2StatusEffect;

        public DistractingFeint2StatusEffect()
        {
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = -8;
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = -8;
        }
    }
}
