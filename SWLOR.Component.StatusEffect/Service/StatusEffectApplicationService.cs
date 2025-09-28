using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Domain.StatusEffect.Enums;
using SWLOR.Shared.Domain.UI.Events;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Component.StatusEffect.Contracts;

namespace SWLOR.Component.StatusEffect.Service
{
    /// <summary>
    /// Service responsible for applying status effects to creatures.
    /// </summary>
    public class StatusEffectApplicationService : IStatusEffectApplicationService
    {
        private readonly IStatusEffectDataService _dataService;
        private readonly IStatusEffectQueryService _queryService;
        private readonly IStatusEffectManagementService _managementService;
        private readonly IGuiService _guiService;
        private readonly IMessagingService _messagingService;

        public StatusEffectApplicationService(
            IStatusEffectDataService dataService,
            IStatusEffectQueryService queryService,
            IStatusEffectManagementService managementService,
            IGuiService guiService,
            IMessagingService messagingService)
        {
            _dataService = dataService;
            _queryService = queryService;
            _managementService = managementService;
            _guiService = guiService;
            _messagingService = messagingService;
        }

        /// <summary>
        /// Gives a status effect to a creature.
        /// If creature already has the status effect, and their timer is shorter than length,
        /// it will be extended to the length specified.
        /// </summary>
        /// <param name="source">The source of the status effect.</param>
        /// <param name="target">The creature receiving the status effect.</param>
        /// <param name="statusEffectType">The type of status effect to give.</param>
        /// <param name="length">The amount of time, in seconds, the status effect should last. Set to 0.0f to make it permanent.</param>
        /// <param name="effectData">Effect data used by the effect.</param>
        /// <param name="concentrationFeatType">If status effect is associated with a concentration ability, this will track the feat type used.</param>
        /// <param name="sendApplicationMessage">If true, a message will be sent to nearby players when the status effect is applied.</param>
        public void Apply(
            uint source,
            uint target,
            StatusEffectType statusEffectType,
            float length,
            object effectData = null,
            FeatType concentrationFeatType = FeatType.Invalid,
            bool sendApplicationMessage = true)
        {
            var statusEffectDetail = _dataService.StatusEffects[statusEffectType];
            if (!_dataService.CreaturesWithStatusEffects.ContainsKey(target))
                _dataService.CreaturesWithStatusEffects[target] = new Dictionary<StatusEffectType, StatusEffectGroup>();

            if (!_dataService.CreaturesWithStatusEffects[target].ContainsKey(statusEffectType))
                _dataService.CreaturesWithStatusEffects[target][statusEffectType] = new StatusEffectGroup();

            var expiration = length == 0.0f ? DateTime.MaxValue : DateTime.UtcNow.AddSeconds(length);
            var addIcon = true;

            // If the existing status effect will expire later than this, exit early.
            if (_dataService.CreaturesWithStatusEffects[target][statusEffectType].Expiration > expiration)
                return;

            // Can't stack - remove the effect then reapply it afterwards.
            if (!statusEffectDetail.CanStack &&
                _queryService.HasStatusEffect(target, statusEffectType))
            {
                RemoveInternal(target, statusEffectType, false, false);
                _dataService.CreaturesWithStatusEffects[target][statusEffectType] = new StatusEffectGroup();
                addIcon = false;
            }

            // Remove any status effects this effect overrides.
            if (statusEffectDetail.ReplacesEffects != null)
            {
                foreach (var effect in statusEffectDetail.ReplacesEffects)
                {
                    RemoveInternal(target, effect, false, false);
                    addIcon = false;
                }
            }

            // Prevent applying the status effect if a more powerful one is already in place.
            if (statusEffectDetail.CannotReplaceEffects != null)
            {
                if (_queryService.HasStatusEffect(target, statusEffectDetail.CannotReplaceEffects))
                {
                    const string Message = "A more powerful effect already exists.";
                    SendMessageToPC(source, Message);

                    if (source != target)
                        SendMessageToPC(target, Message);
                    return;
                }
            }

            // Set the group details.
            _dataService.CreaturesWithStatusEffects[target][statusEffectType].Source = source;
            _dataService.CreaturesWithStatusEffects[target][statusEffectType].Expiration = expiration;
            _dataService.CreaturesWithStatusEffects[target][statusEffectType].ConcentrationFeatType = concentrationFeatType;
            _dataService.CreaturesWithStatusEffects[target][statusEffectType].EffectData = effectData;

            // Run the Grant Action, if applicable.
            statusEffectDetail.AppliedAction?.Invoke(source, target, length, effectData);

            // Add the status effect icon if there is one.
            if (addIcon && statusEffectDetail.EffectIconId != EffectIconType.Invalid)
            {
                var iconEffect = EffectIcon(statusEffectDetail.EffectIconId);
                iconEffect = TagEffect(iconEffect, $"EFFECT_ICON_{statusEffectDetail.EffectIconId}");

                if (length > 0f)
                    ApplyEffectToObject(DurationType.Temporary, iconEffect, target, length);
                else
                    ApplyEffectToObject(DurationType.Permanent, iconEffect, target);
            }

            if (sendApplicationMessage)
                _messagingService.SendMessageNearbyToPlayers(target, $"{GetName(target)} receives the effect of {statusEffectDetail.Name}.", 20f);

            _guiService.PublishRefreshEvent(target, new StatusEffectReceivedRefreshEvent());
        }

