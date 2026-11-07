using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
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

    // Bytes per call, the least of three runs, so a one-off allocation of the runtime cannot fail a comparison.
    private static long PerCall(Action call) => Enumerable.Range(0, 3).Min(_ => Allocated(call)) / Calls;

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
                Allocated(part).Should().Be(0, $"{name} runs per attack or per hit");
                Allocated(forwarder).Should().Be(0, $"Combat.{name} only forwards");
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

    [Test]
    public void TheDamageTypeDetailsAreReadWithoutAllocating()
    {
        var sink = 0;

        Allocated(() => sink += CombatDamageType.Fire.IsPhysicalDamageType() ? 1 : 0).Should().Be(0);
        Allocated(() => sink += (int)CombatDamageType.Fire.GetDefenseDamageType()).Should().Be(0);
        Allocated(() => sink += (int)CombatDamageType.Force.GetDetails().NativeDamageType).Should().Be(0);
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
