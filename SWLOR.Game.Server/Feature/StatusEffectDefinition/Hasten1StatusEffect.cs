using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class Hasten1StatusEffect : StatusEffectBase
    {
        public override string Name => "Hasten I";
        public override EffectIconType Icon => EffectIconType.AttackIncrease;
        public override bool PersistsOnLogout => false;
        public override List<Type> MorePowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(Hasten2StatusEffect),
        };

        public Hasten1StatusEffect()
        {
            StatGroup.Stats[StatType.AttackDelayReductionPercent] = 15;
        }
    }
}
