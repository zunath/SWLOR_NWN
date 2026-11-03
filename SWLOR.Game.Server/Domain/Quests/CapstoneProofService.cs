using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Feature.QuestDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Quests
{
    /// <summary>
    /// Tracks which capstone mechanics each player has used: the logic and state that used to be static in
    /// <c>Service.CapstoneProof</c>, moved here unchanged. Enemy proofs live on the enemy until it dies; support proofs
    /// (heals and buffs on allies) stay on the player for a short window. The facade still forwards every call to the
    /// container's instance.
    /// </summary>
    public class CapstoneProofService : ICapstoneProofService
    {
        /// <summary>How long a support proof holds. <c>Service.CapstoneProof.SupportWindowSeconds</c> repeats it.</summary>
        public const float SupportWindowSeconds = 30f;

        private const string EnemyProofPrefix = "CAPSTONE_PROOF_";
        private const string SupportProofPrefix = "CAPSTONE_SUPPORT_";

        private readonly IBeastMasteryService _beastMastery;
        private readonly Dictionary<string, CapstoneProofDefinition> _proofs = new();
        private readonly Dictionary<Type, List<CapstoneProofDefinition>> _byEnemyStatus = new();
        private readonly Dictionary<Type, List<CapstoneProofDefinition>> _bySupportStatus = new();
        private readonly Dictionary<FeatType, List<CapstoneProofDefinition>> _byAbility = new();
        private readonly Dictionary<FeatType, List<CapstoneProofDefinition>> _bySupportAbility = new();
        private Dictionary<AbilityDetail, FeatType> _abilityFeats;

        /// <summary>
        /// Registers the capstone proofs the quest definitions name. That only fills dictionaries from static data (no
        /// engine, database or service call), as the static constructor of the old class did on first use.
        /// </summary>
        public CapstoneProofService(IBeastMasteryService beastMastery)
        {
            _beastMastery = beastMastery;

            foreach (var proof in CapstoneProofDefinitions.All)
                Register(proof);
        }

        public void Register(CapstoneProofDefinition proof)
        {
            _proofs[proof.Key] = proof;
            foreach (var type in proof.EnemyStatusEffects)
                Add(_byEnemyStatus, type, proof);
            foreach (var type in proof.SupportStatusEffects)
                Add(_bySupportStatus, type, proof);
            foreach (var feat in proof.AbilityHits)
                Add(_byAbility, feat, proof);
            foreach (var feat in proof.SupportAbilityHits)
                Add(_bySupportAbility, feat, proof);
            _abilityFeats = null;
        }

        public CapstoneProofDefinition GetProof(string key)
        {
            if (!_proofs.TryGetValue(key, out var proof))
                throw new KeyNotFoundException($"Capstone proof '{key}' is not registered.");

            return proof;
        }

        public IReadOnlyCollection<CapstoneProofDefinition> GetProofs() => _proofs.Values;

        public void RecordStatusEffect(uint source, uint target, Type statusEffectType, AbilityDetail ability = null)
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
                    if (proof.CountsEnemyStatusEffect(statusEffectType, ability))
                        MarkEnemy(player, target, proof);
            }

            if (hasSupportProof && IsAlly(player, target))
            {
                foreach (var proof in supportProofs)
                    MarkSupport(player, proof);
            }
        }

        public void RecordAbilityImpact(uint activator, uint target, AbilityDetail ability)
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

        public void RecordSupportAbilityImpact(uint source, uint target, AbilityDetail ability)
        {
            if (ability == null || !GetAbilityFeats().TryGetValue(ability, out var feat) ||
                !_bySupportAbility.TryGetValue(feat, out var proofs))
                return;

            var player = ResolvePlayer(source);
            if (!GetIsObjectValid(player) || !GetIsObjectValid(target) || !IsAlly(player, target))
                return;

            foreach (var proof in proofs)
                MarkSupport(player, proof);
        }

        public void RecordBeastDamage(uint beast, uint target)
        {
            if (!GetIsObjectValid(beast) || !_beastMastery.IsPlayerBeast(beast))
                return;

            var player = ResolvePlayer(beast);
            if (!GetIsObjectValid(player) || !IsEnemy(player, target))
                return;

            foreach (var proof in _proofs.Values.Where(proof => proof.CountsOwnedBeastDamage))
                MarkEnemy(player, target, proof);
        }

        public bool HasProof(uint player, uint enemy, string key)
        {
            var proof = GetProof(key);
            if (GetLocalInt(enemy, EnemyProofKey(player, proof)) == 1)
                return true;

            if (proof.SupportStatusEffects.Count + proof.SupportAbilityHits.Count == 0)
                return false;

            var stamp = GetLocalString(player, SupportProofPrefix + proof.Key);
            return long.TryParse(stamp, out var ticks) &&
                   (DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc)).TotalSeconds <= SupportWindowSeconds;
        }

        private void MarkEnemy(uint player, uint target, CapstoneProofDefinition proof)
        {
            SetLocalInt(target, EnemyProofKey(player, proof), 1);
        }

        private void MarkSupport(uint player, CapstoneProofDefinition proof)
        {
            SetLocalString(player, SupportProofPrefix + proof.Key, DateTime.UtcNow.Ticks.ToString());
        }

        private string EnemyProofKey(uint player, CapstoneProofDefinition proof) =>
            EnemyProofPrefix + proof.Key + "_" + GetObjectUUID(player);

        // A beast, droid or other associate proves the mechanic for its master.
        private uint ResolvePlayer(uint source)
        {
            if (!GetIsObjectValid(source))
                return OBJECT_INVALID;
            if (GetIsPC(source))
                return GetIsDM(source) ? OBJECT_INVALID : source;

            var master = GetMaster(source);
            return GetIsObjectValid(master) && GetIsPC(master) && !GetIsDM(master) ? master : OBJECT_INVALID;
        }

        private bool IsEnemy(uint player, uint target)
        {
            return GetIsObjectValid(target) &&
                   !GetIsPC(target) &&
                   GetObjectType(target) == ObjectType.Creature &&
                   GetIsEnemy(target, player);
        }

        private bool IsAlly(uint player, uint target)
        {
            if (target == player)
                return true;

            var targetPlayer = GetIsPC(target) ? target : GetMaster(target);
            return GetIsObjectValid(targetPlayer) && GetIsPC(targetPlayer) &&
                   (targetPlayer == player || GetFactionEqual(targetPlayer, player));
        }

        private Dictionary<AbilityDetail, FeatType> GetAbilityFeats()
        {
            if (_abilityFeats != null)
                return _abilityFeats;

            _abilityFeats = new Dictionary<AbilityDetail, FeatType>();
            foreach (var feat in _byAbility.Keys.Concat(_bySupportAbility.Keys).Distinct().Where(IsFeatRegistered))
                _abilityFeats[GetAbilityDetail(feat)] = feat;

            return _abilityFeats;
        }

        private void Add<TKey>(Dictionary<TKey, List<CapstoneProofDefinition>> index, TKey key, CapstoneProofDefinition proof)
        {
            if (!index.TryGetValue(key, out var list))
                index[key] = list = new List<CapstoneProofDefinition>();
            if (!list.Contains(proof))
                list.Add(proof);
        }

        /// <summary>
        /// Whether a feat is registered to an ability. The original call into <c>Ability</c> (not converted yet; its
        /// ability table is static state that a unit test cannot fill without loading every definition), on its own so a
        /// unit test can answer it.
        /// </summary>
        protected virtual bool IsFeatRegistered(FeatType feat)
        {
            return Ability.IsFeatRegistered(feat);
        }

        /// <summary>
        /// The ability a feat is registered to. The original call into <c>Ability</c>, on its own for the same reason as
        /// <see cref="IsFeatRegistered"/>.
        /// </summary>
        protected virtual AbilityDetail GetAbilityDetail(FeatType feat)
        {
            return Ability.GetAbilityDetail(feat);
        }
    }
}
