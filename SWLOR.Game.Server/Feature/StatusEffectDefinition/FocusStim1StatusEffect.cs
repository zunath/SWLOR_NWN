using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FocusStim1StatusEffect : StatusEffectBase
    {
        public override string Name => "Focus Stim I";
        public override EffectIconType Icon => EffectIconType.AttackIncrease;
        public override bool PersistsOnLogout => false;
        public override List<Type> MorePowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(FocusStim2StatusEffect),
        };

        public FocusStim1StatusEffect()
        {
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = 5;
        }
    }
}
