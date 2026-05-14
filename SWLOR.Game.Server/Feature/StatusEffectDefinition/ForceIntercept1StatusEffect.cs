using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceIntercept1StatusEffect : StatusEffectBase
    {
        public override string Name => "Force Intercept";
        public override EffectIconType Icon => EffectIconType.DamageReduction;
        public override bool PersistsOnLogout => false;

        public ForceIntercept1StatusEffect()
        {
            StatGroup.Stats[StatType.DamageTakenPercentAdjustment] = -50;
        }

        protected override void OnDamageTaken(uint defender, uint attacker, int damage)
        {
            StatusEffect.RemoveStatusEffect(defender, GetType(), Source);
        }
    }
}
