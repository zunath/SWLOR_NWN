namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// What restoring FP or Stamina sets off: the riders a restore grants (part 8 of the stat split,
    /// Readmes/MaintainabilityRefactorStatSplit.md). Character owns the interface and Combat implements it (supply rule 2 of
    /// MaintainabilityRefactorDomainOrder.md), so the pools call it without a construction cycle. The riders read the stat
    /// adjustment through the stat reader their caller passes, which is the pools' own instance.
    /// </summary>
    public interface IResourceRestoredListener
    {
        /// <summary>Grants what restoring FP to a creature grants it.</summary>
        void ApplyFPRestoredEffects(uint creature, IStatService statReader);

        /// <summary>Grants what restoring Stamina to a creature grants it.</summary>
        void ApplyStaminaRestoredEffects(uint creature, IStatService statReader);
    }
}
