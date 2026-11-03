using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Domain.Quests
{
    /// <summary>
    /// Tracks which capstone mechanics each player has used, so capstone quest steps credit a kill only when the
    /// player fought it with their skill's mechanic. The static facade <c>Service.CapstoneProof</c> forwards to the
    /// container's instance of this interface and keeps the constant.
    /// </summary>
    public interface ICapstoneProofService
    {
        /// <summary>
        /// Adds a proof to the registry and to the indexes by status effect and by ability.
        /// </summary>
        /// <param name="proof">The proof to register. A proof with the same key replaces the earlier one in the registry.</param>
        void Register(CapstoneProofDefinition proof);

        /// <summary>
        /// Retrieves a registered proof by its key. Throws when the key is not registered.
        /// </summary>
        /// <param name="key">The key of the proof.</param>
        /// <returns>The proof.</returns>
        CapstoneProofDefinition GetProof(string key);

        /// <summary>
        /// Every registered proof.
        /// </summary>
        IReadOnlyCollection<CapstoneProofDefinition> GetProofs();

        /// <summary>
        /// Records that a status effect landed, for the proofs that count it (on an enemy of the player, or as
        /// support for an ally).
        /// </summary>
        /// <param name="source">Who applied the status effect: the player, or an associate of the player.</param>
        /// <param name="target">Who the status effect landed on.</param>
        /// <param name="statusEffectType">The type of the status effect.</param>
        /// <param name="ability">The ability that applied it, when known.</param>
        void RecordStatusEffect(uint source, uint target, Type statusEffectType, AbilityDetail ability = null);

        /// <summary>
        /// Records that an ability hit an enemy, for the proofs that count the ability.
        /// </summary>
        /// <param name="activator">Who used the ability.</param>
        /// <param name="target">The enemy that was hit.</param>
        /// <param name="ability">The ability.</param>
        void RecordAbilityImpact(uint activator, uint target, AbilityDetail ability);

        /// <summary>
        /// Records that a support ability reached an ally, for the proofs that count the ability.
        /// </summary>
        /// <param name="source">Who used the ability.</param>
        /// <param name="target">The ally it reached.</param>
        /// <param name="ability">The ability.</param>
        void RecordSupportAbilityImpact(uint source, uint target, AbilityDetail ability);

        /// <summary>
        /// Records that a player's beast damaged an enemy, for the proofs that count owned beast damage.
        /// </summary>
        /// <param name="beast">The beast that dealt the damage.</param>
        /// <param name="target">The enemy it damaged.</param>
        void RecordBeastDamage(uint beast, uint target);

        /// <summary>
        /// Whether the player proved the mechanic on this enemy, or supported their party recently enough for a
        /// support proof.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="enemy">The enemy the kill is credited on.</param>
        /// <param name="key">The key of the proof.</param>
        /// <returns>true when the proof holds.</returns>
        bool HasProof(uint player, uint enemy, string key);
    }
}
