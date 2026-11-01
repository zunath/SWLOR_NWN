using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.QuestDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Tracks which capstone mechanics each player has used, so capstone quest steps credit a kill only when the
    /// player fought it with their skill's mechanic. Enemy proofs live on the enemy until it dies; support proofs
    /// (heals and buffs on allies) stay on the player for a short window.
    /// </summary>
    public static class CapstoneProof
    {
        public const float SupportWindowSeconds = 30f;

        private const string EnemyProofPrefix = "CAPSTONE_PROOF_";
        private const string SupportProofPrefix = "CAPSTONE_SUPPORT_";

        private static readonly Dictionary<string, CapstoneProofDefinition> _proofs = new();
        private static readonly Dictionary<Type, List<CapstoneProofDefinition>> _byEnemyStatus = new();
        private static readonly Dictionary<Type, List<CapstoneProofDefinition>> _bySupportStatus = new();
        private static readonly Dictionary<FeatType, List<CapstoneProofDefinition>> _byAbility = new();
        private static Dictionary<AbilityDetail, FeatType> _abilityFeats;

        static CapstoneProof()
        {
            foreach (var proof in CapstoneProofDefinitions.All)
                Register(proof);
        }

        public static void Register(CapstoneProofDefinition proof)
        {
            _proofs[proof.Key] = proof;
            foreach (var type in proof.EnemyStatusEffects)
                Add(_byEnemyStatus, type, proof);
            foreach (var type in proof.SupportStatusEffects)
                Add(_bySupportStatus, type, proof);
            foreach (var feat in proof.AbilityHits)
                Add(_byAbility, feat, proof);
            _abilityFeats = null;
        }

        public static CapstoneProofDefinition GetProof(string key)
        {
            if (!_proofs.TryGetValue(key, out var proof))
                throw new KeyNotFoundException($"Capstone proof '{key}' is not registered.");

            return proof;
        }

        public static IReadOnlyCollection<CapstoneProofDefinition> GetProofs() => _proofs.Values;

        public static void RecordStatusEffect(uint source, uint target, Type statusEffectType)
        {
            if (statusEffectType == null)
                return;

            var hasEnemyProof = _byEnemyStatus.TryGetValue(statusEffectType, out var enemyProofs);
            var hasSupportProof = _bySupportStatus.TryGetValue(statusEffectType, out var supportProofs);
            if (!hasEnemyProof && !hasSupportProof)
                return;

            var player = ResolvePlayer(source);
            if (!GetIsObjectValid(player) || !GetIsObjectValid(target))
                return;

            if (hasEnemyProof && IsEnemy(player, target))
            {
                foreach (var proof in enemyProofs)
                    MarkEnemy(player, target, proof);
            }

            if (hasSupportProof && IsAlly(player, target))
            {
                foreach (var proof in supportProofs)
                    MarkSupport(player, proof);
            }
        }

        public static void RecordAbilityImpact(uint activator, uint target, AbilityDetail ability)
        {
            if (ability == null || _byAbility.Count == 0)
                return;

            if (!GetAbilityFeats().TryGetValue(ability, out var feat) ||
                !_byAbility.TryGetValue(feat, out var proofs))
                return;

            var player = ResolvePlayer(activator);
            if (!GetIsObjectValid(player) || !IsEnemy(player, target))
                return;

            foreach (var proof in proofs)
                MarkEnemy(player, target, proof);
        }

        /// <summary>
        /// Whether the player proved the mechanic on this enemy, or supported their party recently enough for a
        /// support proof.
        /// </summary>
        public static bool HasProof(uint player, uint enemy, string key)
        {
            var proof = GetProof(key);
            if (GetLocalInt(enemy, EnemyProofKey(player, proof)) == 1)
                return true;

            if (proof.SupportStatusEffects.Count == 0)
                return false;

            var stamp = GetLocalString(player, SupportProofPrefix + proof.Key);
            return long.TryParse(stamp, out var ticks) &&
                   (DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc)).TotalSeconds <= SupportWindowSeconds;
        }

        private static void MarkEnemy(uint player, uint target, CapstoneProofDefinition proof)
        {
            SetLocalInt(target, EnemyProofKey(player, proof), 1);
        }

        private static void MarkSupport(uint player, CapstoneProofDefinition proof)
        {
            SetLocalString(player, SupportProofPrefix + proof.Key, DateTime.UtcNow.Ticks.ToString());
        }

        private static string EnemyProofKey(uint player, CapstoneProofDefinition proof) =>
            EnemyProofPrefix + proof.Key + "_" + GetObjectUUID(player);

        // A beast, droid or other associate proves the mechanic for its master.
        private static uint ResolvePlayer(uint source)
        {
            if (!GetIsObjectValid(source))
                return OBJECT_INVALID;
            if (GetIsPC(source))
                return GetIsDM(source) ? OBJECT_INVALID : source;

            var master = GetMaster(source);
            return GetIsObjectValid(master) && GetIsPC(master) && !GetIsDM(master) ? master : OBJECT_INVALID;
        }

        private static bool IsEnemy(uint player, uint target)
        {
            return GetIsObjectValid(target) &&
                   !GetIsPC(target) &&
                   GetObjectType(target) == ObjectType.Creature &&
                   GetIsEnemy(target, player);
        }

        private static bool IsAlly(uint player, uint target)
        {
            if (target == player)
                return true;

            var targetPlayer = GetIsPC(target) ? target : GetMaster(target);
            return GetIsObjectValid(targetPlayer) && GetIsPC(targetPlayer) &&
                   (targetPlayer == player || GetFactionEqual(targetPlayer, player));
        }

        private static Dictionary<AbilityDetail, FeatType> GetAbilityFeats()
        {
            if (_abilityFeats != null)
                return _abilityFeats;

            _abilityFeats = new Dictionary<AbilityDetail, FeatType>();
            foreach (var feat in _byAbility.Keys.Where(Ability.IsFeatRegistered))
                _abilityFeats[Ability.GetAbilityDetail(feat)] = feat;

            return _abilityFeats;
        }

        private static void Add<TKey>(Dictionary<TKey, List<CapstoneProofDefinition>> index, TKey key, CapstoneProofDefinition proof)
        {
            if (!index.TryGetValue(key, out var list))
                index[key] = list = new List<CapstoneProofDefinition>();
            if (!list.Contains(proof))
                list.Add(proof);
        }
    }
}
