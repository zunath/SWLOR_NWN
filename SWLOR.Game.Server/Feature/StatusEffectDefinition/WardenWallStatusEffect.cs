using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Warden Wall stance: while active, hardens the wielder's defenses and radiates the same
    /// hardening to allies within <see cref="AuraRadius"/> meters via a periodic aura tick
    /// (<see cref="WardenWallAuraStatusEffect"/>). A Mimicry defensive stance.
    /// </summary>
    public sealed class WardenWallStatusEffect : StatusEffectBase
    {
        private const float AuraRadius = 10.0f;
        private const float AuraBuffDurationSeconds = 9.0f;

        public override string Name => "Warden Wall";
        public override EffectIconType Icon => EffectIconType.WardenWallStatusEffect;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;
        public override float Frequency => 6f;

        public override IStatusEffect Clone()
        {
            return new WardenWallStatusEffect();
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = 20;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = 20;
        }

        protected override void Tick(uint creature)
        {
            // Radiate the wall to nearby allies. The buff outlives the tick interval slightly so
            // allies who stay in range keep it continuously; leaving range lets it lapse.
            foreach (var ally in AbilityTargeting.GetFriendlyTargetsNearLocation(
                         creature, GetLocation(creature), AuraRadius, includeActivator: false))
            {
                StatusEffect.ApplyStatusEffect(creature, ally, new WardenWallAuraStatusEffect(), AuraBuffDurationSeconds);
            }
        }
    }
}
