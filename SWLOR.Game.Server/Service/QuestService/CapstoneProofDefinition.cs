using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.QuestService
{
    /// <summary>
    /// What a player must do to an enemy (or for an ally) before a capstone kill counts: the central mechanic of the
    /// capstone's style, available before the capstone perk itself.
    /// </summary>
    public sealed class CapstoneProofDefinition
    {
        public string Key { get; }

        /// <summary>Player-facing requirement, completing "Defeat N enemies ...".</summary>
        public string Requirement { get; }

        /// <summary>Status effects the player (or their beast or droid) applies to the enemy.</summary>
        public IReadOnlyCollection<Type> EnemyStatusEffects { get; }

        /// <summary>Abilities whose hit on the enemy counts.</summary>
        public IReadOnlyCollection<FeatType> AbilityHits { get; }

        /// <summary>Status effects the player applies to an ally or themselves; any kill the party makes while one is
        /// recent counts.</summary>
        public IReadOnlyCollection<Type> SupportStatusEffects { get; }

        public IReadOnlyCollection<FeatType> SupportAbilityHits { get; }
        public bool CountsOwnedBeastDamage { get; }
        private readonly Func<Type, AbilityDetail, bool> _enemyStatusEffectFilter;

        public CapstoneProofDefinition(
            string key,
            string requirement,
            IReadOnlyCollection<Type> enemyStatusEffects = null,
            IReadOnlyCollection<FeatType> abilityHits = null,
            IReadOnlyCollection<Type> supportStatusEffects = null,
            IReadOnlyCollection<FeatType> supportAbilityHits = null,
            bool countsOwnedBeastDamage = false,
            Func<Type, AbilityDetail, bool> enemyStatusEffectFilter = null)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("A proof key is required.", nameof(key));
            if (string.IsNullOrWhiteSpace(requirement)) throw new ArgumentException("A requirement is required.", nameof(requirement));

            Key = key;
            Requirement = requirement;
            EnemyStatusEffects = enemyStatusEffects ?? Array.Empty<Type>();
            AbilityHits = abilityHits ?? Array.Empty<FeatType>();
            SupportStatusEffects = supportStatusEffects ?? Array.Empty<Type>();
            SupportAbilityHits = supportAbilityHits ?? Array.Empty<FeatType>();
            CountsOwnedBeastDamage = countsOwnedBeastDamage;
            _enemyStatusEffectFilter = enemyStatusEffectFilter;

            if (EnemyStatusEffects.Count + AbilityHits.Count + SupportStatusEffects.Count + SupportAbilityHits.Count == 0 &&
                !CountsOwnedBeastDamage)
                throw new ArgumentException($"Capstone proof '{key}' needs at least one qualifying action.");
        }

        public bool CountsEnemyStatusEffect(Type type, AbilityDetail ability) =>
            EnemyStatusEffects.Contains(type) && (_enemyStatusEffectFilter?.Invoke(type, ability) ?? true);
    }
}
