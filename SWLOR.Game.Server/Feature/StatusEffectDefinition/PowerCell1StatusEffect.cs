using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PowerCell1StatusEffect : StatusEffectBase
    {
        public override string Name => "Power Cell I";
        public override EffectIconType Icon => EffectIconType.AttackIncrease;
        public override bool PersistsOnLogout => false;
        public override List<Type> MorePowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(PowerCell2StatusEffect),
            typeof(PowerCell3StatusEffect),
        };

        public PowerCell1StatusEffect()
        {
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = 4;
        }
    }
}
