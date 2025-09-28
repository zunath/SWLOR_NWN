using SWLOR.Shared.Domain.StatusEffect.Enums;
using SWLOR.Shared.Domain.StatusEffect.ValueObjects;
using SWLOR.Component.StatusEffect.Contracts;

namespace SWLOR.Component.StatusEffect.Service
{
    /// <summary>
    /// Service responsible for querying status effect information.
    /// </summary>
    public class StatusEffectQueryService : IStatusEffectQueryService
    {
        private readonly IStatusEffectDataService _dataService;

        public StatusEffectQueryService(IStatusEffectDataService dataService)
        {
            _dataService = dataService;
        }

        /// <summary>
        /// Checks if a creature has a status effect.
        /// If no status effect types are specified, false will be returned.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <param name="statusEffectTypes">The status effect types to look for.</param>
        /// <returns>true if creature has status effect, false otherwise</returns>
        public bool HasStatusEffect(uint creature, params StatusEffectType[] statusEffectTypes)
        {
            foreach (var statusEffectType in statusEffectTypes)
            {
                if (HasStatusEffectInternal(creature, statusEffectType, false))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Retrieves a status effect detail by its type.
        /// </summary>
        /// <param name="type">The type to search for.</param>
        /// <returns>A status effect detail</returns>
        public StatusEffectDetail GetDetail(StatusEffectType type)
        {
            return _dataService.StatusEffects[type];
        }

        /// <summary>
        /// Retrieves the effect data associated with a creature's effect.
        /// If creature does not have effect, the default value of T will be returned.
        /// </summary>
        /// <typeparam name="T">The type of data to retrieve from the effect.</typeparam>
        /// <param name="creature">The creature to check.</param>
        /// <param name="effectType">The type of effect.</param>
        /// <returns>An effect data object or a default object of type T</returns>
        public T GetEffectData<T>(uint creature, StatusEffectType effectType)
        {
            if (!_dataService.CreaturesWithStatusEffects.ContainsKey(creature) ||
                !_dataService.CreaturesWithStatusEffects[creature].ContainsKey(effectType))
                return default;

            return (T)_dataService.CreaturesWithStatusEffects[creature][effectType].EffectData;
        }

        /// <summary>
        /// Retrieves the effect duration associated with a creature's effect.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <param name="effectTypes">The type(s) of effect.</param>
        /// <returns>A float time remaining of the status effect</returns>
        public int GetEffectDuration(uint creature, params StatusEffectType[] effectTypes)
        {
            foreach (var effectType in effectTypes)
            {
                if (!_dataService.CreaturesWithStatusEffects.ContainsKey(creature) ||
                !_dataService.CreaturesWithStatusEffects[creature].ContainsKey(effectType))
                    continue;

                if (_dataService.CreaturesWithStatusEffects[creature][effectType].Expiration >= DateTime.MaxValue) return 0;

                var effectTimespan = _dataService.CreaturesWithStatusEffects[creature][effectType].Expiration - DateTime.UtcNow;

                return (int)effectTimespan.TotalSeconds;
            }

            return 0;
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
