using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class IronGuardCoverStatusEffect : StatusEffectBase
    {
        private static readonly Dictionary<(uint Source, uint Attacker), DateTime> RecentGuardedAllyHits = new();
        private readonly float _rangeMeters;
        private readonly int _protectedHitDamageBonus;

        public override string Name => "Iron Guard Cover";
        public override EffectIconType Icon => EffectIconType.IronGuardCoverStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override float Frequency => 1f;
        public override bool PersistsOnLogout => false;

        public IronGuardCoverStatusEffect()
            : this(8, 6, 5.0f, 0)
        {
        }

        public IronGuardCoverStatusEffect(
            int guard,
            int physicalDefensePercent,
            float rangeMeters,
            int protectedHitDamageBonus = 0)
        {
            _rangeMeters = rangeMeters;
            _protectedHitDamageBonus = protectedHitDamageBonus;
            StatGroup.Stats[StatType.Guard] = guard;
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = physicalDefensePercent;
        }

        public override string CanApply(uint creature)
        {
            var hasWard = StatusEffect.HasStatusEffect(creature, typeof(WardBondStatusEffect));
            var hasIronGuard = StatusEffect.HasStatusEffect(creature, typeof(IronGuardCoverStatusEffect)) ||
                               StatusEffect.HasStatusEffect(creature, typeof(GuardedStatusEffect));
            if (!hasWard && !hasIronGuard)
                return string.Empty;

            if (!hasWard &&
                Source != OBJECT_INVALID &&
                StatusEffect.HasStatusEffect(creature, typeof(IronGuardCoverStatusEffect), Source))
            {
                return string.Empty;
            }

            return "Only one ward or guard link can protect a target.";
        }

        protected override void Tick(uint creature)
        {
            if (!IsSourceInRange(creature))
                IsFlaggedForRemoval = true;
        }

        protected override void OnDamageTaken(uint defender, uint attacker, int damage, CombatDamageType damageType)
        {
            if (!GetIsObjectValid(Source) || Source == defender || damage <= 0)
                return;

            if (GetIsObjectValid(attacker) && GetIsReactionTypeHostile(attacker, Source))
            {
                Enmity.ModifyEnmity(Source, attacker, Math.Max(1, damage));
                RecentGuardedAllyHits[(Source, attacker)] = DateTime.UtcNow;
            }

            var damageBonus = Math.Max(
                _protectedHitDamageBonus,
                Stat.GetStatAdjustment(Source, StatType.GuardedAllyHitNextSkillAbilityDamageBonus));
            var window = Stat.GetStatAdjustment(Source, StatType.GuardedAllyHitNextSkillAbilityWindowSeconds);
            Combat.GrantNextSkillAbilityBonuses(Source, SkillType.Katar, damageBonus, 0, window);
        }

        private bool IsSourceInRange(uint creature)
        {
            return GetIsObjectValid(Source) &&
                   !GetIsDead(Source) &&
                   GetArea(Source) == GetArea(creature) &&
                   GetDistanceBetween(Source, creature) <= _rangeMeters;
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
