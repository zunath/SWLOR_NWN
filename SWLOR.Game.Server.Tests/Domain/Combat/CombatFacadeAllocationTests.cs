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
using FeatType = SWLOR.NWN.API.NWScript.Enum.FeatType;
using static SWLOR.Game.Server.Composition.GameServices;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The combat services run per attack and per damage event, so a call that only reads a table must not allocate:
/// <c>GameServices</c> resolves the instance from the cache the facade used and the instance reads its own fields. These
/// tests count the bytes the current thread allocates over many calls, on the part itself and on the accessor path.
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
            ("CalculateHitRate", () => sink += CombatRules.CalculateHitRate(120, 100, 0), () => sink += CombatRules.CalculateHitRate(120, 100, 0)),
            ("CalculateCriticalRate", () => sink += CombatRules.CalculateCriticalRate(30, 10, 50, 2), () => sink += CombatRules.CalculateCriticalRate(30, 10, 50, 2)),
            ("CalculateAbilityCriticalChance", () => sink += CombatRules.CalculateAbilityCriticalChance(10), () => sink += CombatRules.CalculateAbilityCriticalChance(10)),
            ("ClampCriticalDamagePercentAdjustment", () => sink += CombatRules.ClampCriticalDamagePercentAdjustment(250), () => sink += CombatRules.ClampCriticalDamagePercentAdjustment(250)),
            ("ApplyPercentDamageAdjustment", () => sink += CombatRules.ApplyPercentDamageAdjustment(100, -30), () => sink += CombatRules.ApplyPercentDamageAdjustment(100, -30)),
            ("ApplyOrdinaryAttackDamageMultiplier", () => sink += CombatRules.ApplyOrdinaryAttackDamageMultiplier(100, 25), () => sink += CombatRules.ApplyOrdinaryAttackDamageMultiplier(100, 25)),
            ("CapOutgoingDamageBonus", () => sink += CombatRules.CapOutgoingDamageBonus(100, 250, 5), () => sink += CombatRules.CapOutgoingDamageBonus(100, 250, 5)),
            ("ApplyCombinedDamageTakenAdjustment", () => sink += CombatRules.ApplyCombinedDamageTakenAdjustment(100, -50, -50), () => sink += CombatRules.ApplyCombinedDamageTakenAdjustment(100, -50, -50)),
            ("ApplyTargetHPDamageAdjustment", () => sink += CombatRules.ApplyTargetHPDamageAdjustment(100, 20, 100, 25, 50), () => sink += CombatRules.ApplyTargetHPDamageAdjustment(100, 20, 100, 25, 50)),
            ("ApplyDefenseIgnore", () => sink += CombatRules.ApplyDefenseIgnore(100, 25), () => sink += CombatRules.ApplyDefenseIgnore(100, 25)),
            ("ApplyTriggeredDamageTargetAdjustment", () => sink += CombatRules.ApplyTriggeredDamageTargetAdjustment(100, -50, null).Damage, () => sink += CombatRules.ApplyTriggeredDamageTargetAdjustment(100, -50, null).Damage),
            ("GetIncomingPhysicalToForceConversionPortion", () => sink += CombatRules.GetIncomingPhysicalToForceConversionPortion(100, 30), () => sink += CombatRules.GetIncomingPhysicalToForceConversionPortion(100, 30)),
            ("CalculateMaxHPHealingBudget", () => sink += CombatRules.CalculateMaxHPHealingBudget(200, 15), () => sink += CombatRules.CalculateMaxHPHealingBudget(200, 15)),
            ("CalculateDamageEligibleForHealing", () => sink += CombatRules.CalculateDamageEligibleForHealing(50, 30, 5), () => sink += CombatRules.CalculateDamageEligibleForHealing(50, 30, 5)),
            ("CalculateCappedDamageDerivedHealingAmount", () => sink += CombatRules.CalculateCappedDamageDerivedHealingAmount(100, 20, 50), () => sink += CombatRules.CalculateCappedDamageDerivedHealingAmount(100, 20, 50)),
            ("CalculateHitPointsSpentThroughTemporaryHitPoints", () => sink += CombatRules.CalculateHitPointsSpentThroughTemporaryHitPoints(50, 20), () => sink += CombatRules.CalculateHitPointsSpentThroughTemporaryHitPoints(50, 20)),
            ("CalculateHitPointSpendStaminaRestore", () => sink += CombatRules.CalculateHitPointSpendStaminaRestore(100, 10, 10, 20), () => sink += CombatRules.CalculateHitPointSpendStaminaRestore(100, 10, 10, 20)),
            ("CalculateAbilityHitStaminaRestore", () => sink += CombatRules.CalculateAbilityHitStaminaRestore(10, 2, 20), () => sink += CombatRules.CalculateAbilityHitStaminaRestore(10, 2, 20)),
            ("GetRangedDeflectionReflectionAmount", () => sink += CombatRules.GetRangedDeflectionReflectionAmount(100, 50, 40, 100), () => sink += CombatRules.GetRangedDeflectionReflectionAmount(100, 50, 40, 100)),
            ("GetRangedDeflectionReflectionRates", () => sink += CombatRules.GetRangedDeflectionReflectionRates(30, 50, 5, 3, 10, 0, 0).ReflectPercent, () => sink += CombatRules.GetRangedDeflectionReflectionRates(30, 50, 5, 3, 10, 0, 0).ReflectPercent),
            ("IsRangedDamageSkill", () => sink += CombatRules.IsRangedDamageSkill(SkillType.Devices) ? 1 : 0, () => sink += CombatRules.IsRangedDamageSkill(SkillType.Devices) ? 1 : 0),
            ("IsRangedWeaponSkill", () => sink += CombatRules.IsRangedWeaponSkill(SkillType.Rifle) ? 1 : 0, () => sink += CombatRules.IsRangedWeaponSkill(SkillType.Rifle) ? 1 : 0),
            ("GetWeaponEngagementRange", () => sink += (int)CombatRules.GetWeaponEngagementRange(SkillType.Rifle), () => sink += (int)CombatRules.GetWeaponEngagementRange(SkillType.Rifle)),
            ("MatchesNextSkillAbilityCriticalRateSkill", () => sink += CombatRules.MatchesNextSkillAbilityCriticalRateSkill(SkillType.Rifle, SkillType.Invalid, true) ? 1 : 0, () => sink += CombatRules.MatchesNextSkillAbilityCriticalRateSkill(SkillType.Rifle, SkillType.Invalid, true) ? 1 : 0),
            ("AbilityUsedTriggerSkillMatches", () => sink += CombatRules.AbilityUsedTriggerSkillMatches(SkillType.Pistol, SkillType.Invalid, true, true) ? 1 : 0, () => sink += CombatRules.AbilityUsedTriggerSkillMatches(SkillType.Pistol, SkillType.Invalid, true, true) ? 1 : 0),
            ("CalculateAttackDelayMilliseconds", () => sink += CombatRules.CalculateAttackDelayMilliseconds(220, 180, 20, 10), () => sink += CombatRules.CalculateAttackDelayMilliseconds(220, 180, 20, 10)),
            ("CalculateEffectiveAttackDelay", () => sink += CombatRules.CalculateEffectiveAttackDelay(2400), () => sink += CombatRules.CalculateEffectiveAttackDelay(2400)),
            ("CalculateEffectiveAttackDelay(bool)", () => sink += CombatRules.CalculateEffectiveAttackDelay(2400, true), () => sink += CombatRules.CalculateEffectiveAttackDelay(2400, true)),
            ("CalculateAttackSwingDelay", () => sink += CombatRules.CalculateAttackSwingDelay(900), () => sink += CombatRules.CalculateAttackSwingDelay(900)),
            ("CalculateAttacksPerSwing", () => sink += CombatRules.CalculateAttacksPerSwing(700, debt, out debt), () => sink += CombatRules.CalculateAttacksPerSwing(700, debt, out debt)),
            ("CapAttacksPerSwingForLimitedAttackEffect", () => sink += CombatRules.CapAttacksPerSwingForLimitedAttackEffect(3, 1, 2, 1), () => sink += CombatRules.CapAttacksPerSwingForLimitedAttackEffect(3, 1, 2, 1)),
        };

        foreach (var (name, rule, forwarder) in reads)
        {
            Allocated(rule).Should().Be(0, $"CombatRules.{name} runs per attack");
            Allocated(forwarder).Should().Be(0, $"{name} through GameServices allocates nothing");
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
            () => sink += CombatRules.CalculateDamageRange(100, 100, 20, 100, 20, 2).Item2);
        ForwarderAddsNothing("CalculateDamageRange", rangeRule, rangeForwarder, 32);
        var (skillRule, skillForwarder) = InterleavedPerCall(
            () => sink += CombatRules.IsWeaponSkillType(SkillType.Rifle) ? 1 : 0,
            () => sink += CombatRules.IsWeaponSkillType(SkillType.Rifle) ? 1 : 0);
        ForwarderAddsNothing("IsWeaponSkillType", skillRule, skillForwarder, 32);
        GC.KeepAlive(sink);
    }

    [Test]
    public void TheDamageTypeTableReadsAllocateNothing()
    {
        GameServices.CombatDamageCatalog.LoadDamageTypes();
        var sink = 0;

        Allocated(() => sink += GameServices.CombatDamageCatalog.GetDefenseDamageTypes().Count).Should().Be(0);
        Allocated(() => sink += GameServices.CombatDamageCatalog.GetAllDamageTypes().Count).Should().Be(0);
        Allocated(() => sink += GameServices.CombatDamageCatalog.GetDefenseDamageTypes()[0] == CombatDamageType.Physical ? 1 : 0).Should().Be(0);
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
                ("GetMinimumHitRate", () => sink += queries.GetMinimumHitRate(Attacker), () => sink += GameServices.CombatQueries.GetMinimumHitRate(Attacker)),
                ("IsAttackerBehindTarget", () => sink += queries.IsAttackerBehindTarget(Attacker, Defender) ? 1 : 0, () => sink += GameServices.CombatQueries.IsAttackerBehindTarget(Attacker, Defender) ? 1 : 0),
                ("IsAttackerBesideTarget", () => sink += queries.IsAttackerBesideTarget(Attacker, Defender) ? 1 : 0, () => sink += GameServices.CombatQueries.IsAttackerBesideTarget(Attacker, Defender) ? 1 : 0),
                ("IsTargetNotFacingAttacker", () => sink += queries.IsTargetNotFacingAttacker(Attacker, Defender) ? 1 : 0, () => sink += GameServices.CombatQueries.IsTargetNotFacingAttacker(Attacker, Defender) ? 1 : 0),
                ("HasDamageImmunity", () => sink += queries.HasDamageImmunity(Defender, CombatDamageType.Physical) ? 1 : 0, () => sink += GameServices.CombatQueries.HasDamageImmunity(Defender, CombatDamageType.Physical) ? 1 : 0),
                ("GetRangedAttackDamageFlatAdjustment", () => sink += queries.GetRangedAttackDamageFlatAdjustment(Attacker, SkillType.Rifle), () => sink += GameServices.CombatQueries.GetRangedAttackDamageFlatAdjustment(Attacker, SkillType.Rifle)),
                ("GetRangedAttackDefenseIgnorePercentAdjustment", () => sink += queries.GetRangedAttackDefenseIgnorePercentAdjustment(Attacker, SkillType.Rifle), () => sink += GameServices.CombatQueries.GetRangedAttackDefenseIgnorePercentAdjustment(Attacker, SkillType.Rifle)),
                ("GetPowerAttackDMGBonus", () => sink += queries.GetPowerAttackDMGBonus(Attacker), () => sink += GameServices.CombatQueries.GetPowerAttackDMGBonus(Attacker)),
                ("GetAbilitySkillType", () => sink += (int)queries.GetAbilitySkillType(Attacker, hostile), () => sink += (int)GameServices.CombatQueries.GetAbilitySkillType(Attacker, hostile)),
                ("GetEquippedWeaponSkillType", () => sink += (int)queries.GetEquippedWeaponSkillType(Attacker), () => sink += (int)GameServices.CombatQueries.GetEquippedWeaponSkillType(Attacker)),
                ("HasEquippedWeaponForAbility", () => sink += queries.HasEquippedWeaponForAbility(Attacker) ? 1 : 0, () => sink += GameServices.CombatQueries.HasEquippedWeaponForAbility(Attacker) ? 1 : 0),
                ("GetAbilityAttackWeapon", () => sink += (int)queries.GetAbilityAttackWeapon(Attacker, SkillType.Rifle), () => sink += (int)GameServices.CombatQueries.GetAbilityAttackWeapon(Attacker, SkillType.Rifle)),
                ("GetWeaponDamageAbilityType", () => sink += (int)queries.GetWeaponDamageAbilityType(Attacker, BaseItem.Longsword), () => sink += (int)GameServices.CombatQueries.GetWeaponDamageAbilityType(Attacker, BaseItem.Longsword)),
                ("GetWeaponAccuracyAbilityType", () => sink += (int)queries.GetWeaponAccuracyAbilityType(Attacker, BaseItem.Longsword), () => sink += (int)GameServices.CombatQueries.GetWeaponAccuracyAbilityType(Attacker, BaseItem.Longsword)),
                ("GetMiscDMGBonus", () => sink += queries.GetMiscDMGBonus(Attacker, BaseItem.Longsword), () => sink += GameServices.CombatQueries.GetMiscDMGBonus(Attacker, BaseItem.Longsword)),
                ("GetPerkAdjustedAbilityScore", () => sink += queries.GetPerkAdjustedAbilityScore(Attacker), () => sink += GameServices.CombatQueries.GetPerkAdjustedAbilityScore(Attacker)),
                ("GetWeaponImpactProfile", () => sink += queries.GetWeaponImpactProfile(Attacker) == null ? 1 : 0, () => sink += WeaponImpactProfile.Get(Attacker) == null ? 1 : 0),
                ("GetAbilityStaminaPaid", () => sink += resources.GetAbilityStaminaPaid(Attacker, hostile), () => sink += GameServices.CombatResources.GetAbilityStaminaPaid(Attacker, hostile)),
                ("GetPaidAbilityStaminaCost", () => sink += resources.GetPaidAbilityStaminaCost(Attacker, hostile), () => sink += GameServices.CombatResources.GetPaidAbilityStaminaCost(Attacker, hostile)),
                ("GetNextAbilityFPCostAdjustment", () => sink += resources.GetNextAbilityFPCostAdjustment(Attacker, SkillType.Invalid), () => sink += GameServices.CombatResources.GetNextAbilityFPCostAdjustment(Attacker, SkillType.Invalid)),
                ("RestoreAbilityHitStamina", () => sink += resources.RestoreAbilityHitStamina(Attacker, hostile, 0), () => sink += GameServices.CombatResources.RestoreAbilityHitStamina(Attacker, hostile, 0)),
                ("DeferAbilityStaminaCostContext", () => resources.DeferAbilityStaminaCostContext(Attacker, hostile), () => GameServices.CombatResources.DeferAbilityStaminaCostContext(Attacker, hostile)),
                ("CompleteAbilityStaminaCostContext", () => resources.CompleteAbilityStaminaCostContext(Attacker, hostile), () => GameServices.CombatResources.CompleteAbilityStaminaCostContext(Attacker, hostile)),
                ("CompleteDeferredAbilityStaminaCostContext", () => resources.CompleteDeferredAbilityStaminaCostContext(Attacker, hostile), () => GameServices.CombatResources.CompleteDeferredAbilityStaminaCostContext(Attacker, hostile)),
                ("ApplyDamageDerivedHealing", () => sink += resources.ApplyDamageDerivedHealing(Attacker, 10, 10), () => sink += GameServices.CombatResources.ApplyDamageDerivedHealing(Attacker, 10, 10)),
                ("ApplyAbilityFPCostStaminaRestore", () => resources.ApplyAbilityFPCostStaminaRestore(Attacker, plain, 0), () => GameServices.CombatResources.ApplyAbilityFPCostStaminaRestore(Attacker, plain, 0)),
            };

            foreach (var (name, part, forwarder) in reads)
            {
                LeastAllocated(part).Should().Be(0, $"{name} runs per attack or per hit");
                LeastAllocated(forwarder).Should().Be(0, $"{name} through GameServices allocates nothing");
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
                ("CanItemTriggerWeaponAbility", () => sink += queries.CanItemTriggerWeaponAbility(0x7F000000, SkillType.Force) ? 1 : 0, () => sink += GameServices.CombatQueries.CanItemTriggerWeaponAbility(0x7F000000, SkillType.Force) ? 1 : 0),
                ("IncludesCombatImpactWeaponDamage", () => sink += queries.IncludesCombatImpactWeaponDamage(SkillType.Rifle, false) ? 1 : 0, () => sink += GameServices.CombatQueries.IncludesCombatImpactWeaponDamage(SkillType.Rifle, false) ? 1 : 0),
                ("GetQueuedAbilityAccuracyAbilityType", () => sink += (int)queries.GetQueuedAbilityAccuracyAbilityType(Attacker, SkillType.Rifle), () => sink += (int)GameServices.CombatQueries.GetQueuedAbilityAccuracyAbilityType(Attacker, SkillType.Rifle)),
                ("GetCombatImpactWeaponDamage", () => sink += queries.GetCombatImpactWeaponDamage(Attacker, SkillType.Force), () => sink += GameServices.CombatQueries.GetCombatImpactWeaponDamage(Attacker, SkillType.Force)),
                ("IsCurrentFPAndStaminaAtOrBelowPercent", () => sink += resources.IsCurrentFPAndStaminaAtOrBelowPercent(Attacker, 50) ? 1 : 0, () => sink += GameServices.CombatResources.IsCurrentFPAndStaminaAtOrBelowPercent(Attacker, 50) ? 1 : 0),
                ("IsCurrentFPAndStaminaAbovePercent", () => sink += resources.IsCurrentFPAndStaminaAbovePercent(Attacker, 50, hostile) ? 1 : 0, () => sink += GameServices.CombatResources.IsCurrentFPAndStaminaAbovePercent(Attacker, 50, hostile) ? 1 : 0),
                ("GetNextAbilityFPCostAdjustment", () => sink += resources.GetNextAbilityFPCostAdjustment(Attacker, SkillType.Force), () => sink += GameServices.CombatResources.GetNextAbilityFPCostAdjustment(Attacker, SkillType.Force)),
                ("RestoreAbilityHitStamina", () => sink += resources.RestoreAbilityHitStamina(Attacker, hostile, 5), () => sink += GameServices.CombatResources.RestoreAbilityHitStamina(Attacker, hostile, 5)),
                ("BeginDamageDerivedHealing", () => resources.BeginDamageDerivedHealing(Attacker, Defender).Dispose(), () => GameServices.CombatResources.BeginDamageDerivedHealing(Attacker, Defender).Dispose()),
                ("BuildCombatLogMessage", () => sink += feedback.BuildCombatLogMessage(Attacker, Attacker, Defender, 1, 50).Length, () => sink += GameServices.CombatFeedback.BuildCombatLogMessage(Attacker, Attacker, Defender, 1, 50).Length),
                ("BuildAbilityCombatLogMessage", () => sink += feedback.BuildAbilityCombatLogMessage(Attacker, Attacker, Defender, "Shot", 3, 50).Length, () => sink += GameServices.CombatFeedback.BuildAbilityCombatLogMessage(Attacker, Attacker, Defender, "Shot", 3, 50).Length),
                ("BuildAbilityNoTargetCombatLogMessage", () => sink += feedback.BuildAbilityNoTargetCombatLogMessage(Attacker, Attacker, "Shot").Length, () => sink += GameServices.CombatFeedback.BuildAbilityNoTargetCombatLogMessage(Attacker, Attacker, "Shot").Length),
                ("BuildAbilityCriticalHitCombatLogMessage", () => sink += feedback.BuildAbilityCriticalHitCombatLogMessage(Attacker, Attacker, Defender, "Shot").Length, () => sink += GameServices.CombatFeedback.BuildAbilityCriticalHitCombatLogMessage(Attacker, Attacker, Defender, "Shot").Length),
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
        forwarded.Should().BeLessThanOrEqualTo(own + margin + own / 20, $"{name} through GameServices adds no closure or delegate to what the part allocates");

    [Test]
    public void TheTimingSuppressionAndActivityReadsAndWritesAllocateNothingDirectlyOrThroughTheCombatForwarders()
    {
        var scopes = new List<IDisposable>();
        try
        {
            var (timing, suppression, activity, hostile) = InstallTimingParts(scopes);
            var sink = 0;
            var reads = new (string Name, Action Part, Action Forwarder)[]
            {
                // Per swing.
                ("ConsumeAttacksPerSwing(2)", () => sink += timing.ConsumeAttacksPerSwing(Attacker, 1500), () => sink += GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(Attacker, 1500)),
                ("ConsumeAttacksPerSwing(4)", () => sink += timing.ConsumeAttacksPerSwing(Attacker, 1500, 1500, true), () => sink += GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(Attacker, 1500, 1500, true)),
                ("ConsumeAttacksPerSwing(11)", () => sink += timing.ConsumeAttacksPerSwing(Attacker, 900, 1500, false, 1500, 2, 0, 2), () => sink += GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(Attacker, 900, 1500, false, 1500, 2, 0, 2)),
                ("ClearAttackSwingDebt", () => timing.ClearAttackSwingDebt(Attacker), () => GameServices.CombatAttackTiming.ClearAttackSwingDebt(Attacker)),
                ("CalculateAttackDelayReduction", () => sink += timing.CalculateAttackDelayReduction(Attacker), () => sink += GameServices.CombatAttackTiming.CalculateAttackDelayReduction(Attacker)),
                ("CalculateOffhandAttackDelayReduction", () => sink += timing.CalculateOffhandAttackDelayReduction(Attacker), () => sink += GameServices.CombatAttackTiming.CalculateOffhandAttackDelayReduction(Attacker)),
                ("GetTargetAttackDelayReduction", () => sink += timing.GetTargetAttackDelayReduction(Attacker, Defender, SkillType.Rifle), () => sink += GameServices.CombatAttackTiming.GetTargetAttackDelayReduction(Attacker, Defender, SkillType.Rifle)),
                ("GetWeaponDelay", () => sink += timing.GetWeaponDelay(Attacker), () => sink += GameServices.CombatAttackTiming.GetWeaponDelay(Attacker)),
                ("IsAttackDelayReductionSuppressed", () => sink += timing.IsAttackDelayReductionSuppressed(Attacker) ? 1 : 0, () => sink += GameServices.CombatAttackTiming.IsAttackDelayReductionSuppressed(Attacker) ? 1 : 0),
                ("ApplyBleedingStatusExpiredEffects", () => timing.ApplyBleedingStatusExpiredEffects(Attacker), () => GameServices.CombatAttackTiming.ApplyBleedingStatusExpiredEffects(Attacker)),
                ("ForgetAttackTimingState", () => timing.ForgetAttackTimingState(Attacker), () => GameServices.CombatAttackTiming.ForgetAttackTimingState(Attacker)),
                // Per attack.
                ("ApplySuppressionStack", () => sink += suppression.ApplySuppressionStack(Attacker, Defender, 0, 6, CombatDamageType.Physical) ? 1 : 0, () => sink += GameServices.CombatSuppression.ApplySuppressionStack(Attacker, Defender, 0, 6, CombatDamageType.Physical) ? 1 : 0),
                ("GetRangedAbilityLongRangeHitChanceAdjustment", () => sink += suppression.GetRangedAbilityLongRangeHitChanceAdjustment(Attacker, Defender, SkillType.Rifle), () => sink += GameServices.CombatSuppression.GetRangedAbilityLongRangeHitChanceAdjustment(Attacker, Defender, SkillType.Rifle)),
                ("GetRangedAbilityLongRangeCriticalRateAdjustment", () => sink += suppression.GetRangedAbilityLongRangeCriticalRateAdjustment(Attacker, Defender, SkillType.Rifle), () => sink += GameServices.CombatSuppression.GetRangedAbilityLongRangeCriticalRateAdjustment(Attacker, Defender, SkillType.Rifle)),
                ("TrackRecentDamageTarget", () => activity.TrackRecentDamageTarget(Attacker, Defender), () => GameServices.CombatActivity.TrackRecentDamageTarget(Attacker, Defender)),
                ("TrackRecentDamageTaken", () => activity.TrackRecentDamageTaken(Defender), () => GameServices.CombatActivity.TrackRecentDamageTaken(Defender)),
                ("TrackCombatActivity", () => activity.TrackCombatActivity(Attacker), () => GameServices.CombatActivity.TrackCombatActivity(Attacker)),
                ("TrackHostileDefensiveCombatEntryActivity", () => activity.TrackHostileDefensiveCombatEntryActivity(Defender, Attacker), () => GameServices.CombatActivity.TrackHostileDefensiveCombatEntryActivity(Defender, Attacker)),
                ("ApplyFirstCombatAttackStaminaRestore", () => activity.ApplyFirstCombatAttackStaminaRestore(Attacker), () => GameServices.CombatActivity.ApplyFirstCombatAttackStaminaRestore(Attacker)),
                ("GetDirectDamageToStatusCategoryOrStealthBonus", () => sink += activity.GetDirectDamageToStatusCategoryOrStealthBonus(Attacker, Defender), () => sink += GameServices.CombatActivity.GetDirectDamageToStatusCategoryOrStealthBonus(Attacker, Defender)),
                ("GetFirstHostileAbilityHitDamageBonus", () => sink += activity.GetFirstHostileAbilityHitDamageBonus(Attacker, hostile), () => sink += GameServices.CombatActivity.GetFirstHostileAbilityHitDamageBonus(Attacker, hostile)),
                ("ApplyFirstHostileAbilityHitCount", () => activity.ApplyFirstHostileAbilityHitCount(Attacker, hostile), () => GameServices.CombatActivity.ApplyFirstHostileAbilityHitCount(Attacker, hostile)),
                ("ForgetOpeningActivityState", () => activity.ForgetOpeningActivityState(Defender), () => GameServices.CombatActivity.ForgetOpeningActivityState(Defender)),
                // Activity reads.
                ("HasRecentDamageTaken", () => sink += activity.HasRecentDamageTaken(Defender, 6f) ? 1 : 0, () => sink += GameServices.CombatActivity.HasRecentDamageTaken(Defender, 6f) ? 1 : 0),
                ("HasRecentDamageTarget", () => sink += activity.HasRecentDamageTarget(Attacker, Defender, 6f) ? 1 : 0, () => sink += GameServices.CombatActivity.HasRecentDamageTarget(Attacker, Defender, 6f) ? 1 : 0),
                ("HasRecentHostileCombatActivity", () => sink += activity.HasRecentHostileCombatActivity(Attacker) ? 1 : 0, () => sink += GameServices.CombatActivity.HasRecentHostileCombatActivity(Attacker) ? 1 : 0),
                ("HasRecentAttackActivity", () => sink += activity.HasRecentAttackActivity(Attacker, 6f) ? 1 : 0, () => sink += GameServices.CombatActivity.HasRecentAttackActivity(Attacker, 6f) ? 1 : 0),
                ("HasRecentCombatAbilityUse", () => sink += activity.HasRecentCombatAbilityUse(Attacker, 6f) ? 1 : 0, () => sink += GameServices.CombatActivity.HasRecentCombatAbilityUse(Attacker, 6f) ? 1 : 0),
                ("GetLastOffensiveActivityAt", () => sink += activity.GetLastOffensiveActivityAt(Attacker).Second, () => sink += GameServices.CombatActivity.GetLastOffensiveActivityAt(Attacker).Second),
                ("GetLastCompletedOffensiveActivityAt", () => sink += activity.GetLastCompletedOffensiveActivityAt(Attacker).Second, () => sink += GameServices.CombatActivity.GetLastCompletedOffensiveActivityAt(Attacker).Second),
                ("IsUsingAbility", () => sink += activity.IsUsingAbility(Attacker) ? 1 : 0, () => sink += GameServices.CombatActivity.IsUsingAbility(Attacker) ? 1 : 0),
            };

            foreach (var (name, part, forwarder) in reads)
            {
                LeastAllocated(part).Should().Be(0, $"{name} runs per swing, per attack or per hit");
                LeastAllocated(forwarder).Should().Be(0, $"{name} through GameServices allocates nothing");
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
            var reads = new (string Name, long Bytes, Action Part, Action Forwarder)[]
            {
                ("CalculateAttackDelay(1)", 136, () => sink += timing.CalculateAttackDelay(Attacker), () => sink += GameServices.CombatAttackTiming.CalculateAttackDelay(Attacker)),
                ("CalculateAttackDelay(2)", 136, () => sink += timing.CalculateAttackDelay(Attacker, -10), () => sink += GameServices.CombatAttackTiming.CalculateAttackDelay(Attacker, -10)),
                ("GetTargetAttackDelayReduction(Throwing)", 152, () => sink += timing.GetTargetAttackDelayReduction(Attacker, Defender, SkillType.Throwing), () => sink += GameServices.CombatAttackTiming.GetTargetAttackDelayReduction(Attacker, Defender, SkillType.Throwing)),
                ("HasTemporaryNextAutoAttackNoDelay", 112, () => sink += timing.HasTemporaryNextAutoAttackNoDelay(Attacker, SkillType.Rifle) ? 1 : 0, () => sink += GameServices.CombatAttackTiming.HasTemporaryNextAutoAttackNoDelay(Attacker, SkillType.Rifle) ? 1 : 0),
                ("ConsumeNextAutoAttackCriticalRateBonus", 56, () => sink += timing.ConsumeNextAutoAttackCriticalRateBonus(Attacker, SkillType.Rifle), () => sink += GameServices.CombatAttackTiming.ConsumeNextAutoAttackCriticalRateBonus(Attacker, SkillType.Rifle)),
                // 32 before the move: the warning's closure also holds the part, to reach the player name service.
                ("HandleParalyze", 40, () => sink += timing.HandleParalyze(Attacker) ? 1 : 0, () => sink += GameServices.CombatAttackTiming.HandleParalyze(Attacker) ? 1 : 0),
                ("ReconcileOverwatchStatus", 216, () => suppression.ReconcileOverwatchStatus(Attacker), () => GameServices.CombatSuppression.ReconcileOverwatchStatus(Attacker)),
                ("ForgetSuppressionState", 528, () => suppression.ForgetSuppressionState(Attacker), () => GameServices.CombatSuppression.ForgetSuppressionState(Attacker)),
                // 320 before the move: the idle-readiness schedule's closure also holds the part.
                ("TrackAttackActivity", 328, () => activity.TrackAttackActivity(Attacker), () => GameServices.CombatActivity.TrackAttackActivity(Attacker)),
                ("TrackHostileAbilityActivity", 328, () => activity.TrackHostileAbilityActivity(Attacker), () => GameServices.CombatActivity.TrackHostileAbilityActivity(Attacker)),
                ("ScheduleIdleReadinessRefresh", 328, () => activity.ScheduleIdleReadinessRefresh(Attacker), () => GameServices.CombatActivity.ScheduleIdleReadinessRefresh(Attacker)),
                ("TrackStealthOpeningWindow", 32, () => activity.TrackStealthOpeningWindow(Attacker), () => GameServices.CombatActivity.TrackStealthOpeningWindow(Attacker)),
                ("GetIdleSkillAbilityBonuses", 56, () => sink += activity.GetIdleSkillAbilityBonuses(Attacker, SkillType.Rifle).DamageBonus, () => sink += GameServices.CombatActivity.GetIdleSkillAbilityBonuses(Attacker, SkillType.Rifle).DamageBonus),
                ("GetIdleAbilityHitChanceAdjustment", 56, () => sink += activity.GetIdleAbilityHitChanceAdjustment(Attacker, SkillType.Rifle), () => sink += GameServices.CombatActivity.GetIdleAbilityHitChanceAdjustment(Attacker, SkillType.Rifle)),
                // 88 before the move: the channel check's closure also holds the part.
                ("HasConfiguredIdleSkillAbilityChannel", 96, () => sink += activity.HasConfiguredIdleSkillAbilityChannel(Attacker) ? 1 : 0, () => sink += GameServices.CombatActivity.HasConfiguredIdleSkillAbilityChannel(Attacker) ? 1 : 0),
                // 24 before the move: the delayed refresh's closure also holds the part.
                ("RestoreIdleReadinessOnPlayerEnter", 32, () => activity.RestoreIdleReadinessOnPlayerEnter(), () => GameServices.CombatActivity.RestoreIdleReadinessOnPlayerEnter()),
                ("PurgeInvalidOffensiveActivity", 480, () => activity.PurgeInvalidOffensiveActivity(), () => GameServices.CombatActivity.PurgeInvalidOffensiveActivity()),
                ("ForgetActivityState", 184, () => activity.ForgetActivityState(Attacker), () => GameServices.CombatActivity.ForgetActivityState(Attacker)),
            };

            foreach (var (name, bytes, part, forwarder) in reads)
            {
                PerCall(part).Should().Be(bytes, $"{name} allocates what it did before the move");
                PerCall(forwarder).Should().Be(bytes, $"{name} through GameServices adds nothing to what the part allocates");
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
            var reads = new (string Name, Action Part, Action Forwarder)[]
            {
                ("ConsumeNextAbilityDelayReductionPercent", () => sink += timing.ConsumeNextAbilityDelayReductionPercent(Attacker, hostile), () => sink += GameServices.CombatAttackTiming.ConsumeNextAbilityDelayReductionPercent(Attacker, hostile)),
                ("HasNextAutoAttackNoDelay", () => sink += timing.HasNextAutoAttackNoDelay(Attacker, SkillType.Rifle) ? 1 : 0, () => sink += GameServices.CombatAttackTiming.HasNextAutoAttackNoDelay(Attacker, SkillType.Rifle) ? 1 : 0),
                ("ConsumeNextAutoAttackNoDelay", () => sink += timing.ConsumeNextAutoAttackNoDelay(Attacker, SkillType.Rifle) ? 1 : 0, () => sink += GameServices.CombatAttackTiming.ConsumeNextAutoAttackNoDelay(Attacker, SkillType.Rifle) ? 1 : 0),
                ("GrantNextAbilityNoDelay", () => timing.GrantNextAbilityNoDelay(Attacker, SkillType.Rifle, 6, 50), () => GameServices.CombatAttackTiming.GrantNextAbilityNoDelay(Attacker, SkillType.Rifle, 6, 50)),
                ("GetSuppressionStackCount", () => sink += suppression.GetSuppressionStackCount(Defender, Attacker), () => sink += GameServices.CombatSuppression.GetSuppressionStackCount(Defender, Attacker)),
                ("ConsumeSuppressionRangedAttackAccuracyAdjustment", () => sink += suppression.ConsumeSuppressionRangedAttackAccuracyAdjustment(Attacker, Defender, SkillType.Rifle), () => sink += GameServices.CombatSuppression.ConsumeSuppressionRangedAttackAccuracyAdjustment(Attacker, Defender, SkillType.Rifle)),
                ("ApplyAutoAttackSuppressionStack", () => suppression.ApplyAutoAttackSuppressionStack(Attacker, Defender, SkillType.Rifle, CombatDamageType.Physical), () => GameServices.CombatSuppression.ApplyAutoAttackSuppressionStack(Attacker, Defender, SkillType.Rifle, CombatDamageType.Physical)),
                ("ReconcileContainmentNetStatuses", () => suppression.ReconcileContainmentNetStatuses(Defender), () => GameServices.CombatSuppression.ReconcileContainmentNetStatuses(Defender)),
                ("ApplyRangedHitSuppressionStack", () => suppression.ApplyRangedHitSuppressionStack(Attacker, Defender, SkillType.Rifle, CombatDamageType.Physical), () => GameServices.CombatSuppression.ApplyRangedHitSuppressionStack(Attacker, Defender, SkillType.Rifle, CombatDamageType.Physical)),
                ("TrackSuppressionAbilityUse", () => suppression.TrackSuppressionAbilityUse(Defender, FrozenNow), () => GameServices.CombatSuppression.TrackSuppressionAbilityUse(Defender, FrozenNow)),
                // 8 bytes more than before the move: it schedules idle readiness, whose closure now also holds the part.
                ("TrackCombatAbilityUse", () => activity.TrackCombatAbilityUse(Attacker, hostile), () => GameServices.CombatActivity.TrackCombatAbilityUse(Attacker, hostile)),
                ("ClearAbilityActivationIdleBonuses", () => activity.ClearAbilityActivationIdleBonuses(Attacker), () => GameServices.CombatActivity.ClearAbilityActivationIdleBonuses(Attacker)),
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
            var reads = new (string Name, Action Part, Action Forwarder)[]
            {
                // Ability bonuses.
                ("GetNextSkillAbilityStaminaCostAdjustment", () => sink += bonus.GetNextSkillAbilityStaminaCostAdjustment(Attacker, SkillType.Rifle, true), () => sink += GameServices.CombatAbilityBonus.GetNextSkillAbilityStaminaCostAdjustment(Attacker, SkillType.Rifle, true)),
                ("ConsumeNextSkillAbilityStaminaCostAdjustment", () => sink += bonus.ConsumeNextSkillAbilityStaminaCostAdjustment(Attacker, SkillType.Rifle), () => sink += GameServices.CombatAbilityBonus.ConsumeNextSkillAbilityStaminaCostAdjustment(Attacker, SkillType.Rifle)),
                ("StoreQueuedWeaponAbilityCriticalRateBonus", () => bonus.StoreQueuedWeaponAbilityCriticalRateBonus(Attacker, SkillType.Rifle, 0), () => GameServices.CombatAbilityBonus.StoreQueuedWeaponAbilityCriticalRateBonus(Attacker, SkillType.Rifle, 0)),
                ("StoreQueuedWeaponAbilityActivationCriticalRateBonus", () => bonus.StoreQueuedWeaponAbilityActivationCriticalRateBonus(Attacker, SkillType.Rifle, 0), () => GameServices.CombatAbilityBonus.StoreQueuedWeaponAbilityActivationCriticalRateBonus(Attacker, SkillType.Rifle, 0)),
                ("ApplyNextAttackGuardedHitEnmityBonus", () => bonus.ApplyNextAttackGuardedHitEnmityBonus(Attacker, Defender, 0), () => GameServices.CombatAbilityBonus.ApplyNextAttackGuardedHitEnmityBonus(Attacker, Defender, 0)),
                ("GetAbilityStatusCategoryDamageBonus", () => sink += bonus.GetAbilityStatusCategoryDamageBonus(Attacker, SkillType.Rifle, StatusEffectCategory.Control), () => sink += GameServices.CombatAbilityBonus.GetAbilityStatusCategoryDamageBonus(Attacker, SkillType.Rifle, StatusEffectCategory.Control)),
                ("GetAbilityStatusCategoryHitChancePercentAdjustment", () => sink += bonus.GetAbilityStatusCategoryHitChancePercentAdjustment(Attacker, SkillType.Rifle, StatusEffectCategory.Control), () => sink += GameServices.CombatAbilityBonus.GetAbilityStatusCategoryHitChancePercentAdjustment(Attacker, SkillType.Rifle, StatusEffectCategory.Control)),
                ("ApplyAbilityRecastDelayModifiers", () => fsink += bonus.ApplyAbilityRecastDelayModifiers(Attacker, hostile, 10f), () => fsink += GameServices.CombatAbilityBonus.ApplyAbilityRecastDelayModifiers(Attacker, hostile, 10f)),
                // Damage roll modifiers.
                ("TryUseIncomingCriticalHitDowngrade", () => sink += modifiers.TryUseIncomingCriticalHitDowngrade(Defender, 2) ? 1 : 0, () => sink += GameServices.CombatDamageModifiers.TryUseIncomingCriticalHitDowngrade(Defender, 2) ? 1 : 0),
                ("ApplyCriticalDamageModifier", () => sink += modifiers.ApplyCriticalDamageModifier(Attacker, 100, 2), () => sink += GameServices.CombatDamageModifiers.ApplyCriticalDamageModifier(Attacker, 100, 2)),
                ("GetAutoAttackCriticalRateAdjustment", () => sink += modifiers.GetAutoAttackCriticalRateAdjustment(Attacker, Defender, 5), () => sink += GameServices.CombatDamageModifiers.GetAutoAttackCriticalRateAdjustment(Attacker, Defender, 5)),
                ("ApplyRangedAttackDefenseIgnore", () => sink += modifiers.ApplyRangedAttackDefenseIgnore(Attacker, 100, SkillType.Rifle), () => sink += GameServices.CombatDamageModifiers.ApplyRangedAttackDefenseIgnore(Attacker, 100, SkillType.Rifle)),
                ("ApplyOrdinaryAttackDamageMultiplier", () => sink += modifiers.ApplyOrdinaryAttackDamageMultiplier(Attacker, 100), () => sink += GameServices.CombatDamageModifiers.ApplyOrdinaryAttackDamageMultiplier(Attacker, 100)),
                ("ApplyAbilityShapeDamageModifier", () => sink += modifiers.ApplyAbilityShapeDamageModifier(Attacker, 100, true, hostile), () => sink += GameServices.CombatDamageModifiers.ApplyAbilityShapeDamageModifier(Attacker, 100, true, hostile)),
                ("ApplyThrowingSingleTargetDamageModifier", () => sink += modifiers.ApplyThrowingSingleTargetDamageModifier(Attacker, SkillType.Throwing, 100, false, null!), () => sink += GameServices.CombatDamageModifiers.ApplyThrowingSingleTargetDamageModifier(Attacker, SkillType.Throwing, 100, false, null!)),
                ("ApplyMimicryAbilityDamageModifier", () => sink += modifiers.ApplyMimicryAbilityDamageModifier(Attacker, 100, SkillType.Mimicry, true), () => sink += GameServices.CombatDamageModifiers.ApplyMimicryAbilityDamageModifier(Attacker, 100, SkillType.Mimicry, true)),
                ("ApplySkillAbilityDamageModifier", () => sink += modifiers.ApplySkillAbilityDamageModifier(Attacker, 100, SkillType.Rifle, true), () => sink += GameServices.CombatDamageModifiers.ApplySkillAbilityDamageModifier(Attacker, 100, SkillType.Rifle, true)),
                ("ApplySideAttackDamageModifier", () => sink += modifiers.ApplySideAttackDamageModifier(Attacker, Defender, SkillType.Rifle, 100), () => sink += GameServices.CombatDamageModifiers.ApplySideAttackDamageModifier(Attacker, Defender, SkillType.Rifle, 100)),
                ("GetSideAttackHitChanceAdjustment", () => sink += modifiers.GetSideAttackHitChanceAdjustment(Attacker, Defender, SkillType.Rifle), () => sink += GameServices.CombatDamageModifiers.GetSideAttackHitChanceAdjustment(Attacker, Defender, SkillType.Rifle)),
                ("GetSideAttackCriticalRateAdjustment", () => sink += modifiers.GetSideAttackCriticalRateAdjustment(Attacker, Defender, SkillType.Rifle), () => sink += GameServices.CombatDamageModifiers.GetSideAttackCriticalRateAdjustment(Attacker, Defender, SkillType.Rifle)),
                ("ApplySideAttackEvasionIgnore", () => sink += modifiers.ApplySideAttackEvasionIgnore(Attacker, Defender, SkillType.Rifle, 100), () => sink += GameServices.CombatDamageModifiers.ApplySideAttackEvasionIgnore(Attacker, Defender, SkillType.Rifle, 100)),
                ("ClearAutoAttackCycleCriticalRateTracker", () => modifiers.ClearAutoAttackCycleCriticalRateTracker(Attacker), () => GameServices.CombatDamageModifiers.ClearAutoAttackCycleCriticalRateTracker(Attacker)),
                ("GetLowHPAttackAdjustment", () => sink += modifiers.GetLowHPAttackAdjustment(Attacker), () => sink += GameServices.CombatDamageModifiers.GetLowHPAttackAdjustment(Attacker)),
                ("GetTargetStatusCriticalRateAdjustment", () => sink += modifiers.GetTargetStatusCriticalRateAdjustment(Attacker, Defender), () => sink += GameServices.CombatDamageModifiers.GetTargetStatusCriticalRateAdjustment(Attacker, Defender)),
                ("ApplyDamageTypeDealtModifiers", () => sink += modifiers.ApplyDamageTypeDealtModifiers(Attacker, 100, CombatDamageType.Poison), () => sink += GameServices.CombatDamageModifiers.ApplyDamageTypeDealtModifiers(Attacker, 100, CombatDamageType.Poison)),
                ("GetNearbyStatusTargetAttackAdjustment", () => sink += modifiers.GetNearbyStatusTargetAttackAdjustment(Attacker), () => sink += GameServices.CombatDamageModifiers.GetNearbyStatusTargetAttackAdjustment(Attacker)),
                ("ApplyTwinBladeAbilityShapeDamageModifier", () => sink += modifiers.ApplyTwinBladeAbilityShapeDamageModifier(Attacker, SkillType.TwinBlade, 100, true, true), () => sink += GameServices.CombatDamageModifiers.ApplyTwinBladeAbilityShapeDamageModifier(Attacker, SkillType.TwinBlade, 100, true, true)),
                ("ApplyThrowingAbilityShapeDamageModifier", () => sink += modifiers.ApplyThrowingAbilityShapeDamageModifier(Attacker, SkillType.Throwing, 100, true), () => sink += GameServices.CombatDamageModifiers.ApplyThrowingAbilityShapeDamageModifier(Attacker, SkillType.Throwing, 100, true)),
                ("ApplyPhysicalAbilityShapeDamageModifier", () => sink += modifiers.ApplyPhysicalAbilityShapeDamageModifier(Attacker, CombatDamageType.Physical, 100, true), () => sink += GameServices.CombatDamageModifiers.ApplyPhysicalAbilityShapeDamageModifier(Attacker, CombatDamageType.Physical, 100, true)),
                ("ApplySkillAreaAbilityDamageModifier", () => sink += modifiers.ApplySkillAreaAbilityDamageModifier(Attacker, SkillType.Rifle, 100, true), () => sink += GameServices.CombatDamageModifiers.ApplySkillAreaAbilityDamageModifier(Attacker, SkillType.Rifle, 100, true)),
                ("ApplyRangedAbilityLongRangeDamageModifier", () => sink += modifiers.ApplyRangedAbilityLongRangeDamageModifier(Attacker, Defender, SkillType.Rifle, 100), () => sink += GameServices.CombatDamageModifiers.ApplyRangedAbilityLongRangeDamageModifier(Attacker, Defender, SkillType.Rifle, 100)),
                ("ApplyAreaAbilityAfterDeflectionDamageModifier", () => sink += modifiers.ApplyAreaAbilityAfterDeflectionDamageModifier(Attacker, SkillType.Rifle, 100, true), () => sink += GameServices.CombatDamageModifiers.ApplyAreaAbilityAfterDeflectionDamageModifier(Attacker, SkillType.Rifle, 100, true)),
                ("ClearRangedRepeatedTargetDamageTracker", () => modifiers.ClearRangedRepeatedTargetDamageTracker(Attacker), () => GameServices.CombatDamageModifiers.ClearRangedRepeatedTargetDamageTracker(Attacker)),
                ("ApplySameTargetPressureDamageEffects", () => modifiers.ApplySameTargetPressureDamageEffects(Attacker, Defender, SkillType.Rifle), () => GameServices.CombatDamageModifiers.ApplySameTargetPressureDamageEffects(Attacker, Defender, SkillType.Rifle)),
            };

            foreach (var (name, part, forwarder) in reads)
            {
                LeastAllocated(part).Should().Be(0, $"{name} runs per attack, per damage roll or per ability");
                LeastAllocated(forwarder).Should().Be(0, $"{name} through GameServices allocates nothing");
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
            var reads = new (string Name, long Bytes, Action Part, Action Forwarder)[]
            {
                ("ConsumeOpeningAutoAttackCriticalDamageAdjustment", 56, () => sink += bonus.ConsumeOpeningAutoAttackCriticalDamageAdjustment(Attacker), () => sink += GameServices.CombatAbilityBonus.ConsumeOpeningAutoAttackCriticalDamageAdjustment(Attacker)),
                ("ClearOpeningAutoAttackModifiers", 336, () => bonus.ClearOpeningAutoAttackModifiers(Attacker), () => GameServices.CombatAbilityBonus.ClearOpeningAutoAttackModifiers(Attacker)),
                ("HasNextAbilityNoStaminaCost", 56, () => sink += bonus.HasNextAbilityNoStaminaCost(Attacker, SkillType.Rifle) ? 1 : 0, () => sink += GameServices.CombatAbilityBonus.HasNextAbilityNoStaminaCost(Attacker, SkillType.Rifle) ? 1 : 0),
                ("ConsumeNextAbilityNoStaminaCost", 56, () => sink += bonus.ConsumeNextAbilityNoStaminaCost(Attacker, SkillType.Rifle) ? 1 : 0, () => sink += GameServices.CombatAbilityBonus.ConsumeNextAbilityNoStaminaCost(Attacker, SkillType.Rifle) ? 1 : 0),
                ("ConsumeNextSkillAbilityBonuses", 56, () => sink += bonus.ConsumeNextSkillAbilityBonuses(Attacker, SkillType.Rifle).DamageBonus, () => sink += GameServices.CombatAbilityBonus.ConsumeNextSkillAbilityBonuses(Attacker, SkillType.Rifle).DamageBonus),
                ("ConsumePersistentNextSkillAbilityCriticalRateBonus", 112, () => sink += bonus.ConsumePersistentNextSkillAbilityCriticalRateBonus(Attacker, SkillType.Rifle), () => sink += GameServices.CombatAbilityBonus.ConsumePersistentNextSkillAbilityCriticalRateBonus(Attacker, SkillType.Rifle)),
                ("GetPersistentNextSkillAbilityCriticalRateBonus", 112, () => sink += bonus.GetPersistentNextSkillAbilityCriticalRateBonus(Attacker, SkillType.Rifle), () => sink += GameServices.CombatAbilityBonus.GetPersistentNextSkillAbilityCriticalRateBonus(Attacker, SkillType.Rifle)),
                ("StoreQueuedWeaponAbilityActivationIdleBonuses", 56, () => bonus.StoreQueuedWeaponAbilityActivationIdleBonuses(Attacker, SkillType.Rifle), () => GameServices.CombatAbilityBonus.StoreQueuedWeaponAbilityActivationIdleBonuses(Attacker, SkillType.Rifle)),
                ("HasWeaponAbilityActivationIdleSnapshot", 56, () => sink += bonus.HasWeaponAbilityActivationIdleSnapshot(Attacker) ? 1 : 0, () => sink += GameServices.CombatAbilityBonus.HasWeaponAbilityActivationIdleSnapshot(Attacker) ? 1 : 0),
                ("GetWeaponAbilityActivationIdleDamageBonus", 56, () => sink += bonus.GetWeaponAbilityActivationIdleDamageBonus(Attacker), () => sink += GameServices.CombatAbilityBonus.GetWeaponAbilityActivationIdleDamageBonus(Attacker)),
                ("GetWeaponAbilityActivationIdleCriticalRateBonus", 56, () => sink += bonus.GetWeaponAbilityActivationIdleCriticalRateBonus(Attacker), () => sink += GameServices.CombatAbilityBonus.GetWeaponAbilityActivationIdleCriticalRateBonus(Attacker)),
                ("GetWeaponAbilityActivationIdleDefenseIgnorePercent", 56, () => sink += bonus.GetWeaponAbilityActivationIdleDefenseIgnorePercent(Attacker), () => sink += GameServices.CombatAbilityBonus.GetWeaponAbilityActivationIdleDefenseIgnorePercent(Attacker)),
                ("ClearWeaponAbilityActivationIdleBonuses", 224, () => bonus.ClearWeaponAbilityActivationIdleBonuses(Attacker), () => GameServices.CombatAbilityBonus.ClearWeaponAbilityActivationIdleBonuses(Attacker)),
                ("ClearQueuedWeaponAbilityActivationBonuses", 280, () => bonus.ClearQueuedWeaponAbilityActivationBonuses(Attacker), () => GameServices.CombatAbilityBonus.ClearQueuedWeaponAbilityActivationBonuses(Attacker)),
                ("ConsumeQueuedWeaponAbilityBonuses", 112, () => sink += bonus.ConsumeQueuedWeaponAbilityBonuses(Attacker, SkillType.Rifle).DamageBonus, () => sink += GameServices.CombatAbilityBonus.ConsumeQueuedWeaponAbilityBonuses(Attacker, SkillType.Rifle).DamageBonus),
                ("ClearQueuedWeaponAbilityAttemptBonuses", 224, () => bonus.ClearQueuedWeaponAbilityAttemptBonuses(Attacker), () => GameServices.CombatAbilityBonus.ClearQueuedWeaponAbilityAttemptBonuses(Attacker)),
                ("ConsumeNextAttackGuardedHitBonuses", 168, () => sink += bonus.ConsumeNextAttackGuardedHitBonuses(Attacker).DMGBonus, () => sink += GameServices.CombatAbilityBonus.ConsumeNextAttackGuardedHitBonuses(Attacker).DMGBonus),
                ("ConsumeNextAttackGuardedHitAutoAttackBonuses", 112, () => sink += bonus.ConsumeNextAttackGuardedHitAutoAttackBonuses(Attacker).DMGBonus, () => sink += GameServices.CombatAbilityBonus.ConsumeNextAttackGuardedHitAutoAttackBonuses(Attacker).DMGBonus),
                ("ConsumeNextAttackGuardedHitCriticalRateBonus", 56, () => sink += bonus.ConsumeNextAttackGuardedHitCriticalRateBonus(Attacker), () => sink += GameServices.CombatAbilityBonus.ConsumeNextAttackGuardedHitCriticalRateBonus(Attacker)),
                ("ConsumeStatusAppliedNextAttackDamageBonus", 56, () => sink += bonus.ConsumeStatusAppliedNextAttackDamageBonus(Attacker), () => sink += GameServices.CombatAbilityBonus.ConsumeStatusAppliedNextAttackDamageBonus(Attacker)),
                ("GetStatusAppliedNextAttackDamageBonus", 56, () => sink += bonus.GetStatusAppliedNextAttackDamageBonus(Attacker), () => sink += GameServices.CombatAbilityBonus.GetStatusAppliedNextAttackDamageBonus(Attacker)),
                ("GetAbilityDamageFlatAdjustment", 64, () => sink += bonus.GetAbilityDamageFlatAdjustment(Attacker, PerkType.DoubleShot, SkillType.Rifle), () => sink += GameServices.CombatAbilityBonus.GetAbilityDamageFlatAdjustment(Attacker, PerkType.DoubleShot, SkillType.Rifle)),
                ("GetAbilityStaminaCostFlatAdjustment(perk)", 64, () => sink += bonus.GetAbilityStaminaCostFlatAdjustment(Attacker, PerkType.DoubleShot), () => sink += GameServices.CombatAbilityBonus.GetAbilityStaminaCostFlatAdjustment(Attacker, PerkType.DoubleShot)),
                ("GetAbilityStaminaCostFlatAdjustment(ability)", 64, () => sink += bonus.GetAbilityStaminaCostFlatAdjustment(Attacker, hostile), () => sink += GameServices.CombatAbilityBonus.GetAbilityStaminaCostFlatAdjustment(Attacker, hostile)),
                ("GetAbilityStatusDurationPercentAdjustment", 64, () => sink += bonus.GetAbilityStatusDurationPercentAdjustment(Attacker, PerkType.DoubleShot, SkillType.Rifle, typeof(StunnedStatusEffect), Array.Empty<Type>(), null!), () => sink += GameServices.CombatAbilityBonus.GetAbilityStatusDurationPercentAdjustment(Attacker, PerkType.DoubleShot, SkillType.Rifle, typeof(StunnedStatusEffect), Array.Empty<Type>(), null!)),
                ("GetAbilityRecastDelayFlatAdjustment", 64, () => fsink += bonus.GetAbilityRecastDelayFlatAdjustment(Attacker, PerkType.DoubleShot), () => fsink += GameServices.CombatAbilityBonus.GetAbilityRecastDelayFlatAdjustment(Attacker, PerkType.DoubleShot)),
                ("GetAbilityDamageBonus", 312, () => sink += bonus.GetAbilityDamageBonus(Attacker, SkillType.Rifle), () => sink += GameServices.CombatAbilityBonus.GetAbilityDamageBonus(Attacker, SkillType.Rifle)),
                ("ApplyTargetStatusAttackModifiers", 152, () => sink += modifiers.ApplyTargetStatusAttackModifiers(Attacker, Defender, 100, SkillType.Vibroblade), () => sink += GameServices.CombatDamageModifiers.ApplyTargetStatusAttackModifiers(Attacker, Defender, 100, SkillType.Vibroblade)),
                ("GetLowFPAttackAdjustment", 312, () => sink += modifiers.GetLowFPAttackAdjustment(Attacker), () => sink += GameServices.CombatDamageModifiers.GetLowFPAttackAdjustment(Attacker)),
                ("ApplyStatusSourceDefenseModifiers", 64, () => sink += modifiers.ApplyStatusSourceDefenseModifiers(Attacker, Defender, 100), () => sink += GameServices.CombatDamageModifiers.ApplyStatusSourceDefenseModifiers(Attacker, Defender, 100)),
                ("ForgetDamageModifierState", 256, () => modifiers.ForgetDamageModifierState(Attacker), () => GameServices.CombatDamageModifiers.ForgetDamageModifierState(Attacker)),
            };

            foreach (var (name, bytes, part, forwarder) in reads)
            {
                PerCall(part).Should().Be(bytes, $"{name} allocates what it did before the move");
                PerCall(forwarder).Should().Be(bytes, $"{name} through GameServices adds nothing to what the part allocates");
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
        // GetAbilityDefenseIgnorePercentAdjustment was pinned at 504 bytes both ways; in two full unit test runs of P2-CB9b the
        // part measured 504 and the forwarder, measured after it, 454 and 376 (below the part: the JIT had optimized the reads
        // in between), so it is compared here, and its old pin stays as an upper budget for both, so an allocation the part
        // and its forwarder gained together still fails.
        // Named to run last in the fixture: its million damage rolls let the JIT finish optimizing the status reads, which
        // changes what the pinned reads above allocate when they run after it.
        var scopes = new List<IDisposable>();
        try
        {
            var (bonus, modifiers, hostile) = InstallBonusParts(scopes);
            var sink = 0;
            var reads = new (string Name, Action Part, Action Forwarder)[]
            {
                ("GetAbilityDefenseIgnorePercentAdjustment", () => sink += bonus.GetAbilityDefenseIgnorePercentAdjustment(Attacker, PerkType.DoubleShot, SkillType.Rifle, Defender), () => sink += GameServices.CombatAbilityBonus.GetAbilityDefenseIgnorePercentAdjustment(Attacker, PerkType.DoubleShot, SkillType.Rifle, Defender)),
                ("PrepareOpeningAutoAttack", () => sink += bonus.PrepareOpeningAutoAttack(Attacker, SkillType.Rifle), () => sink += GameServices.CombatAbilityBonus.PrepareOpeningAutoAttack(Attacker, SkillType.Rifle)),
                ("GetQueuedWeaponAbilityActivationHitChanceAdjustment", () => sink += bonus.GetQueuedWeaponAbilityActivationHitChanceAdjustment(Attacker, SkillType.Rifle, hostile), () => sink += GameServices.CombatAbilityBonus.GetQueuedWeaponAbilityActivationHitChanceAdjustment(Attacker, SkillType.Rifle, hostile)),
                ("ApplyCriticalDamageModifier", () => sink += modifiers.ApplyCriticalDamageModifier(Attacker, 100, 2, SkillType.Rifle, Defender), () => sink += GameServices.CombatDamageModifiers.ApplyCriticalDamageModifier(Attacker, 100, 2, SkillType.Rifle, Defender)),
                ("GetSkillCriticalRatePercentAdjustment", () => sink += modifiers.GetSkillCriticalRatePercentAdjustment(Attacker, SkillType.Rifle), () => sink += GameServices.CombatDamageModifiers.GetSkillCriticalRatePercentAdjustment(Attacker, SkillType.Rifle)),
                ("ApplyBackAttackDamageModifier", () => sink += modifiers.ApplyBackAttackDamageModifier(Attacker, Defender, SkillType.Rifle, 100), () => sink += GameServices.CombatDamageModifiers.ApplyBackAttackDamageModifier(Attacker, Defender, SkillType.Rifle, 100)),
                ("GetBackAttackCriticalRateAdjustment", () => sink += modifiers.GetBackAttackCriticalRateAdjustment(Attacker, Defender, SkillType.Rifle), () => sink += GameServices.CombatDamageModifiers.GetBackAttackCriticalRateAdjustment(Attacker, Defender, SkillType.Rifle)),
                ("PrepareAutoAttackCycleCriticalRate", () => sink += modifiers.PrepareAutoAttackCycleCriticalRate(Attacker, SkillType.Rifle), () => sink += GameServices.CombatDamageModifiers.PrepareAutoAttackCycleCriticalRate(Attacker, SkillType.Rifle)),
                ("GetSameTargetPressureWeaponAbilityDamageBonus", () => sink += modifiers.GetSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle), () => sink += GameServices.CombatDamageModifiers.GetSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle)),
                ("ConsumeSameTargetPressureWeaponAbilityDamageBonus", () => modifiers.ConsumeSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle, 10), () => GameServices.CombatDamageModifiers.ConsumeSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle, 10)),
                ("CalculateDamageWithCriticalMitigation", () => sink += modifiers.CalculateDamageWithCriticalMitigation(Defender, 100, 50, 20, 80, 10, 2).Damage, () => sink += GameServices.CombatDamageModifiers.CalculateDamageWithCriticalMitigation(Defender, 100, 50, 20, 80, 10, 2).Damage),
                ("ApplyStatusSourceAccuracyModifiers", () => sink += modifiers.ApplyStatusSourceAccuracyModifiers(Attacker, Defender, 100), () => sink += GameServices.CombatDamageModifiers.ApplyStatusSourceAccuracyModifiers(Attacker, Defender, 100)),
                ("ApplyDamageDealtModifiers(13)", () => sink += modifiers.ApplyDamageDealtModifiers(Attacker, Defender, 100, SkillType.Rifle, CombatDamageType.Physical, false, true, true, null!, out _, 0, 0, true), () => sink += GameServices.CombatDamageModifiers.ApplyDamageDealtModifiers(Attacker, Defender, 100, SkillType.Rifle, CombatDamageType.Physical, false, true, true, null!, out _, 0, 0, true)),
                ("ApplyDamageDealtModifiers(13, ability)", () => sink += modifiers.ApplyDamageDealtModifiers(Attacker, Defender, 100, SkillType.Rifle, CombatDamageType.Physical, true, true, true, hostile, out _), () => sink += GameServices.CombatDamageModifiers.ApplyDamageDealtModifiers(Attacker, Defender, 100, SkillType.Rifle, CombatDamageType.Physical, true, true, true, hostile, out _)),
                ("ApplyDamageDealtModifiers(9)", () => sink += modifiers.ApplyDamageDealtModifiers(Attacker, Defender, 100), () => sink += GameServices.CombatDamageModifiers.ApplyDamageDealtModifiers(Attacker, Defender, 100)),
                ("PrepareQueuedWeaponAbilityOpeningAttackAtActivation", () => bonus.PrepareQueuedWeaponAbilityOpeningAttackAtActivation(Attacker, SkillType.Rifle), () => GameServices.CombatAbilityBonus.PrepareQueuedWeaponAbilityOpeningAttackAtActivation(Attacker, SkillType.Rifle)),
                ("StoreQueuedWeaponAbilityActivationCriticalRateBonus", () => bonus.StoreQueuedWeaponAbilityActivationCriticalRateBonus(Attacker, SkillType.Rifle, 5), () => GameServices.CombatAbilityBonus.StoreQueuedWeaponAbilityActivationCriticalRateBonus(Attacker, SkillType.Rifle, 5)),
                ("StoreWeaponAbilityActivationIdleBonuses", () => bonus.StoreWeaponAbilityActivationIdleBonuses(Attacker, true, 1, 2, 3), () => GameServices.CombatAbilityBonus.StoreWeaponAbilityActivationIdleBonuses(Attacker, true, 1, 2, 3)),
                ("ConsumeNextAbilityDamageBonus", () => sink += bonus.ConsumeNextAbilityDamageBonus(Attacker, PerkType.DoubleShot), () => sink += GameServices.CombatAbilityBonus.ConsumeNextAbilityDamageBonus(Attacker, PerkType.DoubleShot)),
                ("GetNextAbilityStaminaCostAdjustment", () => sink += bonus.GetNextAbilityStaminaCostAdjustment(Attacker, PerkType.DoubleShot), () => sink += GameServices.CombatAbilityBonus.GetNextAbilityStaminaCostAdjustment(Attacker, PerkType.DoubleShot)),
                ("ConsumeNextAbilityStaminaCostAdjustment", () => sink += bonus.ConsumeNextAbilityStaminaCostAdjustment(Attacker, PerkType.DoubleShot), () => sink += GameServices.CombatAbilityBonus.ConsumeNextAbilityStaminaCostAdjustment(Attacker, PerkType.DoubleShot)),
                ("GrantNextAbilityDamageBonus", () => bonus.GrantNextAbilityDamageBonus(Attacker, (int)PerkType.DoubleShot, 1, 6), () => GameServices.CombatAbilityBonus.GrantNextAbilityDamageBonus(Attacker, (int)PerkType.DoubleShot, 1, 6)),
                ("GrantNextSkillAbilityBonuses", () => bonus.GrantNextSkillAbilityBonuses(Attacker, (int)SkillType.Rifle, 1, 1, 6), () => GameServices.CombatAbilityBonus.GrantNextSkillAbilityBonuses(Attacker, (int)SkillType.Rifle, 1, 1, 6)),
            };
            var budgets = new Dictionary<string, long> { ["GetAbilityDefenseIgnorePercentAdjustment"] = 504 };

            foreach (var (name, part, forwarder) in reads)
            {
                var (own, forwarded) = InterleavedPerCall(part, forwarder);
                own.Should().BeGreaterThan(0, $"{name} allocated before the move too");
                ForwarderAddsNothing(name, own, forwarded, 32);
                if (budgets.TryGetValue(name, out var budget))
                {
                    own.Should().BeLessThanOrEqualTo(budget, $"{name} allocates no more than it did before the move");
                    forwarded.Should().BeLessThanOrEqualTo(budget, $"{name} through GameServices allocates no more than it did before the move");
                }
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
            var reads = new (string Name, Action Part, Action Forwarder)[]
            {
                // Ability hit roll.
                ("ApplyRangedAbilityTargetDefenseReduction", () => hit.ApplyRangedAbilityTargetDefenseReduction(Attacker, Defender, SkillType.Rifle), () => GameServices.CombatAbilityHit.ApplyRangedAbilityTargetDefenseReduction(Attacker, Defender, SkillType.Rifle)),
                ("ApplyGuardedHitNextAttackEffects", () => hit.ApplyGuardedHitNextAttackEffects(Attacker), () => GameServices.CombatAbilityHit.ApplyGuardedHitNextAttackEffects(Attacker)),
                ("SetAbilityHitResolutionOverride", () => hit.SetAbilityHitResolutionOverride(null), () => GameServices.CombatAbilityHit.SetAbilityHitResolutionOverride(null)),
                ("SetAutoAttackHitResolutionOverride", () => hit.SetAutoAttackHitResolutionOverride(null), () => GameServices.CombatAbilityHit.SetAutoAttackHitResolutionOverride(null)),
                ("GetAutoAttackHitResolutionOverride", () => forced = hit.GetAutoAttackHitResolutionOverride(), () => forced = GameServices.CombatAbilityHit.GetAutoAttackHitResolutionOverride()),
                // Hit effects.
                ("ApplyAutoAttackDamageModifiers(0)", () => sink += effects.ApplyAutoAttackDamageModifiers(Attacker, Defender, 0), () => sink += GameServices.CombatHitEffects.ApplyAutoAttackDamageModifiers(Attacker, Defender, 0)),
                ("ApplyDamageDealtEffects(triggered)", () => effects.ApplyDamageDealtEffects(Attacker, Defender, 10, SkillType.Rifle, CombatDamageType.Fire, CombatDamageDeliveryType.Triggered), () => GameServices.CombatHitEffects.ApplyDamageDealtEffects(Attacker, Defender, 10, SkillType.Rifle, CombatDamageType.Fire, CombatDamageDeliveryType.Triggered)),
                ("ApplyDamageDealtEffects(0)", () => effects.ApplyDamageDealtEffects(Attacker, Defender, 0), () => GameServices.CombatHitEffects.ApplyDamageDealtEffects(Attacker, Defender, 0)),
                ("ApplyCriticalHitEffects(0)", () => effects.ApplyCriticalHitEffects(Attacker, Defender, 10, 0), () => GameServices.CombatHitEffects.ApplyCriticalHitEffects(Attacker, Defender, 10, 0)),
                ("ApplyNonCriticalAbilityEffects", () => effects.ApplyNonCriticalAbilityEffects(Attacker, Defender, SkillType.Rifle), () => GameServices.CombatHitEffects.ApplyNonCriticalAbilityEffects(Attacker, Defender, SkillType.Rifle)),
                ("ApplySourceStatusStackEffects", () => effects.ApplySourceStatusStackEffects(Attacker, Defender), () => GameServices.CombatHitEffects.ApplySourceStatusStackEffects(Attacker, Defender)),
                ("SpreadPoisonFromTarget", () => effects.SpreadPoisonFromTarget(Attacker, Defender), () => GameServices.CombatHitEffects.SpreadPoisonFromTarget(Attacker, Defender)),
            };

            foreach (var (name, part, forwarder) in reads)
            {
                LeastAllocated(part).Should().Be(0, $"{name} runs per ability hit roll or per hit");
                LeastAllocated(forwarder).Should().Be(0, $"{name} through GameServices allocates nothing");
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
            var reads = new (string Name, long Bytes, Action Part, Action Forwarder)[]
            {
                ("ApplyAutoAttackDamageModifiers", 168, () => sink += effects.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Rifle), () => sink += GameServices.CombatHitEffects.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Rifle)),
                ("ConsumeSuppressedAutoAttackDamageBonuses", 168, () => effects.ConsumeSuppressedAutoAttackDamageBonuses(Attacker, SkillType.Rifle), () => GameServices.CombatHitEffects.ConsumeSuppressedAutoAttackDamageBonuses(Attacker, SkillType.Rifle)),
                ("ApplyTriggeredDamage(0)", 32, () => sink += effects.ApplyTriggeredDamage(Attacker, Defender, 0, CombatDamageType.Physical), () => sink += GameServices.CombatHitEffects.ApplyTriggeredDamage(Attacker, Defender, 0, CombatDamageType.Physical)),
                ("ForgetHitEffectState", 192, () => effects.ForgetHitEffectState(Attacker), () => GameServices.CombatHitEffects.ForgetHitEffectState(Attacker)),
            };

            foreach (var (name, bytes, part, forwarder) in reads)
            {
                PerCall(part).Should().Be(bytes, $"{name} allocates what it did before the move");
                PerCall(forwarder).Should().Be(bytes, $"{name} through GameServices adds nothing to what the part allocates");
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
            var reads = new (string Name, Action Part, Action Forwarder)[]
            {
                ("ApplyRecentDamagerDefeatedEnemyEffects", () => effects.ApplyRecentDamagerDefeatedEnemyEffects(Defender), () => GameServices.CombatHitEffects.ApplyRecentDamagerDefeatedEnemyEffects(Defender)),
                ("GetAbilityCriticalRate", () => sink += hit.GetAbilityCriticalRate(Attacker, SkillType.Rifle, false, 0, Defender), () => sink += GameServices.CombatAbilityHit.GetAbilityCriticalRate(Attacker, SkillType.Rifle, false, 0, Defender)),
                ("CalculateAbilityCriticalRating", () => sink += hit.CalculateAbilityCriticalRating(Attacker, SkillType.Rifle, false, 0, Defender), () => sink += GameServices.CombatAbilityHit.CalculateAbilityCriticalRating(Attacker, SkillType.Rifle, false, 0, Defender)),
                ("TryResolveAbilityHit", () => sink += hit.TryResolveAbilityHit(Attacker, Defender, SkillType.Rifle, PerkType.DoubleShot, out _) ? 1 : 0, () => sink += GameServices.CombatAbilityHit.TryResolveAbilityHit(Attacker, Defender, SkillType.Rifle, PerkType.DoubleShot, out _) ? 1 : 0),
                ("GetHitChanceAgainstSunderedTargetAdjustment", () => sink += hit.GetHitChanceAgainstSunderedTargetAdjustment(Attacker, Defender), () => sink += GameServices.CombatAbilityHit.GetHitChanceAgainstSunderedTargetAdjustment(Attacker, Defender)),
                ("GetCriticalRateAgainstSunderedTargetAdjustment", () => sink += hit.GetCriticalRateAgainstSunderedTargetAdjustment(Attacker, Defender), () => sink += GameServices.CombatAbilityHit.GetCriticalRateAgainstSunderedTargetAdjustment(Attacker, Defender)),
                ("ConsumeAutoAttackCycleDamageBonus", () => sink += effects.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Rifle), () => sink += GameServices.CombatHitEffects.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Rifle)),
                ("ApplyDamageDealtEffects(auto)", () => effects.ApplyDamageDealtEffects(Attacker, Defender, 10, SkillType.Rifle), () => GameServices.CombatHitEffects.ApplyDamageDealtEffects(Attacker, Defender, 10, SkillType.Rifle)),
                ("ApplyDamageDealtEffects(ability)", () => effects.ApplyDamageDealtEffects(Attacker, Defender, 10, SkillType.Rifle, CombatDamageType.Physical, CombatDamageDeliveryType.Direct, true), () => GameServices.CombatHitEffects.ApplyDamageDealtEffects(Attacker, Defender, 10, SkillType.Rifle, CombatDamageType.Physical, CombatDamageDeliveryType.Direct, true)),
                ("ApplyCriticalHitEffects", () => effects.ApplyCriticalHitEffects(Attacker, Defender, 10, 2, true, SkillType.Rifle), () => GameServices.CombatHitEffects.ApplyCriticalHitEffects(Attacker, Defender, 10, 2, true, SkillType.Rifle)),
                ("ApplyDefeatedEnemyEffects", () => effects.ApplyDefeatedEnemyEffects(Attacker, Defender), () => GameServices.CombatHitEffects.ApplyDefeatedEnemyEffects(Attacker, Defender)),
                ("ApplyTriggeredDamage", () => sink += effects.ApplyTriggeredDamage(Attacker, Defender, 10, CombatDamageType.Physical, SkillType.Rifle), () => sink += GameServices.CombatHitEffects.ApplyTriggeredDamage(Attacker, Defender, 10, CombatDamageType.Physical, SkillType.Rifle)),
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
            var reads = new (string Name, long Bytes, Action Part, Action Forwarder)[]
            {
                ("ApplyDamageTakenModifiers", 56, () => sink += taken.ApplyDamageTakenModifiers(Defender, 10, Attacker), () => sink += GameServices.CombatDamageTaken.ApplyDamageTakenModifiers(Defender, 10, Attacker)),
                ("ApplyDamageTakenModifiers(fire)", 56, () => sink += taken.ApplyDamageTakenModifiers(Defender, 10, Attacker, CombatDamageType.Fire), () => sink += GameServices.CombatDamageTaken.ApplyDamageTakenModifiers(Defender, 10, Attacker, CombatDamageType.Fire)),
                ("ApplyDamageTakenModifiers(force)", 56, () => sink += taken.ApplyDamageTakenModifiers(Defender, 10, Attacker, CombatDamageType.Force), () => sink += GameServices.CombatDamageTaken.ApplyDamageTakenModifiers(Defender, 10, Attacker, CombatDamageType.Force)),
                ("ApplyDamageTakenModifiers(typed)", 56, () => sink += taken.ApplyDamageTakenModifiers(Defender, 10, Attacker, CombatDamageType.Physical, CombatDamageDeliveryType.Direct, -10, true, true), () => sink += GameServices.CombatDamageTaken.ApplyDamageTakenModifiers(Defender, 10, Attacker, CombatDamageType.Physical, CombatDamageDeliveryType.Direct, -10, true, true)),
                ("ApplyDamageTakenModifiers(transferred)", 32, () => sink += taken.ApplyDamageTakenModifiers(Defender, 10, Attacker, CombatDamageType.Physical, CombatDamageDeliveryType.Transferred), () => sink += GameServices.CombatDamageTaken.ApplyDamageTakenModifiers(Defender, 10, Attacker, CombatDamageType.Physical, CombatDamageDeliveryType.Transferred)),
                ("TrackDeflection", 24, () => taken.TrackDeflection(Defender, DeflectionSource.Melee), () => GameServices.CombatDamageTaken.TrackDeflection(Defender, DeflectionSource.Melee)),
                ("ConsumeGuardedHitNextSkillAbilityExposedDamageBonus", 56, () => sink += taken.ConsumeGuardedHitNextSkillAbilityExposedDamageBonus(Defender, SkillType.Rifle), () => sink += GameServices.CombatDamageTaken.ConsumeGuardedHitNextSkillAbilityExposedDamageBonus(Defender, SkillType.Rifle)),
                ("ApplyIncomingPhysicalToForceConversion", 72, () => { var damage = 10; sink += taken.ApplyIncomingPhysicalToForceConversion(Attacker, Defender, CombatDamageType.Physical, ref damage); }, () => { var damage = 10; sink += GameServices.CombatDamageTaken.ApplyIncomingPhysicalToForceConversion(Attacker, Defender, CombatDamageType.Physical, ref damage); }),
                ("ForgetDamageTakenState", 184, () => taken.ForgetDamageTakenState(Defender), () => GameServices.CombatDamageTaken.ForgetDamageTakenState(Defender)),
                ("BuildDeflectingReturnCombatLogMessage", 968, () => sink += taken.BuildDeflectingReturnCombatLogMessage(Attacker, Defender, Attacker, 5).Length, () => sink += GameServices.CombatDamageTaken.BuildDeflectingReturnCombatLogMessage(Attacker, Defender, Attacker, 5).Length),
            };

            foreach (var (name, bytes, part, forwarder) in reads)
            {
                PerCall(part).Should().Be(bytes, $"{name} allocates what it did before the move");
                PerCall(forwarder).Should().Be(bytes, $"{name} through GameServices adds nothing to what the part allocates");
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
                ("ApplyRangedDeflectionReflection", () => sink += taken.ApplyRangedDeflectionReflection(Defender, Attacker, SkillType.Rifle), () => sink += GameServices.CombatDamageTaken.ApplyRangedDeflectionReflection(Defender, Attacker, SkillType.Rifle)),
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
            var reads = new (string Name, Action Part, Action Forwarder)[]
            {
                ("ApplyDamageTakenModifiers(unlanded)", () => sink += taken.ApplyDamageTakenModifiers(Defender, 10, Attacker, CombatDamageType.Physical, CombatDamageDeliveryType.Direct, 0, false), () => sink += GameServices.CombatDamageTaken.ApplyDamageTakenModifiers(Defender, 10, Attacker, CombatDamageType.Physical, CombatDamageDeliveryType.Direct, 0, false)),
                ("ApplyDamageTakenModifiers(0)", () => sink += taken.ApplyDamageTakenModifiers(Defender, 0), () => sink += GameServices.CombatDamageTaken.ApplyDamageTakenModifiers(Defender, 0)),
                ("ApplyTypedLeadershipDamageTakenModifier", () => sink += taken.ApplyTypedLeadershipDamageTakenModifier(Defender, 10, CombatDamageType.Physical), () => sink += GameServices.CombatDamageTaken.ApplyTypedLeadershipDamageTakenModifier(Defender, 10, CombatDamageType.Physical)),
                ("ApplyTypedLeadershipDamageTakenModifier(force)", () => sink += taken.ApplyTypedLeadershipDamageTakenModifier(Defender, 10, CombatDamageType.Force), () => sink += GameServices.CombatDamageTaken.ApplyTypedLeadershipDamageTakenModifier(Defender, 10, CombatDamageType.Force)),
                ("ApplyTypedLeadershipDamageTakenModifier(fire)", () => sink += taken.ApplyTypedLeadershipDamageTakenModifier(Defender, 10, CombatDamageType.Fire), () => sink += GameServices.CombatDamageTaken.ApplyTypedLeadershipDamageTakenModifier(Defender, 10, CombatDamageType.Fire)),
                ("ApplyDamageReflectionEffects", () => taken.ApplyDamageReflectionEffects(Attacker, Defender, 10, CombatDamageType.Physical), () => GameServices.CombatDamageTaken.ApplyDamageReflectionEffects(Attacker, Defender, 10, CombatDamageType.Physical)),
                ("ApplyDamageReflectionEffects(force)", () => taken.ApplyDamageReflectionEffects(Attacker, Defender, 10, CombatDamageType.Force), () => GameServices.CombatDamageTaken.ApplyDamageReflectionEffects(Attacker, Defender, 10, CombatDamageType.Force)),
                ("ApplyDamageOverTimeTakenModifiers", () => sink += taken.ApplyDamageOverTimeTakenModifiers(Defender, 10, CombatDamageType.Physical), () => sink += GameServices.CombatDamageTaken.ApplyDamageOverTimeTakenModifiers(Defender, 10, CombatDamageType.Physical)),
                ("ApplyDamageOverTimeTakenModifiers(out)", () => sink += taken.ApplyDamageOverTimeTakenModifiers(Defender, 10, CombatDamageType.Force, out _), () => sink += GameServices.CombatDamageTaken.ApplyDamageOverTimeTakenModifiers(Defender, 10, CombatDamageType.Force, out _)),
                ("TryPreventFatalDamageAndGrantTemporaryHP", () => sink += taken.TryPreventFatalDamageAndGrantTemporaryHP(Defender, 10, false) ? 1 : 0, () => sink += GameServices.CombatDamageTaken.TryPreventFatalDamageAndGrantTemporaryHP(Defender, 10, false) ? 1 : 0),
                ("ApplyDamageTakenEffects", () => taken.ApplyDamageTakenEffects(Defender, Attacker, 10), () => GameServices.CombatDamageTaken.ApplyDamageTakenEffects(Defender, Attacker, 10)),
                ("ApplyDamageTakenEffects()", () => taken.ApplyDamageTakenEffects(), () => GameServices.CombatDamageTaken.ApplyDamageTakenEffects()),
                ("ApplyLowHPDamageTakenEffects", () => taken.ApplyLowHPDamageTakenEffects(Defender, 10), () => GameServices.CombatDamageTaken.ApplyLowHPDamageTakenEffects(Defender, 10)),
                ("ApplyLowHPGuardEffectFromProtectedTarget", () => taken.ApplyLowHPGuardEffectFromProtectedTarget(Attacker, Defender, 10), () => GameServices.CombatDamageTaken.ApplyLowHPGuardEffectFromProtectedTarget(Attacker, Defender, 10)),
                ("TrackGuardedHit", () => taken.TrackGuardedHit(Defender), () => GameServices.CombatDamageTaken.TrackGuardedHit(Defender)),
                ("TrackAvoidedAttack", () => taken.TrackAvoidedAttack(Defender, Attacker), () => GameServices.CombatDamageTaken.TrackAvoidedAttack(Defender, Attacker)),
                ("ApplyMeleeDamageTakenEffects", () => taken.ApplyMeleeDamageTakenEffects(Defender, Attacker), () => GameServices.CombatDamageTaken.ApplyMeleeDamageTakenEffects(Defender, Attacker)),
                ("ApplyGuardedHitModifiers", () => sink += taken.ApplyGuardedHitModifiers(Defender, Attacker, 10, CombatDamageType.Physical, true), () => sink += GameServices.CombatDamageTaken.ApplyGuardedHitModifiers(Defender, Attacker, 10, CombatDamageType.Physical, true)),
                ("IsHostileAttackSource", () => sink += taken.IsHostileAttackSource(Defender, Attacker) ? 1 : 0, () => sink += GameServices.CombatDamageTaken.IsHostileAttackSource(Defender, Attacker) ? 1 : 0),
                ("GetGuardDamageReductionPercent", () => sink += taken.GetGuardDamageReductionPercent(Defender), () => sink += GameServices.CombatDamageTaken.GetGuardDamageReductionPercent(Defender)),
                ("HasRecentGuardedHit", () => sink += taken.HasRecentGuardedHit(Defender, 6f) ? 1 : 0, () => sink += GameServices.CombatDamageTaken.HasRecentGuardedHit(Defender, 6f) ? 1 : 0),
                ("HasRecentDeflection", () => sink += taken.HasRecentDeflection(Defender, DeflectionSource.Melee, 6f) ? 1 : 0, () => sink += GameServices.CombatDamageTaken.HasRecentDeflection(Defender, DeflectionSource.Melee, 6f) ? 1 : 0),
                ("ApplyIncomingPhysicalToForceDefenseConversion", () => sink += taken.ApplyIncomingPhysicalToForceDefenseConversion(Defender, CombatDamageType.Physical, 100, forceDefense), () => sink += GameServices.CombatDamageTaken.ApplyIncomingPhysicalToForceDefenseConversion(Defender, CombatDamageType.Physical, 100, forceDefense)),
                ("ApplyAbilityUsedAttackDeflection", () => taken.ApplyAbilityUsedAttackDeflection(Attacker, StatType.AbilityUsedAttackDeflection, StatType.AbilityUsedAttackDeflectionDurationSeconds, StatType.Invalid), () => GameServices.CombatDamageTaken.ApplyAbilityUsedAttackDeflection(Attacker, StatType.AbilityUsedAttackDeflection, StatType.AbilityUsedAttackDeflectionDurationSeconds, StatType.Invalid)),
                ("ApplyAbilityGrantedAttackDeflectionEffects", () => taken.ApplyAbilityGrantedAttackDeflectionEffects(Attacker, DeflectionSource.Ranged), () => GameServices.CombatDamageTaken.ApplyAbilityGrantedAttackDeflectionEffects(Attacker, DeflectionSource.Ranged)),
            };

            foreach (var (name, part, forwarder) in reads)
            {
                LeastAllocated(part).Should().Be(0, $"{name} runs per damage roll or per damage event");
                LeastAllocated(forwarder).Should().Be(0, $"{name} through GameServices allocates nothing");
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
                ("GetAbilityImpactBaseDamageBonus(null)", () => sink += impact.GetAbilityImpactBaseDamageBonus(Attacker, Defender, null!, SkillType.Rifle), () => sink += GameServices.CombatAbilityImpact.GetAbilityImpactBaseDamageBonus(Attacker, Defender, null!, SkillType.Rifle)),
                ("GetHighResourceAbilityDamageBonus", () => sink += impact.GetHighResourceAbilityDamageBonus(Attacker, rifle), () => sink += GameServices.CombatAbilityImpact.GetHighResourceAbilityDamageBonus(Attacker, rifle)),
                ("GetHighResourceAbilityDamageBonus()", () => sink += impact.GetHighResourceAbilityDamageBonus(Attacker), () => sink += GameServices.CombatAbilityImpact.GetHighResourceAbilityDamageBonus(Attacker)),
                ("GetCostlyAbilityDamageBonus", () => sink += impact.GetCostlyAbilityDamageBonus(Attacker, rifle, SkillType.Rifle), () => sink += GameServices.CombatAbilityImpact.GetCostlyAbilityDamageBonus(Attacker, rifle, SkillType.Rifle)),
                ("ApplyHostileAbilityPartyBuff", () => impact.ApplyHostileAbilityPartyBuff(Attacker, rifle, 10, false), () => GameServices.CombatAbilityImpact.ApplyHostileAbilityPartyBuff(Attacker, rifle, 10, false)),
                ("ApplyHostileAbilityPartyBuff(status)", () => impact.ApplyHostileAbilityPartyBuff(Attacker, rifle, 0, true), () => GameServices.CombatAbilityImpact.ApplyHostileAbilityPartyBuff(Attacker, rifle, 0, true)),
                ("ApplyHostileAbilityPartyBuff(miss)", () => impact.ApplyHostileAbilityPartyBuff(Attacker, rifle, 0, false), () => GameServices.CombatAbilityImpact.ApplyHostileAbilityPartyBuff(Attacker, rifle, 0, false)),
                ("ApplySpearDisablerSuppressionRiders", () => impact.ApplySpearDisablerSuppressionRiders(Attacker, Defender), () => GameServices.CombatAbilityImpact.ApplySpearDisablerSuppressionRiders(Attacker, Defender)),
                ("IsTargetAttackingAllyForStatusAppliedRiders", () => sink += impact.IsTargetAttackingAllyForStatusAppliedRiders(Attacker, Defender) ? 1 : 0, () => sink += GameServices.CombatAbilityImpact.IsTargetAttackingAllyForStatusAppliedRiders(Attacker, Defender) ? 1 : 0),
                ("ApplyStatusAppliedTargetStaminaDrain", () => impact.ApplyStatusAppliedTargetStaminaDrain(Attacker, Defender, StatusEffectCategory.Debuff), () => GameServices.CombatAbilityImpact.ApplyStatusAppliedTargetStaminaDrain(Attacker, Defender, StatusEffectCategory.Debuff)),
                ("ApplyAreaHitHaste", () => impact.ApplyAreaHitHaste(Attacker, 3), () => GameServices.CombatAbilityImpact.ApplyAreaHitHaste(Attacker, 3)),
                ("ApplyAbilityImpactEffects(empty)", () => impact.ApplyAbilityImpactEffects(Attacker, empty), () => GameServices.CombatAbilityImpact.ApplyAbilityImpactEffects(Attacker, empty)),
            };
            foreach (var summary in summaries)
                reads.Add(($"ApplyAbilityImpactEffects({summary.SkillType})", () => impact.ApplyAbilityImpactEffects(Attacker, summary), () => GameServices.CombatAbilityImpact.ApplyAbilityImpactEffects(Attacker, summary)));

            foreach (var (name, part, forwarder) in reads)
            {
                LeastAllocated(part).Should().Be(0, $"{name} runs per ability hit, per target or per impact");
                LeastAllocated(forwarder).Should().Be(0, $"{name} through GameServices allocates nothing");
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
            var rifle = ImpactAbility(SkillType.Rifle, false);
            var reads = new (string Name, long Bytes, Action Part, Action Forwarder)[]
            {
                ("IsAttackingAllyOf", 72, () => sink += impact.IsAttackingAllyOf(Attacker, Defender) ? 1 : 0, () => sink += GameServices.CombatAbilityImpact.IsAttackingAllyOf(Attacker, Defender) ? 1 : 0),
                ("GetAbilityImpactBaseDamageBonus(Rifle)", 56, () => sink += impact.GetAbilityImpactBaseDamageBonus(Attacker, Defender, rifle, SkillType.Rifle), () => sink += GameServices.CombatAbilityImpact.GetAbilityImpactBaseDamageBonus(Attacker, Defender, rifle, SkillType.Rifle)),
                ("ForgetImpactState", 344, () => impact.ForgetImpactState(Defender), () => GameServices.CombatAbilityImpact.ForgetImpactState(Defender)),
            };

            foreach (var (name, bytes, part, forwarder) in reads)
            {
                PerCall(part).Should().Be(bytes, $"{name} allocates what it did before the move");
                PerCall(forwarder).Should().Be(bytes, $"{name} through GameServices adds nothing to what the part allocates");
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
                ("GetAbilityImpactBaseDamageBonus(TwinBlade)", () => sink += impact.GetAbilityImpactBaseDamageBonus(Attacker, Defender, twinBlade, SkillType.TwinBlade), () => sink += GameServices.CombatAbilityImpact.GetAbilityImpactBaseDamageBonus(Attacker, Defender, twinBlade, SkillType.TwinBlade)),
                ("GetAbilityImpactBaseDamageBonus(Lightsaber)", () => sink += impact.GetAbilityImpactBaseDamageBonus(Attacker, Defender, lightsaber, SkillType.Lightsaber), () => sink += GameServices.CombatAbilityImpact.GetAbilityImpactBaseDamageBonus(Attacker, Defender, lightsaber, SkillType.Lightsaber)),
                ("ApplySuccessfulAbilityImpactRiders(no damage)",
                    () => impact.ApplySuccessfulAbilityImpactRiders(Attacker, Defender, rifle, SkillType.Rifle, CombatDamageType.Physical, 0, false, null!, none, false, false, StatusEffectCategory.None, false, true),
                    () => GameServices.CombatAbilityImpact.ApplySuccessfulAbilityImpactRiders(Attacker, Defender, rifle, SkillType.Rifle, CombatDamageType.Physical, 0, false, null!, none, false, false, StatusEffectCategory.None, false, true)),
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
                    () => GameServices.CombatAbilityImpact.ApplySuccessfulAbilityImpactRiders(Attacker, Defender, ability, skill, CombatDamageType.Physical, 10, true, typeof(DazedStatusEffect), applied, true, true, StatusEffectCategory.Debuff)));
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

    private sealed record ActivationAbilities(AbilityDetail Hostile, AbilityDetail Area, AbilityDetail Support, AbilityDetail Weapon,
        AbilityImpactSummary Single, AbilityImpactSummary Empty, AbilityImpactSummary AreaSummary);

    // The same world as the damage taken part (a container of its own, the constant engine, the clock held still, a status table
    // for both creatures and the stat service that allocates nothing), with the ability activation part and one ability of each
    // kind an activation sees. The tests that use it are named to run after every other combat part test of the fixture, for
    // the reason the hit part tests give.
    private static (ICombatAbilityActivationService Part, ActivationAbilities Abilities) InstallActivationPart(List<IDisposable> scopes)
    {
        InstallTimingParts(scopes);
        var hostile = new AbilityDetail { Name = "Strike", IsHostileAbility = true, IsSingleTargetAbility = true, SkillType = SkillType.Rifle };
        hostile.Requirements.Add(new AbilityRequirementStamina(10));
        var area = new AbilityDetail { Name = "Burst", IsHostileAbility = true, IsAreaAbility = true, SkillType = SkillType.Force };
        area.Requirements.Add(new AbilityRequirementFP(4));
        var abilities = new ActivationAbilities(
            hostile,
            area,
            new AbilityDetail { Name = "Mend", SkillType = SkillType.FirstAid },
            new AbilityDetail { Name = "Shot", IsHostileAbility = true, ActivationType = AbilityActivationType.Weapon, SkillType = SkillType.Rifle },
            new AbilityImpactSummary { SkillType = SkillType.Rifle, IsSingleTargetAbility = true },
            new AbilityImpactSummary(),
            new AbilityImpactSummary { SkillType = SkillType.Force, IsAreaAbility = true });
        return (FacadeContainer.Current.Get<ICombatAbilityActivationService>(), abilities);
    }

    [Test]
    public void WhileAnAbilityActivatesWhatAllocatedNothingStillAllocatesNothingThroughCombat()
    {
        // The HP-cost riders with none configured, an activation with no ability and the forget step of a death or exit. Each
        // allocated nothing before the move either (measured against the base assembly in the same setup).
        var scopes = new List<IDisposable>();
        try
        {
            var (part, abilities) = InstallActivationPart(scopes);
            var reads = new (string Name, Action Part, Action Forwarder)[]
            {
                ("ApplyAbilityActivatedEffects(no ability)", () => part.ApplyAbilityActivatedEffects(Attacker, Defender, FeatType.DoubleShot1, null!, abilities.Single), () => GameServices.CombatAbilityActivation.ApplyAbilityActivatedEffects(Attacker, Defender, FeatType.DoubleShot1, null!, abilities.Single)),
                ("ApplyHitPointSpendAbilityEffects", () => part.ApplyHitPointSpendAbilityEffects(Attacker, 10, 12), () => GameServices.CombatAbilityActivation.ApplyHitPointSpendAbilityEffects(Attacker, 10, 12)),
                ("ApplyHitPointSpendAbilityEffects(0)", () => part.ApplyHitPointSpendAbilityEffects(Attacker, 0), () => GameServices.CombatAbilityActivation.ApplyHitPointSpendAbilityEffects(Attacker, 0)),
                ("ForgetActivationState", () => part.ForgetActivationState(Attacker), () => GameServices.CombatAbilityActivation.ForgetActivationState(Attacker)),
            };

            foreach (var (name, own, forwarder) in reads)
            {
                LeastAllocated(own).Should().Be(0, $"{name} runs per ability use or per death");
                LeastAllocated(forwarder).Should().Be(0, $"{name} through GameServices allocates nothing");
            }
        }
        finally
        {
            DisposeInReverse(scopes);
        }
    }

    [Test]
    public void WhileAnAbilityActivatesEachMemberKeepsItsBytesThroughCombat()
    {
        // Bytes per call in this setup, each the same as the base assembly's in the same setup with tiered compilation on and
        // off: an activation of a hostile ability records a new hostile ability sequence state (32 bytes), and tracking the use
        // records the suppression use and schedules the idle readiness refresh in the activity part, as before the move.
        var scopes = new List<IDisposable>();
        try
        {
            var (part, abilities) = InstallActivationPart(scopes);
            var reads = new (string Name, long Bytes, Action Part, Action Forwarder)[]
            {
                ("ApplyAbilityActivatedEffects(weapon)", 32, () => part.ApplyAbilityActivatedEffects(Attacker, Defender, FeatType.DoubleShot1, abilities.Weapon, abilities.Single), () => GameServices.CombatAbilityActivation.ApplyAbilityActivatedEffects(Attacker, Defender, FeatType.DoubleShot1, abilities.Weapon, abilities.Single)),
                ("ApplyAbilityActivatedEffects(hostile)", 728, () => part.ApplyAbilityActivatedEffects(Attacker, Defender, FeatType.DoubleShot1, abilities.Hostile, abilities.Single), () => GameServices.CombatAbilityActivation.ApplyAbilityActivatedEffects(Attacker, Defender, FeatType.DoubleShot1, abilities.Hostile, abilities.Single)),
                ("ApplyAbilityActivatedEffects(hostile, no summary skill)", 728, () => part.ApplyAbilityActivatedEffects(Attacker, Defender, FeatType.DoubleShot1, abilities.Hostile, abilities.Empty), () => GameServices.CombatAbilityActivation.ApplyAbilityActivatedEffects(Attacker, Defender, FeatType.DoubleShot1, abilities.Hostile, abilities.Empty)),
                ("ApplyAbilityActivatedEffects(area)", 816, () => part.ApplyAbilityActivatedEffects(Attacker, Defender, FeatType.ForcePush1, abilities.Area, abilities.AreaSummary), () => GameServices.CombatAbilityActivation.ApplyAbilityActivatedEffects(Attacker, Defender, FeatType.ForcePush1, abilities.Area, abilities.AreaSummary)),
                ("ApplyAbilityActivatedEffects(support)", 368, () => part.ApplyAbilityActivatedEffects(Attacker, Defender, FeatType.MedKit1, abilities.Support, abilities.Empty), () => GameServices.CombatAbilityActivation.ApplyAbilityActivatedEffects(Attacker, Defender, FeatType.MedKit1, abilities.Support, abilities.Empty)),
                ("TrackQueuedWeaponAbilityUse", 696, () => part.TrackQueuedWeaponAbilityUse(Attacker, abilities.Weapon), () => GameServices.CombatAbilityActivation.TrackQueuedWeaponAbilityUse(Attacker, abilities.Weapon)),
            };

            foreach (var (name, bytes, own, forwarder) in reads)
            {
                PerCall(own).Should().Be(bytes, $"{name} allocates what it did before the move");
                PerCall(forwarder).Should().Be(bytes, $"{name} through GameServices adds nothing to what the part allocates");
            }
        }
        finally
        {
            DisposeInReverse(scopes);
        }
    }

    // The world of the activation tests (a container of its own, the constant engine, the clock held still, a status table for
    // both creatures and the stat service that allocates nothing) with an engine whose script context names the defeated or
    // exiting creature and its killer, for what is left of Combat (CombatService). P2-CB10 moved these five members from
    // static bodies on Combat to the service, so their forwarders are the one route that changed; the tests are named to run
    // after every other combat part test of the fixture, for the reason the hit part tests give.
    private static ICombatService InstallServicePart(List<IDisposable> scopes)
    {
        InstallTimingParts(scopes);
        var answers = StatReadEngine.NpcAnswers();
        answers["get_OBJECT_SELF"] = Defender;
        answers["GetLastKiller"] = Attacker;
        answers["GetExitingObject"] = Defender;
        answers["GetIsInCombat"] = false;
        scopes.Add(NWScriptRuntime.Override(StatReadEngine.Create(answers)));
        return FacadeContainer.Current.Get<ICombatService>();
    }

    [Test]
    public void WithWhatIsLeftOfCombatTheFacingCleanupAllocatesNothingThroughCombat()
    {
        // The facing cleanup runs per player every six seconds. It allocated nothing as a static body of Combat; through the
        // facade it makes one more interface call and allocates nothing either.
        var scopes = new List<IDisposable>();
        try
        {
            var service = InstallServicePart(scopes);

            LeastAllocated(() => service.ClearCombatState()).Should().Be(0, "ClearCombatState runs per player every six seconds");
            LeastAllocated(() => GameServices.Combat.ClearCombatState()).Should().Be(0, "ClearCombatState through GameServices allocates nothing");
        }
        finally
        {
            DisposeInReverse(scopes);
        }
    }

    [Test]
    public void WithWhatIsLeftOfCombatEachMemberThatAllocatesAllocatesNoMoreThroughCombat()
    {
        // The death and exit handlers forget the creature in every part (each forget step lists the keys it removes), the
        // tracker refresh schedules the idle readiness refresh in the activity part, and the damage roll's range rule writes its
        // attack log lines (as CalculateDamageRange does above): what they allocate is that work, as on the base, where the same
        // bodies were static members of Combat that called the parts through Combat's forwarders. The forwarder could only add
        // a closure and its delegate (88 bytes or more), so it is compared with the service's own call on the upper side only,
        // interleaved (ForwarderAddsNothing).
        var scopes = new List<IDisposable>();
        try
        {
            var service = InstallServicePart(scopes);
            var sink = 0;
            var reads = new (string Name, Action Part, Action Forwarder)[]
            {
                ("CalculateDamage", () => sink += service.CalculateDamage(100, 45, 40, 100, 10, 0, 5), () => sink += GameServices.Combat.CalculateDamage(100, 45, 40, 100, 10, 0, 5)),
                ("CalculateDamage(critical, default cap)", () => sink += service.CalculateDamage(100, 45, 40, 100, 10, 120), () => sink += GameServices.Combat.CalculateDamage(100, 45, 40, 100, 10, 120)),
                ("ApplyDefeatedEnemyEffects", () => service.ApplyDefeatedEnemyEffects(), () => GameServices.Combat.ApplyDefeatedEnemyEffects()),
                ("ClearStatTriggerState", () => service.ClearStatTriggerState(), () => GameServices.Combat.ClearStatTriggerState()),
                ("RefreshStatDrivenTrackerEffects", () => service.RefreshStatDrivenTrackerEffects(Attacker), () => GameServices.Combat.RefreshStatDrivenTrackerEffects(Attacker)),
            };

            foreach (var (name, part, forwarder) in reads)
            {
                var (own, forwarded) = InterleavedPerCall(part, forwarder);
                TestContext.Out.WriteLine($"{name}: service {own} bytes, Combat {forwarded} bytes per call");
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
