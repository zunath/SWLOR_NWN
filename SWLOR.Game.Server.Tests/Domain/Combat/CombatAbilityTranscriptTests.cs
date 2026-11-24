using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.Game.Server.Tests.Support.CombatTranscriptBench;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The ordered call-and-draw transcripts of the ability orchestrators, taken before the services are split:
/// <c>ApplyAbilityActivatedEffects</c> (24 steps), <c>ApplySuccessfulAbilityImpactRiders</c>, <c>ApplyAbilityImpactEffects</c>, the
/// status-applied listener notices, <c>ApplyDamageDealtModifiers</c>, <c>TryResolveAbilityHit</c> and the ability bonus reads. Each
/// scenario runs with every stat at zero and again with every stat raised; the weapon-family riders run once per weapon skill.
/// Regenerate with <c>SWLOR_UPDATE_SNAPSHOTS=1</c> only for a deliberate change.
/// </summary>
[TestFixture]
[NonParallelizable]
public class CombatAbilityTranscriptTests
{
    private static readonly SkillType[] WeaponSkills =
    {
        SkillType.Vibroblade, SkillType.Vibroknife, SkillType.Lightsaber, SkillType.HeavyVibroblade, SkillType.Spear, SkillType.TwinBlade,
        SkillType.Saberstaff, SkillType.Katar, SkillType.Staff, SkillType.Pistol, SkillType.Rifle, SkillType.Throwing,
        SkillType.TwinPistol, SkillType.Unarmed, SkillType.Force
    };

    private static AbilityDetail Ability(SkillType skill, bool area = false, bool hostile = true) => new()
    {
        Name = "Transcript " + skill,
        SkillType = skill,
        IsHostileAbility = hostile,
        IsAreaAbility = area,
        IsSingleTargetAbility = !area,
        MaxRange = 10f
    };

    private static AbilityImpactSummary Summary(AbilityDetail ability, int targets) => new()
    {
        Ability = ability,
        SkillType = ability.SkillType,
        IsAreaAbility = ability.IsAreaAbility,
        IsSingleTargetAbility = ability.IsSingleTargetAbility,
        ImpactedTargetCount = targets,
        CriticalHitCount = 1,
        AttributedDamage = 90,
        ImpactedTargets = targets == 1 ? new[] { Defender } : new[] { Defender, Other, Ally }
    };

    [Test]
    public void TheAbilityActivationStepsProduceTheCapturedTranscript()
    {
        using var bench = new CombatTranscriptBench();
        var activation = bench.Get<ICombatAbilityActivationService>();

        foreach (var stat in new[] { 0, 5 })
        {
            bench.DefaultStat = stat;
            var tag = " (stats " + stat + ")";
            foreach (var skill in stat == 0 ? new[] { SkillType.Rifle } : WeaponSkills)
            {
                var ability = Ability(skill);
                bench.Step($"activate a hostile {skill} ability" + tag, () =>
                    activation.ApplyAbilityActivatedEffects(Attacker, Defender, FeatType.Invalid, ability, Summary(ability, 1)));
            }

            var area = Ability(SkillType.Rifle, area: true);
            bench.Step("activate a hostile area ability" + tag, () =>
                activation.ApplyAbilityActivatedEffects(Attacker, Defender, FeatType.Invalid, area, Summary(area, 3)));
            var friendly = Ability(SkillType.Force, hostile: false);
            bench.Step("activate a friendly ability" + tag, () =>
                activation.ApplyAbilityActivatedEffects(Attacker, Attacker, FeatType.Invalid, friendly, Summary(friendly, 1)));
            bench.Step("activate with no ability" + tag, () =>
                activation.ApplyAbilityActivatedEffects(Attacker, Defender, FeatType.Invalid, null, null));
            bench.Step("track a queued weapon ability" + tag, () => activation.TrackQueuedWeaponAbilityUse(Attacker, Ability(SkillType.Pistol)));
            bench.Step("spend hit points on an ability" + tag, () => bench.Get<ICombatAbilityCostEffectsService>().ApplyHitPointSpendAbilityEffects(Attacker, 30, 30));
            bench.Step("forget the activator" + tag, () => activation.ForgetActivationState(Attacker));
        }

        AssertGolden("SWLOR.Game.Server.Tests/Fixtures/CombatTranscripts/AbilityActivation.txt", bench.Text);
    }

