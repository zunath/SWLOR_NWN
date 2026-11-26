using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Domain.Abilities;

/// <summary>
/// The default of each call into unconverted code reaches the real static code. The engine fake reports every creature as
/// gone or not a player, so each call stops at its first check; the calls that are pure arithmetic are checked by value.
/// </summary>
[TestFixture]
public class AbilityServiceDefaultSeamsTests
{
    private const uint Creature = 61251;

    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;
    private IDisposable _engineScope = null!;
    private AbilityService _service = null!;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void SetUp()
    {
        _facadeScope = _facades.Install();
        _engineScope = NWScriptRuntime.Override(new FakeNWScript());
        _service = AbilityInternals.NoNeighbours();
    }

    [TearDown]
    public void TearDown()
    {
        _engineScope.Dispose();
        _facadeScope.Dispose();
    }

    private static IEnumerable<MethodInfo> Seams() => Seams(typeof(AbilityService));

    private static IEnumerable<MethodInfo> Seams(Type type) =>
        type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.IsVirtual && method.IsFamily);

    // The activation part of the ability split with every service forwarded to the installed container.
    private static AbilityActivationService ActivationPart() =>
        new(FacadeForwarder.For<IAbilityCatalog>(), FacadeForwarder.For<ISpaceModeQuery>(), FacadeForwarder.For<IPerkService>(),
            FacadeForwarder.For<IMimicryService>(), FacadeForwarder.For<IActivityService>(), FacadeForwarder.For<IStatService>(),
            FacadeForwarder.For<ICombatQueriesService>(), FacadeForwarder.For<IAbilityTargetCheckService>());

    // The damage parts of the ability split with every service forwarded to the installed container.
    private static AbilityImpactMessageService ImpactMessagePart() =>
        new(FacadeForwarder.For<IMessagingService>(), FacadeForwarder.For<ICombatRecorderService>(), FacadeForwarder.For<ICombatFeedbackService>());

    private static AbilityDarkForceService DarkForcePart() =>
        new(FacadeForwarder.For<IAbilityImpactState>(), FacadeForwarder.For<ICombatRecorderService>(), FacadeForwarder.For<IStatService>(),
            FacadeForwarder.For<ICombatResourcesService>());

    private static AbilityDamageDeliveryService DamageDeliveryPart() =>
        new(FacadeForwarder.For<IStatusEffectCombatEventService>(), FacadeForwarder.For<ICombatRecorderService>(), FacadeForwarder.For<IAbilityDarkForceService>(),
            FacadeForwarder.For<ICombatFeedbackService>(), FacadeForwarder.For<ICombatRepeatedTargetService>(), FacadeForwarder.For<ICombatHitEffectsService>(),
            FacadeForwarder.For<ICombatDamageReflectionService>(), FacadeForwarder.For<ICombatAbilityBonusService>());

    private static AbilityStatusApplicationService StatusApplicationPart() =>
        new(FacadeForwarder.For<IStatusEffectService>(), FacadeForwarder.For<IStatusEffectApplicationService>(), FacadeForwarder.For<IResistanceService>(), FacadeForwarder.For<IAbilityImpactState>(),
            FacadeForwarder.For<ICombatAbilityBonusService>());

    private static AbilityDamageInputService DamageInputPart() =>
        new(FacadeForwarder.For<IAbilityImpactState>(), FacadeForwarder.For<IStatService>(), FacadeForwarder.For<ICombatRatingService>(),
            FacadeForwarder.For<ICombatActivityService>(), FacadeForwarder.For<ICombatQueriesService>(), FacadeForwarder.For<ICombatAbilityBonusService>(),
            FacadeForwarder.For<ICombatAbilityDamageBonusService>(),
            FacadeForwarder.For<ICombatDamageModifiersService>(), FacadeForwarder.For<ICombatRepeatedTargetService>(), FacadeForwarder.For<ICombatDamageTakenService>(), FacadeForwarder.For<ICombatDamageReflectionService>(),
            FacadeForwarder.For<ICombatAbilityMagnitudeService>(), FacadeForwarder.For<IResistanceService>());

    private static AbilityDamageStageService DamageStagePart() =>
        new(FacadeForwarder.For<IAbilityDamageInputService>(), FacadeForwarder.For<IAbilityImpactState>(), FacadeForwarder.For<IPerkForceAffinityService>(),
            FacadeForwarder.For<ICombatAbilityHitAdjustmentService>(), FacadeForwarder.For<ICombatDamageModifiersService>(), FacadeForwarder.For<ICombatDamageTakenService>(), FacadeForwarder.For<ICombatDamageReflectionService>(),
            FacadeForwarder.For<ICombatFeedbackService>(), FacadeForwarder.For<ICombatCriticalHitEffectsService>(), FacadeForwarder.For<ICombatAbilityMagnitudeService>(),
            FacadeForwarder.For<IResistanceService>());

    // The lifecycle and per-target parts of the ability split with every service forwarded to the installed container.
    private static AbilityImpactLifecycleService LifecyclePart() =>
        new(FacadeForwarder.For<IAbilityImpactState>(), FacadeForwarder.For<IMeleeTechniqueService>(), FacadeForwarder.For<ICombatRecorderService>(),
            FacadeForwarder.For<IAbilityDamageDeliveryService>(), FacadeForwarder.For<IStatusEffectCombatEventService>(), FacadeForwarder.For<ICombatQueriesService>(),
            FacadeForwarder.For<ICombatAbilityBonusService>(), FacadeForwarder.For<ICombatQueuedAbilityBonusService>(), FacadeForwarder.For<ICombatAbilityHitRidersService>(), FacadeForwarder.For<ICombatResourcesService>(),
            FacadeForwarder.For<ICombatCriticalHitEffectsService>());

    private static AbilityHitRollService HitRollPart() =>
        new(Array.Empty<IAbilityImpactObserver>(), FacadeForwarder.For<IMeleeTechniqueService>(), FacadeForwarder.For<IAbilityDamageInputService>(),
            FacadeForwarder.For<IAbilityStatusApplicationService>(), FacadeForwarder.For<IAbilityImpactMessageService>(), FacadeForwarder.For<ICombatPointService>(),
            FacadeForwarder.For<IEnmityService>(), FacadeForwarder.For<ICombatActivityService>(), FacadeForwarder.For<ICombatAbilityBonusService>(),
            FacadeForwarder.For<ICombatAbilityHitService>());

    private static AbilityTargetImpactService TargetImpactPart() =>
        new(FacadeForwarder.For<IAbilityImpactState>(), FacadeForwarder.For<IAbilityHitRollService>(), FacadeForwarder.For<IAbilityHostileImpactService>(),
            FacadeForwarder.For<IAbilityDarkForceService>(), FacadeForwarder.For<IAbilityDamageInputService>(), FacadeForwarder.For<IAbilityDamageStageService>(),
            FacadeForwarder.For<ICombatResourcesService>(), FacadeForwarder.For<ICombatAbilityDamageBonusService>(), FacadeForwarder.For<ICombatAbilityBonusService>());

    private static AbilityHostileImpactService HostileImpactPart() =>
        new(FacadeForwarder.For<IAbilityImpactState>(), Array.Empty<IAbilityImpactObserver>(), FacadeForwarder.For<ICombatPointService>(),
            FacadeForwarder.For<IEnmityService>(), FacadeForwarder.For<ICombatResourcesService>(), FacadeForwarder.For<ICombatAbilityImpactService>(),
            FacadeForwarder.For<IAbilityDamageDeliveryService>(), FacadeForwarder.For<IAbilityStatusApplicationService>(),
            FacadeForwarder.For<IAbilityDarkForceService>(), FacadeForwarder.For<IAbilityImpactVisualService>());

    // Each damage, lifecycle and per-target part with the number of seams it took from the service.
    private static IEnumerable<TestCaseData> DamageParts()
    {
        yield return new TestCaseData((Func<object>)ImpactMessagePart, 2).SetArgDisplayNames(nameof(AbilityImpactMessageService));
        yield return new TestCaseData((Func<object>)DarkForcePart, 3).SetArgDisplayNames(nameof(AbilityDarkForceService));
        yield return new TestCaseData((Func<object>)DamageDeliveryPart, 5).SetArgDisplayNames(nameof(AbilityDamageDeliveryService));
        yield return new TestCaseData((Func<object>)StatusApplicationPart, 1).SetArgDisplayNames(nameof(AbilityStatusApplicationService));
        yield return new TestCaseData((Func<object>)DamageInputPart, 29).SetArgDisplayNames(nameof(AbilityDamageInputService));
        yield return new TestCaseData((Func<object>)DamageStagePart, 11).SetArgDisplayNames(nameof(AbilityDamageStageService));
        yield return new TestCaseData((Func<object>)LifecyclePart, 15).SetArgDisplayNames(nameof(AbilityImpactLifecycleService));
        yield return new TestCaseData((Func<object>)HitRollPart, 4).SetArgDisplayNames(nameof(AbilityHitRollService));
        yield return new TestCaseData((Func<object>)TargetImpactPart, 3).SetArgDisplayNames(nameof(AbilityTargetImpactService));
        yield return new TestCaseData((Func<object>)HostileImpactPart, 3).SetArgDisplayNames(nameof(AbilityHostileImpactService));
    }

    private static object? Sample(Type type)
    {
        if (type.IsByRef)
            type = type.GetElementType()!;
        if (type == typeof(uint))
            return Creature;
        if (type == typeof(string))
            return "sample";
        if (type == typeof(AbilityDetail))
            return new AbilityDetail { Name = "Sample", Requirements = new List<IAbilityActivationRequirement>() };
        if (type == typeof(AbilityImpactSummary))
            return new AbilityImpactSummary();
        if (type == typeof(Type))
            return typeof(ProbeEffect);
        if (type == typeof(IEnumerable<Type>))
            return Array.Empty<Type>();
        if (type == typeof(Func<int>))
            return (Func<int>)(() => 0);
        if (type == typeof(Func<IStatusEffect>))
            return (Func<IStatusEffect>)(() => new ProbeEffect());
        if (type == typeof(IReadOnlyDictionary<FeatType, AbilityDetail>))
            return new Dictionary<FeatType, AbilityDetail>();
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    private static object? Run(object instance, string name, params object?[] arguments)
    {
        var method = Seams(instance.GetType()).Single(seam => seam.Name == name &&
            seam.GetParameters().Select(parameter => parameter.ParameterType)
                .SequenceEqual(arguments.Select(argument => argument!.GetType())));
        return method.Invoke(instance, arguments);
    }

    [Test]
    public void There_is_a_seam_for_every_unconverted_call()
    {
        // The activation checks, the damage parts and the lifecycle and per-target parts took their seams
        // with them, so the count spans the service and those parts.
        new[]
            {
                typeof(AbilityActivationService), typeof(AbilityImpactMessageService), typeof(AbilityDarkForceService),
                typeof(AbilityDamageDeliveryService), typeof(AbilityStatusApplicationService), typeof(AbilityDamageInputService),
                typeof(AbilityDamageStageService), typeof(AbilityImpactLifecycleService), typeof(AbilityHitRollService),
                typeof(AbilityTargetImpactService), typeof(AbilityHostileImpactService)
            }
            .SelectMany(Seams).Concat(Seams()).Should().HaveCountGreaterThan(70);
    }

    [Test]
    public void Every_default_reaches_the_static_code_and_stops_without_a_server()
    {
        // The service keeps the seams of the telegraphed impact.
        Seams().Should().HaveCount(3);
        // These need the database or the native engine for a real creature, so they are left to the in-engine tests:
        // the space mode and FP reads ask the database and the NPC stats read a creature's skin.
        var engineOnly = new HashSet<string>
        {
            "GetAbilityListDefinitionTypes"
        };

        InvokeEveryDefault(_service, Seams().Where(method => !engineOnly.Contains(method.Name))).Should().BeEmpty();
    }

    [Test]
    public void Every_default_of_the_activation_part_reaches_the_static_code_and_stops_without_a_server()
    {
        Seams(typeof(AbilityActivationService)).Should().HaveCount(8);
        InvokeEveryDefault(ActivationPart(), Seams(typeof(AbilityActivationService))).Should().BeEmpty();
    }

    [TestCaseSource(nameof(DamageParts))]
    public void Every_default_of_a_damage_part_reaches_the_static_code_and_stops_without_a_server(Func<object> part, int seams)
    {
        var instance = part();
        Seams(instance.GetType()).Should().HaveCount(seams);
        InvokeEveryDefault(instance, Seams(instance.GetType())).Should().BeEmpty();
    }

    [Test]
    public void The_activation_part_reads_space_mode_through_the_query_and_the_weapon_skills_from_combat()
    {
        var part = ActivationPart();
        object? Call(string name, params object?[] arguments) =>
            Seams(typeof(AbilityActivationService)).Single(seam => seam.Name == name).Invoke(part, arguments);

        Call("IsPlayerInSpaceMode", Creature).Should().Be(false, "the engine fake reports no player");
        Call("IsWeaponSkillType", SkillType.Vibroblade).Should().Be(true);
        Call("IsWeaponSkillType", SkillType.Force).Should().Be(false);
        Call("GetStatAdjustment", Creature, StatType.ForceAbilityActivationDisabled).Should().Be(0);
        Call("GetUnusableEquipmentError", Creature).Should().Be(string.Empty);
    }

    private static List<string> InvokeEveryDefault(object instance, IEnumerable<MethodInfo> seams)
    {
        var failures = new List<string>();
        foreach (var seam in seams)
        {
            var arguments = seam.GetParameters().Select(parameter => Sample(parameter.ParameterType)).ToArray();
            try
            {
                seam.Invoke(instance, arguments);
            }
            catch (TargetInvocationException exception)
            {
                failures.Add($"{seam}: {exception.InnerException?.GetType().Name}: {exception.InnerException?.Message}");
            }
        }

        return failures;
    }

    [Test]
    public void The_definition_search_is_lazy_and_finds_only_concrete_definitions()
    {
        // The search moved to the catalog part with the ability split.
        var types = (IEnumerable<Type>)typeof(AbilityCatalogService).GetMethod("GetAbilityListDefinitionTypes", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(new AbilityCatalogService(null!), null)!;

        types.Should().NotBeAssignableTo<ICollection<Type>>("each definition is built when the loop reaches it");
        types.Should().OnlyContain(type => typeof(IAbilityListDefinition).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface);
    }

    [Test]
    public void The_stat_adjustment_is_stats_total()
    {
        // Dark Force and the damage inputs took the read with them.
        Run(DarkForcePart(), "GetStatAdjustment", Creature, StatType.Attack).Should().Be(0);
        Run(DamageInputPart(), "GetStatAdjustment", Creature, StatType.Attack).Should().Be(0);
    }

    [Test]
    public void The_weapon_skill_check_is_combats()
    {
        // The damage inputs took the check with them.
        Run(DamageInputPart(), "IsWeaponSkillType", SkillType.Vibroblade).Should().Be(true);
        Run(DamageInputPart(), "IsWeaponSkillType", SkillType.Force).Should().Be(false);
    }

    [Test]
    public void The_ability_skill_is_the_definitions_for_a_creature_with_no_override()
    {
        // The impact lifecycle took the read with it.
        Run(LifecyclePart(), "GetAbilitySkillType", Creature, new AbilityDetail { SkillType = SkillType.Force }).Should().Be(SkillType.Force);
    }

    [TestCase(100, 50, 50)]
    [TestCase(100, 0, 100)]
    public void Defense_ignore_is_combats_arithmetic(int defense, int percent, int expected)
    {
        Run(DamageInputPart(), "ApplyDefenseIgnore", defense, percent).Should().Be(expected);
    }

    [Test]
    public void The_healing_eligibility_is_combats_arithmetic()
    {
        // The impact state store calls the rule directly since the ability split.
        CombatRules.CalculateDamageEligibleForHealing(50, 50, 20).Should().Be(30);
    }

    [Test]
    public void The_npc_attack_formula_is_stats()
    {
        Run(DamageInputPart(), "GetAttack", 10, 12, 3).Should().Be(CombatRating.GetAttack(10, 12, 3));
    }

    [Test]
    public void The_damage_roll_is_combats_with_no_critical_on_a_creature_with_no_stats()
    {
        var roll = ((int Damage, int CriticalRating, bool WasCriticalDowngraded))Run(
            DamageStagePart(), "CalculateDamageWithCriticalMitigation", Creature, 40, 10, 12, 20, 12, 0)!;

        roll.CriticalRating.Should().Be(0);
        roll.WasCriticalDowngraded.Should().BeFalse();
    }

    [Test]
    public void The_constructor_only_assigns()
    {
        var engine = new FakeNWScript();
        using var scope = NWScriptRuntime.Override(engine);

        _ = AbilityInternals.NoNeighbours();

        engine.Calls.Should().BeEmpty();
    }
}
