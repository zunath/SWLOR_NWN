using SWLOR.Game.Server.Core;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.WeaponAbility
{
    /// <summary>
    /// What the toggle, self status and party status abilities do when they run: turning a stance off, applying a status to the
    /// caster, and applying a status to the caster's party in range.
    /// </summary>
    public class StatusAbilityEffects
    {
        private IAbilityService Ability { get; }
        private ICombatRecorderService CombatRecorder { get; }
        private IPartyService Party { get; }
        private IStatusEffectService StatusEffect { get; }
        private IStatusEffectApplicationService StatusEffectApplication { get; }
        private IStatusEffectCatalogService StatusEffectCatalog { get; }

        public StatusAbilityEffects(IAbilityService ability, ICombatRecorderService combatRecorder, IPartyService party, IStatusEffectService statusEffect, IStatusEffectApplicationService statusEffectApplication, IStatusEffectCatalogService statusEffectCatalog)
        {
            Ability = ability;
            CombatRecorder = combatRecorder;
            Party = party;
            StatusEffect = statusEffect;
            StatusEffectApplication = statusEffectApplication;
            StatusEffectCatalog = statusEffectCatalog;
        }

        public bool ToggleSelfStatus(uint activator, Type type)
        {
            if (!StatusEffect.HasStatusEffect(activator, type))
                return true;

            StatusEffect.RemoveStatusEffect(activator, type, false);
            SendMessageToPC(activator, $"{StatusEffectCatalog.GetStatusEffectName(type)} deactivated.");
            if (CombatRecorder.IsRecording)
                CombatRecorder.NoteToggleDeactivated(activator);
            return false;
        }

        public void ApplyToggleStatus(uint activator, Type type, Func<IStatusEffect> statusEffectFactory)
        {
            StatusEffect.RemoveOtherStanceStatuses(activator, type);
            if (StatusEffectApplication.ApplyStatusEffect(activator, activator, statusEffectFactory(), 0f))
                Ability.PlaySuccessfulImpactVisualEffect(activator, activator);
        }

        public void ApplySelfStatus(uint activator, Func<IStatusEffect> statusEffectFactory, float duration, Action<uint> additionalAction)
        {
            var statusEffect = statusEffectFactory();
            if (StatusEffectApplication.ApplyStatusEffect(activator, activator, statusEffect, duration))
                Ability.PlaySuccessfulImpactVisualEffect(activator, activator);
            additionalAction?.Invoke(activator);
        }

        public void ApplyStatusToNearbyParty(uint activator, Type type, float duration, bool includeSelf)
        {
            ApplyStatusToNearbyParty(activator, () => (IStatusEffect)Activator.CreateInstance(type), duration, includeSelf);
        }

        public void ApplyStatusToNearbyParty(uint activator, Func<IStatusEffect> statusEffectFactory, float duration, bool includeSelf)
        {
            if (includeSelf)
            {
                if (StatusEffectApplication.ApplyStatusEffect(activator, activator, statusEffectFactory(), duration))
                    Ability.PlaySuccessfulImpactVisualEffect(activator, activator);
            }

            var location = GetLocation(activator);
            foreach (var creature in ObjectSnapshot.InShape(Shape.Sphere, 5f, location, true))
            {
                if (!GetIsObjectValid(creature))
                    continue;

                if (creature != activator && Party.IsInParty(activator, creature))
                {
                    if (StatusEffectApplication.ApplyStatusEffect(activator, creature, statusEffectFactory(), duration, CombatDamageType.Physical))
                        Ability.PlaySuccessfulImpactVisualEffect(activator, creature);
                }

            }
        }
    }
}
