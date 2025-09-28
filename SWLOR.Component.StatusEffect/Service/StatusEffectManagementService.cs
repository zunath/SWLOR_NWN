using Microsoft.Extensions.DependencyInjection;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Domain.StatusEffect.Enums;
using SWLOR.Shared.Domain.UI.Events;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Events.Events.Associate;

namespace SWLOR.Component.StatusEffect.Service
{
    /// <summary>
    /// Service responsible for managing status effect lifecycle and state.
    /// </summary>
    public class StatusEffectManagementService : IStatusEffectManagementService
    {
        private readonly IStatusEffectDataService _dataService;
        private readonly IStatusEffectQueryService _queryService;
        private readonly IGuiService _guiService;
        private readonly IMessagingService _messagingService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IEventAggregator _eventAggregator;

        public StatusEffectManagementService(
            IStatusEffectDataService dataService,
            IStatusEffectQueryService queryService,
            IGuiService guiService,
            IMessagingService messagingService,
            IServiceProvider serviceProvider,
            IEventAggregator eventAggregator)
        {
            _dataService = dataService;
            _queryService = queryService;
            _guiService = guiService;
            _messagingService = messagingService;
            _serviceProvider = serviceProvider;
            _eventAggregator = eventAggregator;
        }

        /// <summary>
        /// When the module loads, cache all status effects.
        /// </summary>
        public void CacheStatusEffects()
        {
            // Organize perks to make later reads quicker.
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IStatusEffectListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (IStatusEffectListDefinition)_serviceProvider.GetRequiredService(type);
                var statusEffects = instance.BuildStatusEffects();

                foreach (var (statusEffectType, detail) in statusEffects)
                {
                    _dataService.StatusEffects[statusEffectType] = detail;
                    if (!_dataService.EffectIconToStatusEffects.ContainsKey(detail.EffectIconId))
                        _dataService.EffectIconToStatusEffects[detail.EffectIconId] = new List<StatusEffectType>();
                    _dataService.EffectIconToStatusEffects[detail.EffectIconId].Add(statusEffectType);
                }
            }
        }

        /// <summary>
        /// When a player enters the server, if any of their status effects in limbo, re-add them to the
        /// dictionary for processing.
        /// </summary>
        public void PlayerEnter()
        {
            var player = GetEnteringObject();

            if (_dataService.LoggedOutPlayersWithEffects.ContainsKey(player))
            {
                var effects = _dataService.LoggedOutPlayersWithEffects[player].ToDictionary(x => x.Key, y => y.Value);
                _dataService.CreaturesWithStatusEffects[player] = effects;

                _dataService.LoggedOutPlayersWithEffects.Remove(player);
            }

            _eventAggregator.Publish(new OnAssociateStateEffect(), player);
        }

        /// <summary>
        /// When a player leaves the server, move their status effects to a different dictionary
        /// so they aren't processed unnecessarily.  
        /// </summary>
        public void PlayerExit()
        {
            var player = GetExitingObject();

            if (!_dataService.CreaturesWithStatusEffects.ContainsKey(player))
                return;

            var effects = _dataService.CreaturesWithStatusEffects[player].ToDictionary(x => x.Key, y => y.Value);
            _dataService.LoggedOutPlayersWithEffects[player] = effects;

            _dataService.CreaturesWithStatusEffects.Remove(player);
        }

        /// <summary>
        /// When the module heartbeat runs, execute and clean up status effects on all creatures.
        /// </summary>
        public void TickStatusEffects()
        {
            var now = DateTime.UtcNow;
            var abilityService = _serviceProvider.GetRequiredService<IAbilityService>();

            foreach (var (creature, statusEffects) in _dataService.CreaturesWithStatusEffects.ToDictionary(x => x.Key, y => y.Value))
            {
                // Creature is dead or invalid. Remove its status effects.
                var removeAllEffects = !GetIsObjectValid(creature) || GetIsDead(creature);

                // Iterate over each status effect, cleaning them up if they've expired or executing their tick if applicable.
                foreach (var (statusEffect, group) in statusEffects)
                {
                    var activeConcentration = abilityService.GetActiveConcentration(group.Source);

                    // Concentration check - If caster is no longer channeling this feat, remove the status effect.
                    if (group.ConcentrationFeatType != FeatType.Invalid)
                    {
                        if (activeConcentration.Feat != group.ConcentrationFeatType)
                        {
                            Remove(creature, statusEffect);
                            continue;
                        }
                    }

                    // Status effect has expired or creature is no longer valid. Remove it.
                    if (removeAllEffects || now > group.Expiration)
                    {
                        Remove(creature, statusEffect);

                        // Concentration - End the ability if this status effect was tied to a concentration ability
                        // and the creature was the target.
                        if (group.ConcentrationFeatType != FeatType.Invalid &&
                            activeConcentration.Feat == group.ConcentrationFeatType &&
                            activeConcentration.Target == creature)
                        {
                            abilityService.EndConcentrationAbility(group.Source);
                        }
                    }
                    // Otherwise do a Tick.
                    else
                    {
                        var detail = _dataService.StatusEffects[statusEffect];
                        detail.TickAction?.Invoke(group.Source, creature, group.EffectData);
                    }
                }

                // No more status effects. Remove the creature from the cache.
                if (statusEffects.Count <= 0)
                {
                    _dataService.CreaturesWithStatusEffects.Remove(creature);
                }
            }
        }

        /// <summary>
        /// When a player dies, remove any status effects which are present.
        /// </summary>
        public void OnPlayerDeath()
        {
            var player = GetLastPlayerDied();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            if (!_dataService.CreaturesWithStatusEffects.ContainsKey(player))
                return;

            var statusEffects = _dataService.CreaturesWithStatusEffects[player].Select(s => s.Key);

            foreach (var effect in statusEffects)
            {
                Remove(player, effect);
            }
        }

        /// <summary>
        /// Removes a status effect from a creature.
        /// </summary>
        /// <param name="creature">The creature to remove the status effect from.</param>
        /// <param name="statusEffectType">The type of status effect to remove.</param>
        /// <param name="showMessage">If true, a message will be displayed. Otherwise no message is displayed.</param>
        public void Remove(uint creature, StatusEffectType statusEffectType, bool showMessage = true)
        {
            Remove(creature, statusEffectType, showMessage, true);
        }

        /// <summary>
        /// Removes all status effects from a creature.
        /// </summary>
        /// <param name="creature">The creature to remove all effects from.</param>
        public void RemoveAll(uint creature)
        {
            if (!_dataService.CreaturesWithStatusEffects.ContainsKey(creature))
                return;

            foreach (var effectType in _dataService.CreaturesWithStatusEffects[creature].Keys)
            {
                Remove(creature, effectType);
            }
        }

        /// <summary>
        /// Internal method to remove a status effect with additional parameters.
        /// </summary>
        /// <param name="creature">The creature to remove the status effect from.</param>
        /// <param name="statusEffectType">The type of status effect to remove.</param>
        /// <param name="showMessage">If true, a message will be displayed.</param>
        /// <param name="removeIcon">If true, the effect icon will be removed.</param>
        internal void Remove(uint creature, StatusEffectType statusEffectType, bool showMessage, bool removeIcon)
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