    [Test]
    public void TheAbilityImpactOrchestratorsProduceTheCapturedTranscript()
    {
        using var bench = new CombatTranscriptBench();
        var impact = bench.Get<ICombatAbilityImpactService>();
        var listeners = new List<IStatusEffectAppliedListener>();
        bench.Leaf<IStatusEffectService>().Script(nameof(IStatusEffectService.RegisterAppliedListener), args =>
        {
            listeners.Add((IStatusEffectAppliedListener)args[0]!);
            return null;
        });

        foreach (var stat in new[] { 0, 5 })
        {
            bench.DefaultStat = stat;
            var tag = " (stats " + stat + ")";
            foreach (var skill in stat == 0 ? new[] { SkillType.Rifle } : WeaponSkills)
            {
                var ability = Ability(skill);
                bench.Step($"riders of a successful {skill} ability hit" + tag, () =>
                    impact.ApplySuccessfulAbilityImpactRiders(Attacker, Defender, ability, skill, CombatDamageType.Physical, 90, true,
                        typeof(ExposedStatusEffect), new[] { typeof(PoisonStatusEffect) }, true, true, StatusEffectCategory.Debuff | StatusEffectCategory.Bleeding));
            }

            var area = Ability(SkillType.Rifle, area: true);
            bench.Step("riders of an area hit that applied nothing" + tag, () =>
                impact.ApplySuccessfulAbilityImpactRiders(Attacker, Defender, area, SkillType.Rifle, CombatDamageType.Force, 40, false,
                    null, null, false, false, StatusEffectCategory.None, false, true));
            bench.Step("damage bonus before the hit" + tag, () =>
            {
                impact.GetAbilityImpactBaseDamageBonus(Attacker, Defender, Ability(SkillType.Lightsaber), SkillType.Lightsaber);
                impact.GetHighResourceAbilityDamageBonus(Attacker, Ability(SkillType.Force));
                impact.GetCostlyAbilityDamageBonus(Attacker, Ability(SkillType.Pistol), SkillType.Pistol);
            });
            foreach (var skill in new[] { SkillType.Rifle, SkillType.Spear, SkillType.Throwing })
            {
                var ability = Ability(skill, area: skill != SkillType.Spear);
                bench.Step($"impact effects of a {skill} ability" + tag, () => impact.ApplyAbilityImpactEffects(Attacker, Summary(ability, 3)));
            }

            bench.Step("area hit haste" + tag, () => impact.ApplyAreaHitHaste(Attacker, 3));
            bench.Step("spear disabler suppression" + tag, () => impact.ApplySpearDisablerSuppressionRiders(Attacker, Defender));
            bench.Step("is the target attacking an ally" + tag, () => impact.IsTargetAttackingAllyForStatusAppliedRiders(Attacker, Defender));
            bench.Step("hostile ability party buff" + tag, () => impact.ApplyHostileAbilityPartyBuff(Attacker, Ability(SkillType.Force), 30, true));
            bench.Step("forget the attacker" + tag, () => impact.ForgetImpactState(Attacker));
        }

        // The notices the status effect service sends to what registered at boot.
        bench.DefaultStat = 5;
        bench.Step("register the status-applied listener at boot", () => ((CombatAbilityImpactService)impact).RegisterStatusEffectListener());
        bench.Step("the listeners are notified of an applied status", () =>
        {
            foreach (var listener in listeners)
                listener.OnStatusApplied(Attacker, Defender, StatusEffectCategory.StaminaDrainTrigger | StatusEffectCategory.Debuff);
        });
        bench.Step("the listeners are notified of a hostile debuff", () =>
        {
            foreach (var listener in listeners)
                listener.OnHostileDebuffApplied(Attacker, Ability(SkillType.Force));
        });
        bench.Step("the listeners are notified of a completed application", () =>
        {
            foreach (var listener in listeners)
                listener.OnApplicationCompleted(Attacker, Defender, typeof(ExposedStatusEffect), Ability(SkillType.Force));
        });

        AssertGolden("SWLOR.Game.Server.Tests/Fixtures/CombatTranscripts/AbilityImpact.txt", bench.Text);
    }

