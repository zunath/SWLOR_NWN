using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class Intercept1StatusEffect : StatusEffectBase
    {
        public override string Name => "Intercept I";
        public override EffectIconType Icon => EffectIconType.DamageReduction;
        public override bool PersistsOnLogout => false;
        public override List<Type> MorePowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(Intercept2StatusEffect),
        };

        public Intercept1StatusEffect()
        {
            StatGroup.Stats[StatType.DamageTakenRedirectToStatusSourcePercent] = 35;
        }
    }
}
