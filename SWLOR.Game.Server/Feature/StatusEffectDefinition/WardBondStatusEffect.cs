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
    public sealed class WardBondStatusEffect : StatusEffectBase
    {
        private static readonly Dictionary<(uint Source, uint Attacker), DateTime> RecentWardHits = new();
        private readonly float _rangeMeters;

        public override string Name => "Ward Bond";
        public override EffectIconType Icon => EffectIconType.WardBondStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override float Frequency => 1f;
        public override bool PersistsOnLogout => false;

        public WardBondStatusEffect()
            : this(30, 6, 6, 0, 8.0f)
        {
        }

        public WardBondStatusEffect(
            int sharedDamagePercent,
            int physicalDefensePercent,
            int forceDefensePercent,
            int guard,
            float rangeMeters)
        {
            _rangeMeters = rangeMeters;
            StatGroup.Stats[StatType.DamageTakenShareToStatusSourcePercent] = sharedDamagePercent;
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = physicalDefensePercent;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = forceDefensePercent;
            if (guard > 0)
                StatGroup.Stats[StatType.Guard] = guard;
        }

        public override string CanApply(uint creature)
        {
            var hasWard = StatusEffect.HasStatusEffect(creature, typeof(WardBondStatusEffect));
            var hasGuarded = StatusEffect.HasStatusEffect(creature, typeof(GuardedStatusEffect));
            if (!hasWard && !hasGuarded)
                return string.Empty;

            if (!hasGuarded &&
                Source != OBJECT_INVALID &&
                StatusEffect.HasStatusEffect(creature, typeof(WardBondStatusEffect), Source))
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
                RecentWardHits[(Source, attacker)] = DateTime.UtcNow;

            var damageBonus = Stat.GetStatAdjustment(Source, StatType.WardSharedDamageNextSkillAbilityDamageBonus);
            var window = Stat.GetStatAdjustment(Source, StatType.WardSharedDamageNextSkillAbilityWindowSeconds);
            Combat.GrantNextSkillAbilityBonuses(Source, SkillType.Lightsaber, damageBonus, 0, window);
        }

        private bool IsSourceInRange(uint creature)
        {
            return GetIsObjectValid(Source) &&
                   !GetIsDead(Source) &&
                   GetArea(Source) == GetArea(creature) &&
                   GetDistanceBetween(Source, creature) <= _rangeMeters;
        }

        public static bool HasRecentWardHit(uint source, uint attacker, int windowSeconds)
        {
            if (!GetIsObjectValid(source) ||
                !GetIsObjectValid(attacker) ||
                windowSeconds <= 0 ||
                !RecentWardHits.TryGetValue((source, attacker), out var hitTime))
            {
                return false;
            }

            if ((DateTime.UtcNow - hitTime).TotalSeconds <= windowSeconds)
                return true;

            RecentWardHits.Remove((source, attacker));
            return false;
        }
    }
}
