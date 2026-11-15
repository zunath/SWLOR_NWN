using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The target queries of the ability definitions (moved to Combat with the ability split, P5-3c) over an engine fake: the
/// friendly target checks, the single-target friendly search, the hostile search with a priority target, and where an impact
/// lands. The party search reads the roster and is covered by the Combat part tests that use it.
/// </summary>
[TestFixture]
public class AbilityTargetingTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint Area = 7000;
    private const uint Caster = 501;
    private const uint Ally = 502;
    private const uint Enemy = 503;
    private const uint Other = 504;

    private FakeNWScript _engine = null!;
    private IDisposable _scope = null!;
    private readonly HashSet<uint> _dead = new();
    private readonly HashSet<(uint Target, uint Source)> _hostile = new();
    private readonly Dictionary<uint, Vector3> _positions = new();
    private readonly Dictionary<nint, (uint Area, Vector3 Position)> _locations = new();
    private readonly List<uint> _nearest = new();
    private nint _nextHandle = 100;

    [SetUp]
    public void SetUp()
    {
        _dead.Clear();
        _hostile.Clear();
        _positions.Clear();
        _locations.Clear();
        _nearest.Clear();
        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != ObjectInvalid);
        _engine.SetReturnHandler("GetIsDead", args => _dead.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetCurrentHitPoints", args => _dead.Contains((uint)args[0]!) ? 0 : 10);
        _engine.SetReturnHandler("GetIsReactionTypeHostile", args => _hostile.Contains(((uint)args[0]!, (uint)args[1]!)));
        _engine.SetReturnHandler("GetArea", args => (uint)args[0]! == ObjectInvalid ? ObjectInvalid : Area);
        _engine.SetReturnHandler("GetLocation", args => At(Area, _positions.GetValueOrDefault((uint)args[0]!)));
        _engine.SetReturnHandler("GetAreaFromLocation", args =>
            _locations.TryGetValue(((Location)args[0]!).Handle, out var location) ? location.Area : ObjectInvalid);
        _engine.SetReturnHandler("GetDistanceBetweenLocations", args =>
            Vector3.Distance(_locations[((Location)args[0]!).Handle].Position, _locations[((Location)args[1]!).Handle].Position));
        _engine.SetReturnHandler("GetNearestCreatureToLocation", args =>
        {
            var nth = (int)args[3]!;
            return nth <= _nearest.Count ? _nearest[nth - 1] : ObjectInvalid;
        });
        _scope = NWScriptRuntime.Override(_engine);
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    private Location At(uint area, Vector3 position)
    {
        var handle = ++_nextHandle;
        _locations[handle] = (area, position);
        return EngineStructures.Create<Location>(handle);
    }

    [Test]
    public void A_friendly_target_is_checked_in_order()
    {
        AbilityTargeting.ValidateFriendlyTarget(Caster, ObjectInvalid).Should().Be("A friendly target is required.");
        AbilityTargeting.ValidateFriendlyTarget(Caster, Caster, allowSelf: false).Should().Be("You cannot target yourself with this ability.");

        _hostile.Add((Enemy, Caster));
        AbilityTargeting.ValidateFriendlyTarget(Caster, Enemy).Should().Be("You may only use this ability on allies.");

        AbilityTargeting.ValidateFriendlyTarget(Caster, Ally, requireDead: true).Should().Be("Your target is not unconscious.");
        AbilityTargeting.ValidateFriendlyTarget(Caster, Ally).Should().BeEmpty();

        _dead.Add(Ally);
        AbilityTargeting.ValidateFriendlyTarget(Caster, Ally).Should().Be("Your target must be alive.");
        AbilityTargeting.ValidateFriendlyTarget(Caster, Ally, requireDead: true).Should().BeEmpty();
    }

    [Test]
    public void A_friendly_target_falls_back_to_the_activator_only_when_self_is_allowed()
    {
        _hostile.Add((Enemy, Caster));

        AbilityTargeting.ResolveFriendlyTarget(Caster, Ally).Should().Be(Ally);
        AbilityTargeting.ResolveFriendlyTarget(Caster, Enemy).Should().Be(Caster);
        AbilityTargeting.ResolveFriendlyTarget(Caster, Caster, allowSelf: false).Should().Be(ObjectInvalid);
        AbilityTargeting.ResolveFriendlyTarget(Caster, ObjectInvalid, allowSelf: false).Should().Be(ObjectInvalid);
    }

    [Test]
    public void Without_the_party_the_friendly_search_is_the_living_target_or_else_the_living_activator()
    {
        _hostile.Add((Enemy, Caster));

        AbilityTargeting.GetFriendlyTargets(Caster, Ally, affectsParty: false).Should().Equal(Ally);
        AbilityTargeting.GetFriendlyTargets(Caster, Enemy, affectsParty: false).Should().Equal(Caster);

        _dead.Add(Ally);
        AbilityTargeting.GetFriendlyTargets(Caster, Ally, affectsParty: false).Should().Equal(Caster);

        _dead.Add(Caster);
        AbilityTargeting.GetFriendlyTargets(Caster, Ally, affectsParty: false).Should().BeEmpty();
    }

    [Test]
    public void The_hostile_search_takes_the_priority_target_first_and_stops_at_the_cap()
    {
        _hostile.Add((Enemy, Caster));
        _hostile.Add((Other, Caster));
        _positions[Enemy] = new Vector3(1f, 0f, 0f);
        _positions[Other] = new Vector3(2f, 0f, 0f);
        _nearest.AddRange(new[] { Enemy, Other });
        var centre = At(Area, Vector3.Zero);

        AbilityTargeting.GetHostileTargetsNearLocation(Caster, centre, 5f, 1, priorityTarget: Other).Should().Equal(Other);
        AbilityTargeting.GetHostileTargetsNearLocation(Caster, centre, 5f, 0, priorityTarget: Other).Should().Equal(Other, Enemy);
        AbilityTargeting.GetHostileTargetsNearLocation(Caster, centre, 5f, 0, predicate: creature => creature != Enemy).Should().Equal(Other);
    }

    [Test]
    public void An_impact_lands_on_the_target_then_on_a_location_in_an_area_then_on_the_activator()
    {
        _positions[Ally] = new Vector3(3f, 0f, 0f);
        _positions[Caster] = new Vector3(9f, 0f, 0f);
        var chosen = At(Area, new Vector3(5f, 0f, 0f));
        var nowhere = EngineStructures.Create<Location>(0);

        Position(AbilityTargeting.ResolveImpactLocation(Caster, Ally, chosen)).Should().Be(new Vector3(3f, 0f, 0f));
        AbilityTargeting.ResolveImpactLocation(Caster, ObjectInvalid, chosen).Handle.Should().Be(chosen.Handle);
        Position(AbilityTargeting.ResolveImpactLocation(Caster, ObjectInvalid, nowhere)).Should().Be(new Vector3(9f, 0f, 0f));
    }

    private Vector3 Position(Location location) => _locations[location.Handle].Position;
}
