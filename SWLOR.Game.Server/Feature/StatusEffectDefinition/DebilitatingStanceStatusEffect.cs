using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DebilitatingStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Debilitating Stance";
        public override EffectIconType Icon => EffectIconType.DebilitatingStanceStatusEffect;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;

        protected override void OnDamageDealt(uint attacker, uint defender, int damage, CombatDamageType damageType)
        {
            StatusEffect.ApplyStatusEffect(attacker, defender, typeof(HamstringStatusEffect), 8f, CombatDamageType.Physical);
        }
        public DebilitatingStanceStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = -10;
        }

    }
}
