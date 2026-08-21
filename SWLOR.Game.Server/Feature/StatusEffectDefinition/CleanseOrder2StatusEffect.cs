using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CleanseOrder2StatusEffect : SocialScalingStatusEffectBase
    {
        public const string TemporaryHitPointEffectKey = "CLEANSE_ORDER";

        public override string Name => "Cleanse Order II";
        public override EffectIconType Icon => EffectIconType.CleanseOrder2StatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Command;

        // Removing the rank-II command early also removes its associated native temporary-HP pool.
        protected override void Remove(uint creature)
        {
            TemporaryHitPointEffects.Remove(creature, TemporaryHitPointEffectKey);
        }
    }
}
