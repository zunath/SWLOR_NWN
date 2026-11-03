using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the capstone proofs. The logic and state live in <see cref="ICapstoneProofService"/>; every
    /// member here forwards to the container's instance. Capstone proofs track which capstone mechanics each player has
    /// used, so capstone quest steps credit a kill only when the player fought it with their skill's mechanic. Enemy
    /// proofs live on the enemy until it dies; support proofs (heals and buffs on allies) stay on the player for a short
    /// window.
    /// </summary>
    public static class CapstoneProof
    {
        // A compile-time constant: callers get the value inlined, as before.
        public const float SupportWindowSeconds = Domain.Quests.CapstoneProofService.SupportWindowSeconds;

        private static ICapstoneProofService Instance => FacadeAccessor.Resolve<ICapstoneProofService>();

        public static void Register(CapstoneProofDefinition proof)
        {
            Instance.Register(proof);
        }

        public static CapstoneProofDefinition GetProof(string key)
        {
            return Instance.GetProof(key);
        }

        public static IReadOnlyCollection<CapstoneProofDefinition> GetProofs() => Instance.GetProofs();

        public static void RecordStatusEffect(uint source, uint target, Type statusEffectType, AbilityDetail ability = null)
        {
            Instance.RecordStatusEffect(source, target, statusEffectType, ability);
        }

        public static void RecordAbilityImpact(uint activator, uint target, AbilityDetail ability)
        {
            Instance.RecordAbilityImpact(activator, target, ability);
        }

        public static void RecordSupportAbilityImpact(uint source, uint target, AbilityDetail ability)
        {
            Instance.RecordSupportAbilityImpact(source, target, ability);
        }

        public static void RecordBeastDamage(uint beast, uint target)
        {
            Instance.RecordBeastDamage(beast, target);
        }

        /// <summary>
        /// Whether the player proved the mechanic on this enemy, or supported their party recently enough for a
        /// support proof.
        /// </summary>
        public static bool HasProof(uint player, uint enemy, string key)
        {
            return Instance.HasProof(player, enemy, key);
        }
    }
}
