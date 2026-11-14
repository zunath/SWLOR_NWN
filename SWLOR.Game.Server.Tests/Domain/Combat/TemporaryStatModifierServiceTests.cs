using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// Unit tests of the temporary stat modifier instance: the engine is replaced by a recording fake and the clock by a
/// settable one, so no server is needed. The stat aggregation goes through the Stat facade, which resolves from a container.
/// </summary>
[TestFixture]
public class TemporaryStatModifierServiceTests
{
    private const uint Player = 7001;
    private const uint Npc = 7002;
    private const uint Other = 7003;

    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;
    private FakeNWScript _engine = null!;
    private IDisposable _engineScope = null!;
    private IDisposable _clockScope = null!;
    private DateTime _now;
    private TemporaryStatModifierService _service = null!;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void SetUp()
    {
        _facadeScope = _facades.Install();
        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsPC", arguments => (uint)arguments[0]! == Player);
        _engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != Other);
        _engineScope = NWScriptRuntime.Override(_engine);
        _now = new DateTime(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);
        _clockScope = Clock.OverrideForTests(() => _now);
        _service = new TemporaryStatModifierService(FacadeForwarder.For<IGuiService>());
    }

    [TearDown]
    public void TearDown()
    {
        _clockScope.Dispose();
        _engineScope.Dispose();
        _facadeScope.Dispose();
    }

    private int Refreshes => _engine.CountOf("DelayCommand");

    [Test]
    public void Add_sums_the_amounts_of_the_same_stat()
    {
        _service.Add(Npc, StatType.Attack, 5, 30f);
        _service.Add(Npc, StatType.Attack, 7, 30f, "other");
        _service.Add(Npc, StatType.Defense, 3, 30f);

        _service.GetStatAdjustment(Npc, StatType.Attack).Should().Be(12);
        _service.GetStatAdjustment(Npc, StatType.Defense).Should().Be(3);
        _service.GetStatAdjustment(Npc, StatType.Evasion).Should().Be(0);
    }

    [Test]
    public void Add_ignores_an_invalid_creature_a_zero_amount_and_a_nonpositive_duration()
    {
        _service.Add(NWScript.OBJECT_INVALID, StatType.Attack, 5, 30f);
        _service.Add(Npc, StatType.Attack, 0, 30f);
        _service.Add(Npc, StatType.Attack, 5, 0f);
        _service.Add(Npc, StatType.Attack, 5, -1f);

        _service.GetStatAdjustment(Npc, StatType.Attack).Should().Be(0);
        _service.GetStatAdjustment(NWScript.OBJECT_INVALID, StatType.Attack).Should().Be(0);
    }

    [Test]
    public void GetStatAdjustment_filters_by_group_and_a_null_group_matches_all()
    {
        _service.Add(Npc, StatType.Attack, 5, 30f, "a");
        _service.Add(Npc, StatType.Attack, 7, 30f, "b");
        _service.Add(Npc, StatType.Attack, 1, 30f);

        _service.GetStatAdjustment(Npc, StatType.Attack, "a").Should().Be(5);
        _service.GetStatAdjustment(Npc, StatType.Attack, "b").Should().Be(7);
        _service.GetStatAdjustment(Npc, StatType.Attack, "").Should().Be(1);
        _service.GetStatAdjustment(Npc, StatType.Attack, (string)null!).Should().Be(13);
    }

    [Test]
    public void The_stat_type_overloads_use_the_name_of_the_stat_as_the_group()
    {
        _service.Add(Npc, StatType.Attack, 5, 30f, StatType.Defense);
        _service.Add(Npc, StatType.Attack, 2, 30f, "Defense");
        _service.Add(Npc, StatType.Attack, 9, 30f, "Other");

        _service.GetStatAdjustment(Npc, StatType.Attack, StatType.Defense).Should().Be(7);
        _service.Refresh(Npc, StatType.Attack, 100f, StatType.Defense);
        _service.Consume(Npc, StatType.Attack, StatType.Defense).Should().Be(7);
        _service.GetStatAdjustment(Npc, StatType.Attack).Should().Be(9);
    }

    [Test]
    public void A_modifier_expires_when_its_duration_has_passed()
    {
        _service.Add(Npc, StatType.Attack, 5, 30f);
        _service.Add(Npc, StatType.Attack, 7, 60f);

        _now = _now.AddSeconds(29);
        _service.GetStatAdjustment(Npc, StatType.Attack).Should().Be(12);

        _now = _now.AddSeconds(2);
        _service.GetStatAdjustment(Npc, StatType.Attack).Should().Be(7);

        _now = _now.AddSeconds(60);
        _service.GetStatAdjustment(Npc, StatType.Attack).Should().Be(0);
    }

    [Test]
    public void Reading_an_expired_player_modifier_publishes_one_refresh()
    {
        _service.Add(Player, StatType.Attack, 5, 30f);
        _engine.ClearCalls();

        _now = _now.AddSeconds(31);
        _service.GetStatAdjustment(Player, StatType.Attack).Should().Be(0);
        Refreshes.Should().Be(1);

        _service.GetStatAdjustment(Player, StatType.Attack).Should().Be(0);
        Refreshes.Should().Be(1);
    }

    [Test]
    public void Only_a_player_gets_a_refresh_and_it_is_delayed_by_zero_seconds()
    {
        _service.Add(Npc, StatType.Attack, 5, 30f);
        Refreshes.Should().Be(0);

        _service.Add(Player, StatType.Attack, 5, 30f);
        Refreshes.Should().Be(1);
        _engine.CallsTo("DelayCommand")[0].Arguments[0].Should().Be(0.0f);
    }

    [Test]
    public void Replace_removes_the_stacks_of_the_group_and_publishes_once()
    {
        _service.Add(Player, StatType.Attack, 5, 30f, "buff");
        _service.Add(Player, StatType.Attack, 5, 30f, "buff");
        _service.Add(Player, StatType.Attack, 4, 30f, "other");
        _engine.ClearCalls();

        _service.Replace(Player, StatType.Attack, 8, 30f, "buff");

        _service.GetStatAdjustment(Player, StatType.Attack, "buff").Should().Be(8);
        _service.GetStatAdjustment(Player, StatType.Attack, "other").Should().Be(4);
        Refreshes.Should().Be(1);
    }

    [Test]
    public void Replace_with_a_stat_type_group_matches_the_named_group()
    {
        _service.Add(Npc, StatType.Attack, 5, 30f, "Defense");

        _service.Replace(Npc, StatType.Attack, 3, 30f, StatType.Defense);

        _service.GetStatAdjustment(Npc, StatType.Attack).Should().Be(3);
    }

    [Test]
    public void AddCapped_adds_whole_stacks_up_to_the_cap()
    {
        _service.AddCapped(Npc, StatType.Attack, 5, 30f, 12, "g", 5).Should().Be(2);
        _service.GetStatAdjustment(Npc, StatType.Attack, "g").Should().Be(10);

        _service.AddCapped(Npc, StatType.Attack, 5, 30f, 12, "g", 5).Should().Be(0);
        _service.GetStatAdjustment(Npc, StatType.Attack, "g").Should().Be(10);
    }

    [Test]
    public void AddCapped_adds_no_more_than_the_requested_stacks()
    {
        _service.AddCapped(Npc, StatType.Attack, 5, 30f, 100, "g", 3).Should().Be(3);
        _service.GetStatAdjustment(Npc, StatType.Attack, "g").Should().Be(15);
    }

    [TestCase(0, 30f, 10, 1)]
    [TestCase(-1, 30f, 10, 1)]
    [TestCase(5, 0f, 10, 1)]
    [TestCase(5, 30f, 0, 1)]
    [TestCase(5, 30f, 10, 0)]
    public void AddCapped_adds_nothing_for_an_invalid_request(int amount, float seconds, int maxTotal, int stacks)
    {
        _service.AddCapped(Npc, StatType.Attack, amount, seconds, maxTotal, "g", stacks).Should().Be(0);
        _service.GetStatAdjustment(Npc, StatType.Attack).Should().Be(0);
    }

    [Test]
    public void AddCapped_publishes_one_refresh_only_when_it_added_a_stack()
    {
        _service.AddCapped(Player, StatType.Attack, 5, 30f, 10, "g", 4).Should().Be(2);
        Refreshes.Should().Be(1);

        _service.AddCapped(Player, StatType.Attack, 5, 30f, 10, "g", 4).Should().Be(0);
        Refreshes.Should().Be(1);
    }

    [Test]
    public void AddCapped_can_refresh_the_existing_stacks_to_one_clock()
    {
        _service.Add(Npc, StatType.Attack, 5, 30f, "g");

        _now = _now.AddSeconds(20);
        _service.AddCapped(Npc, StatType.Attack, 5, 30f, 10, "g", 1, refreshExistingStacks: true).Should().Be(1);

        _now = _now.AddSeconds(20);
        _service.GetStatAdjustment(Npc, StatType.Attack, "g").Should().Be(10);
    }

    [Test]
    public void AddCapped_refreshes_the_existing_stacks_even_at_the_cap()
    {
        _service.Add(Npc, StatType.Attack, 10, 30f, "g");

        _now = _now.AddSeconds(20);
        _service.AddCapped(Npc, StatType.Attack, 5, 30f, 10, "g", 1, refreshExistingStacks: true).Should().Be(0);

        _now = _now.AddSeconds(20);
        _service.GetStatAdjustment(Npc, StatType.Attack, "g").Should().Be(10);
    }

    [Test]
    public void AddCapped_without_a_refresh_leaves_the_old_stacks_on_their_own_clock()
    {
        _service.Add(Npc, StatType.Attack, 5, 30f, "g");

        _now = _now.AddSeconds(20);
        _service.AddCapped(Npc, StatType.Attack, 5, 30f, 10, "g", 1).Should().Be(1);

        _now = _now.AddSeconds(20);
        _service.GetStatAdjustment(Npc, StatType.Attack, "g").Should().Be(5);
    }

    [Test]
    public void AddCapped_with_a_stat_type_group_matches_the_named_group()
    {
        _service.AddCapped(Npc, StatType.Attack, 5, 30f, 10, StatType.Defense, 5).Should().Be(2);

        _service.GetStatAdjustment(Npc, StatType.Attack, "Defense").Should().Be(10);
    }

    [Test]
    public void Consume_returns_the_total_and_removes_the_matching_modifiers()
    {
        _service.Add(Npc, StatType.Attack, 5, 30f, "a");
        _service.Add(Npc, StatType.Attack, 6, 30f, "a");
        _service.Add(Npc, StatType.Attack, 7, 30f, "b");
        _service.Add(Npc, StatType.Defense, 2, 30f, "a");

        _service.Consume(Npc, StatType.Attack, "a").Should().Be(11);

        _service.GetStatAdjustment(Npc, StatType.Attack).Should().Be(7);
        _service.GetStatAdjustment(Npc, StatType.Defense).Should().Be(2);
        _service.Consume(Npc, StatType.Attack, "a").Should().Be(0);
    }

    [Test]
    public void Consume_of_the_last_modifier_forgets_the_creature()
    {
        _service.Add(Npc, StatType.Attack, 5, 30f);

        _service.Consume(Npc, StatType.Attack).Should().Be(5);

        TemporaryStatModifierInternals.ModifiersOf(_service).Contains(Npc).Should().BeFalse();
    }

    [Test]
    public void Consume_publishes_a_refresh_only_when_something_changed()
    {
        _service.Add(Player, StatType.Attack, 5, 30f, "a");
        _engine.ClearCalls();

        _service.Consume(Player, StatType.Attack, "missing").Should().Be(0);
        Refreshes.Should().Be(0);

        _service.Consume(Player, StatType.Attack, "a").Should().Be(5);
        Refreshes.Should().Be(1);

        _service.Consume(Player, StatType.Attack, "a").Should().Be(0);
        Refreshes.Should().Be(1);
    }

    [Test]
    public void Consume_of_expired_modifiers_publishes_a_refresh()
    {
        _service.Add(Player, StatType.Attack, 5, 30f, "a");
        _engine.ClearCalls();

        _now = _now.AddSeconds(31);
        _service.Consume(Player, StatType.Attack, "a").Should().Be(0);

        Refreshes.Should().Be(1);
    }

    [Test]
    public void Consume_of_a_creature_without_modifiers_returns_zero_without_a_refresh()
    {
        _service.Consume(Player, StatType.Attack).Should().Be(0);

        Refreshes.Should().Be(0);
    }

    [Test]
    public void GetStatSources_groups_the_modifiers_that_carry_the_payload_stat()
    {
        _service.Add(Npc, StatType.Attack, 5, 30f, "a");
        _service.Add(Npc, StatType.Defense, 2, 30f, "a");
        _service.Add(Npc, StatType.Attack, 7, 30f, "b");
        _service.Add(Npc, StatType.Defense, 9, 30f, "c");

        var sources = _service.GetStatSources(Npc, StatType.Attack);

        sources.Select(source => source.Key).Should().BeEquivalentTo("temporary:a", "temporary:b");
        var a = sources.Single(source => source.Key == "temporary:a");
        a[StatType.Attack].Should().Be(5);
        a[StatType.Defense].Should().Be(2);
        sources.Single(source => source.Key == "temporary:b")[StatType.Attack].Should().Be(7);
    }

    [Test]
    public void GetStatSources_is_empty_without_a_creature_entry_or_a_matching_payload()
    {
        _service.GetStatSources(Npc, StatType.Attack).Should().BeEmpty();

        _service.Add(Npc, StatType.Defense, 2, 30f, "a");
        _service.GetStatSources(Npc, StatType.Attack).Should().BeEmpty();
    }

    [Test]
    public void GetStatSources_purges_expired_modifiers_and_publishes_a_refresh()
    {
        _service.Add(Player, StatType.Attack, 5, 30f, "a");
        _engine.ClearCalls();

        _now = _now.AddSeconds(31);

        _service.GetStatSources(Player, StatType.Attack).Should().BeEmpty();
        Refreshes.Should().Be(1);
    }

    [Test]
    public void ConsumeSource_consumes_only_the_group_of_the_inspected_source()
    {
        _service.Add(Npc, StatType.Attack, 5, 30f, "a");
        _service.Add(Npc, StatType.Attack, 7, 30f, "b");
        var source = _service.GetStatSources(Npc, StatType.Attack).Single(x => x.Key == "temporary:a");

        _service.ConsumeSource(Npc, StatType.Attack, source).Should().Be(5);

        _service.GetStatAdjustment(Npc, StatType.Attack).Should().Be(7);
    }

    [Test]
    public void ConsumeSource_returns_zero_when_the_source_is_gone()
    {
        _service.Add(Npc, StatType.Attack, 5, 30f, "a");
        var source = _service.GetStatSources(Npc, StatType.Attack).Single();
        _service.Consume(Npc, StatType.Attack, "a");

        _service.ConsumeSource(Npc, StatType.Attack, source).Should().Be(0);

        _service.Add(Npc, StatType.Attack, 7, 30f, "b");
        _service.ConsumeSource(Npc, StatType.Attack, source).Should().Be(0);
        _service.GetStatAdjustment(Npc, StatType.Attack).Should().Be(7);
    }

    [Test]
    public void Refresh_moves_the_expiration_of_the_matching_modifiers_only()
    {
        _service.Add(Npc, StatType.Attack, 5, 30f, "a");
        _service.Add(Npc, StatType.Attack, 7, 30f, "b");

        _service.Refresh(Npc, StatType.Attack, 100f, "a");

        _now = _now.AddSeconds(60);
        _service.GetStatAdjustment(Npc, StatType.Attack).Should().Be(5);
    }

    [Test]
    public void Refresh_ignores_a_nonpositive_duration_and_a_creature_without_modifiers()
    {
        _service.Add(Npc, StatType.Attack, 5, 30f);

        _service.Refresh(Npc, StatType.Attack, 0f);
        _service.Refresh(Other, StatType.Attack, 100f);

        _now = _now.AddSeconds(31);
        _service.GetStatAdjustment(Npc, StatType.Attack).Should().Be(0);
    }

    [Test]
    public void Refresh_purges_expired_modifiers_first_and_publishes_a_refresh()
    {
        _service.Add(Player, StatType.Attack, 5, 30f);
        _engine.ClearCalls();

        _now = _now.AddSeconds(31);
        _service.Refresh(Player, StatType.Attack, 100f);

        Refreshes.Should().Be(1);
        _service.GetStatAdjustment(Player, StatType.Attack).Should().Be(0);
    }

    [Test]
    public void Clear_removes_every_modifier_of_the_creature_and_publishes_once()
    {
        _service.Add(Player, StatType.Attack, 5, 30f);
        _service.Add(Player, StatType.Defense, 5, 30f);
        _service.Add(Npc, StatType.Attack, 5, 30f);
        _engine.ClearCalls();

        _service.Clear(Player);
        _service.Clear(Player);

        _service.GetStatAdjustment(Player, StatType.Attack).Should().Be(0);
        _service.GetStatAdjustment(Npc, StatType.Attack).Should().Be(5);
        Refreshes.Should().Be(1);
    }

    [Test]
    public void ClearExitingObject_clears_the_creature_that_left()
    {
        _engine.SetReturn("GetExitingObject", Npc);
        _service.Add(Npc, StatType.Attack, 5, 30f);
        _service.Add(Other, StatType.Attack, 5, 30f);

        _service.ClearExitingObject();

        _service.GetStatAdjustment(Npc, StatType.Attack).Should().Be(0);
        _service.GetStatAdjustment(Other, StatType.Attack).Should().Be(5);
    }

    [Test]
    public void PurgeAllExpired_purges_valid_creatures_and_clears_the_invalid_ones()
    {
        _service.Add(Player, StatType.Attack, 5, 30f);
        _service.Add(Player, StatType.Defense, 5, 90f);
        _service.Add(Npc, StatType.Attack, 5, 90f);
        _service.Add(Other, StatType.Attack, 5, 90f);
        _engine.ClearCalls();

        _now = _now.AddSeconds(31);
        _service.PurgeAllExpired();

        TemporaryStatModifierInternals.ModifiersOf(_service).Contains(Other).Should().BeFalse();
        TemporaryStatModifierInternals.ModifiersOf(_service).Contains(Npc).Should().BeTrue();
        _service.GetStatAdjustment(Player, StatType.Attack).Should().Be(0);
        _service.GetStatAdjustment(Player, StatType.Defense).Should().Be(5);
        Refreshes.Should().Be(1);
    }

    [Test]
    public void PurgeAllExpired_publishes_nothing_when_nothing_expired()
    {
        _service.Add(Player, StatType.Attack, 5, 30f);
        _engine.ClearCalls();

        _service.PurgeAllExpired();

        Refreshes.Should().Be(0);
    }

    [Test]
    public void The_state_belongs_to_the_instance()
    {
        var second = new TemporaryStatModifierService(FacadeForwarder.For<IGuiService>());
        _service.Add(Npc, StatType.Attack, 5, 30f);

        second.GetStatAdjustment(Npc, StatType.Attack).Should().Be(0);
    }
}
