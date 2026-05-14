using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceRage2StatusEffect : StatusEffectBase
    {
        public override string Name => "Force Rage II";
        public override EffectIconType Icon => EffectIconType.DamageReduction;
        public override bool PersistsOnLogout => false;
        public override List<Type> LessPowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(ForceRage1StatusEffect),
        };

        public ForceRage2StatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = 14;
            StatGroup.Stats[StatType.CriticalDamagePercentAdjustment] = 15;
            StatGroup.Stats[StatType.DamageTakenPercentAdjustment] = 8;
        }
    }
}
