using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// Told what a hostile ability does to each target, one way: the observer never answers. Abilities owns the interface and
    /// a higher service implements it (supply rule 2 of MaintainabilityRefactorDomainOrder.md): the music service starts the
    /// battle theme and the capstone proof service records the ability hit. The per-target parts take every implementation and
    /// copy it to an array once, so an implementation must not need those parts, and a notice it does not use is empty.
    /// </summary>
    public interface IAbilityImpactObserver
    {
        /// <summary>A hostile ability engages <paramref name="target"/>, before its hit roll and again when it lands.</summary>
        void OnTargetEngaged(uint activator, uint target);

        /// <summary>A hostile ability's impact landed on <paramref name="target"/>, after its status effects and before its riders.</summary>
        void OnAbilityImpact(uint activator, uint target, AbilityDetail ability);
    }
}
