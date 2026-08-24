using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>Visible marker for a target whose Suppression is restricting collateral damage.</summary>
    [StatConfiguredIcon]
    public sealed class ContainmentNetStatusEffect : StatusEffectBase, IRemoveWhenSourceExits
    {
        public override string Name => "Containment Net";
        public override EffectIconType Icon => EffectIconType.SuppressionStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectStackType StackingType => StatusEffectStackType.StackFromMultipleSources;
        public override bool PersistsOnLogout => false;

        public int DamageAdjustmentPercent { get; }

        public ContainmentNetStatusEffect()
            : this(-10)
        {
        }

        public ContainmentNetStatusEffect(int damageAdjustmentPercent)
        {
            DamageAdjustmentPercent = damageAdjustmentPercent;
            StatGroup.Stats[StatType.DamageDealtPercentAdjustment] = damageAdjustmentPercent;
        }

        public override float Frequency => 1f;

        protected override void Tick(uint creature)
        {
            if (!GetIsObjectValid(Source))
            {
                IsFlaggedForRemoval = true;
                return;
            }

            var requiredStacks = Stat.GetStatAdjustment(Source, StatType.SuppressionStackDamageDealtRequiredStacks);
            var adjustment = Stat.GetStatAdjustment(Source, StatType.SuppressionStackDamageDealtPercentAdjustment);
            if (!ShouldRemainActive(
                    Combat.GetSuppressionStackCount(creature, Source),
                    requiredStacks,
                    adjustment))
            {
                IsFlaggedForRemoval = true;
            }
        }

        // Kept as a small deterministic seam for unit tests and for status reconciliation.
        public static bool ShouldRemainActive(int stackCount, int requiredStacks, int damageAdjustment)
        {
            return requiredStacks > 0 && damageAdjustment != 0 && stackCount >= requiredStacks;
        }
        public override IStatusEffect Clone() => new ContainmentNetStatusEffect(DamageAdjustmentPercent);
    }
}
