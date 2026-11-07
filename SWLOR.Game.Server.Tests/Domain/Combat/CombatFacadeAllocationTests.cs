using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;
using CombatFacade = SWLOR.Game.Server.Service.Combat;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The combat services run per attack and per damage event, so a facade call that only reads a table must not allocate:
/// the facade resolves its instance from a cache and the instance reads its own fields. These tests count the bytes the
/// current thread allocates over many calls.
/// </summary>
[TestFixture]
[NonParallelizable]
public class CombatFacadeAllocationTests
{
    private const int Calls = 20000;
    private const uint Attacker = 501;
    private const uint Defender = 502;

    private FacadeContainer _container = null!;
    private IDisposable _installed = null!;

    // The stat service answers its calls into unconverted code without allocating, so the part reads count only Combat.
    [OneTimeSetUp]
    public void BuildContainer() => _container = new FacadeContainer(services =>
        services.Replace(ServiceDescriptor.Singleton<IStatService, AllocationStatService>()));

    [OneTimeTearDown]
    public void DisposeContainer() => _container.Dispose();

    [SetUp]
    public void SetUp() => _installed = _container.Install();

    [TearDown]
    public void TearDown() => _installed.Dispose();

    private static long Allocated(Action call)
    {
        for (var i = 0; i < 100; i++)
            call();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < Calls; i++)
            call();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Test]
    public void TheEnmityReadsAndRulesAllocateNothing()
    {
        var sink = 0;

        Allocated(() => sink += Enmity.HasEnmity(5) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += Enmity.IsThreatSecured(100, 5) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += Enmity.HasProximityEnmity(5, 100) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += Enmity.HasNonProximityEnmity(5, 100) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += Enmity.CalculateDamageEnmity(40, 25)).Should().Be(0);
        Allocated(() => sink += Enmity.CalculateAdjustedEnmity(40, 25)).Should().Be(0);
        Allocated(() => sink += Enmity.CalculateHealingEnmity(40, 10, 60)).Should().Be(0);
        Allocated(() => sink += Enmity.CalculateReducedEnmity(40, 25)).Should().Be(0);
        GC.KeepAlive(sink);
    }

    [Test]
    public void TheCombatPointReadsAllocateNothing()
    {
        var sink = 0;

        Allocated(() => sink += CombatPoint.HasCombatPoints(5, 100) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += CombatPoint.GetTaggedCreatureCount(5)).Should().Be(0);
        GC.KeepAlive(sink);
    }

    [Test]
    public void TheRecastNameAndVisibilityReadsAllocateNothing()
    {
        Recast.CacheRecastGroups();
        var sink = 0;

        Allocated(() => sink += Recast.IsRecastGroupVisible(RecastGroup.Flash) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += Recast.GetRecastGroupName(RecastGroup.Flash).Length).Should().Be(0);
        Allocated(() => sink += Recast.GetRecastGroupDisplayName(RecastGroup.Flash).Length).Should().Be(0);
        GC.KeepAlive(sink);
    }

    [Test]
    public void TheCombatRulesAllocateNothingDirectlyOrThroughTheCombatForwarders()
    {
        var sink = 0;
        var debt = 0f;
        var reads = new (string Name, Action Rule, Action Forwarder)[]
        {
            ("CalculateHitRate", () => sink += CombatRules.CalculateHitRate(120, 100, 0), () => sink += CombatFacade.CalculateHitRate(120, 100, 0)),
            ("CalculateCriticalRate", () => sink += CombatRules.CalculateCriticalRate(30, 10, 50, 2), () => sink += CombatFacade.CalculateCriticalRate(30, 10, 50, 2)),
            ("CalculateAbilityCriticalChance", () => sink += CombatRules.CalculateAbilityCriticalChance(10), () => sink += CombatFacade.CalculateAbilityCriticalChance(10)),
            ("ClampCriticalDamagePercentAdjustment", () => sink += CombatRules.ClampCriticalDamagePercentAdjustment(250), () => sink += CombatFacade.ClampCriticalDamagePercentAdjustment(250)),
            ("ApplyPercentDamageAdjustment", () => sink += CombatRules.ApplyPercentDamageAdjustment(100, -30), () => sink += CombatFacade.ApplyPercentDamageAdjustment(100, -30)),
            ("ApplyOrdinaryAttackDamageMultiplier", () => sink += CombatRules.ApplyOrdinaryAttackDamageMultiplier(100, 25), () => sink += CombatFacade.ApplyOrdinaryAttackDamageMultiplier(100, 25)),
            ("CapOutgoingDamageBonus", () => sink += CombatRules.CapOutgoingDamageBonus(100, 250, 5), () => sink += CombatFacade.CapOutgoingDamageBonus(100, 250, 5)),
            ("ApplyCombinedDamageTakenAdjustment", () => sink += CombatRules.ApplyCombinedDamageTakenAdjustment(100, -50, -50), () => sink += CombatFacade.ApplyCombinedDamageTakenAdjustment(100, -50, -50)),
            ("ApplyTargetHPDamageAdjustment", () => sink += CombatRules.ApplyTargetHPDamageAdjustment(100, 20, 100, 25, 50), () => sink += CombatFacade.ApplyTargetHPDamageAdjustment(100, 20, 100, 25, 50)),
            ("ApplyDefenseIgnore", () => sink += CombatRules.ApplyDefenseIgnore(100, 25), () => sink += CombatFacade.ApplyDefenseIgnore(100, 25)),
            ("ApplyTriggeredDamageTargetAdjustment", () => sink += CombatRules.ApplyTriggeredDamageTargetAdjustment(100, -50, null).Damage, () => sink += CombatFacade.ApplyTriggeredDamageTargetAdjustment(100, -50, null).Damage),
            ("GetIncomingPhysicalToForceConversionPortion", () => sink += CombatRules.GetIncomingPhysicalToForceConversionPortion(100, 30), () => sink += CombatFacade.GetIncomingPhysicalToForceConversionPortion(100, 30)),
            ("CalculateMaxHPHealingBudget", () => sink += CombatRules.CalculateMaxHPHealingBudget(200, 15), () => sink += CombatFacade.CalculateMaxHPHealingBudget(200, 15)),
            ("CalculateDamageEligibleForHealing", () => sink += CombatRules.CalculateDamageEligibleForHealing(50, 30, 5), () => sink += CombatFacade.CalculateDamageEligibleForHealing(50, 30, 5)),
            ("CalculateCappedDamageDerivedHealingAmount", () => sink += CombatRules.CalculateCappedDamageDerivedHealingAmount(100, 20, 50), () => sink += CombatFacade.CalculateCappedDamageDerivedHealingAmount(100, 20, 50)),
            ("CalculateHitPointsSpentThroughTemporaryHitPoints", () => sink += CombatRules.CalculateHitPointsSpentThroughTemporaryHitPoints(50, 20), () => sink += CombatFacade.CalculateHitPointsSpentThroughTemporaryHitPoints(50, 20)),
            ("CalculateHitPointSpendStaminaRestore", () => sink += CombatRules.CalculateHitPointSpendStaminaRestore(100, 10, 10, 20), () => sink += CombatFacade.CalculateHitPointSpendStaminaRestore(100, 10, 10, 20)),
            ("CalculateAbilityHitStaminaRestore", () => sink += CombatRules.CalculateAbilityHitStaminaRestore(10, 2, 20), () => sink += CombatFacade.CalculateAbilityHitStaminaRestore(10, 2, 20)),
            ("GetRangedDeflectionReflectionAmount", () => sink += CombatRules.GetRangedDeflectionReflectionAmount(100, 50, 40, 100), () => sink += CombatFacade.GetRangedDeflectionReflectionAmount(100, 50, 40, 100)),
            ("GetRangedDeflectionReflectionRates", () => sink += CombatRules.GetRangedDeflectionReflectionRates(30, 50, 5, 3, 10, 0, 0).ReflectPercent, () => sink += CombatFacade.GetRangedDeflectionReflectionRates(30, 50, 5, 3, 10, 0, 0).ReflectPercent),
            ("IsRangedDamageSkill", () => sink += CombatRules.IsRangedDamageSkill(SkillType.Devices) ? 1 : 0, () => sink += CombatFacade.IsRangedDamageSkill(SkillType.Devices) ? 1 : 0),
            ("IsRangedWeaponSkill", () => sink += CombatRules.IsRangedWeaponSkill(SkillType.Rifle) ? 1 : 0, () => sink += CombatFacade.IsRangedWeaponSkill(SkillType.Rifle) ? 1 : 0),
            ("GetWeaponEngagementRange", () => sink += (int)CombatRules.GetWeaponEngagementRange(SkillType.Rifle), () => sink += (int)CombatFacade.GetWeaponEngagementRange(SkillType.Rifle)),
            ("MatchesNextSkillAbilityCriticalRateSkill", () => sink += CombatRules.MatchesNextSkillAbilityCriticalRateSkill(SkillType.Rifle, SkillType.Invalid, true) ? 1 : 0, () => sink += CombatFacade.MatchesNextSkillAbilityCriticalRateSkill(SkillType.Rifle, SkillType.Invalid, true) ? 1 : 0),
            ("AbilityUsedTriggerSkillMatches", () => sink += CombatRules.AbilityUsedTriggerSkillMatches(SkillType.Pistol, SkillType.Invalid, true, true) ? 1 : 0, () => sink += CombatFacade.AbilityUsedTriggerSkillMatches(SkillType.Pistol, SkillType.Invalid, true, true) ? 1 : 0),
            ("CalculateAttackDelayMilliseconds", () => sink += CombatRules.CalculateAttackDelayMilliseconds(220, 180, 20, 10), () => sink += CombatFacade.CalculateAttackDelayMilliseconds(220, 180, 20, 10)),
            ("CalculateEffectiveAttackDelay", () => sink += CombatRules.CalculateEffectiveAttackDelay(2400), () => sink += CombatFacade.CalculateEffectiveAttackDelay(2400)),
            ("CalculateEffectiveAttackDelay(bool)", () => sink += CombatRules.CalculateEffectiveAttackDelay(2400, true), () => sink += CombatFacade.CalculateEffectiveAttackDelay(2400, true)),
            ("CalculateAttackSwingDelay", () => sink += CombatRules.CalculateAttackSwingDelay(900), () => sink += CombatFacade.CalculateAttackSwingDelay(900)),
            ("CalculateAttacksPerSwing", () => sink += CombatRules.CalculateAttacksPerSwing(700, debt, out debt), () => sink += CombatFacade.CalculateAttacksPerSwing(700, debt, out debt)),
            ("CapAttacksPerSwingForLimitedAttackEffect", () => sink += CombatRules.CapAttacksPerSwingForLimitedAttackEffect(3, 1, 2, 1), () => sink += CombatFacade.CapAttacksPerSwingForLimitedAttackEffect(3, 1, 2, 1)),
        };

        foreach (var (name, rule, forwarder) in reads)
        {
            Allocated(rule).Should().Be(0, $"CombatRules.{name} runs per attack");
            Allocated(forwarder).Should().Be(0, $"Combat.{name} only forwards");
        }

        GC.KeepAlive(sink);
        GC.KeepAlive(debt);
    }

    [Test]
    public void AForwarderAddsNothingWhereTheRuleItselfAllocates()
    {
        // CalculateDamageRange formats its Log.Write lines and IsWeaponSkillType reads the skill's attribute by
        // reflection, on every call, as before the move; the forwarder must not add to that.
        var sink = 0;

        var (rangeRule, rangeForwarder) = InterleavedPerCall(
            () => sink += CombatRules.CalculateDamageRange(100, 100, 20, 100, 20, 2).Item2,
            () => sink += CombatFacade.CalculateDamageRange(100, 100, 20, 100, 20, 2).Item2);
        ForwarderAddsNothing("CalculateDamageRange", rangeRule, rangeForwarder, 32);
        var (skillRule, skillForwarder) = InterleavedPerCall(
            () => sink += CombatRules.IsWeaponSkillType(SkillType.Rifle) ? 1 : 0,
            () => sink += CombatFacade.IsWeaponSkillType(SkillType.Rifle) ? 1 : 0);
        ForwarderAddsNothing("IsWeaponSkillType", skillRule, skillForwarder, 32);
        GC.KeepAlive(sink);
    }

    [Test]
    public void TheDamageTypeTableReadsAllocateNothing()
    {
        CombatFacade.LoadDamageTypes();
        var sink = 0;

        Allocated(() => sink += CombatFacade.GetDefenseDamageTypes().Count).Should().Be(0);
        Allocated(() => sink += CombatFacade.GetAllDamageTypes().Count).Should().Be(0);
        Allocated(() => sink += CombatFacade.GetDefenseDamageTypes()[0] == CombatDamageType.Physical ? 1 : 0).Should().Be(0);
        GC.KeepAlive(sink);
    }

    // The least of three runs, so a one-off allocation of the runtime (tiering, a lazily built table) cannot fail a pin;
    // an allocation per call shows in every run.
    private static long LeastAllocated(Action call) => Enumerable.Range(0, 3).Min(_ => Allocated(call));

    // Bytes per call, from the least of three runs.
    private static long PerCall(Action call) => LeastAllocated(call) / Calls;

    private sealed record PartReads(ICombatQueriesService Queries, ICombatResourcesService Resources, ICombatFeedbackService Feedback,
        AbilityDetail Hostile, AbilityDetail Plain);

    // The parts of the container with an engine that answers from constants and records nothing, and a stat service whose
    // calls into unconverted code allocate nothing, so what is counted is Combat's forwarder and the part.
    private PartReads InstallPartReads(List<IDisposable> scopes)
    {
        scopes.Add(NWScriptRuntime.Override(StatReadEngine.Create(StatReadEngine.NpcAnswers())));
        var hostile = new AbilityDetail { IsHostileAbility = true, SkillType = SkillType.Rifle };
        hostile.Requirements.Add(new AbilityRequirementStamina(10));
        return new PartReads(_container.Get<ICombatQueriesService>(), _container.Get<ICombatResourcesService>(),
            _container.Get<ICombatFeedbackService>(), hostile, new AbilityDetail { SkillType = SkillType.Force });
    }

    [Test]
    public void TheQueryAndResourceReadsAllocateNothingDirectlyOrThroughTheCombatForwarders()
    {
        var scopes = new List<IDisposable>();
        try
        {
            var (queries, resources, _, hostile, plain) = InstallPartReads(scopes);
            var sink = 0;
            var reads = new (string Name, Action Part, Action Forwarder)[]
            {
                ("GetMinimumHitRate", () => sink += queries.GetMinimumHitRate(Attacker), () => sink += CombatFacade.GetMinimumHitRate(Attacker)),
                ("IsAttackerBehindTarget", () => sink += queries.IsAttackerBehindTarget(Attacker, Defender) ? 1 : 0, () => sink += CombatFacade.IsAttackerBehindTarget(Attacker, Defender) ? 1 : 0),
                ("IsAttackerBesideTarget", () => sink += queries.IsAttackerBesideTarget(Attacker, Defender) ? 1 : 0, () => sink += CombatFacade.IsAttackerBesideTarget(Attacker, Defender) ? 1 : 0),
                ("IsTargetNotFacingAttacker", () => sink += queries.IsTargetNotFacingAttacker(Attacker, Defender) ? 1 : 0, () => sink += CombatFacade.IsTargetNotFacingAttacker(Attacker, Defender) ? 1 : 0),
                ("HasDamageImmunity", () => sink += queries.HasDamageImmunity(Defender, CombatDamageType.Physical) ? 1 : 0, () => sink += CombatFacade.HasDamageImmunity(Defender, CombatDamageType.Physical) ? 1 : 0),
                ("GetRangedAttackDamageFlatAdjustment", () => sink += queries.GetRangedAttackDamageFlatAdjustment(Attacker, SkillType.Rifle), () => sink += CombatFacade.GetRangedAttackDamageFlatAdjustment(Attacker, SkillType.Rifle)),
                ("GetRangedAttackDefenseIgnorePercentAdjustment", () => sink += queries.GetRangedAttackDefenseIgnorePercentAdjustment(Attacker, SkillType.Rifle), () => sink += CombatFacade.GetRangedAttackDefenseIgnorePercentAdjustment(Attacker, SkillType.Rifle)),
                ("GetPowerAttackDMGBonus", () => sink += queries.GetPowerAttackDMGBonus(Attacker), () => sink += CombatFacade.GetPowerAttackDMGBonus(Attacker)),
                ("GetAbilitySkillType", () => sink += (int)queries.GetAbilitySkillType(Attacker, hostile), () => sink += (int)CombatFacade.GetAbilitySkillType(Attacker, hostile)),
                ("GetEquippedWeaponSkillType", () => sink += (int)queries.GetEquippedWeaponSkillType(Attacker), () => sink += (int)CombatFacade.GetEquippedWeaponSkillType(Attacker)),
                ("HasEquippedWeaponForAbility", () => sink += queries.HasEquippedWeaponForAbility(Attacker) ? 1 : 0, () => sink += CombatFacade.HasEquippedWeaponForAbility(Attacker) ? 1 : 0),
                ("GetAbilityAttackWeapon", () => sink += (int)queries.GetAbilityAttackWeapon(Attacker, SkillType.Rifle), () => sink += (int)CombatFacade.GetAbilityAttackWeapon(Attacker, SkillType.Rifle)),
                ("GetWeaponDamageAbilityType", () => sink += (int)queries.GetWeaponDamageAbilityType(Attacker, BaseItem.Longsword), () => sink += (int)CombatFacade.GetWeaponDamageAbilityType(Attacker, BaseItem.Longsword)),
                ("GetWeaponAccuracyAbilityType", () => sink += (int)queries.GetWeaponAccuracyAbilityType(Attacker, BaseItem.Longsword), () => sink += (int)CombatFacade.GetWeaponAccuracyAbilityType(Attacker, BaseItem.Longsword)),
                ("GetMiscDMGBonus", () => sink += queries.GetMiscDMGBonus(Attacker, BaseItem.Longsword), () => sink += CombatFacade.GetMiscDMGBonus(Attacker, BaseItem.Longsword)),
                ("GetPerkAdjustedAbilityScore", () => sink += queries.GetPerkAdjustedAbilityScore(Attacker), () => sink += CombatFacade.GetPerkAdjustedAbilityScore(Attacker)),
                ("GetWeaponImpactProfile", () => sink += queries.GetWeaponImpactProfile(Attacker) == null ? 1 : 0, () => sink += WeaponImpactProfile.Get(Attacker) == null ? 1 : 0),
                ("GetAbilityStaminaPaid", () => sink += resources.GetAbilityStaminaPaid(Attacker, hostile), () => sink += CombatFacade.GetAbilityStaminaPaid(Attacker, hostile)),
                ("GetPaidAbilityStaminaCost", () => sink += resources.GetPaidAbilityStaminaCost(Attacker, hostile), () => sink += CombatFacade.GetPaidAbilityStaminaCost(Attacker, hostile)),
                ("GetNextAbilityFPCostAdjustment", () => sink += resources.GetNextAbilityFPCostAdjustment(Attacker, SkillType.Invalid), () => sink += CombatFacade.GetNextAbilityFPCostAdjustment(Attacker, SkillType.Invalid)),
                ("RestoreAbilityHitStamina", () => sink += resources.RestoreAbilityHitStamina(Attacker, hostile, 0), () => sink += CombatFacade.RestoreAbilityHitStamina(Attacker, hostile, 0)),
                ("DeferAbilityStaminaCostContext", () => resources.DeferAbilityStaminaCostContext(Attacker, hostile), () => CombatFacade.DeferAbilityStaminaCostContext(Attacker, hostile)),
                ("CompleteAbilityStaminaCostContext", () => resources.CompleteAbilityStaminaCostContext(Attacker, hostile), () => CombatFacade.CompleteAbilityStaminaCostContext(Attacker, hostile)),
                ("CompleteDeferredAbilityStaminaCostContext", () => resources.CompleteDeferredAbilityStaminaCostContext(Attacker, hostile), () => CombatFacade.CompleteDeferredAbilityStaminaCostContext(Attacker, hostile)),
                ("ApplyDamageDerivedHealing", () => sink += resources.ApplyDamageDerivedHealing(Attacker, 10, 10), () => sink += CombatFacade.ApplyDamageDerivedHealing(Attacker, 10, 10)),
                ("ApplyAbilityFPCostStaminaRestore", () => resources.ApplyAbilityFPCostStaminaRestore(Attacker, plain, 0), () => CombatFacade.ApplyAbilityFPCostStaminaRestore(Attacker, plain, 0)),
            };

            foreach (var (name, part, forwarder) in reads)
            {
                LeastAllocated(part).Should().Be(0, $"{name} runs per attack or per hit");
                LeastAllocated(forwarder).Should().Be(0, $"Combat.{name} only forwards");
            }

            GC.KeepAlive(sink);
        }
        finally
        {
            foreach (var scope in scopes)
                scope.Dispose();
        }
    }

    [Test]
    public void APartForwarderAddsNothingWhereTheMemberItselfAllocates()
    {
        // As on the base: IsWeaponSkillType reads the skill's attribute by reflection, the NPC stat reads build their
        // NPCStats, TemporaryStatModifier.GetStatAdjustment builds a closure, RestoreAbilityHitStamina's OfType().Any()
        // builds an iterator and the log lines build strings. A healing scope builds its state and its scope object, which
        // now also holds the service that began it (8 bytes more than the base's).
        var scopes = new List<IDisposable>();
        try
        {
            var (queries, resources, feedback, hostile, _) = InstallPartReads(scopes);
            var sink = 0;
            var reads = new (string Name, Action Part, Action Forwarder)[]
            {
                ("CanItemTriggerWeaponAbility", () => sink += queries.CanItemTriggerWeaponAbility(0x7F000000, SkillType.Force) ? 1 : 0, () => sink += CombatFacade.CanItemTriggerWeaponAbility(0x7F000000, SkillType.Force) ? 1 : 0),
                ("IncludesCombatImpactWeaponDamage", () => sink += queries.IncludesCombatImpactWeaponDamage(SkillType.Rifle, false) ? 1 : 0, () => sink += CombatFacade.IncludesCombatImpactWeaponDamage(SkillType.Rifle, false) ? 1 : 0),
                ("GetQueuedAbilityAccuracyAbilityType", () => sink += (int)queries.GetQueuedAbilityAccuracyAbilityType(Attacker, SkillType.Rifle), () => sink += (int)CombatFacade.GetQueuedAbilityAccuracyAbilityType(Attacker, SkillType.Rifle)),
                ("GetCombatImpactWeaponDamage", () => sink += queries.GetCombatImpactWeaponDamage(Attacker, SkillType.Force), () => sink += CombatFacade.GetCombatImpactWeaponDamage(Attacker, SkillType.Force)),
                ("IsCurrentFPAndStaminaAtOrBelowPercent", () => sink += resources.IsCurrentFPAndStaminaAtOrBelowPercent(Attacker, 50) ? 1 : 0, () => sink += CombatFacade.IsCurrentFPAndStaminaAtOrBelowPercent(Attacker, 50) ? 1 : 0),
                ("IsCurrentFPAndStaminaAbovePercent", () => sink += resources.IsCurrentFPAndStaminaAbovePercent(Attacker, 50, hostile) ? 1 : 0, () => sink += CombatFacade.IsCurrentFPAndStaminaAbovePercent(Attacker, 50, hostile) ? 1 : 0),
                ("GetNextAbilityFPCostAdjustment", () => sink += resources.GetNextAbilityFPCostAdjustment(Attacker, SkillType.Force), () => sink += CombatFacade.GetNextAbilityFPCostAdjustment(Attacker, SkillType.Force)),
                ("RestoreAbilityHitStamina", () => sink += resources.RestoreAbilityHitStamina(Attacker, hostile, 5), () => sink += CombatFacade.RestoreAbilityHitStamina(Attacker, hostile, 5)),
                ("BeginDamageDerivedHealing", () => resources.BeginDamageDerivedHealing(Attacker, Defender).Dispose(), () => CombatFacade.BeginDamageDerivedHealing(Attacker, Defender).Dispose()),
                ("BuildCombatLogMessage", () => sink += feedback.BuildCombatLogMessage(Attacker, Attacker, Defender, 1, 50).Length, () => sink += CombatFacade.BuildCombatLogMessage(Attacker, Attacker, Defender, 1, 50).Length),
                ("BuildAbilityCombatLogMessage", () => sink += feedback.BuildAbilityCombatLogMessage(Attacker, Attacker, Defender, "Shot", 3, 50).Length, () => sink += CombatFacade.BuildAbilityCombatLogMessage(Attacker, Attacker, Defender, "Shot", 3, 50).Length),
                ("BuildAbilityNoTargetCombatLogMessage", () => sink += feedback.BuildAbilityNoTargetCombatLogMessage(Attacker, Attacker, "Shot").Length, () => sink += CombatFacade.BuildAbilityNoTargetCombatLogMessage(Attacker, Attacker, "Shot").Length),
                ("BuildAbilityCriticalHitCombatLogMessage", () => sink += feedback.BuildAbilityCriticalHitCombatLogMessage(Attacker, Attacker, Defender, "Shot").Length, () => sink += CombatFacade.BuildAbilityCriticalHitCombatLogMessage(Attacker, Attacker, Defender, "Shot").Length),
            };

            foreach (var (name, part, forwarder) in reads)
            {
                // Interleaved and checked on the upper side only: these reads moved by 6 to 24 bytes between separate measurements.
                var (own, forwarded) = InterleavedPerCall(part, forwarder);
                own.Should().BeGreaterThan(0, $"{name} allocated before the move too");
                ForwarderAddsNothing(name, own, forwarded, 32);
            }

            GC.KeepAlive(sink);
        }
        finally
        {
            foreach (var scope in scopes)
                scope.Dispose();
        }
    }

    private static readonly DateTime FrozenNow = new(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);

    private sealed record TimingParts(ICombatAttackTimingService Timing, ICombatSuppressionService Suppression,
        ICombatActivityService Activity, AbilityDetail Hostile);

    // A container of its own, so the activity, timing and status tables start empty whatever ran before; the engine that answers
    // from constants; the clock held still, so no window or temporary modifier expires between runs; and the stat service
    // whose calls into unconverted code allocate nothing. What is counted is Combat's forwarder and the part.
    private static TimingParts InstallTimingParts(List<IDisposable> scopes)
    {
        var container = new FacadeContainer(services =>
            services.Replace(ServiceDescriptor.Singleton<IStatService, AllocationStatService>()));
        scopes.Add(container);
        scopes.Add(container.Install());
        scopes.Add(NWScriptRuntime.Override(StatReadEngine.Create(StatReadEngine.NpcAnswers())));
        scopes.Add(Clock.OverrideForTests(() => FrozenNow));
        // Both creatures have a status table, as a creature in combat does: the status service hands an unknown creature a
        // new empty table on every read, which would dwarf what the parts allocate.
        foreach (var creature in new[] { Attacker, Defender })
            StatusEffectInternals.CreatureEffects[creature] = new CreatureStatusEffect { StatGroup = new StatGroup() };
        var hostile = new AbilityDetail { IsHostileAbility = true, SkillType = SkillType.Rifle };
        hostile.Requirements.Add(new AbilityRequirementStamina(10));
        return new TimingParts(container.Get<ICombatAttackTimingService>(), container.Get<ICombatSuppressionService>(),
            container.Get<ICombatActivityService>(), hostile);
    }

    private static void DisposeInReverse(List<IDisposable> scopes)
    {
        for (var i = scopes.Count - 1; i >= 0; i--)
            scopes[i].Dispose();
    }

    // Bytes per call of the part and of its forwarder, the least of three rounds taken in turn, so both are measured with the
    // same warm caches and table sizes.
    private static (long Part, long Forwarder) InterleavedPerCall(Action part, Action forwarder)
    {
        long ownBytes = long.MaxValue, forwardedBytes = long.MaxValue;
        for (var round = 0; round < 3; round++)
        {
            ownBytes = Math.Min(ownBytes, Allocated(part));
            forwardedBytes = Math.Min(forwardedBytes, Allocated(forwarder));
        }

        return (ownBytes / Calls, forwardedBytes / Calls);
    }

    // A forwarder is one call with the part's parameter types (CombatPartForwardingTests checks the single call), so it can
    // only add to what the part allocates (a closure and its delegate are 88 bytes or more). It measures less than the part
    // when the JIT finishes optimizing the status and stat reads between the two measurements, which says nothing about the
    // forwarder, so only the upper side is checked. The members that allocate kilobytes per call (the temporary modifier
    // writes, the activity tables) move by up to about 4% between rounds as their tables fill, so the margin grows by a
    // twentieth of what the part allocates, which keeps it under a closure's 88 bytes for the members that allocate little.
    private static void ForwarderAddsNothing(string name, long own, long forwarded, long margin) =>
        forwarded.Should().BeLessThanOrEqualTo(own + margin + own / 20, $"Combat.{name} adds no closure or delegate to what the part allocates");

    // A private forwarder of Combat (a member only other parts call), as a delegate so the call itself costs nothing.
    private static TDelegate Private<TDelegate>(string name, params Type[] parameterTypes)
        where TDelegate : Delegate
    {
        var method = typeof(CombatFacade).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static, null, parameterTypes, null)
                     ?? throw new AssertionException($"Combat has no private forwarder {name}.");
        return method.CreateDelegate<TDelegate>();
    }

    [Test]
    public void TheTimingSuppressionAndActivityReadsAndWritesAllocateNothingDirectlyOrThroughTheCombatForwarders()
    {
        var scopes = new List<IDisposable>();
        try
        {
            var (timing, suppression, activity, hostile) = InstallTimingParts(scopes);
            var sink = 0;
            var trackRecentDamageTarget = Private<Action<uint, uint>>("TrackRecentDamageTarget", typeof(uint), typeof(uint));
            var trackRecentDamageTaken = Private<Action<uint>>("TrackRecentDamageTaken", typeof(uint));
            var trackCombatActivity = Private<Action<uint>>("TrackCombatActivity", typeof(uint));
            var firstAttackRestore = Private<Action<uint>>("ApplyFirstCombatAttackStaminaRestore", typeof(uint));
            var directDamage = Private<Func<uint, uint, int>>("GetDirectDamageToStatusCategoryOrStealthBonus", typeof(uint), typeof(uint));
            var firstStrikeBonus = Private<Func<uint, AbilityDetail, int>>("GetFirstHostileAbilityHitDamageBonus", typeof(uint), typeof(AbilityDetail));
            var firstStrikeCount = Private<Action<uint, AbilityDetail>>("ApplyFirstHostileAbilityHitCount", typeof(uint), typeof(AbilityDetail));
            var forgetOpening = Private<Action<uint>>("ForgetOpeningActivityState", typeof(uint));
            var forgetTiming = Private<Action<uint>>("ForgetAttackTimingState", typeof(uint));
            var reads = new (string Name, Action Part, Action Forwarder)[]
            {
                // Per swing.
                ("ConsumeAttacksPerSwing(2)", () => sink += timing.ConsumeAttacksPerSwing(Attacker, 1500), () => sink += CombatFacade.ConsumeAttacksPerSwing(Attacker, 1500)),
                ("ConsumeAttacksPerSwing(4)", () => sink += timing.ConsumeAttacksPerSwing(Attacker, 1500, 1500, true), () => sink += CombatFacade.ConsumeAttacksPerSwing(Attacker, 1500, 1500, true)),
                ("ConsumeAttacksPerSwing(11)", () => sink += timing.ConsumeAttacksPerSwing(Attacker, 900, 1500, false, 1500, 2, 0, 2), () => sink += CombatFacade.ConsumeAttacksPerSwing(Attacker, 900, 1500, false, 1500, 2, 0, 2)),
                ("ClearAttackSwingDebt", () => timing.ClearAttackSwingDebt(Attacker), () => CombatFacade.ClearAttackSwingDebt(Attacker)),
                ("CalculateAttackDelayReduction", () => sink += timing.CalculateAttackDelayReduction(Attacker), () => sink += CombatFacade.CalculateAttackDelayReduction(Attacker)),
                ("CalculateOffhandAttackDelayReduction", () => sink += timing.CalculateOffhandAttackDelayReduction(Attacker), () => sink += CombatFacade.CalculateOffhandAttackDelayReduction(Attacker)),
                ("GetTargetAttackDelayReduction", () => sink += timing.GetTargetAttackDelayReduction(Attacker, Defender, SkillType.Rifle), () => sink += CombatFacade.GetTargetAttackDelayReduction(Attacker, Defender, SkillType.Rifle)),
                ("GetWeaponDelay", () => sink += timing.GetWeaponDelay(Attacker), () => sink += CombatFacade.GetWeaponDelay(Attacker)),
                ("IsAttackDelayReductionSuppressed", () => sink += timing.IsAttackDelayReductionSuppressed(Attacker) ? 1 : 0, () => sink += CombatFacade.IsAttackDelayReductionSuppressed(Attacker) ? 1 : 0),
                ("ApplyBleedingStatusExpiredEffects", () => timing.ApplyBleedingStatusExpiredEffects(Attacker), () => CombatFacade.ApplyBleedingStatusExpiredEffects(Attacker)),
                ("ForgetAttackTimingState", () => timing.ForgetAttackTimingState(Attacker), () => forgetTiming(Attacker)),
                // Per attack.
                ("ApplySuppressionStack", () => sink += suppression.ApplySuppressionStack(Attacker, Defender, 0, 6, CombatDamageType.Physical) ? 1 : 0, () => sink += CombatFacade.ApplySuppressionStack(Attacker, Defender, 0, 6, CombatDamageType.Physical) ? 1 : 0),
                ("GetRangedAbilityLongRangeHitChanceAdjustment", () => sink += suppression.GetRangedAbilityLongRangeHitChanceAdjustment(Attacker, Defender, SkillType.Rifle), () => sink += CombatFacade.GetRangedAbilityLongRangeHitChanceAdjustment(Attacker, Defender, SkillType.Rifle)),
                ("GetRangedAbilityLongRangeCriticalRateAdjustment", () => sink += suppression.GetRangedAbilityLongRangeCriticalRateAdjustment(Attacker, Defender, SkillType.Rifle), () => sink += CombatFacade.GetRangedAbilityLongRangeCriticalRateAdjustment(Attacker, Defender, SkillType.Rifle)),
                ("TrackRecentDamageTarget", () => activity.TrackRecentDamageTarget(Attacker, Defender), () => trackRecentDamageTarget(Attacker, Defender)),
                ("TrackRecentDamageTaken", () => activity.TrackRecentDamageTaken(Defender), () => trackRecentDamageTaken(Defender)),
                ("TrackCombatActivity", () => activity.TrackCombatActivity(Attacker), () => trackCombatActivity(Attacker)),
                ("TrackHostileDefensiveCombatEntryActivity", () => activity.TrackHostileDefensiveCombatEntryActivity(Defender, Attacker), () => CombatFacade.TrackHostileDefensiveCombatEntryActivity(Defender, Attacker)),
                ("ApplyFirstCombatAttackStaminaRestore", () => activity.ApplyFirstCombatAttackStaminaRestore(Attacker), () => firstAttackRestore(Attacker)),
                ("GetDirectDamageToStatusCategoryOrStealthBonus", () => sink += activity.GetDirectDamageToStatusCategoryOrStealthBonus(Attacker, Defender), () => sink += directDamage(Attacker, Defender)),
                ("GetFirstHostileAbilityHitDamageBonus", () => sink += activity.GetFirstHostileAbilityHitDamageBonus(Attacker, hostile), () => sink += firstStrikeBonus(Attacker, hostile)),
                ("ApplyFirstHostileAbilityHitCount", () => activity.ApplyFirstHostileAbilityHitCount(Attacker, hostile), () => firstStrikeCount(Attacker, hostile)),
                ("ForgetOpeningActivityState", () => activity.ForgetOpeningActivityState(Defender), () => forgetOpening(Defender)),
                // Activity reads.
                ("HasRecentDamageTaken", () => sink += activity.HasRecentDamageTaken(Defender, 6f) ? 1 : 0, () => sink += CombatFacade.HasRecentDamageTaken(Defender, 6f) ? 1 : 0),
                ("HasRecentDamageTarget", () => sink += activity.HasRecentDamageTarget(Attacker, Defender, 6f) ? 1 : 0, () => sink += CombatFacade.HasRecentDamageTarget(Attacker, Defender, 6f) ? 1 : 0),
                ("HasRecentHostileCombatActivity", () => sink += activity.HasRecentHostileCombatActivity(Attacker) ? 1 : 0, () => sink += CombatFacade.HasRecentHostileCombatActivity(Attacker) ? 1 : 0),
                ("HasRecentAttackActivity", () => sink += activity.HasRecentAttackActivity(Attacker, 6f) ? 1 : 0, () => sink += CombatFacade.HasRecentAttackActivity(Attacker, 6f) ? 1 : 0),
                ("HasRecentCombatAbilityUse", () => sink += activity.HasRecentCombatAbilityUse(Attacker, 6f) ? 1 : 0, () => sink += CombatFacade.HasRecentCombatAbilityUse(Attacker, 6f) ? 1 : 0),
                ("GetLastOffensiveActivityAt", () => sink += activity.GetLastOffensiveActivityAt(Attacker).Second, () => sink += CombatFacade.GetLastOffensiveActivityAt(Attacker).Second),
                ("GetLastCompletedOffensiveActivityAt", () => sink += activity.GetLastCompletedOffensiveActivityAt(Attacker).Second, () => sink += CombatFacade.GetLastCompletedOffensiveActivityAt(Attacker).Second),
                ("IsUsingAbility", () => sink += activity.IsUsingAbility(Attacker) ? 1 : 0, () => sink += CombatFacade.IsUsingAbility(Attacker) ? 1 : 0),
            };

            foreach (var (name, part, forwarder) in reads)
            {
                LeastAllocated(part).Should().Be(0, $"{name} runs per swing, per attack or per hit");
                LeastAllocated(forwarder).Should().Be(0, $"Combat.{name} only forwards");
            }

            // The hit effects and damage taken parts read the recent damage targets through the activity part itself.
            LeastAllocated(() => sink += activity.RecentDamageTargets.Count).Should().Be(0, "RecentDamageTargets runs per hit");

            GC.KeepAlive(sink);
        }
        finally
        {
            DisposeInReverse(scopes);
        }
    }

    [Test]
    public void TheTimingSuppressionAndActivityMembersThatAllocateKeepTheirBytesThroughTheCombatForwarders()
    {
        // Bytes per call in this setup. Each allocated before the move too, the same amount unless a comment says otherwise:
        // a closure of a moved method now captures the part's instance to reach its injected services, which makes the
        // closure object 8 bytes larger (HandleParalyze's warning, the idle-readiness schedule and channel checks, the login
        // refresh). The rest comes from LINQ over the stat channels, the temporary modifiers and the log lines, as before.
        var scopes = new List<IDisposable>();
        try
        {
            var (timing, suppression, activity, _) = InstallTimingParts(scopes);
            var sink = 0;
            var schedule = Private<Action<uint>>("ScheduleIdleReadinessRefresh", typeof(uint));
            var hasChannel = Private<Func<uint, bool>>("HasConfiguredIdleSkillAbilityChannel", typeof(uint));
            var idleHitChance = Private<Func<uint, SkillType, int>>("GetIdleAbilityHitChanceAdjustment", typeof(uint), typeof(SkillType));
            var forgetActivity = Private<Action<uint>>("ForgetActivityState", typeof(uint));
            var forgetSuppression = Private<Action<uint>>("ForgetSuppressionState", typeof(uint));
            var reads = new (string Name, long Bytes, Action Part, Action Forwarder)[]
            {
                ("CalculateAttackDelay(1)", 136, () => sink += timing.CalculateAttackDelay(Attacker), () => sink += CombatFacade.CalculateAttackDelay(Attacker)),
                ("CalculateAttackDelay(2)", 136, () => sink += timing.CalculateAttackDelay(Attacker, -10), () => sink += CombatFacade.CalculateAttackDelay(Attacker, -10)),
                ("GetTargetAttackDelayReduction(Throwing)", 152, () => sink += timing.GetTargetAttackDelayReduction(Attacker, Defender, SkillType.Throwing), () => sink += CombatFacade.GetTargetAttackDelayReduction(Attacker, Defender, SkillType.Throwing)),
                ("HasTemporaryNextAutoAttackNoDelay", 112, () => sink += timing.HasTemporaryNextAutoAttackNoDelay(Attacker, SkillType.Rifle) ? 1 : 0, () => sink += CombatFacade.HasTemporaryNextAutoAttackNoDelay(Attacker, SkillType.Rifle) ? 1 : 0),
                ("ConsumeNextAutoAttackCriticalRateBonus", 56, () => sink += timing.ConsumeNextAutoAttackCriticalRateBonus(Attacker, SkillType.Rifle), () => sink += CombatFacade.ConsumeNextAutoAttackCriticalRateBonus(Attacker, SkillType.Rifle)),
                // 32 before the move: the warning's closure also holds the part, to reach the player name service.
                ("HandleParalyze", 40, () => sink += timing.HandleParalyze(Attacker) ? 1 : 0, () => sink += CombatFacade.HandleParalyze(Attacker) ? 1 : 0),
                ("ReconcileOverwatchStatus", 216, () => suppression.ReconcileOverwatchStatus(Attacker), () => CombatFacade.ReconcileOverwatchStatus(Attacker)),
                ("ForgetSuppressionState", 528, () => suppression.ForgetSuppressionState(Attacker), () => forgetSuppression(Attacker)),
                // 320 before the move: the idle-readiness schedule's closure also holds the part.
                ("TrackAttackActivity", 328, () => activity.TrackAttackActivity(Attacker), () => CombatFacade.TrackAttackActivity(Attacker)),
                ("TrackHostileAbilityActivity", 328, () => activity.TrackHostileAbilityActivity(Attacker), () => CombatFacade.TrackHostileAbilityActivity(Attacker)),
                ("ScheduleIdleReadinessRefresh", 328, () => activity.ScheduleIdleReadinessRefresh(Attacker), () => schedule(Attacker)),
                ("TrackStealthOpeningWindow", 32, () => activity.TrackStealthOpeningWindow(Attacker), () => CombatFacade.TrackStealthOpeningWindow(Attacker)),
                ("GetIdleSkillAbilityBonuses", 56, () => sink += activity.GetIdleSkillAbilityBonuses(Attacker, SkillType.Rifle).DamageBonus, () => sink += CombatFacade.GetIdleSkillAbilityBonuses(Attacker, SkillType.Rifle).DamageBonus),
                ("GetIdleAbilityHitChanceAdjustment", 56, () => sink += activity.GetIdleAbilityHitChanceAdjustment(Attacker, SkillType.Rifle), () => sink += idleHitChance(Attacker, SkillType.Rifle)),
                // 88 before the move: the channel check's closure also holds the part.
                ("HasConfiguredIdleSkillAbilityChannel", 96, () => sink += activity.HasConfiguredIdleSkillAbilityChannel(Attacker) ? 1 : 0, () => sink += hasChannel(Attacker) ? 1 : 0),
                // 24 before the move: the delayed refresh's closure also holds the part.
                ("RestoreIdleReadinessOnPlayerEnter", 32, () => activity.RestoreIdleReadinessOnPlayerEnter(), () => CombatFacade.RestoreIdleReadinessOnPlayerEnter()),
                ("PurgeInvalidOffensiveActivity", 480, () => activity.PurgeInvalidOffensiveActivity(), () => CombatFacade.PurgeInvalidOffensiveActivity()),
                ("ForgetActivityState", 184, () => activity.ForgetActivityState(Attacker), () => forgetActivity(Attacker)),
            };

            foreach (var (name, bytes, part, forwarder) in reads)
            {
                PerCall(part).Should().Be(bytes, $"{name} allocates what it did before the move");
                PerCall(forwarder).Should().Be(bytes, $"Combat.{name} adds nothing to what the part allocates");
            }

            GC.KeepAlive(sink);
        }
        finally
        {
            DisposeInReverse(scopes);
        }
    }

    [Test]
    public void ATimingSuppressionOrActivityForwarderAddsNothingToTheLargeReads()
    {
        // These read the skill's attribute by reflection (IsWeaponSkillType), a creature's status effects or the temporary
        // modifiers, as before the move, so what they allocate per call moves between rounds: the runtime rebuilds its
        // reflection cache after a collection (up to 24 bytes a call) and the tables grow. A forwarder is one call with the
        // same parameter types (CombatPartForwardingTests), so it could only add a closure and its delegate (88 bytes or more).
        // StoreAbilityActivationIdleBonuses is left out: its temporary modifier writes vary by hundreds of bytes from run to run.
        var scopes = new List<IDisposable>();
        try
        {
            var (timing, suppression, activity, hostile) = InstallTimingParts(scopes);
            var sink = 0;
            var autoAttackSuppression = Private<Action<uint, uint, SkillType, CombatDamageType>>("ApplyAutoAttackSuppressionStack",
                typeof(uint), typeof(uint), typeof(SkillType), typeof(CombatDamageType));
            var rangedSuppression = Private<Action<uint, uint, SkillType, CombatDamageType>>("ApplyRangedHitSuppressionStack",
                typeof(uint), typeof(uint), typeof(SkillType), typeof(CombatDamageType));
            var trackSuppression = Private<Action<uint, DateTime>>("TrackSuppressionAbilityUse", typeof(uint), typeof(DateTime));
            var trackAbility = Private<Action<uint, AbilityDetail>>("TrackCombatAbilityUse", typeof(uint), typeof(AbilityDetail));
            var reads = new (string Name, Action Part, Action Forwarder)[]
            {
                ("ConsumeNextAbilityDelayReductionPercent", () => sink += timing.ConsumeNextAbilityDelayReductionPercent(Attacker, hostile), () => sink += CombatFacade.ConsumeNextAbilityDelayReductionPercent(Attacker, hostile)),
                ("HasNextAutoAttackNoDelay", () => sink += timing.HasNextAutoAttackNoDelay(Attacker, SkillType.Rifle) ? 1 : 0, () => sink += CombatFacade.HasNextAutoAttackNoDelay(Attacker, SkillType.Rifle) ? 1 : 0),
                ("ConsumeNextAutoAttackNoDelay", () => sink += timing.ConsumeNextAutoAttackNoDelay(Attacker, SkillType.Rifle) ? 1 : 0, () => sink += CombatFacade.ConsumeNextAutoAttackNoDelay(Attacker, SkillType.Rifle) ? 1 : 0),
                ("GrantNextAbilityNoDelay", () => timing.GrantNextAbilityNoDelay(Attacker, SkillType.Rifle, 6, 50), () => CombatFacade.GrantNextAbilityNoDelay(Attacker, SkillType.Rifle, 6, 50)),
                ("GetSuppressionStackCount", () => sink += suppression.GetSuppressionStackCount(Defender, Attacker), () => sink += CombatFacade.GetSuppressionStackCount(Defender, Attacker)),
                ("ConsumeSuppressionRangedAttackAccuracyAdjustment", () => sink += suppression.ConsumeSuppressionRangedAttackAccuracyAdjustment(Attacker, Defender, SkillType.Rifle), () => sink += CombatFacade.ConsumeSuppressionRangedAttackAccuracyAdjustment(Attacker, Defender, SkillType.Rifle)),
                ("ApplyAutoAttackSuppressionStack", () => suppression.ApplyAutoAttackSuppressionStack(Attacker, Defender, SkillType.Rifle, CombatDamageType.Physical), () => autoAttackSuppression(Attacker, Defender, SkillType.Rifle, CombatDamageType.Physical)),
                ("ReconcileContainmentNetStatuses", () => suppression.ReconcileContainmentNetStatuses(Defender), () => CombatFacade.ReconcileContainmentNetStatuses(Defender)),
                ("ApplyRangedHitSuppressionStack", () => suppression.ApplyRangedHitSuppressionStack(Attacker, Defender, SkillType.Rifle, CombatDamageType.Physical), () => rangedSuppression(Attacker, Defender, SkillType.Rifle, CombatDamageType.Physical)),
                ("TrackSuppressionAbilityUse", () => suppression.TrackSuppressionAbilityUse(Defender, FrozenNow), () => trackSuppression(Defender, FrozenNow)),
                // 8 bytes more than before the move: it schedules idle readiness, whose closure now also holds the part.
                ("TrackCombatAbilityUse", () => activity.TrackCombatAbilityUse(Attacker, hostile), () => trackAbility(Attacker, hostile)),
                ("ClearAbilityActivationIdleBonuses", () => activity.ClearAbilityActivationIdleBonuses(Attacker), () => CombatFacade.ClearAbilityActivationIdleBonuses(Attacker)),
            };

            foreach (var (name, part, forwarder) in reads)
            {
                var (own, forwarded) = InterleavedPerCall(part, forwarder);
                own.Should().BeGreaterThan(0, $"{name} allocated before the move too");
                ForwarderAddsNothing(name, own, forwarded, 32);
            }

            GC.KeepAlive(sink);
        }
        finally
        {
            DisposeInReverse(scopes);
        }
    }

    private sealed record BonusParts(ICombatAbilityBonusService Bonus, ICombatDamageModifiersService Modifiers, AbilityDetail Hostile);

    // The same world as the timing parts (a container of its own, the constant engine, the clock held still, a status table
    // for both creatures and the stat service that allocates nothing), with the ability bonus and damage modifier parts.
    private static BonusParts InstallBonusParts(List<IDisposable> scopes)
    {
        InstallTimingParts(scopes);
        var container = FacadeContainer.Current;
        var hostile = new AbilityDetail { IsHostileAbility = true, SkillType = SkillType.Rifle, EffectiveLevelPerkType = PerkType.DoubleShot };
        hostile.Requirements.Add(new AbilityRequirementStamina(10));
        return new BonusParts(container.Get<ICombatAbilityBonusService>(), container.Get<ICombatDamageModifiersService>(), hostile);
    }

    [Test]
    public void TheAbilityBonusAndDamageModifierReadsAndWritesAllocateNothingDirectlyOrThroughTheCombatForwarders()
    {
        // Run per attack, per damage roll or per ability. Each allocated nothing before the move either (measured against
        // the base assembly in the same sequence).
        var scopes = new List<IDisposable>();
        try
        {
            var (bonus, modifiers, hostile) = InstallBonusParts(scopes);
            var sink = 0;
            var fsink = 0f;
            var tryDowngrade = Private<Func<uint, int, bool>>("TryUseIncomingCriticalHitDowngrade", typeof(uint), typeof(int));
            var clearCycle = Private<Action<uint>>("ClearAutoAttackCycleCriticalRateTracker", typeof(uint));
            var targetStatusRate = Private<Func<uint, uint, int>>("GetTargetStatusCriticalRateAdjustment", typeof(uint), typeof(uint));
            var clearRanged = Private<Action<uint>>("ClearRangedRepeatedTargetDamageTracker", typeof(uint));
            var pressure = Private<Action<uint, uint, SkillType>>("ApplySameTargetPressureDamageEffects", typeof(uint), typeof(uint), typeof(SkillType));
            var reads = new (string Name, Action Part, Action Forwarder)[]
            {
                // Ability bonuses.
                ("GetNextSkillAbilityStaminaCostAdjustment", () => sink += bonus.GetNextSkillAbilityStaminaCostAdjustment(Attacker, SkillType.Rifle, true), () => sink += CombatFacade.GetNextSkillAbilityStaminaCostAdjustment(Attacker, SkillType.Rifle, true)),
                ("ConsumeNextSkillAbilityStaminaCostAdjustment", () => sink += bonus.ConsumeNextSkillAbilityStaminaCostAdjustment(Attacker, SkillType.Rifle), () => sink += CombatFacade.ConsumeNextSkillAbilityStaminaCostAdjustment(Attacker, SkillType.Rifle)),
                ("StoreQueuedWeaponAbilityCriticalRateBonus", () => bonus.StoreQueuedWeaponAbilityCriticalRateBonus(Attacker, SkillType.Rifle, 0), () => CombatFacade.StoreQueuedWeaponAbilityCriticalRateBonus(Attacker, SkillType.Rifle, 0)),
                ("StoreQueuedWeaponAbilityActivationCriticalRateBonus", () => bonus.StoreQueuedWeaponAbilityActivationCriticalRateBonus(Attacker, SkillType.Rifle, 0), () => CombatFacade.StoreQueuedWeaponAbilityActivationCriticalRateBonus(Attacker, SkillType.Rifle, 0)),
                ("ApplyNextAttackGuardedHitEnmityBonus", () => bonus.ApplyNextAttackGuardedHitEnmityBonus(Attacker, Defender, 0), () => CombatFacade.ApplyNextAttackGuardedHitEnmityBonus(Attacker, Defender, 0)),
                ("GetAbilityStatusCategoryDamageBonus", () => sink += bonus.GetAbilityStatusCategoryDamageBonus(Attacker, SkillType.Rifle, StatusEffectCategory.Control), () => sink += CombatFacade.GetAbilityStatusCategoryDamageBonus(Attacker, SkillType.Rifle, StatusEffectCategory.Control)),
                ("GetAbilityStatusCategoryHitChancePercentAdjustment", () => sink += bonus.GetAbilityStatusCategoryHitChancePercentAdjustment(Attacker, SkillType.Rifle, StatusEffectCategory.Control), () => sink += CombatFacade.GetAbilityStatusCategoryHitChancePercentAdjustment(Attacker, SkillType.Rifle, StatusEffectCategory.Control)),
                ("ApplyAbilityRecastDelayModifiers", () => fsink += bonus.ApplyAbilityRecastDelayModifiers(Attacker, hostile, 10f), () => fsink += CombatFacade.ApplyAbilityRecastDelayModifiers(Attacker, hostile, 10f)),
                // Damage roll modifiers.
                ("TryUseIncomingCriticalHitDowngrade", () => sink += modifiers.TryUseIncomingCriticalHitDowngrade(Defender, 2) ? 1 : 0, () => sink += tryDowngrade(Defender, 2) ? 1 : 0),
                ("ApplyCriticalDamageModifier", () => sink += modifiers.ApplyCriticalDamageModifier(Attacker, 100, 2), () => sink += CombatFacade.ApplyCriticalDamageModifier(Attacker, 100, 2)),
                ("GetAutoAttackCriticalRateAdjustment", () => sink += modifiers.GetAutoAttackCriticalRateAdjustment(Attacker, Defender, 5), () => sink += CombatFacade.GetAutoAttackCriticalRateAdjustment(Attacker, Defender, 5)),
                ("ApplyRangedAttackDefenseIgnore", () => sink += modifiers.ApplyRangedAttackDefenseIgnore(Attacker, 100, SkillType.Rifle), () => sink += CombatFacade.ApplyRangedAttackDefenseIgnore(Attacker, 100, SkillType.Rifle)),
                ("ApplyOrdinaryAttackDamageMultiplier", () => sink += modifiers.ApplyOrdinaryAttackDamageMultiplier(Attacker, 100), () => sink += CombatFacade.ApplyOrdinaryAttackDamageMultiplier(Attacker, 100)),
                ("ApplyAbilityShapeDamageModifier", () => sink += modifiers.ApplyAbilityShapeDamageModifier(Attacker, 100, true, hostile), () => sink += CombatFacade.ApplyAbilityShapeDamageModifier(Attacker, 100, true, hostile)),
                ("ApplyThrowingSingleTargetDamageModifier", () => sink += modifiers.ApplyThrowingSingleTargetDamageModifier(Attacker, SkillType.Throwing, 100, false, null!), () => sink += CombatFacade.ApplyThrowingSingleTargetDamageModifier(Attacker, SkillType.Throwing, 100, false, null!)),
                ("ApplyMimicryAbilityDamageModifier", () => sink += modifiers.ApplyMimicryAbilityDamageModifier(Attacker, 100, SkillType.Mimicry, true), () => sink += CombatFacade.ApplyMimicryAbilityDamageModifier(Attacker, 100, SkillType.Mimicry, true)),
                ("ApplySkillAbilityDamageModifier", () => sink += modifiers.ApplySkillAbilityDamageModifier(Attacker, 100, SkillType.Rifle, true), () => sink += CombatFacade.ApplySkillAbilityDamageModifier(Attacker, 100, SkillType.Rifle, true)),
                ("ApplySideAttackDamageModifier", () => sink += modifiers.ApplySideAttackDamageModifier(Attacker, Defender, SkillType.Rifle, 100), () => sink += CombatFacade.ApplySideAttackDamageModifier(Attacker, Defender, SkillType.Rifle, 100)),
                ("GetSideAttackHitChanceAdjustment", () => sink += modifiers.GetSideAttackHitChanceAdjustment(Attacker, Defender, SkillType.Rifle), () => sink += CombatFacade.GetSideAttackHitChanceAdjustment(Attacker, Defender, SkillType.Rifle)),
                ("GetSideAttackCriticalRateAdjustment", () => sink += modifiers.GetSideAttackCriticalRateAdjustment(Attacker, Defender, SkillType.Rifle), () => sink += CombatFacade.GetSideAttackCriticalRateAdjustment(Attacker, Defender, SkillType.Rifle)),
                ("ApplySideAttackEvasionIgnore", () => sink += modifiers.ApplySideAttackEvasionIgnore(Attacker, Defender, SkillType.Rifle, 100), () => sink += CombatFacade.ApplySideAttackEvasionIgnore(Attacker, Defender, SkillType.Rifle, 100)),
                ("ClearAutoAttackCycleCriticalRateTracker", () => modifiers.ClearAutoAttackCycleCriticalRateTracker(Attacker), () => clearCycle(Attacker)),
                ("GetLowHPAttackAdjustment", () => sink += modifiers.GetLowHPAttackAdjustment(Attacker), () => sink += CombatFacade.GetLowHPAttackAdjustment(Attacker)),
                ("GetTargetStatusCriticalRateAdjustment", () => sink += modifiers.GetTargetStatusCriticalRateAdjustment(Attacker, Defender), () => sink += targetStatusRate(Attacker, Defender)),
                ("ApplyDamageTypeDealtModifiers", () => sink += modifiers.ApplyDamageTypeDealtModifiers(Attacker, 100, CombatDamageType.Poison), () => sink += CombatFacade.ApplyDamageTypeDealtModifiers(Attacker, 100, CombatDamageType.Poison)),
                ("GetNearbyStatusTargetAttackAdjustment", () => sink += modifiers.GetNearbyStatusTargetAttackAdjustment(Attacker), () => sink += CombatFacade.GetNearbyStatusTargetAttackAdjustment(Attacker)),
                ("ApplyTwinBladeAbilityShapeDamageModifier", () => sink += modifiers.ApplyTwinBladeAbilityShapeDamageModifier(Attacker, SkillType.TwinBlade, 100, true, true), () => sink += CombatFacade.ApplyTwinBladeAbilityShapeDamageModifier(Attacker, SkillType.TwinBlade, 100, true, true)),
                ("ApplyThrowingAbilityShapeDamageModifier", () => sink += modifiers.ApplyThrowingAbilityShapeDamageModifier(Attacker, SkillType.Throwing, 100, true), () => sink += CombatFacade.ApplyThrowingAbilityShapeDamageModifier(Attacker, SkillType.Throwing, 100, true)),
                ("ApplyPhysicalAbilityShapeDamageModifier", () => sink += modifiers.ApplyPhysicalAbilityShapeDamageModifier(Attacker, CombatDamageType.Physical, 100, true), () => sink += CombatFacade.ApplyPhysicalAbilityShapeDamageModifier(Attacker, CombatDamageType.Physical, 100, true)),
                ("ApplySkillAreaAbilityDamageModifier", () => sink += modifiers.ApplySkillAreaAbilityDamageModifier(Attacker, SkillType.Rifle, 100, true), () => sink += CombatFacade.ApplySkillAreaAbilityDamageModifier(Attacker, SkillType.Rifle, 100, true)),
                ("ApplyRangedAbilityLongRangeDamageModifier", () => sink += modifiers.ApplyRangedAbilityLongRangeDamageModifier(Attacker, Defender, SkillType.Rifle, 100), () => sink += CombatFacade.ApplyRangedAbilityLongRangeDamageModifier(Attacker, Defender, SkillType.Rifle, 100)),
                ("ApplyAreaAbilityAfterDeflectionDamageModifier", () => sink += modifiers.ApplyAreaAbilityAfterDeflectionDamageModifier(Attacker, SkillType.Rifle, 100, true), () => sink += CombatFacade.ApplyAreaAbilityAfterDeflectionDamageModifier(Attacker, SkillType.Rifle, 100, true)),
                ("ClearRangedRepeatedTargetDamageTracker", () => modifiers.ClearRangedRepeatedTargetDamageTracker(Attacker), () => clearRanged(Attacker)),
                ("ApplySameTargetPressureDamageEffects", () => modifiers.ApplySameTargetPressureDamageEffects(Attacker, Defender, SkillType.Rifle), () => pressure(Attacker, Defender, SkillType.Rifle)),
            };

            foreach (var (name, part, forwarder) in reads)
            {
                LeastAllocated(part).Should().Be(0, $"{name} runs per attack, per damage roll or per ability");
                LeastAllocated(forwarder).Should().Be(0, $"Combat.{name} only forwards");
            }

            GC.KeepAlive(sink);
            GC.KeepAlive(fsink);
        }
        finally
        {
            DisposeInReverse(scopes);
        }
    }

    [Test]
    public void TheAbilityBonusAndDamageModifierMembersThatAllocateKeepTheirBytesThroughTheCombatForwarders()
    {
        // Bytes per call in this setup, each the same as the base assembly's in the same sequence: the temporary modifier
        // reads build their closures and LINQ iterators, the NPC level read its NPCStats and the low FP read the stat
        // service's FP lookups, as before the move. No closure of a moved method captures the part.
        var scopes = new List<IDisposable>();
        try
        {
            var (bonus, modifiers, hostile) = InstallBonusParts(scopes);
            var sink = 0;
            var fsink = 0f;
            var forget = Private<Action<uint>>("ForgetDamageModifierState", typeof(uint));
            var reads = new (string Name, long Bytes, Action Part, Action Forwarder)[]
            {
                ("ConsumeOpeningAutoAttackCriticalDamageAdjustment", 56, () => sink += bonus.ConsumeOpeningAutoAttackCriticalDamageAdjustment(Attacker), () => sink += CombatFacade.ConsumeOpeningAutoAttackCriticalDamageAdjustment(Attacker)),
                ("ClearOpeningAutoAttackModifiers", 336, () => bonus.ClearOpeningAutoAttackModifiers(Attacker), () => CombatFacade.ClearOpeningAutoAttackModifiers(Attacker)),
                ("HasNextAbilityNoStaminaCost", 56, () => sink += bonus.HasNextAbilityNoStaminaCost(Attacker, SkillType.Rifle) ? 1 : 0, () => sink += CombatFacade.HasNextAbilityNoStaminaCost(Attacker, SkillType.Rifle) ? 1 : 0),
                ("ConsumeNextAbilityNoStaminaCost", 56, () => sink += bonus.ConsumeNextAbilityNoStaminaCost(Attacker, SkillType.Rifle) ? 1 : 0, () => sink += CombatFacade.ConsumeNextAbilityNoStaminaCost(Attacker, SkillType.Rifle) ? 1 : 0),
                ("ConsumeNextSkillAbilityBonuses", 56, () => sink += bonus.ConsumeNextSkillAbilityBonuses(Attacker, SkillType.Rifle).DamageBonus, () => sink += CombatFacade.ConsumeNextSkillAbilityBonuses(Attacker, SkillType.Rifle).DamageBonus),
                ("ConsumePersistentNextSkillAbilityCriticalRateBonus", 112, () => sink += bonus.ConsumePersistentNextSkillAbilityCriticalRateBonus(Attacker, SkillType.Rifle), () => sink += CombatFacade.ConsumePersistentNextSkillAbilityCriticalRateBonus(Attacker, SkillType.Rifle)),
                ("GetPersistentNextSkillAbilityCriticalRateBonus", 112, () => sink += bonus.GetPersistentNextSkillAbilityCriticalRateBonus(Attacker, SkillType.Rifle), () => sink += CombatFacade.GetPersistentNextSkillAbilityCriticalRateBonus(Attacker, SkillType.Rifle)),
                ("StoreQueuedWeaponAbilityActivationIdleBonuses", 56, () => bonus.StoreQueuedWeaponAbilityActivationIdleBonuses(Attacker, SkillType.Rifle), () => CombatFacade.StoreQueuedWeaponAbilityActivationIdleBonuses(Attacker, SkillType.Rifle)),
                ("HasWeaponAbilityActivationIdleSnapshot", 56, () => sink += bonus.HasWeaponAbilityActivationIdleSnapshot(Attacker) ? 1 : 0, () => sink += CombatFacade.HasWeaponAbilityActivationIdleSnapshot(Attacker) ? 1 : 0),
                ("GetWeaponAbilityActivationIdleDamageBonus", 56, () => sink += bonus.GetWeaponAbilityActivationIdleDamageBonus(Attacker), () => sink += CombatFacade.GetWeaponAbilityActivationIdleDamageBonus(Attacker)),
                ("GetWeaponAbilityActivationIdleCriticalRateBonus", 56, () => sink += bonus.GetWeaponAbilityActivationIdleCriticalRateBonus(Attacker), () => sink += CombatFacade.GetWeaponAbilityActivationIdleCriticalRateBonus(Attacker)),
                ("GetWeaponAbilityActivationIdleDefenseIgnorePercent", 56, () => sink += bonus.GetWeaponAbilityActivationIdleDefenseIgnorePercent(Attacker), () => sink += CombatFacade.GetWeaponAbilityActivationIdleDefenseIgnorePercent(Attacker)),
                ("ClearWeaponAbilityActivationIdleBonuses", 224, () => bonus.ClearWeaponAbilityActivationIdleBonuses(Attacker), () => CombatFacade.ClearWeaponAbilityActivationIdleBonuses(Attacker)),
                ("ClearQueuedWeaponAbilityActivationBonuses", 280, () => bonus.ClearQueuedWeaponAbilityActivationBonuses(Attacker), () => CombatFacade.ClearQueuedWeaponAbilityActivationBonuses(Attacker)),
                ("ConsumeQueuedWeaponAbilityBonuses", 112, () => sink += bonus.ConsumeQueuedWeaponAbilityBonuses(Attacker, SkillType.Rifle).DamageBonus, () => sink += CombatFacade.ConsumeQueuedWeaponAbilityBonuses(Attacker, SkillType.Rifle).DamageBonus),
                ("ClearQueuedWeaponAbilityAttemptBonuses", 224, () => bonus.ClearQueuedWeaponAbilityAttemptBonuses(Attacker), () => CombatFacade.ClearQueuedWeaponAbilityAttemptBonuses(Attacker)),
                ("ConsumeNextAttackGuardedHitBonuses", 168, () => sink += bonus.ConsumeNextAttackGuardedHitBonuses(Attacker).DMGBonus, () => sink += CombatFacade.ConsumeNextAttackGuardedHitBonuses(Attacker).DMGBonus),
                ("ConsumeNextAttackGuardedHitAutoAttackBonuses", 112, () => sink += bonus.ConsumeNextAttackGuardedHitAutoAttackBonuses(Attacker).DMGBonus, () => sink += CombatFacade.ConsumeNextAttackGuardedHitAutoAttackBonuses(Attacker).DMGBonus),
                ("ConsumeNextAttackGuardedHitCriticalRateBonus", 56, () => sink += bonus.ConsumeNextAttackGuardedHitCriticalRateBonus(Attacker), () => sink += CombatFacade.ConsumeNextAttackGuardedHitCriticalRateBonus(Attacker)),
                ("ConsumeStatusAppliedNextAttackDamageBonus", 56, () => sink += bonus.ConsumeStatusAppliedNextAttackDamageBonus(Attacker), () => sink += CombatFacade.ConsumeStatusAppliedNextAttackDamageBonus(Attacker)),
                ("GetStatusAppliedNextAttackDamageBonus", 56, () => sink += bonus.GetStatusAppliedNextAttackDamageBonus(Attacker), () => sink += CombatFacade.GetStatusAppliedNextAttackDamageBonus(Attacker)),
                ("GetAbilityDamageFlatAdjustment", 64, () => sink += bonus.GetAbilityDamageFlatAdjustment(Attacker, PerkType.DoubleShot, SkillType.Rifle), () => sink += CombatFacade.GetAbilityDamageFlatAdjustment(Attacker, PerkType.DoubleShot, SkillType.Rifle)),
                ("GetAbilityStaminaCostFlatAdjustment(perk)", 64, () => sink += bonus.GetAbilityStaminaCostFlatAdjustment(Attacker, PerkType.DoubleShot), () => sink += CombatFacade.GetAbilityStaminaCostFlatAdjustment(Attacker, PerkType.DoubleShot)),
                ("GetAbilityStaminaCostFlatAdjustment(ability)", 64, () => sink += bonus.GetAbilityStaminaCostFlatAdjustment(Attacker, hostile), () => sink += CombatFacade.GetAbilityStaminaCostFlatAdjustment(Attacker, hostile)),
                ("GetAbilityStatusDurationPercentAdjustment", 64, () => sink += bonus.GetAbilityStatusDurationPercentAdjustment(Attacker, PerkType.DoubleShot, SkillType.Rifle, typeof(StunnedStatusEffect), Array.Empty<Type>(), null!), () => sink += CombatFacade.GetAbilityStatusDurationPercentAdjustment(Attacker, PerkType.DoubleShot, SkillType.Rifle, typeof(StunnedStatusEffect), Array.Empty<Type>(), null!)),
                ("GetAbilityDefenseIgnorePercentAdjustment", 504, () => sink += bonus.GetAbilityDefenseIgnorePercentAdjustment(Attacker, PerkType.DoubleShot, SkillType.Rifle, Defender), () => sink += CombatFacade.GetAbilityDefenseIgnorePercentAdjustment(Attacker, PerkType.DoubleShot, SkillType.Rifle, Defender)),
                ("GetAbilityRecastDelayFlatAdjustment", 64, () => fsink += bonus.GetAbilityRecastDelayFlatAdjustment(Attacker, PerkType.DoubleShot), () => fsink += CombatFacade.GetAbilityRecastDelayFlatAdjustment(Attacker, PerkType.DoubleShot)),
                ("GetAbilityDamageBonus", 312, () => sink += bonus.GetAbilityDamageBonus(Attacker, SkillType.Rifle), () => sink += CombatFacade.GetAbilityDamageBonus(Attacker, SkillType.Rifle)),
                ("ApplyTargetStatusAttackModifiers", 152, () => sink += modifiers.ApplyTargetStatusAttackModifiers(Attacker, Defender, 100, SkillType.Vibroblade), () => sink += CombatFacade.ApplyTargetStatusAttackModifiers(Attacker, Defender, 100, SkillType.Vibroblade)),
                ("GetLowFPAttackAdjustment", 312, () => sink += modifiers.GetLowFPAttackAdjustment(Attacker), () => sink += CombatFacade.GetLowFPAttackAdjustment(Attacker)),
                ("ApplyStatusSourceDefenseModifiers", 64, () => sink += modifiers.ApplyStatusSourceDefenseModifiers(Attacker, Defender, 100), () => sink += CombatFacade.ApplyStatusSourceDefenseModifiers(Attacker, Defender, 100)),
                ("ForgetDamageModifierState", 256, () => modifiers.ForgetDamageModifierState(Attacker), () => forget(Attacker)),
            };

            foreach (var (name, bytes, part, forwarder) in reads)
            {
                PerCall(part).Should().Be(bytes, $"{name} allocates what it did before the move");
                PerCall(forwarder).Should().Be(bytes, $"Combat.{name} adds nothing to what the part allocates");
            }

            GC.KeepAlive(sink);
            GC.KeepAlive(fsink);
        }
        finally
        {
            DisposeInReverse(scopes);
        }
    }

    [Test]
    public void WhereAnAbilityBonusOrDamageModifierMemberAllocatesMoreItsForwarderAddsNothing()
    {
        // These read the skill's attribute by reflection (IsWeaponSkillType), a status table the status service builds for
        // an unknown creature, write temporary modifiers, or format the damage range's log lines or a perk's group name, as
        // before the move, so what they allocate per call moves between rounds and runs on the base assembly too (by up to
        // about 70 bytes). A forwarder is one call with the same parameter types (CombatPartForwardingTests), so it could only
        // add a closure and its delegate (88 bytes or more).
        // StoreQueuedWeaponAbilityCriticalRateBonus with a bonus is left out: its temporary modifier writes vary by up to 70
        // bytes between part and forwarder rounds (2399 to 2466 on the base and here alike).
        // Named to run last in the fixture: its million damage rolls let the JIT finish optimizing the status reads, which
        // changes what the pinned reads above allocate when they run after it.
        var scopes = new List<IDisposable>();
        try
        {
            var (bonus, modifiers, hostile) = InstallBonusParts(scopes);
            var sink = 0;
            var reads = new (string Name, Action Part, Action Forwarder)[]
            {
                ("PrepareOpeningAutoAttack", () => sink += bonus.PrepareOpeningAutoAttack(Attacker, SkillType.Rifle), () => sink += CombatFacade.PrepareOpeningAutoAttack(Attacker, SkillType.Rifle)),
                ("GetQueuedWeaponAbilityActivationHitChanceAdjustment", () => sink += bonus.GetQueuedWeaponAbilityActivationHitChanceAdjustment(Attacker, SkillType.Rifle, hostile), () => sink += CombatFacade.GetQueuedWeaponAbilityActivationHitChanceAdjustment(Attacker, SkillType.Rifle, hostile)),
                ("ApplyCriticalDamageModifier", () => sink += modifiers.ApplyCriticalDamageModifier(Attacker, 100, 2, SkillType.Rifle, Defender), () => sink += CombatFacade.ApplyCriticalDamageModifier(Attacker, 100, 2, SkillType.Rifle, Defender)),
                ("GetSkillCriticalRatePercentAdjustment", () => sink += modifiers.GetSkillCriticalRatePercentAdjustment(Attacker, SkillType.Rifle), () => sink += CombatFacade.GetSkillCriticalRatePercentAdjustment(Attacker, SkillType.Rifle)),
                ("ApplyBackAttackDamageModifier", () => sink += modifiers.ApplyBackAttackDamageModifier(Attacker, Defender, SkillType.Rifle, 100), () => sink += CombatFacade.ApplyBackAttackDamageModifier(Attacker, Defender, SkillType.Rifle, 100)),
                ("GetBackAttackCriticalRateAdjustment", () => sink += modifiers.GetBackAttackCriticalRateAdjustment(Attacker, Defender, SkillType.Rifle), () => sink += CombatFacade.GetBackAttackCriticalRateAdjustment(Attacker, Defender, SkillType.Rifle)),
                ("PrepareAutoAttackCycleCriticalRate", () => sink += modifiers.PrepareAutoAttackCycleCriticalRate(Attacker, SkillType.Rifle), () => sink += CombatFacade.PrepareAutoAttackCycleCriticalRate(Attacker, SkillType.Rifle)),
                ("GetSameTargetPressureWeaponAbilityDamageBonus", () => sink += modifiers.GetSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle), () => sink += CombatFacade.GetSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle)),
                ("ConsumeSameTargetPressureWeaponAbilityDamageBonus", () => modifiers.ConsumeSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle, 10), () => CombatFacade.ConsumeSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle, 10)),
                ("CalculateDamageWithCriticalMitigation", () => sink += modifiers.CalculateDamageWithCriticalMitigation(Defender, 100, 50, 20, 80, 10, 2).Damage, () => sink += CombatFacade.CalculateDamageWithCriticalMitigation(Defender, 100, 50, 20, 80, 10, 2).Damage),
                ("ApplyStatusSourceAccuracyModifiers", () => sink += modifiers.ApplyStatusSourceAccuracyModifiers(Attacker, Defender, 100), () => sink += CombatFacade.ApplyStatusSourceAccuracyModifiers(Attacker, Defender, 100)),
                ("ApplyDamageDealtModifiers(13)", () => sink += modifiers.ApplyDamageDealtModifiers(Attacker, Defender, 100, SkillType.Rifle, CombatDamageType.Physical, false, true, true, null!, out _, 0, 0, true), () => sink += CombatFacade.ApplyDamageDealtModifiers(Attacker, Defender, 100, SkillType.Rifle, CombatDamageType.Physical, false, true, true, null!, out _, 0, 0, true)),
                ("ApplyDamageDealtModifiers(13, ability)", () => sink += modifiers.ApplyDamageDealtModifiers(Attacker, Defender, 100, SkillType.Rifle, CombatDamageType.Physical, true, true, true, hostile, out _), () => sink += CombatFacade.ApplyDamageDealtModifiers(Attacker, Defender, 100, SkillType.Rifle, CombatDamageType.Physical, true, true, true, hostile, out _)),
                ("ApplyDamageDealtModifiers(9)", () => sink += modifiers.ApplyDamageDealtModifiers(Attacker, Defender, 100), () => sink += CombatFacade.ApplyDamageDealtModifiers(Attacker, Defender, 100)),
                ("PrepareQueuedWeaponAbilityOpeningAttackAtActivation", () => bonus.PrepareQueuedWeaponAbilityOpeningAttackAtActivation(Attacker, SkillType.Rifle), () => CombatFacade.PrepareQueuedWeaponAbilityOpeningAttackAtActivation(Attacker, SkillType.Rifle)),
                ("StoreQueuedWeaponAbilityActivationCriticalRateBonus", () => bonus.StoreQueuedWeaponAbilityActivationCriticalRateBonus(Attacker, SkillType.Rifle, 5), () => CombatFacade.StoreQueuedWeaponAbilityActivationCriticalRateBonus(Attacker, SkillType.Rifle, 5)),
                ("StoreWeaponAbilityActivationIdleBonuses", () => bonus.StoreWeaponAbilityActivationIdleBonuses(Attacker, true, 1, 2, 3), () => CombatFacade.StoreWeaponAbilityActivationIdleBonuses(Attacker, true, 1, 2, 3)),
                ("ConsumeNextAbilityDamageBonus", () => sink += bonus.ConsumeNextAbilityDamageBonus(Attacker, PerkType.DoubleShot), () => sink += CombatFacade.ConsumeNextAbilityDamageBonus(Attacker, PerkType.DoubleShot)),
                ("GetNextAbilityStaminaCostAdjustment", () => sink += bonus.GetNextAbilityStaminaCostAdjustment(Attacker, PerkType.DoubleShot), () => sink += CombatFacade.GetNextAbilityStaminaCostAdjustment(Attacker, PerkType.DoubleShot)),
                ("ConsumeNextAbilityStaminaCostAdjustment", () => sink += bonus.ConsumeNextAbilityStaminaCostAdjustment(Attacker, PerkType.DoubleShot), () => sink += CombatFacade.ConsumeNextAbilityStaminaCostAdjustment(Attacker, PerkType.DoubleShot)),
                ("GrantNextAbilityDamageBonus", () => bonus.GrantNextAbilityDamageBonus(Attacker, (int)PerkType.DoubleShot, 1, 6), () => CombatFacade.GrantNextAbilityDamageBonus(Attacker, (int)PerkType.DoubleShot, 1, 6)),
                ("GrantNextSkillAbilityBonuses", () => bonus.GrantNextSkillAbilityBonuses(Attacker, (int)SkillType.Rifle, 1, 1, 6), () => CombatFacade.GrantNextSkillAbilityBonuses(Attacker, (int)SkillType.Rifle, 1, 1, 6)),
            };

            foreach (var (name, part, forwarder) in reads)
            {
                var (own, forwarded) = InterleavedPerCall(part, forwarder);
                own.Should().BeGreaterThan(0, $"{name} allocated before the move too");
                ForwarderAddsNothing(name, own, forwarded, 32);
            }

            GC.KeepAlive(sink);
        }
        finally
        {
            DisposeInReverse(scopes);
        }
    }

    private sealed record HitParts(ICombatAbilityHitService Hit, ICombatHitEffectsService Effects);

    // The same world as the timing parts (a container of its own, the constant engine, the clock held still, a status table
    // for both creatures and the stat service that allocates nothing), with the ability hit and hit effects parts. The three
    // tests that use it are named to run after every other combat part test of the fixture: the hit paths run the same status
    // and stat reads, so running them first would change what the JIT has done to the reads those tests compare, and running
    // after the damage roll loop above finds those reads already settled.
    private static HitParts InstallHitParts(List<IDisposable> scopes)
    {
        InstallTimingParts(scopes);
        var container = FacadeContainer.Current;
        return new HitParts(container.Get<ICombatAbilityHitService>(), container.Get<ICombatHitEffectsService>());
    }

    [Test]
    public void WhereAnAbilityHitOrHitEffectAllocatesNothingItsForwarderAllocatesNothing()
    {
        // Run per ability hit roll or per hit. Each allocated nothing before the move either (measured against the base
        // assembly in the same sequence).
        var scopes = new List<IDisposable>();
        try
        {
            var (hit, effects) = InstallHitParts(scopes);
            var sink = 0;
            bool? forced = null;
            var guarded = Private<Action<uint>>("ApplyGuardedHitNextAttackEffects", typeof(uint));
            var sourceStack = Private<Action<uint, uint>>("ApplySourceStatusStackEffects", typeof(uint), typeof(uint));
            var spreadPoison = Private<Action<uint, uint>>("SpreadPoisonFromTarget", typeof(uint), typeof(uint));
            var reads = new (string Name, Action Part, Action Forwarder)[]
            {
                // Ability hit roll.
                ("ApplyRangedAbilityTargetDefenseReduction", () => hit.ApplyRangedAbilityTargetDefenseReduction(Attacker, Defender, SkillType.Rifle), () => CombatFacade.ApplyRangedAbilityTargetDefenseReduction(Attacker, Defender, SkillType.Rifle)),
                ("ApplyGuardedHitNextAttackEffects", () => hit.ApplyGuardedHitNextAttackEffects(Attacker), () => guarded(Attacker)),
                ("SetAbilityHitResolutionOverride", () => hit.SetAbilityHitResolutionOverride(null), () => CombatFacade.SetAbilityHitResolutionOverride(null)),
                ("SetAutoAttackHitResolutionOverride", () => hit.SetAutoAttackHitResolutionOverride(null), () => CombatFacade.SetAutoAttackHitResolutionOverride(null)),
                ("GetAutoAttackHitResolutionOverride", () => forced = hit.GetAutoAttackHitResolutionOverride(), () => forced = CombatFacade.GetAutoAttackHitResolutionOverride()),
                // Hit effects.
                ("ApplyAutoAttackDamageModifiers(0)", () => sink += effects.ApplyAutoAttackDamageModifiers(Attacker, Defender, 0), () => sink += CombatFacade.ApplyAutoAttackDamageModifiers(Attacker, Defender, 0)),
                ("ApplyDamageDealtEffects(triggered)", () => effects.ApplyDamageDealtEffects(Attacker, Defender, 10, SkillType.Rifle, CombatDamageType.Fire, CombatDamageDeliveryType.Triggered), () => CombatFacade.ApplyDamageDealtEffects(Attacker, Defender, 10, SkillType.Rifle, CombatDamageType.Fire, CombatDamageDeliveryType.Triggered)),
                ("ApplyDamageDealtEffects(0)", () => effects.ApplyDamageDealtEffects(Attacker, Defender, 0), () => CombatFacade.ApplyDamageDealtEffects(Attacker, Defender, 0)),
                ("ApplyCriticalHitEffects(0)", () => effects.ApplyCriticalHitEffects(Attacker, Defender, 10, 0), () => CombatFacade.ApplyCriticalHitEffects(Attacker, Defender, 10, 0)),
                ("ApplyNonCriticalAbilityEffects", () => effects.ApplyNonCriticalAbilityEffects(Attacker, Defender, SkillType.Rifle), () => CombatFacade.ApplyNonCriticalAbilityEffects(Attacker, Defender, SkillType.Rifle)),
                ("ApplySourceStatusStackEffects", () => effects.ApplySourceStatusStackEffects(Attacker, Defender), () => sourceStack(Attacker, Defender)),
                ("SpreadPoisonFromTarget", () => effects.SpreadPoisonFromTarget(Attacker, Defender), () => spreadPoison(Attacker, Defender)),
            };

            foreach (var (name, part, forwarder) in reads)
            {
                LeastAllocated(part).Should().Be(0, $"{name} runs per ability hit roll or per hit");
                LeastAllocated(forwarder).Should().Be(0, $"Combat.{name} only forwards");
            }

            GC.KeepAlive(sink);
            GC.KeepAlive(forced);
        }
        finally
        {
            DisposeInReverse(scopes);
        }
    }

    [Test]
    public void WhereAnAbilityHitOrHitEffectAllocatesItsPinnedBytesItsForwarderAddsNothing()
    {
        // Bytes per call in this setup, each the same as the base assembly's in the same sequence and with tiered compilation
        // off: the temporary modifier reads build their closures, the forget query its LINQ iterator, and triggered damage its
        // effect closure on entry (which holds only the target, the damage and its type, not the part), as before the move.
        var scopes = new List<IDisposable>();
        try
        {
            var (_, effects) = InstallHitParts(scopes);
            var sink = 0;
            var forget = Private<Action<uint>>("ForgetHitEffectState", typeof(uint));
            var reads = new (string Name, long Bytes, Action Part, Action Forwarder)[]
            {
                ("ApplyAutoAttackDamageModifiers", 168, () => sink += effects.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Rifle), () => sink += CombatFacade.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Rifle)),
                ("ConsumeSuppressedAutoAttackDamageBonuses", 168, () => effects.ConsumeSuppressedAutoAttackDamageBonuses(Attacker, SkillType.Rifle), () => CombatFacade.ConsumeSuppressedAutoAttackDamageBonuses(Attacker, SkillType.Rifle)),
                ("ApplyTriggeredDamage(0)", 32, () => sink += effects.ApplyTriggeredDamage(Attacker, Defender, 0, CombatDamageType.Physical), () => sink += CombatFacade.ApplyTriggeredDamage(Attacker, Defender, 0, CombatDamageType.Physical)),
                ("ForgetHitEffectState", 192, () => effects.ForgetHitEffectState(Attacker), () => forget(Attacker)),
            };

            foreach (var (name, bytes, part, forwarder) in reads)
            {
                PerCall(part).Should().Be(bytes, $"{name} allocates what it did before the move");
                PerCall(forwarder).Should().Be(bytes, $"Combat.{name} adds nothing to what the part allocates");
            }

            GC.KeepAlive(sink);
        }
        finally
        {
            DisposeInReverse(scopes);
        }
    }

    [Test]
    public void WhereAnAbilityHitOrHitEffectMemberAllocatesMoreItsForwarderAddsNothing()
    {
        // These read the skill's attribute by reflection (IsWeaponSkillType), the status table, the NPC stats of an accuracy
        // or evasion read, or run the damage taken part, as before the move. What they allocate per call depends on what the
        // JIT has done with those reads by the time they run (with tiered compilation off, the base and this head allocate the
        // same bytes for each), so it moves with test order, and between rounds by up to about 50 bytes. A forwarder is one
        // call with the same parameter types (CombatPartForwardingTests), so it could only add a closure and its delegate
        // (88 bytes or more): the margin is below that.
        // The direct auto-attack is 8 bytes more than before the move: the splash rider's closure class, created on entry,
        // now also holds the part.
        var scopes = new List<IDisposable>();
        try
        {
            var (hit, effects) = InstallHitParts(scopes);
            var sink = 0;
            var recentDamagers = Private<Action<uint>>("ApplyRecentDamagerDefeatedEnemyEffects", typeof(uint));
            var reads = new (string Name, Action Part, Action Forwarder)[]
            {
                ("ApplyRecentDamagerDefeatedEnemyEffects", () => effects.ApplyRecentDamagerDefeatedEnemyEffects(Defender), () => recentDamagers(Defender)),
                ("GetAbilityCriticalRate", () => sink += hit.GetAbilityCriticalRate(Attacker, SkillType.Rifle, false, 0, Defender), () => sink += CombatFacade.GetAbilityCriticalRate(Attacker, SkillType.Rifle, false, 0, Defender)),
                ("CalculateAbilityCriticalRating", () => sink += hit.CalculateAbilityCriticalRating(Attacker, SkillType.Rifle, false, 0, Defender), () => sink += CombatFacade.CalculateAbilityCriticalRating(Attacker, SkillType.Rifle, false, 0, Defender)),
                ("TryResolveAbilityHit", () => sink += hit.TryResolveAbilityHit(Attacker, Defender, SkillType.Rifle, PerkType.DoubleShot, out _) ? 1 : 0, () => sink += CombatFacade.TryResolveAbilityHit(Attacker, Defender, SkillType.Rifle, PerkType.DoubleShot, out _) ? 1 : 0),
                ("GetHitChanceAgainstSunderedTargetAdjustment", () => sink += hit.GetHitChanceAgainstSunderedTargetAdjustment(Attacker, Defender), () => sink += CombatFacade.GetHitChanceAgainstSunderedTargetAdjustment(Attacker, Defender)),
                ("GetCriticalRateAgainstSunderedTargetAdjustment", () => sink += hit.GetCriticalRateAgainstSunderedTargetAdjustment(Attacker, Defender), () => sink += CombatFacade.GetCriticalRateAgainstSunderedTargetAdjustment(Attacker, Defender)),
                ("ConsumeAutoAttackCycleDamageBonus", () => sink += effects.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Rifle), () => sink += CombatFacade.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Rifle)),
                ("ApplyDamageDealtEffects(auto)", () => effects.ApplyDamageDealtEffects(Attacker, Defender, 10, SkillType.Rifle), () => CombatFacade.ApplyDamageDealtEffects(Attacker, Defender, 10, SkillType.Rifle)),
                ("ApplyDamageDealtEffects(ability)", () => effects.ApplyDamageDealtEffects(Attacker, Defender, 10, SkillType.Rifle, CombatDamageType.Physical, CombatDamageDeliveryType.Direct, true), () => CombatFacade.ApplyDamageDealtEffects(Attacker, Defender, 10, SkillType.Rifle, CombatDamageType.Physical, CombatDamageDeliveryType.Direct, true)),
                ("ApplyCriticalHitEffects", () => effects.ApplyCriticalHitEffects(Attacker, Defender, 10, 2, true, SkillType.Rifle), () => CombatFacade.ApplyCriticalHitEffects(Attacker, Defender, 10, 2, true, SkillType.Rifle)),
                ("ApplyDefeatedEnemyEffects", () => effects.ApplyDefeatedEnemyEffects(Attacker, Defender), () => CombatFacade.ApplyDefeatedEnemyEffects(Attacker, Defender)),
                ("ApplyTriggeredDamage", () => sink += effects.ApplyTriggeredDamage(Attacker, Defender, 10, CombatDamageType.Physical, SkillType.Rifle), () => sink += CombatFacade.ApplyTriggeredDamage(Attacker, Defender, 10, CombatDamageType.Physical, SkillType.Rifle)),
            };

            foreach (var (name, part, forwarder) in reads)
            {
                var (own, forwarded) = InterleavedPerCall(part, forwarder);
                own.Should().BeGreaterThan(0, $"{name} allocated before the move too");
                ForwarderAddsNothing(name, own, forwarded, 64);
            }

            GC.KeepAlive(sink);
        }
        finally
        {
            DisposeInReverse(scopes);
        }
    }

    // The same world as the hit parts (a container of its own, the constant engine, the clock held still, a status table for
    // both creatures and the stat service that allocates nothing), with the damage taken part. The two tests that use it are
    // named to run after every other combat part test of the fixture, for the reason the hit part tests give.
    private static ICombatDamageTakenService InstallDamageTakenPart(List<IDisposable> scopes)
    {
        InstallTimingParts(scopes);
        return FacadeContainer.Current.Get<ICombatDamageTakenService>();
    }

    [Test]
    public void WhereTheDamageTakenPartAllocatesItsPinnedBytesItsForwarderAddsNothing()
    {
        // Bytes per call in this setup, each the same as the base assembly's in the same sequence (with tiered compilation on
        // and off) unless a comment says otherwise. A landed hit builds the closures of the redirect and share stages on entry
        // (32 and 24 bytes; neither holds the part), the deflection record the nearby ally guard's closure (24), the exposed
        // bonus read the temporary modifier's closures, the forget query its LINQ iterator and list, and the combat log line
        // its strings, as before the move.
        // The physical-to-Force conversion is 8 bytes more than before the move (64 to 72): the closure class it creates on
        // entry now also holds the part, because its converted hit calls the recorder and the hit effects part.
        var scopes = new List<IDisposable>();
        try
        {
            var taken = InstallDamageTakenPart(scopes);
            var sink = 0;
            var consume = Private<Func<uint, SkillType, int>>("ConsumeGuardedHitNextSkillAbilityExposedDamageBonus", typeof(uint), typeof(SkillType));
            var forget = Private<Action<uint>>("ForgetDamageTakenState", typeof(uint));
            var reads = new (string Name, long Bytes, Action Part, Action Forwarder)[]
            {
                ("ApplyDamageTakenModifiers", 56, () => sink += taken.ApplyDamageTakenModifiers(Defender, 10, Attacker), () => sink += CombatFacade.ApplyDamageTakenModifiers(Defender, 10, Attacker)),
                ("ApplyDamageTakenModifiers(fire)", 56, () => sink += taken.ApplyDamageTakenModifiers(Defender, 10, Attacker, CombatDamageType.Fire), () => sink += CombatFacade.ApplyDamageTakenModifiers(Defender, 10, Attacker, CombatDamageType.Fire)),
                ("ApplyDamageTakenModifiers(force)", 56, () => sink += taken.ApplyDamageTakenModifiers(Defender, 10, Attacker, CombatDamageType.Force), () => sink += CombatFacade.ApplyDamageTakenModifiers(Defender, 10, Attacker, CombatDamageType.Force)),
                ("ApplyDamageTakenModifiers(typed)", 56, () => sink += taken.ApplyDamageTakenModifiers(Defender, 10, Attacker, CombatDamageType.Physical, CombatDamageDeliveryType.Direct, -10, true, true), () => sink += CombatFacade.ApplyDamageTakenModifiers(Defender, 10, Attacker, CombatDamageType.Physical, CombatDamageDeliveryType.Direct, -10, true, true)),
                ("ApplyDamageTakenModifiers(transferred)", 32, () => sink += taken.ApplyDamageTakenModifiers(Defender, 10, Attacker, CombatDamageType.Physical, CombatDamageDeliveryType.Transferred), () => sink += CombatFacade.ApplyDamageTakenModifiers(Defender, 10, Attacker, CombatDamageType.Physical, CombatDamageDeliveryType.Transferred)),
                ("TrackDeflection", 24, () => taken.TrackDeflection(Defender, DeflectionSource.Melee), () => CombatFacade.TrackDeflection(Defender, DeflectionSource.Melee)),
                ("ConsumeGuardedHitNextSkillAbilityExposedDamageBonus", 56, () => sink += taken.ConsumeGuardedHitNextSkillAbilityExposedDamageBonus(Defender, SkillType.Rifle), () => sink += consume(Defender, SkillType.Rifle)),
                ("ApplyIncomingPhysicalToForceConversion", 72, () => { var damage = 10; sink += taken.ApplyIncomingPhysicalToForceConversion(Attacker, Defender, CombatDamageType.Physical, ref damage); }, () => { var damage = 10; sink += CombatFacade.ApplyIncomingPhysicalToForceConversion(Attacker, Defender, CombatDamageType.Physical, ref damage); }),
                ("ForgetDamageTakenState", 184, () => taken.ForgetDamageTakenState(Defender), () => forget(Defender)),
                ("BuildDeflectingReturnCombatLogMessage", 968, () => sink += taken.BuildDeflectingReturnCombatLogMessage(Attacker, Defender, Attacker, 5).Length, () => sink += CombatFacade.BuildDeflectingReturnCombatLogMessage(Attacker, Defender, Attacker, 5).Length),
            };

            foreach (var (name, bytes, part, forwarder) in reads)
            {
                PerCall(part).Should().Be(bytes, $"{name} allocates what it did before the move");
                PerCall(forwarder).Should().Be(bytes, $"Combat.{name} adds nothing to what the part allocates");
            }

            GC.KeepAlive(sink);
        }
        finally
        {
            DisposeInReverse(scopes);
        }
    }

    [Test]
    public void WhereTheDamageTakenPartAllocatesMoreItsForwarderAddsNothing()
    {
        // Deflecting Return reads the Embattled stack count from the status table, as before the move, so what it allocates
        // per call depends on what the JIT has done with those reads by the time it runs (336 bytes on the base and 344 here
        // when run alone, with tiered compilation on or off; less after the fixture's other tests). It is 8 bytes more than
        // before the move: the closure class it creates on entry for its message builder now also holds the part, because
        // the builder calls the part's own BuildDeflectingReturnCombatLogMessage. The forwarder is checked on the upper side
        // only (ForwarderAddsNothing): it could only add a closure and its delegate (88 bytes or more).
        var scopes = new List<IDisposable>();
        try
        {
            var taken = InstallDamageTakenPart(scopes);
            var sink = 0;
            var reads = new (string Name, Action Part, Action Forwarder)[]
            {
                ("ApplyRangedDeflectionReflection", () => sink += taken.ApplyRangedDeflectionReflection(Defender, Attacker, SkillType.Rifle), () => sink += CombatFacade.ApplyRangedDeflectionReflection(Defender, Attacker, SkillType.Rifle)),
            };

            foreach (var (name, part, forwarder) in reads)
            {
                var (own, forwarded) = InterleavedPerCall(part, forwarder);
                own.Should().BeGreaterThan(0, $"{name} allocated before the move too");
                ForwarderAddsNothing(name, own, forwarded, 64);
            }

            GC.KeepAlive(sink);
        }
        finally
        {
            DisposeInReverse(scopes);
        }
    }

    [Test]
    public void WhereTheDamageTakenPartAllocatesNothingItsForwarderAllocatesNothing()
    {
        // Run per damage roll or per damage event. Each allocated nothing before the move either (measured against the base
        // assembly in the same sequence).
        var scopes = new List<IDisposable>();
        try
        {
            var taken = InstallDamageTakenPart(scopes);
            var sink = 0;
            Func<int> forceDefense = () => 80;
            var hostile = Private<Func<uint, uint, bool>>("IsHostileAttackSource", typeof(uint), typeof(uint));
            var used = Private<Action<uint, StatType, StatType, StatType>>("ApplyAbilityUsedAttackDeflection", typeof(uint), typeof(StatType), typeof(StatType), typeof(StatType));
            var reads = new (string Name, Action Part, Action Forwarder)[]
            {
                ("ApplyDamageTakenModifiers(unlanded)", () => sink += taken.ApplyDamageTakenModifiers(Defender, 10, Attacker, CombatDamageType.Physical, CombatDamageDeliveryType.Direct, 0, false), () => sink += CombatFacade.ApplyDamageTakenModifiers(Defender, 10, Attacker, CombatDamageType.Physical, CombatDamageDeliveryType.Direct, 0, false)),
                ("ApplyDamageTakenModifiers(0)", () => sink += taken.ApplyDamageTakenModifiers(Defender, 0), () => sink += CombatFacade.ApplyDamageTakenModifiers(Defender, 0)),
                ("ApplyTypedLeadershipDamageTakenModifier", () => sink += taken.ApplyTypedLeadershipDamageTakenModifier(Defender, 10, CombatDamageType.Physical), () => sink += CombatFacade.ApplyTypedLeadershipDamageTakenModifier(Defender, 10, CombatDamageType.Physical)),
                ("ApplyTypedLeadershipDamageTakenModifier(force)", () => sink += taken.ApplyTypedLeadershipDamageTakenModifier(Defender, 10, CombatDamageType.Force), () => sink += CombatFacade.ApplyTypedLeadershipDamageTakenModifier(Defender, 10, CombatDamageType.Force)),
                ("ApplyTypedLeadershipDamageTakenModifier(fire)", () => sink += taken.ApplyTypedLeadershipDamageTakenModifier(Defender, 10, CombatDamageType.Fire), () => sink += CombatFacade.ApplyTypedLeadershipDamageTakenModifier(Defender, 10, CombatDamageType.Fire)),
                ("ApplyDamageReflectionEffects", () => taken.ApplyDamageReflectionEffects(Attacker, Defender, 10, CombatDamageType.Physical), () => CombatFacade.ApplyDamageReflectionEffects(Attacker, Defender, 10, CombatDamageType.Physical)),
                ("ApplyDamageReflectionEffects(force)", () => taken.ApplyDamageReflectionEffects(Attacker, Defender, 10, CombatDamageType.Force), () => CombatFacade.ApplyDamageReflectionEffects(Attacker, Defender, 10, CombatDamageType.Force)),
                ("ApplyDamageOverTimeTakenModifiers", () => sink += taken.ApplyDamageOverTimeTakenModifiers(Defender, 10, CombatDamageType.Physical), () => sink += CombatFacade.ApplyDamageOverTimeTakenModifiers(Defender, 10, CombatDamageType.Physical)),
                ("ApplyDamageOverTimeTakenModifiers(out)", () => sink += taken.ApplyDamageOverTimeTakenModifiers(Defender, 10, CombatDamageType.Force, out _), () => sink += CombatFacade.ApplyDamageOverTimeTakenModifiers(Defender, 10, CombatDamageType.Force, out _)),
                ("TryPreventFatalDamageAndGrantTemporaryHP", () => sink += taken.TryPreventFatalDamageAndGrantTemporaryHP(Defender, 10, false) ? 1 : 0, () => sink += CombatFacade.TryPreventFatalDamageAndGrantTemporaryHP(Defender, 10, false) ? 1 : 0),
                ("ApplyDamageTakenEffects", () => taken.ApplyDamageTakenEffects(Defender, Attacker, 10), () => CombatFacade.ApplyDamageTakenEffects(Defender, Attacker, 10)),
                ("ApplyDamageTakenEffects()", () => taken.ApplyDamageTakenEffects(), () => CombatFacade.ApplyDamageTakenEffects()),
                ("ApplyLowHPDamageTakenEffects", () => taken.ApplyLowHPDamageTakenEffects(Defender, 10), () => CombatFacade.ApplyLowHPDamageTakenEffects(Defender, 10)),
                ("ApplyLowHPGuardEffectFromProtectedTarget", () => taken.ApplyLowHPGuardEffectFromProtectedTarget(Attacker, Defender, 10), () => CombatFacade.ApplyLowHPGuardEffectFromProtectedTarget(Attacker, Defender, 10)),
                ("TrackGuardedHit", () => taken.TrackGuardedHit(Defender), () => CombatFacade.TrackGuardedHit(Defender)),
                ("TrackAvoidedAttack", () => taken.TrackAvoidedAttack(Defender, Attacker), () => CombatFacade.TrackAvoidedAttack(Defender, Attacker)),
                ("ApplyMeleeDamageTakenEffects", () => taken.ApplyMeleeDamageTakenEffects(Defender, Attacker), () => CombatFacade.ApplyMeleeDamageTakenEffects(Defender, Attacker)),
                ("ApplyGuardedHitModifiers", () => sink += taken.ApplyGuardedHitModifiers(Defender, Attacker, 10, CombatDamageType.Physical, true), () => sink += CombatFacade.ApplyGuardedHitModifiers(Defender, Attacker, 10, CombatDamageType.Physical, true)),
                ("IsHostileAttackSource", () => sink += taken.IsHostileAttackSource(Defender, Attacker) ? 1 : 0, () => sink += hostile(Defender, Attacker) ? 1 : 0),
                ("GetGuardDamageReductionPercent", () => sink += taken.GetGuardDamageReductionPercent(Defender), () => sink += CombatFacade.GetGuardDamageReductionPercent(Defender)),
                ("HasRecentGuardedHit", () => sink += taken.HasRecentGuardedHit(Defender, 6f) ? 1 : 0, () => sink += CombatFacade.HasRecentGuardedHit(Defender, 6f) ? 1 : 0),
                ("HasRecentDeflection", () => sink += taken.HasRecentDeflection(Defender, DeflectionSource.Melee, 6f) ? 1 : 0, () => sink += CombatFacade.HasRecentDeflection(Defender, DeflectionSource.Melee, 6f) ? 1 : 0),
                ("ApplyIncomingPhysicalToForceDefenseConversion", () => sink += taken.ApplyIncomingPhysicalToForceDefenseConversion(Defender, CombatDamageType.Physical, 100, forceDefense), () => sink += CombatFacade.ApplyIncomingPhysicalToForceDefenseConversion(Defender, CombatDamageType.Physical, 100, forceDefense)),
                ("ApplyAbilityUsedAttackDeflection", () => taken.ApplyAbilityUsedAttackDeflection(Attacker, StatType.AbilityUsedAttackDeflection, StatType.AbilityUsedAttackDeflectionDurationSeconds, StatType.Invalid), () => used(Attacker, StatType.AbilityUsedAttackDeflection, StatType.AbilityUsedAttackDeflectionDurationSeconds, StatType.Invalid)),
                ("ApplyAbilityGrantedAttackDeflectionEffects", () => taken.ApplyAbilityGrantedAttackDeflectionEffects(Attacker, DeflectionSource.Ranged), () => CombatFacade.ApplyAbilityGrantedAttackDeflectionEffects(Attacker, DeflectionSource.Ranged)),
            };

            foreach (var (name, part, forwarder) in reads)
            {
                LeastAllocated(part).Should().Be(0, $"{name} runs per damage roll or per damage event");
                LeastAllocated(forwarder).Should().Be(0, $"Combat.{name} only forwards");
            }

            GC.KeepAlive(sink);
        }
        finally
        {
            DisposeInReverse(scopes);
        }
    }

    // The same world as the hit parts (a container of its own, the constant engine, the clock held still, a status table for
    // both creatures and the stat service that allocates nothing), with the ability impact part. The three tests that use it
    // are named to run after every other combat part test of the fixture, for the reason the hit part tests give: a hit runs
    // the same status and stat reads, so they cannot change what the JIT has done to the reads those tests compare.
    private static ICombatAbilityImpactService InstallImpactPart(List<IDisposable> scopes)
    {
        InstallTimingParts(scopes);
        return FacadeContainer.Current.Get<ICombatAbilityImpactService>();
    }

    private static AbilityDetail ImpactAbility(SkillType skill, bool area) => new()
    {
        SkillType = skill,
        IsHostileAbility = true,
        IsAreaAbility = area,
        IsSingleTargetAbility = !area,
        MaxRange = 5f
    };

    private static AbilityImpactSummary ImpactSummary(SkillType skill) => new()
    {
        Ability = ImpactAbility(skill, true),
        SkillType = skill,
        IsAreaAbility = true,
        ImpactedTargetCount = 3
    };

    [Test]
    public void WhereTheImpactPartAllocatesNothingItsForwarderAllocatesNothing()
    {
        // Run per ability hit, per target or per finished impact. Each allocated nothing before the move either (measured
        // against the base assembly in the same sequence, with tiered compilation on and off).
        var scopes = new List<IDisposable>();
        try
        {
            var impact = InstallImpactPart(scopes);
            var sink = 0;
            var rifle = ImpactAbility(SkillType.Rifle, false);
            var summaries = new[] { SkillType.Throwing, SkillType.Saberstaff, SkillType.Spear, SkillType.Rifle }.Select(ImpactSummary).ToArray();
            var empty = new AbilityImpactSummary { SkillType = SkillType.Rifle };
            var reads = new List<(string Name, Action Part, Action Forwarder)>
            {
                ("GetAbilityImpactBaseDamageBonus(null)", () => sink += impact.GetAbilityImpactBaseDamageBonus(Attacker, Defender, null!, SkillType.Rifle), () => sink += CombatFacade.GetAbilityImpactBaseDamageBonus(Attacker, Defender, null!, SkillType.Rifle)),
                ("GetHighResourceAbilityDamageBonus", () => sink += impact.GetHighResourceAbilityDamageBonus(Attacker, rifle), () => sink += CombatFacade.GetHighResourceAbilityDamageBonus(Attacker, rifle)),
                ("GetHighResourceAbilityDamageBonus()", () => sink += impact.GetHighResourceAbilityDamageBonus(Attacker), () => sink += CombatFacade.GetHighResourceAbilityDamageBonus(Attacker)),
                ("GetCostlyAbilityDamageBonus", () => sink += impact.GetCostlyAbilityDamageBonus(Attacker, rifle, SkillType.Rifle), () => sink += CombatFacade.GetCostlyAbilityDamageBonus(Attacker, rifle, SkillType.Rifle)),
                ("ApplyHostileAbilityPartyBuff", () => impact.ApplyHostileAbilityPartyBuff(Attacker, rifle, 10, false), () => CombatFacade.ApplyHostileAbilityPartyBuff(Attacker, rifle, 10, false)),
                ("ApplyHostileAbilityPartyBuff(status)", () => impact.ApplyHostileAbilityPartyBuff(Attacker, rifle, 0, true), () => CombatFacade.ApplyHostileAbilityPartyBuff(Attacker, rifle, 0, true)),
                ("ApplyHostileAbilityPartyBuff(miss)", () => impact.ApplyHostileAbilityPartyBuff(Attacker, rifle, 0, false), () => CombatFacade.ApplyHostileAbilityPartyBuff(Attacker, rifle, 0, false)),
                ("ApplySpearDisablerSuppressionRiders", () => impact.ApplySpearDisablerSuppressionRiders(Attacker, Defender), () => CombatFacade.ApplySpearDisablerSuppressionRiders(Attacker, Defender)),
                ("IsTargetAttackingAllyForStatusAppliedRiders", () => sink += impact.IsTargetAttackingAllyForStatusAppliedRiders(Attacker, Defender) ? 1 : 0, () => sink += CombatFacade.IsTargetAttackingAllyForStatusAppliedRiders(Attacker, Defender) ? 1 : 0),
                ("ApplyStatusAppliedTargetStaminaDrain", () => impact.ApplyStatusAppliedTargetStaminaDrain(Attacker, Defender, StatusEffectCategory.Debuff), () => CombatFacade.ApplyStatusAppliedTargetStaminaDrain(Attacker, Defender, StatusEffectCategory.Debuff)),
                ("ApplyAreaHitHaste", () => impact.ApplyAreaHitHaste(Attacker, 3), () => CombatFacade.ApplyAreaHitHaste(Attacker, 3)),
                ("ApplyAbilityImpactEffects(empty)", () => impact.ApplyAbilityImpactEffects(Attacker, empty), () => CombatFacade.ApplyAbilityImpactEffects(Attacker, empty)),
            };
            foreach (var summary in summaries)
                reads.Add(($"ApplyAbilityImpactEffects({summary.SkillType})", () => impact.ApplyAbilityImpactEffects(Attacker, summary), () => CombatFacade.ApplyAbilityImpactEffects(Attacker, summary)));

            foreach (var (name, part, forwarder) in reads)
            {
                LeastAllocated(part).Should().Be(0, $"{name} runs per ability hit, per target or per impact");
                LeastAllocated(forwarder).Should().Be(0, $"Combat.{name} only forwards");
            }

            GC.KeepAlive(sink);
        }
        finally
        {
            DisposeInReverse(scopes);
        }
    }

    [Test]
    public void WhereTheImpactPartAllocatesItsPinnedBytesItsForwarderAddsNothing()
    {
        // Bytes per call in this setup, each the same as the base assembly's in the same sequence with tiered compilation on and
        // off, and the same whether the test runs alone or after the rest of the fixture: the ally check asks the party service,
        // the base damage bonus reads the guarded-hit bonus's temporary modifiers, and the forget query builds its LINQ iterators
        // and lists, as before the move. No closure of a moved method captures the part.
        var scopes = new List<IDisposable>();
        try
        {
            var impact = InstallImpactPart(scopes);
            var sink = 0;
            var forget = Private<Action<uint>>("ForgetImpactState", typeof(uint));
            var rifle = ImpactAbility(SkillType.Rifle, false);
            var reads = new (string Name, long Bytes, Action Part, Action Forwarder)[]
            {
                ("IsAttackingAllyOf", 72, () => sink += impact.IsAttackingAllyOf(Attacker, Defender) ? 1 : 0, () => sink += CombatFacade.IsAttackingAllyOf(Attacker, Defender) ? 1 : 0),
                ("GetAbilityImpactBaseDamageBonus(Rifle)", 56, () => sink += impact.GetAbilityImpactBaseDamageBonus(Attacker, Defender, rifle, SkillType.Rifle), () => sink += CombatFacade.GetAbilityImpactBaseDamageBonus(Attacker, Defender, rifle, SkillType.Rifle)),
                ("ForgetImpactState", 344, () => impact.ForgetImpactState(Defender), () => forget(Defender)),
            };

            foreach (var (name, bytes, part, forwarder) in reads)
            {
                PerCall(part).Should().Be(bytes, $"{name} allocates what it did before the move");
                PerCall(forwarder).Should().Be(bytes, $"Combat.{name} adds nothing to what the part allocates");
            }

            GC.KeepAlive(sink);
        }
        finally
        {
            DisposeInReverse(scopes);
        }
    }

    [Test]
    public void WhereTheImpactPartAllocatesMoreItsForwarderAddsNothing()
    {
        // A damaging hit records it, reads the target's status table, the skill's attribute by reflection (IsWeaponSkillType) and
        // the riders' temporary modifiers, as before the move; with tiered compilation off the base and this head allocate the same
        // bytes for each (1285 to 1892), with it on the bytes depend on what the JIT has done by the time they run (by up to about
        // 60 bytes on either side). The Twin Blade bonus reads the Reversal Cut temporary modifiers (96 or 168 bytes, base and here
        // alike) and the single-target Lightsaber bonus the target's status table (208 bytes in a probe of its own on both, 144
        // after this fixture's other tests). A hit with no damage records the hit and reads its riders' temporary modifiers and the
        // skill's attribute (432 bytes in a probe on both, 432 to 456 here depending on what ran before). A forwarder is one call
        // with the same parameter types (CombatPartForwardingTests), so it could only add a closure and its delegate (88 bytes or
        // more): the margin is below that.
        var scopes = new List<IDisposable>();
        try
        {
            var impact = InstallImpactPart(scopes);
            var sink = 0;
            var applied = new[] { typeof(DazedStatusEffect) };
            var none = Array.Empty<Type>();
            var rifle = ImpactAbility(SkillType.Rifle, false);
            var twinBlade = ImpactAbility(SkillType.TwinBlade, false);
            var lightsaber = ImpactAbility(SkillType.Lightsaber, false);
            var reads = new List<(string Name, Action Part, Action Forwarder)>
            {
                ("GetAbilityImpactBaseDamageBonus(TwinBlade)", () => sink += impact.GetAbilityImpactBaseDamageBonus(Attacker, Defender, twinBlade, SkillType.TwinBlade), () => sink += CombatFacade.GetAbilityImpactBaseDamageBonus(Attacker, Defender, twinBlade, SkillType.TwinBlade)),
                ("GetAbilityImpactBaseDamageBonus(Lightsaber)", () => sink += impact.GetAbilityImpactBaseDamageBonus(Attacker, Defender, lightsaber, SkillType.Lightsaber), () => sink += CombatFacade.GetAbilityImpactBaseDamageBonus(Attacker, Defender, lightsaber, SkillType.Lightsaber)),
                ("ApplySuccessfulAbilityImpactRiders(no damage)",
                    () => impact.ApplySuccessfulAbilityImpactRiders(Attacker, Defender, rifle, SkillType.Rifle, CombatDamageType.Physical, 0, false, null!, none, false, false, StatusEffectCategory.None, false, true),
                    () => CombatFacade.ApplySuccessfulAbilityImpactRiders(Attacker, Defender, rifle, SkillType.Rifle, CombatDamageType.Physical, 0, false, null!, none, false, false, StatusEffectCategory.None, false, true)),
            };
            foreach (var (skill, area) in new[]
                     {
                         (SkillType.Rifle, false), (SkillType.Rifle, true), (SkillType.Katar, false), (SkillType.Lightsaber, false),
                         (SkillType.Spear, true), (SkillType.Throwing, true), (SkillType.TwinBlade, false), (SkillType.Vibroknife, false)
                     })
            {
                var ability = ImpactAbility(skill, area);
                reads.Add(($"ApplySuccessfulAbilityImpactRiders({skill}, {(area ? "area" : "single")})",
                    () => impact.ApplySuccessfulAbilityImpactRiders(Attacker, Defender, ability, skill, CombatDamageType.Physical, 10, true, typeof(DazedStatusEffect), applied, true, true, StatusEffectCategory.Debuff),
                    () => CombatFacade.ApplySuccessfulAbilityImpactRiders(Attacker, Defender, ability, skill, CombatDamageType.Physical, 10, true, typeof(DazedStatusEffect), applied, true, true, StatusEffectCategory.Debuff)));
            }

            foreach (var (name, part, forwarder) in reads)
            {
                var (own, forwarded) = InterleavedPerCall(part, forwarder);
                own.Should().BeGreaterThan(0, $"{name} allocated before the move too");
                ForwarderAddsNothing(name, own, forwarded, 64);
            }

            GC.KeepAlive(sink);
        }
        finally
        {
            DisposeInReverse(scopes);
        }
    }

    [Test]
    public void TheDamageTypeDetailsAreReadWithoutAllocating()
    {
        var sink = 0;

        LeastAllocated(() => sink += CombatDamageType.Fire.IsPhysicalDamageType() ? 1 : 0).Should().Be(0);
        LeastAllocated(() => sink += (int)CombatDamageType.Fire.GetDefenseDamageType()).Should().Be(0);
        LeastAllocated(() => sink += (int)CombatDamageType.Force.GetDetails().NativeDamageType).Should().Be(0);
        GC.KeepAlive(sink);
    }

    [Test]
    public void TheWeaponDelayLookupAllocatesNothing()
    {
        var rifle = _container.Get<IItemService>().RifleBaseItemTypes[0];
        var sink = 0;

        Allocated(() => sink += WeaponDelay.GetWeaponDelay(rifle).HasValue ? 1 : 0).Should().Be(0);
        GC.KeepAlive(sink);
    }
}
