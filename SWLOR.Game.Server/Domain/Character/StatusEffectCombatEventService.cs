using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// Status effects reacting to combat (part 6 of the status effect split): the damage and attack notices, the guarded hit, the
    /// native attack swing with the applications deferred until it ends, and the limited attack-timing reads. The trackers and the
    /// removal stay in <see cref="StatusEffectService"/>; this part calls them through <see cref="IStatusEffectService"/>. The
    /// suppression stat read sits behind a one-line <c>protected virtual</c> method whose body is the original call.
    /// </summary>
    public class StatusEffectCombatEventService : IStatusEffectCombatEventService
    {
        [ReloadState(ReloadStateKind.Runtime)]
        private readonly Dictionary<uint, int> _nativeAttackSwingDepth = new();
        [ReloadState(ReloadStateKind.Runtime)]
        private readonly Dictionary<uint, List<Action>> _deferredNativeAttackStatusEffects = new();
        private readonly IStatusEffectService _statusEffect;
        private readonly Lazy<IStatService> _stat;

        public StatusEffectCombatEventService(
            IStatusEffectService statusEffect,
            Lazy<IStatService> stat)
        {
            _statusEffect = statusEffect;
            _stat = stat;
        }

        [NWNEventHandler(ScriptName.OnSWLORDamage)]
        public void OnDealtDamage()
        {
            var attacker = OBJECT_SELF;
            var defender = StringToObject(EventsPlugin.GetEventData("DEFENDER"));
            var damage = Convert.ToInt32(EventsPlugin.GetEventData("DAMAGE"));
            var damageTypeText = EventsPlugin.GetEventData("DAMAGE_TYPE");
            var damageType = int.TryParse(damageTypeText, out var parsedDamageType)
                ? (CombatDamageType)parsedDamageType
                : CombatDamageType.Physical;

            NotifyDamageStatusEffects(attacker, defender, damage, damageType);

            var effects = _statusEffect.GetCreatureStatusEffects(attacker);

            foreach (var effect in effects.GetAllOnHitEffects())
            {
                effect.OnHitEffect(attacker, defender, damage);
            }
        }

        public void NotifyDamageStatusEffects(
            uint attacker,
            uint defender,
            int damage,
            CombatDamageType damageType,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct)
        {
            if (damage <= 0 || !GetIsObjectValid(attacker) || !GetIsObjectValid(defender))
                return;

            foreach (var effect in _statusEffect.GetCreatureStatusEffects(attacker).GetAllEffects())
            {
                effect.OnDamageDealtEffect(attacker, defender, damage, damageType, deliveryType);
            }

            foreach (var effect in _statusEffect.GetCreatureStatusEffects(defender).GetAllEffects())
            {
                effect.OnDamageTakenEffect(defender, attacker, damage, damageType, deliveryType);
            }
        }

        /// <summary>
        /// Notifies status effects that must validate their state before an originating hit is
        /// applied. Ordinary damage reactions remain in NotifyDamageStatusEffects so they observe
        /// the defender's post-hit state whenever the damage path supports it.
        /// </summary>
        public void NotifyPreDamageStatusEffects(
            uint attacker,
            uint defender,
            int damage,
            CombatDamageType damageType,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct)
        {
            if (damage <= 0 || !GetIsObjectValid(attacker) || !GetIsObjectValid(defender))
                return;

            foreach (var effect in _statusEffect.GetCreatureStatusEffects(defender)
                         .GetAllEffects()
                         .OfType<IPreDamageStatusEffect>()
                         .ToList())
            {
                effect.OnBeforeDamageTaken(defender, attacker, damage, damageType, deliveryType);
            }
            _statusEffect.NotifyBeforeDirectDamage(attacker, defender, deliveryType);
        }

        /// <summary>
        /// Notifies limited-attack status effects once per originating hostile attack, including
        /// misses and deflections. Effects exhausted by the attempt are removed immediately so
        /// they cannot affect another attack before the next status-effect tick.
        /// </summary>
        public void NotifyAttackAttemptStatusEffects(
            uint attacker,
            SkillType skillType,
            AbilityImpactSummary abilityImpact = null)
        {
            if (!GetIsObjectValid(attacker))
                return;

            var effects = _statusEffect.GetCreatureStatusEffects(attacker)
                .GetAllEffects()
                .OfType<IAttackAttemptStatusEffect>()
                .ToList();

            foreach (var effect in effects)
            {
                effect.OnAttackAttemptedEffect(attacker, skillType, abilityImpact);
            }

            foreach (var effect in effects.Where(effect => effect.IsFlaggedForRemoval))
            {
                _statusEffect.RemoveStatusEffect(
                    attacker,
                    effect.GetType(),
                    effect.Source,
                    effect.SendsWornOffMessage);
            }
        }

        /// <summary>
        /// Marks the start of a native attack cycle. Limited attack-timing effects granted
        /// while its already-scheduled rolls resolve are deferred until the outermost cycle ends.
        /// Dual-wield cycles retain this scope across the gap between hands.
        /// </summary>
        public void BeginNativeAttackSwing(uint attacker)
        {
            _nativeAttackSwingDepth.TryGetValue(attacker, out var depth);
            _nativeAttackSwingDepth[attacker] = depth + 1;
        }

        /// <summary>
        /// Discards a destroyed attacker's open native attack cycle and its deferred effects without applying them,
        /// so a reused object ID never starts inside a stale cycle.
        /// </summary>
        public void ForgetDestroyedNativeAttackSwing(uint attacker)
        {
            _nativeAttackSwingDepth.Remove(attacker);
            _deferredNativeAttackStatusEffects.Remove(attacker);
        }

        /// <summary>
        /// Ends (or cancels) a native attack cycle and applies limited attack-timing effects
        /// granted while its precomputed rolls resolved.
        /// </summary>
        public void EndNativeAttackSwing(uint attacker)
        {
            if (!_nativeAttackSwingDepth.TryGetValue(attacker, out var depth))
                return;

            if (depth > 1)
            {
                _nativeAttackSwingDepth[attacker] = depth - 1;
                return;
            }

            _nativeAttackSwingDepth.Remove(attacker);
            if (!_deferredNativeAttackStatusEffects.Remove(attacker, out var deferredEffects))
                return;

            foreach (var applyDeferredEffect in deferredEffects)
            {
                applyDeferredEffect();
            }
        }

        /// <summary>
        /// Holds an application until the outermost native attack cycle of <paramref name="creature"/> ends
        /// (<see cref="IsDeferredNativeAttackStatusEffect"/> says whether it must wait); the deferred action runs then.
        /// </summary>
        public void DeferNativeAttackStatusEffect(uint creature, Action applyDeferredEffect)
        {
            if (!_deferredNativeAttackStatusEffects.TryGetValue(creature, out var deferredEffects))
            {
                deferredEffects = new List<Action>();
                _deferredNativeAttackStatusEffects[creature] = deferredEffects;
            }

            deferredEffects.Add(applyDeferredEffect);
        }

        /// <summary>Whether a limited attack-timing effect granted to <paramref name="creature"/> now must wait for its native attack cycle to end.</summary>
        public bool IsDeferredNativeAttackStatusEffect(IStatusEffect statusEffect, uint creature)
        {
            return (statusEffect is ILimitedAttackDelayReductionStatusEffect ||
                    statusEffect is ILimitedAttackNoDelayStatusEffect) &&
                   _nativeAttackSwingDepth.ContainsKey(creature);
        }

        /// <summary>
        /// Gets the attack-delay reduction and earliest remaining charge count supplied by active
        /// limited-attack effects that apply to the requested skill.
        /// </summary>
        public bool TryGetLimitedAttackDelayReduction(
            uint attacker,
            SkillType skillType,
            out int reductionPercent,
            out int remainingAttacks)
        {
            if (IsAttackDelayReductionSuppressed(attacker))
            {
                reductionPercent = 0;
                remainingAttacks = 0;
                return false;
            }

            var effects = _statusEffect.GetCreatureStatusEffects(attacker)
                .GetAllEffects()
                .OfType<ILimitedAttackDelayReductionStatusEffect>()
                .Where(effect => effect.AppliesToSkill(skillType) && effect.RemainingAttacks > 0)
                .ToList();

            reductionPercent = effects.Sum(effect => effect.AttackDelayReductionPercent);
            remainingAttacks = effects.Count == 0
                ? 0
                : effects.Min(effect => effect.RemainingAttacks);
            return effects.Count > 0 && reductionPercent > 0;
        }

        /// <summary>
        /// Gets the earliest remaining charge count supplied by active limited no-delay effects
        /// that apply to the requested attack skill.
        /// </summary>
        public bool TryGetLimitedAttackNoDelay(
            uint attacker,
            SkillType skillType,
            out int remainingAttacks)
        {
            if (IsAttackDelayReductionSuppressed(attacker))
            {
                remainingAttacks = 0;
                return false;
            }

            var effects = _statusEffect.GetCreatureStatusEffects(attacker)
                .GetAllEffects()
                .OfType<ILimitedAttackNoDelayStatusEffect>()
                .Where(effect => effect.AppliesToSkill(skillType) && effect.RemainingAttacks > 0)
                .ToList();

            remainingAttacks = effects.Count == 0
                ? 0
                : effects.Min(effect => effect.RemainingAttacks);
            return effects.Count > 0;
        }

        public void OnGuardedHit(uint defender, uint attacker, int preventedDamage)
        {
            if (!_statusEffect.TryGetCreatureStatusEffects(defender, out var creatureEffects))
                return;

            foreach (var effect in creatureEffects
                         .GetAllEffects()
                         .OfType<IGuardedHitStatusEffect>()
                         .ToList())
            {
                effect.OnGuardedHitEffect(defender, attacker, preventedDamage);
            }
        }

        protected virtual int GetCreatureStatAdjustment(uint creature, StatType stat) => _stat.Value.GetStatAdjustment(creature, stat);

        protected virtual bool IsAttackDelayReductionSuppressed(uint creature) => GetCreatureStatAdjustment(creature, StatType.AttackDelayReductionSuppressed) > 0;
    }
}
