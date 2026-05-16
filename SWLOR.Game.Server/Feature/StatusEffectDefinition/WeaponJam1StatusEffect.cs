using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class WeaponJam1StatusEffect : StatusEffectBase
    {
        public override string Name => "Weapon Jam I";
        public override EffectIconType Icon => EffectIconType.AttackDecrease;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override ResistanceType ResistanceType => ResistanceType.Disruption;
        public override bool PersistsOnLogout => false;
        public override List<Type> MorePowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(WeaponJam2StatusEffect),
        };

        public WeaponJam1StatusEffect()
        {
            StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment] = -6;
        }
    }
}
