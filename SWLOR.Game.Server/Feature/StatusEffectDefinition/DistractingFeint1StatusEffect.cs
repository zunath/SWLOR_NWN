using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DistractingFeint1StatusEffect : StatusEffectBase
    {
        public override string Name => "Distracting Feint I";
        public override EffectIconType Icon => EffectIconType.AttackDecrease;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override ResistanceType ResistanceType => ResistanceType.Trauma;
        public override bool PersistsOnLogout => false;
        public override List<Type> MorePowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(DistractingFeint2StatusEffect),
            typeof(DistractingFeint3StatusEffect),
        };

        public DistractingFeint1StatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment] = -6;
        }
    }
}
