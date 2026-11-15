using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Feature;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// The activation and queued-attack state <c>UsePerkFeat</c> runs on (part 20 of the ability split,
    /// Readmes/MaintainabilityRefactorAbilitySplit.md), moved from <c>AbilityService</c> unchanged: the casts and channels in
    /// progress, the trait snapshots of queued weapon abilities and their reserved attack rolls, and the reload hook that
    /// interrupts every cast. The interruption calls into <c>UsePerkFeat</c> through a <c>protected virtual</c> method so a
    /// unit test can answer it.
    /// </summary>
    public class AbilityActivationStateService : IAbilityActivationStateService
    {
        [ReloadState(ReloadStateKind.Runtime)]
        private readonly Dictionary<uint, AbilityActiveActivation> _activeAbilityActivations = new();
        [ReloadState(ReloadStateKind.Runtime)]
        private readonly Dictionary<uint, WeaponAbilityTraitActivation> _queuedWeaponTraits = new();
        [ReloadState(ReloadStateKind.Runtime)]
        private readonly Dictionary<uint, AbilityQueuedWeaponAttack> _queuedWeaponAttacks = new();

        public bool TryGetActiveAbilityActivation(uint activator, out AbilityActiveActivation activation) =>
            _activeAbilityActivations.TryGetValue(activator, out activation);

        public void SetActiveAbilityActivation(uint activator, AbilityActiveActivation activation) =>
            _activeAbilityActivations[activator] = activation;

        public void RemoveActiveAbilityActivation(uint activator) => _activeAbilityActivations.Remove(activator);

        /// <summary>
        /// While a hot reload quiesces this build (a <see cref="ReloadHookPhase.Quiesce"/> hook): every creature's cast or delayed impact is
        /// interrupted, as damage or movement interrupts it, which releases its busy flag, timing bar, channel and stamina context.
        /// The closures that would finish it are dropped by the reload and the activations are not carried, so the new build could
        /// neither finish nor interrupt it, and the caster would stay busy.
        /// </summary>
        [ReloadHook(ReloadHookPhase.Quiesce)]
        public void QuiesceForReload(IReadOnlyList<uint> players)
        {
            foreach (var activator in _activeAbilityActivations.Keys.ToArray())
                InterruptActivationForReload(activator);
        }

        /// <summary>The interruption of one activation (<c>UsePerkFeat</c>'s), on its own so a unit test can answer it.</summary>
        protected virtual void InterruptActivationForReload(uint activator)
        {
            UsePerkFeat.InterruptAbilityActivation(activator);
        }

        public bool TryGetQueuedWeaponTraits(uint activator, out WeaponAbilityTraitActivation traits) =>
            _queuedWeaponTraits.TryGetValue(activator, out traits);

        public void SetQueuedWeaponTraits(uint activator, WeaponAbilityTraitActivation traits) =>
            _queuedWeaponTraits[activator] = traits;

        public void RemoveQueuedWeaponTraits(uint activator) => _queuedWeaponTraits.Remove(activator);

        public bool TryGetQueuedWeaponAttack(uint activator, out AbilityQueuedWeaponAttack reservation) =>
            _queuedWeaponAttacks.TryGetValue(activator, out reservation);

        public void TryAddQueuedWeaponAttack(uint activator, AbilityQueuedWeaponAttack reservation) =>
            _queuedWeaponAttacks.TryAdd(activator, reservation);

        public void RemoveQueuedWeaponAttack(uint activator) => _queuedWeaponAttacks.Remove(activator);
    }
}
