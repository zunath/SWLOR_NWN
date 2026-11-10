using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Tests.Support;
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

    private static IEnumerable<MethodInfo> Seams() =>
        typeof(AbilityService).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.IsVirtual && method.IsFamily);

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

    private object? Run(string name, params object?[] arguments)
    {
        var method = Seams().Single(seam => seam.Name == name &&
            seam.GetParameters().Select(parameter => parameter.ParameterType)
                .SequenceEqual(arguments.Select(argument => argument!.GetType())));
        return method.Invoke(_service, arguments);
    }

    [Test]
    public void There_is_a_seam_for_every_unconverted_call()
    {
        Seams().Should().HaveCountGreaterThan(70);
    }

    [Test]
    public void Every_default_reaches_the_static_code_and_stops_without_a_server()
    {
        // These need the database or the native engine for a real creature, so they are left to the in-engine tests:
        // the space mode and FP reads ask the database and the NPC stats read a creature's skin.
        var engineOnly = new HashSet<string>
        {
            "GetAbilityListDefinitionTypes"
        };

        var failures = new List<string>();
        foreach (var seam in Seams().Where(method => !engineOnly.Contains(method.Name)))
        {
            var arguments = seam.GetParameters().Select(parameter => Sample(parameter.ParameterType)).ToArray();
            try
            {
                seam.Invoke(_service, arguments);
            }
            catch (TargetInvocationException exception)
            {
                failures.Add($"{seam}: {exception.InnerException?.GetType().Name}: {exception.InnerException?.Message}");
            }
        }

        failures.Should().BeEmpty();
    }

    [Test]
    public void The_definition_search_is_lazy_and_finds_only_concrete_definitions()
    {
        var types = (IEnumerable<Type>)Run("GetAbilityListDefinitionTypes")!;

        types.Should().NotBeAssignableTo<ICollection<Type>>("each definition is built when the loop reaches it");
        types.Should().OnlyContain(type => typeof(IAbilityListDefinition).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface);
    }

    [Test]
    public void The_stat_adjustment_is_stats_total()
    {
        Run("GetStatAdjustment", Creature, StatType.Attack).Should().Be(0);
    }

    [Test]
    public void The_weapon_skill_check_is_combats()
    {
        Run("IsWeaponSkillType", SkillType.Vibroblade).Should().Be(true);
        Run("IsWeaponSkillType", SkillType.Force).Should().Be(false);
    }

    [Test]
    public void The_ability_skill_is_the_definitions_for_a_creature_with_no_override()
    {
        Run("GetAbilitySkillType", Creature, new AbilityDetail { SkillType = SkillType.Force }).Should().Be(SkillType.Force);
    }

    [TestCase(100, 50, 50)]
    [TestCase(100, 0, 100)]
    public void Defense_ignore_is_combats_arithmetic(int defense, int percent, int expected)
    {
        Run("ApplyDefenseIgnore", defense, percent).Should().Be(expected);
    }

    [Test]
    public void The_healing_eligibility_is_combats_arithmetic()
    {
        Run("CalculateDamageEligibleForHealing", 50, 50, 20).Should().Be(30);
    }

    [Test]
    public void The_npc_attack_formula_is_stats()
    {
        Run("GetAttack", 10, 12, 3).Should().Be(Stat.GetAttack(10, 12, 3));
    }

    [Test]
    public void The_damage_roll_is_combats_with_no_critical_on_a_creature_with_no_stats()
    {
        var roll = ((int Damage, int CriticalRating, bool WasCriticalDowngraded))Run(
            "CalculateDamageWithCriticalMitigation", Creature, 40, 10, 12, 20, 12, 0)!;

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
