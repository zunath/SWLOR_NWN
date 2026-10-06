using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class LastBastionBarrierStatusEffect : StatusEffectBase
    {
        public const string TemporaryHitPointEffectKey = "LAST_BASTION";

        public override string Name => "Last Bastion Barrier";
        public override EffectIconType Icon => EffectIconType.Invalid;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        protected override void Apply(uint creature, int durationTicks)
        {
            TemporaryHitPointEffects.ApplyFlatOwned(creature, TemporaryHitPointEffectKey, 30,
                GetDurationSeconds(durationTicks), Id);
        }

        protected override void Remove(uint creature)
        {
            TemporaryHitPointEffects.RemoveIfCurrent(creature, TemporaryHitPointEffectKey, Id);
        }
    }
}
