using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Service.CombatRecorderService;

public class CombatRecorderHealApplicationTests
{
    // Applying a status effect checks the combat recorder, which the facade resolves from a container.
    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void InstallFacades() => _facadeScope = _facades.Install();

    [TearDown]
    public void RemoveFacades() => _facadeScope.Dispose();

    private const uint Target = 7;
    private const uint OtherTarget = 8;

    [Test]
    public void AHealNestedOnTheSameTarget_KeepsItsOwnPvpReduction()
    {
        CombatRecorder.BeginHealApplication(Target);
        CombatRecorder.RecordPvpHealingReduced(Target, 100, 40);
        CombatRecorder.BeginHealApplication(Target);
        CombatRecorder.RecordPvpHealingReduced(Target, 50, 25);

        CombatRecorder.EndHealApplication(Target).Should().Be(25, "the inner heal closes first with its own cut");
        CombatRecorder.EndHealApplication(Target).Should().Be(60, "the outer heal keeps the cut made before the inner one");
    }

    [Test]
    public void AnUnreducedNestedHeal_DoesNotTakeTheOuterReduction()
    {
        CombatRecorder.BeginHealApplication(Target);
        CombatRecorder.RecordPvpHealingReduced(Target, 100, 40);
        CombatRecorder.BeginHealApplication(Target);

        CombatRecorder.EndHealApplication(Target).Should().Be(0, "the inner heal was not cut");
        CombatRecorder.EndHealApplication(Target).Should().Be(60);
    }

    [Test]
    public void ANestedHealOnAnotherTarget_LeavesTheOuterFrameAlone()
    {
        CombatRecorder.BeginHealApplication(Target);
        CombatRecorder.BeginHealApplication(OtherTarget);
        CombatRecorder.RecordPvpHealingReduced(Target, 100, 40);

        CombatRecorder.EndHealApplication(OtherTarget).Should().Be(0);
        CombatRecorder.EndHealApplication(Target).Should().Be(60);
        CombatRecorder.EndHealApplication(Target).Should().Be(0, "a frame closes once");
    }
}
