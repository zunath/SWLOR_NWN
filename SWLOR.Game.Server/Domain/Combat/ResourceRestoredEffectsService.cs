using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The riders restoring FP or Stamina grants: part 8 of the stat split (<c>Readmes/MaintainabilityRefactorStatSplit.md</c>),
    /// moved from <c>CombatResourcesService</c> unchanged except that the stat reader is an argument. The pools in
    /// <c>StatService</c> call it as Character's <see cref="IResourceRestoredListener"/> and pass themselves, so it takes only the
    /// status effects and no service above it.
    /// </summary>
    public class ResourceRestoredEffectsService : IResourceRestoredEffectsService
    {
        private readonly IStatusEffectApplicationService _statusEffect;

        public ResourceRestoredEffectsService(IStatusEffectApplicationService statusEffect)
        {
            _statusEffect = statusEffect;
        }

        public void ApplyFPRestoredEffects(uint creature, IStatService statReader)
        {
            var forceAttack = statReader.GetStatAdjustment(creature, StatType.RestoredFPForceAttackPercentAdjustment);
            var duration = statReader.GetStatAdjustment(creature, StatType.RestoredFPForceAttackDurationSeconds);
            if (forceAttack == 0 || duration <= 0)
                return;

            _statusEffect.ApplyStatusEffect(
                creature,
                creature,
                new ForceLensForceAttackStatusEffect(forceAttack),
                duration);
        }

        public void ApplyStaminaRestoredEffects(uint creature, IStatService statReader)
        {
            var attack = statReader.GetStatAdjustment(creature, StatType.RestoredStaminaAttackPercentAdjustment);
            var duration = statReader.GetStatAdjustment(creature, StatType.RestoredStaminaAttackDurationSeconds);
            if (attack == 0 || duration <= 0)
                return;

            _statusEffect.ApplyStatusEffect(
                creature,
                creature,
                new ForceLensAttackStatusEffect(attack),
                duration);
        }
    }
}
