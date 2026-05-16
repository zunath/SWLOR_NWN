using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PowerCell2StatusEffect : StatusEffectBase
    {
        public override string Name => "Power Cell II";
        public override EffectIconType Icon => EffectIconType.AttackIncrease;
        public override bool PersistsOnLogout => false;
        public override List<Type> MorePowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(PowerCell3StatusEffect),
        };
        public override List<Type> LessPowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(PowerCell1StatusEffect),
        };

        public PowerCell2StatusEffect()
        {
            StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment] = 6;
        }
    }
}
