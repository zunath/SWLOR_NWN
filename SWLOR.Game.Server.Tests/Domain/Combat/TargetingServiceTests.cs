using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>The targeting instance: entering targeting mode, and the one-shot action a selection runs.</summary>
[TestFixture]
public class TargetingServiceTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint Player = 1001;
    private const uint Target = 2002;

    private FakeNWScript _engine = null!;
    private IDisposable _scope = null!;
    private TargetingService _targeting = null!;
    private readonly List<uint> _selected = new();

    [SetUp]
    public void SetUp()
    {
        _selected.Clear();
        _engine = new FakeNWScript();
        _engine.SetReturn("GetLastPlayerToSelectTarget", Player);
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != ObjectInvalid);
        _scope = NWScriptRuntime.Override(_engine);
        _targeting = new TargetingService();
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    private void Enter(string message = "", bool allowsLocation = false) =>
        _targeting.EnterTargetingMode(Player, ObjectType.Creature, message, _selected.Add, allowsLocation);

    [Test]
    public void EnterTargetingMode_StartsTargetingAndTellsThePlayer()
    {
        Enter("Pick a target.");

        _engine.CallsTo("EnterTargetingMode").Should().ContainSingle();
        _engine.CallsTo("SendMessageToPC").Should().ContainSingle()
            .Which.Arguments[1].Should().Be("Pick a target.");
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void EnterTargetingMode_SendsNoMessageWhenThereIsNone(string? message)
    {
        Enter(message!);

        _engine.CallsTo("EnterTargetingMode").Should().ContainSingle();
        _engine.CountOf("SendMessageToPC").Should().Be(0);
    }

    [Test]
    public void RunTargetedItemAction_DoesNothingForAPlayerWithoutARegistration()
    {
        _engine.SetReturn("GetTargetingModeSelectedObject", Target);

        _targeting.RunTargetedItemAction();

        _selected.Should().BeEmpty();
    }

    [Test]
    public void RunTargetedItemAction_RunsTheActionOnceWithTheSelectedObject()
    {
        Enter();
        _engine.SetReturn("GetTargetingModeSelectedObject", Target);

        _targeting.RunTargetedItemAction();
        _targeting.RunTargetedItemAction();

        _selected.Should().Equal(Target);
    }

    [Test]
    public void RunTargetedItemAction_RunsWithNoObjectOnAGroundSelectionOnlyWhenLocationsAreAllowed()
    {
        _engine.SetReturn("GetTargetingModeSelectedObject", ObjectInvalid);
        _engine.SetReturn("GetTargetingModeSelectedPosition", new Vector3(1f, 2f, 0f));

        Enter(allowsLocation: false);
        _targeting.RunTargetedItemAction();
        _selected.Should().BeEmpty();

        Enter(allowsLocation: true);
        _targeting.RunTargetedItemAction();
        _selected.Should().Equal(ObjectInvalid);
    }

    [Test]
    public void RunTargetedItemAction_IgnoresAnEmptyGroundSelection()
    {
        _engine.SetReturn("GetTargetingModeSelectedObject", ObjectInvalid);
        _engine.SetReturn("GetTargetingModeSelectedPosition", new Vector3());

        Enter(allowsLocation: true);
        _targeting.RunTargetedItemAction();

        _selected.Should().BeEmpty();
    }

    [Test]
    public void ARegistrationReplacesTheEarlierOneForTheSamePlayer()
    {
        var first = new List<uint>();
        _targeting.EnterTargetingMode(Player, ObjectType.Creature, "", first.Add);
        Enter();
        _engine.SetReturn("GetTargetingModeSelectedObject", Target);

        _targeting.RunTargetedItemAction();

        first.Should().BeEmpty();
        _selected.Should().Equal(Target);
    }
}
