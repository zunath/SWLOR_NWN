using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The ability records part of the recorder: what it holds, how it reaches the stat service and the recorder, and where its
/// state lives. The records themselves (what each event writes) are pinned by <see cref="CombatRecorderServiceTests"/> and the
/// recorder transcript, which call this part.
/// </summary>
[TestFixture]
public class CombatRecordAbilityServiceTests
{
    private sealed class SeamAbility : CombatRecordAbilityService
    {
        public SeamAbility(ICombatRecordSession session, ICombatRecorderService recorder, IStatService stat)
            : base(session, recorder, stat)
        {
        }

        public int CallFP(uint creature) => GetCurrentFP(creature);
        public int CallStamina(uint creature) => GetCurrentStamina(creature);
    }

    [Test]
    public void ItHoldsTheSessionTheRecorderAndTheStatServiceAndNothingElse()
    {
        var constructor = typeof(CombatRecordAbilityService).GetConstructors().Should().ContainSingle().Subject;

        constructor.GetParameters().Select(parameter => parameter.ParameterType).Should().Equal(
            typeof(ICombatRecordSession), typeof(ICombatRecorderService), typeof(IStatService));
    }

    [Test]
    public void ItTakesTheStatServiceDirectlyAndNotThroughALazyOne()
    {
        // Nothing the stat service holds reaches the ability records, and no service holds them but one content part, so the
        // plain edge cannot close a cycle (the container build would hang on one).
        typeof(CombatRecordAbilityService).GetConstructors().Single().GetParameters()
            .Should().NotContain(parameter => parameter.ParameterType.IsGenericType);
    }

    [Test]
    public void TheDefaultSeamsReadTheFpAndStaminaFromTheStatService()
    {
        var stat = ScriptedProxy.Create<IStatService>(out var script);
        script.On("GetCurrentFP", arguments => (int)(uint)arguments[0]! + 100);
        script.On("GetCurrentStamina", arguments => (int)(uint)arguments[0]! + 200);
        var session = new CombatRecordSessionCore();
        var recorder = new PlainRecorder(ScriptedProxy.Create<IDMPresenceService>(out _), new InMemoryDBService());
        var ability = new SeamAbility(session, recorder, stat);

        ability.CallFP(7).Should().Be(107);
        ability.CallStamina(7).Should().Be(207);
        script.Calls.Select(call => call.Member).Should().Equal("GetCurrentFP", "GetCurrentStamina");
        script.Calls.Select(call => call.Arguments[1]).Should().OnlyContain(player => player == null, "the default reads no stored player");
    }

    [Test]
    public void TheToggleNotesAreThePartsOwnStateAndNoSessionListenerClearsThem()
    {
        // Start and Stop do not clear the toggle note, so the part registers no session listener and the recorder keeps no copy.
        var fields = typeof(CombatRecordAbilityService).GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Select(field => field.Name).OrderBy(name => name).ToArray();

        fields.Should().Equal("_recorder", "_session", "_stat", "_toggleDeactivatedBy", "_toggleDeactivatedFrame");
        typeof(CombatRecorderService).GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Should().NotContain(field => field.Name.StartsWith("_toggle"));
        typeof(CombatRecordAbilityService).GetMethods().Should().NotContain(method => method.Name.StartsWith("OnRecording"));
    }

    [Test]
    public void TheRecorderNoLongerDeclaresTheMovedMembers()
    {
        var moved = new[]
        {
            "RecordAbilityCast", "RecordAbilityDenied", "RecordAbilityInterrupted", "RecordAbilityQueued", "RecordAbilityImpact",
            "BeginAbilityCost", "CompleteAbilityCost", "NoteToggleDeactivated", "TakeToggleDeactivation"
        };
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        typeof(ICombatRecorderService).GetMethods().Select(method => method.Name).Should().NotIntersectWith(moved);
        typeof(CombatRecorderService).GetMethods(All).Select(method => method.Name).Should().NotIntersectWith(moved);
        typeof(ICombatRecordAbilityService).GetMethods().Select(method => method.Name).Should().BeEquivalentTo(moved);
    }

    [Test]
    public void TheContainerHoldsOnePartThatIsNotTheRecorder()
    {
        using var facades = new FacadeContainer();
        using var installed = facades.Install();

        var part = facades.Get<ICombatRecordAbilityService>();

        part.Should().BeOfType<CombatRecordAbilityService>();
        part.Should().NotBeSameAs(facades.Get<ICombatRecorderService>());
        GameServices.CombatRecordAbility.Should().BeSameAs(part);
        facades.Get<ICombatRecordAbilityService>().Should().BeSameAs(part, "it is one singleton");
    }
}
