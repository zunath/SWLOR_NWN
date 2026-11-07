using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
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

        Allocated(() => sink += CombatFacade.CalculateDamageRange(100, 100, 20, 100, 20, 2).Item2)
            .Should().Be(Allocated(() => sink += CombatRules.CalculateDamageRange(100, 100, 20, 100, 20, 2).Item2));
        Allocated(() => sink += CombatFacade.IsWeaponSkillType(SkillType.Rifle) ? 1 : 0)
            .Should().Be(Allocated(() => sink += CombatRules.IsWeaponSkillType(SkillType.Rifle) ? 1 : 0));
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
                var own = PerCall(part);
                own.Should().BeGreaterThan(0, $"{name} allocated before the move too");
                PerCall(forwarder).Should().Be(own, $"Combat.{name} adds nothing to what the part allocates");
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
            var recentDamageTargets = Private<Func<Dictionary<(uint, uint), DateTime>>>("get__recentDamageTargets");
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
                ("RecentDamageTargets", () => sink += activity.RecentDamageTargets.Count, () => sink += recentDamageTargets().Count),
            };

            foreach (var (name, part, forwarder) in reads)
            {
                LeastAllocated(part).Should().Be(0, $"{name} runs per swing, per attack or per hit");
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
                forwarded.Should().BeCloseTo(own, 32, $"Combat.{name} adds no closure or delegate to what the part allocates");
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