    [Test]
    public void TheDamageModifierAndAbilityHitReadsProduceTheCapturedTranscript()
    {
        using var bench = new CombatTranscriptBench();
        var modifiers = bench.Get<ICombatDamageModifiersService>();
        var rate = bench.Get<ICombatRateModifiersService>();
        var repeated = bench.Get<ICombatRepeatedTargetService>();
        var hit = bench.Get<ICombatAbilityHitService>();
        var guardedEffects = bench.Get<ICombatGuardedHitEffectsService>();
        var bonus = bench.Get<ICombatAbilityBonusService>();

        foreach (var stat in new[] { 0, 5 })
        {
            bench.DefaultStat = stat;
            var tag = " (stats " + stat + ")";
            foreach (var skill in stat == 0 ? new[] { SkillType.Rifle } : WeaponSkills)
            {
                var ability = Ability(skill);
                bench.Step($"damage dealt by a {skill} ability" + tag, () =>
                    modifiers.ApplyDamageDealtModifiers(Attacker, Defender, 100, skill, CombatDamageType.Physical, true, true, true, ability));
            }

            bench.Step("damage dealt by an auto-attack" + tag, () =>
                modifiers.ApplyDamageDealtModifiers(Attacker, Defender, 100, SkillType.Vibroblade, CombatDamageType.Physical, false, true, true,
                    null, out _, 0, 0, true));
            bench.Step("damage dealt by area force damage" + tag, () =>
                modifiers.ApplyDamageDealtModifiers(Attacker, Defender, 100, SkillType.Force, CombatDamageType.Force, true, false, false, Ability(SkillType.Force, true)));
            bench.Step("critical damage" + tag, () => modifiers.ApplyCriticalDamageModifier(Attacker, 100, 120, SkillType.Rifle, Defender));
            bench.Step("critical mitigation" + tag, () => modifiers.CalculateDamageWithCriticalMitigation(Defender, 80, 40, 30, 60, 20, 120, 5));
            bench.Step("the cycle critical rate" + tag, () =>
            {
                rate.PrepareAutoAttackCycleCriticalRate(Attacker, SkillType.Rifle);
                rate.GetAutoAttackCriticalRateAdjustment(Attacker, Defender, 5);
                rate.GetTargetStatusCriticalRateAdjustment(Attacker, Defender);
                rate.ClearAutoAttackCycleCriticalRateTracker(Attacker);
            });
            bench.Step("the positional modifiers" + tag, () =>
            {
                rate.ApplySideAttackDamageModifier(Attacker, Defender, SkillType.Vibroknife, 100);
                rate.ApplyBackAttackDamageModifier(Attacker, Defender, SkillType.Vibroknife, 100);
                rate.GetSideAttackCriticalRateAdjustment(Attacker, Defender, SkillType.Vibroknife);
                rate.GetBackAttackCriticalRateAdjustment(Attacker, Defender, SkillType.Vibroknife);
            });
            bench.Step("repeated and same-target pressure" + tag, () =>
            {
                repeated.ApplySameTargetPressureDamageEffects(Attacker, Defender, SkillType.Rifle);
                repeated.GetSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle);
                repeated.ConsumeSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle, 50);
                repeated.ClearRangedRepeatedTargetDamageTracker(Attacker);
            });
            bench.Step("forget the attacker's modifier state" + tag, () => repeated.ForgetDamageModifierState(Attacker));

