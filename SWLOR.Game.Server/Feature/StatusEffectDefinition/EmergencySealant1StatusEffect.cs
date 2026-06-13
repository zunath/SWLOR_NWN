using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class EmergencySealant1StatusEffect : StatusEffectBase
    {
        public override string Name => "Emergency Sealant";
        public override EffectIconType Icon => EffectIconType.EmergencySealant1StatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override float Frequency => 3f;
        public override bool PersistsOnLogout => false;

        protected override void Tick(uint creature)
        {
            AbilityEffectScaling.ApplyScaledHeal(Source, creature, 2);
        }
    }
}
