using System.Numerics;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Abilities;

/// <summary>
/// A new <see cref="AbilityBench"/> per test, around a <see cref="FacadeContainer"/> for the definitions and status effects
/// that ask a facade.
/// </summary>
public abstract class AbilityServiceTestBase
{
    protected const uint Caster = 501;
    protected const uint Target = 502;
    protected const uint Other = 503;
    protected const uint ObjectInvalid = AbilityBench.ObjectInvalid;

    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;

    protected AbilityBench Bench { get; private set; } = null!;

    protected TestAbilityService Service => Bench.Service;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void SetUpBench()
    {
        _facadeScope = _facades.Install();
        Bench = new AbilityBench();
        Bench.Positions[Caster] = new Vector3(0f, 0f, 0f);
        Bench.Positions[Target] = new Vector3(3f, 0f, 0f);
        Bench.Positions[Other] = new Vector3(4f, 0f, 0f);
        Bench.Hostile.Add((Target, Caster));
        Bench.Hostile.Add((Other, Caster));
    }

    [TearDown]
    public void TearDownBench()
    {
        Bench.Dispose();
        _facadeScope.Dispose();
    }

    protected static AbilityDetail Hostile(string name = "Blast", SkillType skill = SkillType.Force) => new()
    {
        Name = name,
        IsHostileAbility = true,
        IsSingleTargetAbility = true,
        SkillType = skill,
        ActivationType = AbilityActivationType.Casted,
        Requirements = new List<IAbilityActivationRequirement>()
    };

    /// <summary>The entries of the log that start with one of <paramref name="prefixes"/>, in order.</summary>
    protected List<string> LogOf(params string[] prefixes) =>
        Bench.Log.Where(entry => prefixes.Any(prefix => entry.StartsWith(prefix, StringComparison.Ordinal))).ToList();
}