            foreach (var skill in new[] { SkillType.Rifle, SkillType.Force, SkillType.Lightsaber })
            {
                bench.Step($"ability hit roll of a {skill} ability" + tag, () => hit.TryResolveAbilityHit(Attacker, Defender, skill, PerkType.Invalid, out _));
                bench.Step($"ability hit roll of a {skill} ability with adjustments" + tag, () =>
                    hit.TryResolveAbilityHit(Attacker, Defender, skill, PerkType.Invalid, out _, 15, 20, AbilityType.Might, 8));
            }

            bench.Step("ability hit forced to hit" + tag, () =>
            {
                hit.SetAbilityHitResolutionOverride(true);
                try
                {
                    hit.TryResolveAbilityHit(Attacker, Defender, SkillType.Rifle, PerkType.Invalid, out _);
                }
                finally
                {
                    hit.SetAbilityHitResolutionOverride(null);
                }
            });
            bench.Step("ability hit forced to miss" + tag, () =>
            {
                hit.SetAbilityHitResolutionOverride(false);
                try
                {
                    hit.TryResolveAbilityHit(Attacker, Defender, SkillType.Rifle, PerkType.Invalid, out _);
                }
                finally
                {
                    hit.SetAbilityHitResolutionOverride(null);
                }
            });
            bench.Step("critical rating and adjustments of an ability" + tag, () =>
            {
                hit.CalculateAbilityCriticalRating(Attacker, SkillType.Rifle, false, 5, Defender);
                hit.GetAbilityCriticalRate(Attacker, SkillType.Rifle, true, 5, Defender);
                hit.GetHitChanceAgainstSunderedTargetAdjustment(Attacker, Defender);
                hit.GetCriticalRateAgainstSunderedTargetAdjustment(Attacker, Defender);
                hit.ApplyRangedAbilityTargetDefenseReduction(Attacker, Defender, SkillType.Rifle);
                guardedEffects.ApplyGuardedHitNextAttackEffects(Attacker);
            });
            bench.Step("ability bonus reads" + tag, () =>
            {
                bonus.ConsumeNextSkillAbilityBonuses(Attacker, SkillType.Rifle, StatusEffectCategory.Debuff);
                bonus.GetAbilityDamageBonus(Attacker, SkillType.Rifle);
                bonus.GetAbilityStatusCategoryDamageBonus(Attacker, SkillType.Rifle, StatusEffectCategory.Bleeding);
                bonus.GetAbilityDefenseIgnorePercentAdjustment(Attacker, PerkType.Invalid, SkillType.Rifle, Defender);
                bonus.ApplyAbilityRecastDelayModifiers(Attacker, Ability(SkillType.Rifle), 12f);
                bonus.GetAbilityDamageFlatAdjustment(Attacker, PerkType.Invalid, SkillType.Rifle);
                bonus.ConsumePersistentNextSkillAbilityCriticalRateBonus(Attacker, SkillType.Rifle);
            });
            bench.Step("ability bonus grants and the queued weapon ability bonuses" + tag, () =>
            {
                bonus.GrantNextAbilityDamageBonus(Attacker, 0, 10, 30);
                bonus.GrantNextSkillAbilityBonuses(Attacker, (int)SkillType.Rifle, 10, 5, 30);
                bonus.PrepareOpeningAutoAttack(Attacker, SkillType.Rifle);
                bonus.PrepareQueuedWeaponAbilityOpeningAttackAtActivation(Attacker, SkillType.Pistol);
                bonus.StoreQueuedWeaponAbilityCriticalRateBonus(Attacker, SkillType.Pistol, 7);
                bonus.StoreQueuedWeaponAbilityActivationIdleBonuses(Attacker, SkillType.Pistol);
                bonus.ConsumeQueuedWeaponAbilityBonuses(Attacker, SkillType.Pistol);
                bonus.ClearQueuedWeaponAbilityActivationBonuses(Attacker);
                bonus.ConsumeNextAttackGuardedHitBonuses(Attacker);
                bonus.ConsumeStatusAppliedNextAttackDamageBonus(Attacker);
            });
        }

        AssertGolden("SWLOR.Game.Server.Tests/Fixtures/CombatTranscripts/AbilityModifiersHitBonus.txt", bench.Text);
    }
}