        /// <summary>
        /// Internal method to remove a status effect with additional parameters.
        /// </summary>
        /// <param name="creature">The creature to remove the status effect from.</param>
        /// <param name="statusEffectType">The type of status effect to remove.</param>
        /// <param name="showMessage">If true, a message will be displayed.</param>
        /// <param name="removeIcon">If true, the effect icon will be removed.</param>
        private void RemoveInternal(uint creature, StatusEffectType statusEffectType, bool showMessage, bool removeIcon)
        {
            if (!HasStatusEffectInternal(creature, statusEffectType, true)) return;

            var effectInstance = _dataService.CreaturesWithStatusEffects[creature][statusEffectType];
            _dataService.CreaturesWithStatusEffects[creature].Remove(statusEffectType);

            var statusEffectDetail = _dataService.StatusEffects[statusEffectType];
            statusEffectDetail.RemoveAction?.Invoke(creature, effectInstance.EffectData);

            if (removeIcon && statusEffectDetail.EffectIconId > 0 && GetIsObjectValid(creature))
            {
                RemoveEffectByTag(creature, $"EFFECT_ICON_{statusEffectDetail.EffectIconId}");
            }

            if (showMessage)
                _messagingService.SendMessageNearbyToPlayers(creature, $"{GetName(creature)}'s {statusEffectDetail.Name} effect has worn off.");

            _guiService.PublishRefreshEvent(creature, new StatusEffectRemovedRefreshEvent());
        }

        /// <summary>
        /// Checks if a creature has a status effect.
        /// If ignoreExpiration is true, even if the effect is expired this will return true.
        /// This should only be used within this class to avoid confusion.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <param name="statusEffectType">The status effect type to look for.</param>
        /// <param name="ignoreExpiration">If true, expired effects will return true. Otherwise, expiration will be checked.</param>
        /// <returns>true if creature has status effect, false otherwise</returns>
        private bool HasStatusEffectInternal(uint creature, StatusEffectType statusEffectType, bool ignoreExpiration)
        {
            // Creature doesn't exist in the cache.
            if (!_dataService.CreaturesWithStatusEffects.ContainsKey(creature))
                return false;

            // Status effect doesn't exist for this creature in the cache.
            if (!_dataService.CreaturesWithStatusEffects[creature].ContainsKey(statusEffectType))
                return false;

            // Status effect has expired, but hasn't cleaned up yet.
            if (!ignoreExpiration)
            {
                var now = DateTime.UtcNow;
                if (now > _dataService.CreaturesWithStatusEffects[creature][statusEffectType].Expiration)
                    return false;
            }

            // Status effect hasn't expired.
            return true;
        }
    }
}
