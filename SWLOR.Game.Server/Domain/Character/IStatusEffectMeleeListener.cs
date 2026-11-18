namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// What the melee techniques are told about status effects (P5-5d, Readmes/MaintainabilityRefactorStatusEffectSplit.md).
    /// Character owns the interface and <c>MeleeTechniqueService</c> implements it and registers itself with
    /// <see cref="IStatusEffectService.RegisterMeleeListener"/> at boot, so the status effects notify Combat without holding it.
    /// A listener that has nothing to do for a notice leaves its method empty.
    /// </summary>
    public interface IStatusEffectMeleeListener
    {
        /// <summary>A stance status effect was removed from a creature: the technique state that depends on the stance is reset.</summary>
        void OnStanceChanged(uint creature);

        /// <summary>A direct hit is about to land: the techniques of the attacker prepare their effect on the defender.</summary>
        void OnBeforeDirectDamage(uint attacker, uint defender, CombatDamageDeliveryType deliveryType);
    }
}
