using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class GuardedStatusEffect : StatusEffectBase, IGuardedHitStatusEffect
    {
        private const string GuardShareGroup = "IronGuard:GuardedShare";
        private const float GuardShareRefreshSeconds = 1.5f;

        private static readonly Dictionary<uint, HashSet<uint>> GuardedTargetsBySource = new();
        private static readonly Dictionary<uint, uint> SourceByGuardedTarget = new();
        private static readonly Dictionary<(uint Source, uint Attacker), DateTime> RecentGuardedAllyHits = new();

        private readonly int _guardSharePercent;
        private readonly float _rangeMeters;

        public override string Name => "Guarded";
        public override EffectIconType Icon => EffectIconType.GuardedStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override float Frequency => 1f;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;

        public GuardedStatusEffect()
            : this(50, 5.0f)
        {
        }

        public GuardedStatusEffect(int guardSharePercent, float rangeMeters)
        {
            _guardSharePercent = guardSharePercent;
            _rangeMeters = rangeMeters;
        }

        public override string CanApply(uint creature)
        {
            if (!GetIsObjectValid(Source))
                return "Guarding source is invalid.";

            if (Source == creature)
                return "You cannot guard yourself with Steel Shoulder.";

            if (!Party.IsInParty(Source, creature))
                return "Target must be in your party.";

            var hasWard = StatusEffect.HasStatusEffect(creature, typeof(WardBondStatusEffect));
            var hasGuarded = StatusEffect.HasStatusEffect(creature, typeof(GuardedStatusEffect));
            if (!hasWard && !hasGuarded)
                return string.Empty;

            if (!hasWard && StatusEffect.HasStatusEffect(creature, typeof(GuardedStatusEffect), Source))
                return string.Empty;

            return "Only one ward or guard link can protect a target.";
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            TrackGuardedTarget(Source, creature);
            StatusEffect.ApplyStatusEffect(Source, Source, typeof(GuardingStatusEffect), 0f);
            RefreshGuardBenefit(creature);
            SendGuardingMessages(creature);
        }

        protected override void Remove(uint creature)
        {
            ClearGuardBenefit(creature);
            UntrackGuardedTarget(Source, creature);

            if (GetIsObjectValid(Source) && !HasGuardedTarget(Source))
                StatusEffect.RemoveStatusEffect(Source, typeof(GuardingStatusEffect), Source, false);
        }

        protected override void Tick(uint creature)
        {
            if (!IsRelationshipValid(creature))
            {
                ClearGuardBenefit(creature);
                IsFlaggedForRemoval = true;
                return;
            }

            if (IsSourceInBenefitRange(creature))
            {
                RefreshGuardBenefit(creature);
            }
            else
            {
                ClearGuardBenefit(creature);
            }
        }

        protected override void OnDamageTaken(uint defender, uint attacker, int damage, CombatDamageType damageType)
        {
            if (!IsProtectionActive(defender) || damage <= 0)
                return;

            if (GetIsObjectValid(attacker) && GetIsReactionTypeHostile(attacker, Source))
            {
                Enmity.ModifyEnmity(Source, attacker, Math.Max(1, damage));
                RecentGuardedAllyHits[(Source, attacker)] = DateTime.UtcNow;
            }

            var damageBonus = Stat.GetStatAdjustment(Source, StatType.GuardedAllyHitNextSkillAbilityDamageBonus);
            var window = Stat.GetStatAdjustment(Source, StatType.GuardedAllyHitNextSkillAbilityWindowSeconds);
            Combat.GrantNextSkillAbilityBonuses(Source, SkillType.Katar, damageBonus, 0, window);
            Combat.ApplyLowHPGuardEffectFromProtectedTarget(Source, defender, damage);
        }

        public void OnGuardedHitEffect(uint defender, uint attacker, int preventedDamage)
        {
            if (!IsProtectionActive(defender))
                return;

            Combat.TrackGuardedHit(Source);
        }

        private void RefreshGuardBenefit(uint creature)
        {
            var guard = GetShareableGuard(Source);
            var sharedGuard = Math.Max(0, guard * _guardSharePercent / 100);

            if (sharedGuard <= 0)
            {
                ClearGuardBenefit(creature);
                return;
            }

            TemporaryStatModifier.Replace(
                creature,
                StatType.Guard,
                sharedGuard,
                GuardShareRefreshSeconds,
                GuardShareGroup);
        }

        private static int GetShareableGuard(uint source)
        {
            return Math.Max(
                0,
                Stat.GetStatAdjustment(source, StatType.Guard) -
                TemporaryStatModifier.GetStatAdjustment(source, StatType.Guard, GuardShareGroup));
        }

        private static void ClearGuardBenefit(uint creature)
        {
            TemporaryStatModifier.Consume(creature, StatType.Guard, GuardShareGroup);
        }

        private bool IsProtectionActive(uint creature)
        {
            return IsRelationshipValid(creature) && IsSourceInBenefitRange(creature);
        }

        private bool IsRelationshipValid(uint creature)
        {
            return GetIsObjectValid(Source) &&
                   GetIsObjectValid(creature) &&
                   Source != creature &&
                   !GetIsDead(Source) &&
                   !GetIsDead(creature) &&
                   Party.IsInParty(Source, creature);
        }

        private bool IsSourceInBenefitRange(uint creature)
        {
            return GetArea(Source) == GetArea(creature) &&
                   GetDistanceBetween(Source, creature) <= _rangeMeters;
        }

        private static void SendGuardingMessages(uint guarded)
        {
            var source = SourceByGuardedTarget.TryGetValue(guarded, out var guarding)
                ? guarding
                : OBJECT_INVALID;
            if (!GetIsObjectValid(source))
                return;

            if (GetIsPC(guarded))
            {
                var sourceName = PlayerName.GetColoredDisplayName(guarded, source);
                SendMessageToPC(guarded, ColorToken.Combat($"{sourceName} is guarding you."));
            }

            if (GetIsPC(source))
            {
                var guardedName = PlayerName.GetColoredDisplayName(source, guarded);
                SendMessageToPC(source, ColorToken.Combat($"You are guarding {guardedName}."));
            }
        }

        private static void TrackGuardedTarget(uint source, uint target)
        {
            if (!GuardedTargetsBySource.TryGetValue(source, out var targets))
            {
                targets = new HashSet<uint>();
                GuardedTargetsBySource[source] = targets;
            }

            targets.Add(target);
            SourceByGuardedTarget[target] = source;
        }

        private static void UntrackGuardedTarget(uint source, uint target)
        {
            if (GuardedTargetsBySource.TryGetValue(source, out var targets))
            {
                targets.Remove(target);
                if (targets.Count <= 0)
                    GuardedTargetsBySource.Remove(source);
            }

            if (SourceByGuardedTarget.TryGetValue(target, out var trackedSource) && trackedSource == source)
                SourceByGuardedTarget.Remove(target);
        }

        public static bool HasGuardedTarget(uint source)
        {
            return GuardedTargetsBySource.TryGetValue(source, out var targets) && targets.Count > 0;
        }

        public static bool IsGuardedBySource(uint target, uint source)
        {
            return GetIsObjectValid(target) &&
                   GetIsObjectValid(source) &&
                   StatusEffect.HasStatusEffect(target, typeof(GuardedStatusEffect), source);
        }

        public static bool IsActiveGuardedBySource(uint target, uint source)
        {
            var effect = StatusEffect.GetStatusEffect<GuardedStatusEffect>(target);
            return effect != null &&
                   effect.Source == source &&
                   effect.IsProtectionActive(target);
        }

        public static uint GetActiveGuardedTarget(uint source)
        {
            if (!GuardedTargetsBySource.TryGetValue(source, out var targets))
                return OBJECT_INVALID;

            foreach (var target in targets)
            {
                if (IsActiveGuardedBySource(target, source))
                    return target;
            }

            return OBJECT_INVALID;
        }

        public static uint GetActiveGuardSource(uint target)
        {
            if (!SourceByGuardedTarget.TryGetValue(target, out var source))
                return OBJECT_INVALID;

            return IsActiveGuardedBySource(target, source)
                ? source
                : OBJECT_INVALID;
        }

        public static void RefreshGuardBenefitsFromSource(uint source)
        {
            if (!GuardedTargetsBySource.TryGetValue(source, out var targets))
                return;

            foreach (var target in targets.ToList())
            {
                var effect = StatusEffect.GetStatusEffect<GuardedStatusEffect>(target);
                if (effect?.Source != source)
                    continue;

                if (effect.IsProtectionActive(target))
                    effect.RefreshGuardBenefit(target);
                else
                    ClearGuardBenefit(target);
            }
        }

        public static bool HasRecentGuardedAllyHit(uint source, uint attacker, int windowSeconds)
        {
            if (!GetIsObjectValid(source) ||
                !GetIsObjectValid(attacker) ||
                windowSeconds <= 0 ||
                !RecentGuardedAllyHits.TryGetValue((source, attacker), out var hitTime))
            {
                return false;
            }

            if ((DateTime.UtcNow - hitTime).TotalSeconds <= windowSeconds)
                return true;

            RecentGuardedAllyHits.Remove((source, attacker));
            return false;
        }
    }
}
