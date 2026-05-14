using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceRage1StatusEffect : StatusEffectBase
    {
        public override string Name => "Force Rage I";
        public override EffectIconType Icon => EffectIconType.DamageReduction;
        public override bool PersistsOnLogout => false;
        public override List<Type> MorePowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(ForceRage2StatusEffect),
        };

        public ForceRage1StatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = 8;
            StatGroup.Stats[StatType.CriticalDamagePercentAdjustment] = 10;
            StatGroup.Stats[StatType.DamageTakenPercentAdjustment] = 5;
        }
    }
}
