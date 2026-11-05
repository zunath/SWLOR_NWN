using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>Unit tests of the busy-status instance, over a fake engine with real local variables.</summary>
[TestFixture]
public class ActivityServiceTests
{
    private const uint Player = 1001;
    private const uint Creature = 1002;

    private FakeNWScript _engine = null!;
    private LocalVariables _variables = null!;
    private ActivityService _service = null!;
    private IDisposable _scope = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new FakeNWScript();
        _variables = new LocalVariables(_engine);
        _service = new ActivityService();
        _scope = NWScriptRuntime.Override(_engine);
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    [Test]
    public void A_target_is_not_busy_until_it_is_marked()
    {
        _service.IsBusy(Player).Should().BeFalse();
        _service.GetBusyType(Player).Should().Be(ActivityStatusType.Invalid);
    }

    [TestCase(ActivityStatusType.UseItem)]
    [TestCase(ActivityStatusType.AbilityActivation)]
    [TestCase(ActivityStatusType.Resting)]
    [TestCase(ActivityStatusType.Quest)]
    [TestCase(ActivityStatusType.Fishing)]
    [TestCase(ActivityStatusType.Prospecting)]
    [TestCase(ActivityStatusType.Performing)]
    public void SetBusy_stores_the_flag_and_the_type_on_the_target(ActivityStatusType type)
    {
        _service.SetBusy(Player, type);

        _service.IsBusy(Player).Should().BeTrue();
        _service.GetBusyType(Player).Should().Be(type);
        _variables.Int(Player, "IS_BUSY").Should().Be(1);
        _variables.Int(Player, "BUSY_TYPE").Should().Be((int)type);
    }

    [Test]
    public void Busy_status_is_kept_per_target()
    {
        _service.SetBusy(Player, ActivityStatusType.Fishing);

        _service.IsBusy(Creature).Should().BeFalse();
        _service.GetBusyType(Creature).Should().Be(ActivityStatusType.Invalid);
    }

    [Test]
    public void ClearBusy_removes_the_flag_and_the_type()
    {
        _service.SetBusy(Player, ActivityStatusType.Quest);

        _service.ClearBusy(Player);

        _service.IsBusy(Player).Should().BeFalse();
        _service.GetBusyType(Player).Should().Be(ActivityStatusType.Invalid);
        _variables.Has(Player, "IS_BUSY").Should().BeFalse();
        _variables.Has(Player, "BUSY_TYPE").Should().BeFalse();
    }

    [Test]
    public void The_type_of_a_target_that_is_not_busy_is_invalid_even_when_a_type_is_left_behind()
    {
        _engine.SetReturn("GetLocalBool", false);
        _engine.SetReturn("GetLocalInt", (int)ActivityStatusType.Resting);

        _service.GetBusyType(Player).Should().Be(ActivityStatusType.Invalid);
    }

    [Test]
    public void Entering_the_module_wipes_the_busy_status_of_the_entering_player()
    {
        _service.SetBusy(Player, ActivityStatusType.Resting);
        _engine.SetReturn("GetEnteringObject", Player);

        _service.WipeStatusOnEntry();

        _service.IsBusy(Player).Should().BeFalse();
    }

    [Test]
    public void Dying_wipes_the_busy_status_of_the_player_who_died()
    {
        _service.SetBusy(Player, ActivityStatusType.UseItem);
        _engine.SetReturn("GetLastPlayerDied", Player);

        _service.WipeStatusOnDeath();

        _service.IsBusy(Player).Should().BeFalse();
        _service.GetBusyType(Player).Should().Be(ActivityStatusType.Invalid);
    }
}
