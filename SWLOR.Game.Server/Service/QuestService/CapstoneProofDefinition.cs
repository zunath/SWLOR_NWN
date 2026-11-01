using System;
using System.Collections.Generic;
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

        public CapstoneProofDefinition(
            string key,
            string requirement,
            IReadOnlyCollection<Type> enemyStatusEffects = null,
            IReadOnlyCollection<FeatType> abilityHits = null,
            IReadOnlyCollection<Type> supportStatusEffects = null)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("A proof key is required.", nameof(key));
            if (string.IsNullOrWhiteSpace(requirement)) throw new ArgumentException("A requirement is required.", nameof(requirement));

            Key = key;
            Requirement = requirement;
            EnemyStatusEffects = enemyStatusEffects ?? Array.Empty<Type>();
            AbilityHits = abilityHits ?? Array.Empty<FeatType>();
            SupportStatusEffects = supportStatusEffects ?? Array.Empty<Type>();

            if (EnemyStatusEffects.Count + AbilityHits.Count + SupportStatusEffects.Count == 0)
                throw new ArgumentException($"Capstone proof '{key}' needs at least one qualifying action.");
        }
    }
}
