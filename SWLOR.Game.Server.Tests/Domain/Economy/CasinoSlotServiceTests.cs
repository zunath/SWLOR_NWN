using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.Economy;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Tests.Support;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Tests.Domain.Economy;

/// <summary>
/// Unit tests of the casino slot machine instance: the engine is replaced by a recording fake, the database by an
/// in-memory one and the window table of the GUI service by an override, so no server is needed. The one random draw a
/// spin makes is replayed with <see cref="System.Random"/> from the seed the test gives the shared generator.
/// </summary>
[TestFixture]
public class CasinoSlotServiceTests
{
    private const uint PlayerObject = 1001;
    private const uint MachineObject = 2002;
    private const uint AreaObject = 3003;
    private const string PublicCdKey = "ABCD1234";
    private const string AccountId = "casino-slots:" + PublicCdKey;

    private PlayerEngine _engine = null!;
    private InMemoryDBService _db = null!;
    private TestCasinoSlotService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new PlayerEngine();
        _db = new InMemoryDBService();
        _service = new TestCasinoSlotService(_db);
        PlaceEveryoneAtTheCasinoMachine();
    }

    [TearDown]
    public void TearDown()
    {
        Random.ResetSeed();
        _engine.Dispose();
    }

    /// <summary>A player standing beside the one casino machine, with a public CD key and plenty of credits.</summary>
    private void PlaceEveryoneAtTheCasinoMachine()
    {
        var engine = _engine.Engine;
        engine.SetReturn("GetIsObjectValid", true);
        engine.SetReturnHandler("GetArea", _ => AreaObject);
        engine.SetReturnHandler("GetResRef", arguments => (uint)arguments[0]! switch
        {
            AreaObject => "pw_ar_nscasino",
            MachineObject => "swd_smach02",
            _ => string.Empty
        });
        engine.SetReturn("GetTag", "SWTOR_SLOT2");
        engine.SetReturn("GetDistanceBetween", 2.0f);
        engine.SetReturn("GetPCPublicCDKey", PublicCdKey);
        engine.SetReturn("GetGold", 500);
        engine.SetReturn("GetLastUsedBy", PlayerObject);
        engine.SetReturn("OBJECT_SELF", MachineObject);
    }

    private static int SeedWhere(Func<int, bool> firstRollPays)
    {
        for (var seed = 0; seed < 100000; seed++)
        {
            if (firstRollPays(new System.Random(seed).Next(10000)))
                return seed;
        }

        throw new InvalidOperationException("No seed found.");
    }

    private CasinoSlotService ServiceWithPayout(int payout)
    {
        var seed = SeedWhere(roll => _service.GetPayout(roll) == payout);
        Random.SetSeed(seed);
        return _service;
    }

    private void RunAssignedCommands()
    {
        foreach (var call in _engine.Engine.CallsTo("AssignCommand").ToList())
            ((Action)call.Arguments[1]!).Invoke();
    }

    // ---- what a roll pays

    [Test]
    public void GetPayout_keeps_the_house_edge_over_every_roll()
    {
        var payouts = Enumerable.Range(0, 10000).Select(_service.GetPayout).ToArray();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(payouts.Count(p => p == 0), Is.EqualTo(7000));
            Assert.That(payouts.Count(p => p == 100), Is.EqualTo(2000));
            Assert.That(payouts.Count(p => p == 500), Is.EqualTo(800));
            Assert.That(payouts.Count(p => p == 1000), Is.EqualTo(190));
            Assert.That(payouts.Count(p => p == 10000), Is.EqualTo(10));
            Assert.That(payouts.Max(), Is.EqualTo(CasinoSlotService.MaximumPayout));
            Assert.That(payouts.Average(), Is.EqualTo(89));
            Assert.That(CasinoSlotService.SpinCost, Is.EqualTo(100));
        }
    }

    [TestCase(0, 0)]
    [TestCase(6999, 0)]
    [TestCase(7000, 100)]
    [TestCase(8999, 100)]
    [TestCase(9000, 500)]
    [TestCase(9799, 500)]
    [TestCase(9800, 1000)]
    [TestCase(9989, 1000)]
    [TestCase(9990, 10000)]
    [TestCase(9999, 10000)]
    public void GetPayout_changes_tier_at_each_threshold(int roll, int expected)
    {
        _service.GetPayout(roll).Should().Be(expected);
    }

    [TestCase(-1)]
    [TestCase(10000)]
    public void GetPayout_rejects_rolls_outside_the_table(int roll)
    {
        var payout = () => _service.GetPayout(roll);

        payout.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void The_constants_are_the_ones_the_facade_repeats()
    {
        CasinoSlot.SpinCost.Should().Be(CasinoSlotService.SpinCost).And.Be(100);
        CasinoSlot.MaximumPayout.Should().Be(CasinoSlotService.MaximumPayout).And.Be(10000);
    }

    // ---- which placeable is the machine

    [TestCase("pw_ar_nscasino", "swd_smach02", "SWTOR_SLOT2", true)]
    [TestCase("pw_ar_narshahub", "swd_smach02", "SWTOR_SLOT2", false)]
    [TestCase("tat_anc_junix", "swd_smach02", "SWTOR_SLOT2", false)]
    [TestCase("pw_ar_nscasino", "swd_smach06", "SWTOR_SLOT2", false)]
    [TestCase("pw_ar_nscasino", "swd_smach02", "OTHER", false)]
    public void IsCasinoMachine_accepts_only_the_casino_machine(string area, string resref, string tag, bool expected)
    {
        _service.IsCasinoMachine(area, resref, tag).Should().Be(expected);
    }

    // ---- using the machine

    [Test]
    public void OnUsed_opens_the_window_for_a_player_beside_the_machine()
    {
        _service.OnUsed();

        _service.OpenedWindows.Should().ContainSingle();
        var opened = _service.OpenedWindows[0];
        opened.Player.Should().Be(PlayerObject);
        opened.Machine.Should().Be(MachineObject, "the window is tethered to the machine");
        opened.Payload.Should().BeOfType<CasinoSlotPayload>().Which.Machine.Should().Be(MachineObject);
    }

    [TestCase("invalid player", "GetIsObjectValid")]
    [TestCase("not a player", "GetIsPC")]
    public void OnUsed_ignores_an_object_that_is_not_a_player(string reason, string member)
    {
        _engine.Engine.SetReturnHandler(member, arguments => (uint)arguments[0]! != PlayerObject);

        _service.OnUsed();

        _service.OpenedWindows.Should().BeEmpty(reason);
    }

    [TestCase("GetIsDM")]
    [TestCase("GetIsDMPossessed")]
    [TestCase("GetIsDead")]
    public void OnUsed_ignores_a_dm_a_possessing_dm_and_a_dead_player(string member)
    {
        _engine.Engine.SetReturn(member, true);

        _service.OnUsed();

        _service.OpenedWindows.Should().BeEmpty();
    }

    [Test]
    public void OnUsed_ignores_a_player_in_another_area_than_the_machine()
    {
        _engine.Engine.SetReturnHandler("GetArea", arguments => (uint)arguments[0]! == PlayerObject ? 9999u : AreaObject);

        _service.OnUsed();

        _service.OpenedWindows.Should().BeEmpty();
    }

    [Test]
    public void OnUsed_ignores_a_machine_that_is_not_the_casino_machine()
    {
        _engine.Engine.SetReturn("GetTag", "OTHER");

        _service.OnUsed();

        _service.OpenedWindows.Should().BeEmpty();
    }

    [Test]
    public void OnUsed_ignores_a_player_more_than_four_meters_away()
    {
        _engine.Engine.SetReturn("GetDistanceBetween", 4.5f);

        _service.OnUsed();

        _service.OpenedWindows.Should().BeEmpty();
    }

    [Test]
    public void OnUsed_allows_a_player_exactly_four_meters_away()
    {
        _engine.Engine.SetReturn("GetDistanceBetween", 4.0f);

        _service.OnUsed();

        _service.OpenedWindows.Should().ContainSingle();
    }

    [Test]
    public void The_window_opener_default_goes_to_the_gui_service()
    {
        // The default body is the original Gui call. Gui's window table is empty here, so the call is proved by the
        // first thing Gui.TogglePlayerWindow does: it asks the engine whether the target is a player, and does
        // nothing for one that is not.
        _engine.Engine.SetReturn("GetIsPC", false);

        _service.OpenWithTheGuiService(PlayerObject, new CasinoSlotPayload(MachineObject), MachineObject);

        _engine.Engine.CallsTo("GetIsPC").Select(call => call.Arguments[0]).Should().Equal(PlayerObject);
    }

    // ---- the next spin

    [Test]
    public void GetNextSpinUtc_is_the_minimum_without_a_public_cd_key()
    {
        _engine.Engine.SetReturn("GetPCPublicCDKey", "  ");

        _service.GetNextSpinUtc(PlayerObject).Should().Be(DateTime.MinValue);
        _db.Reads.Should().BeEmpty("a player with no account is not looked up");
    }

    [Test]
    public void GetNextSpinUtc_is_the_minimum_for_an_account_that_never_spun()
    {
        _service.GetNextSpinUtc(PlayerObject).Should().Be(DateTime.MinValue);

        _db.Reads.Should().Equal(AccountId);
    }

    [Test]
    public void GetNextSpinUtc_is_the_time_the_account_may_spin_again()
    {
        var next = new DateTime(2026, 11, 3, 12, 0, 3, DateTimeKind.Utc);
        _db.With(new CasinoDailyPlay { Id = AccountId, NextSpinUtc = next });

        _service.GetNextSpinUtc(PlayerObject).Should().Be(next);
    }

    // ---- spinning

    [Test]
    public void Spin_refuses_a_player_who_is_not_beside_the_machine()
    {
        _engine.Engine.SetReturn("GetDistanceBetween", 10f);

        var result = _service.Spin(PlayerObject, MachineObject);

        result.Should().Be(new CasinoSpinResult(false, 0, "You must be beside the slot machine in the Smuggler's Moon Casino."));
        _db.Reads.Should().BeEmpty();
        _db.Saved.Should().BeEmpty();
        _engine.Engine.CountOf("AssignCommand").Should().Be(0, "no credits move");
    }

    [Test]
    public void Spin_takes_no_credits_when_the_account_cannot_be_loaded()
    {
        _engine.Engine.SetReturn("GetPCPublicCDKey", string.Empty);

        var result = _service.Spin(PlayerObject, MachineObject);

        result.Should().Be(new CasinoSpinResult(false, 0, "Your casino account could not be loaded. No credits were taken."));
        _db.Saved.Should().BeEmpty();
        _engine.Engine.CountOf("AssignCommand").Should().Be(0);
    }

    [Test]
    public void Spin_answers_nothing_while_the_cooldown_runs_and_draws_no_roll()
    {
        Random.SetSeed(7);
        _db.With(new CasinoDailyPlay { Id = AccountId, DayUtc = DateTime.UtcNow.Date, NextSpinUtc = DateTime.UtcNow.AddMinutes(5), Spins = 3 });

        var result = _service.Spin(PlayerObject, MachineObject);

        result.Should().Be(new CasinoSpinResult(false, 0, string.Empty));
        _db.Saved.Should().BeEmpty();
        _engine.Engine.CountOf("AssignCommand").Should().Be(0);
        Random.Next(10000).Should().Be(new System.Random(7).Next(10000), "a refused spin does not consume the roll");
    }

    [Test]
    public void Spin_needs_the_stake_in_the_players_purse()
    {
        Random.SetSeed(11);
        _engine.Engine.SetReturn("GetGold", CasinoSlotService.SpinCost - 1);

        var result = _service.Spin(PlayerObject, MachineObject);

        result.Should().Be(new CasinoSpinResult(false, 0, "You need 100 credits to spin."));
        _db.Saved.Should().BeEmpty();
        Random.Next(10000).Should().Be(new System.Random(11).Next(10000), "a refused spin does not consume the roll");
    }

    [Test]
    public void Spin_may_spend_the_last_credits_of_the_stake()
    {
        _engine.Engine.SetReturn("GetGold", CasinoSlotService.SpinCost);
        var service = ServiceWithPayout(0);

        service.Spin(PlayerObject, MachineObject).Succeeded.Should().BeTrue();
    }

    [TestCase(0, "0 credits")]
    [TestCase(100, "100 credits")]
    [TestCase(500, "500 credits")]
    [TestCase(1000, "1,000 credits")]
    [TestCase(10000, "10,000 credits")]
    public void Spin_pays_the_roll_and_names_the_payout(int payout, string message)
    {
        var service = ServiceWithPayout(payout);

        var result = service.Spin(PlayerObject, MachineObject);

        result.Should().Be(new CasinoSpinResult(true, payout, message));
    }

    [Test]
    public void Spin_makes_exactly_one_draw_of_ten_thousand()
    {
        const int Seed = 42;
        Random.SetSeed(Seed);
        var replay = new System.Random(Seed);
        var expected = _service.GetPayout(replay.Next(10000));

        var result = _service.Spin(PlayerObject, MachineObject);

        result.Payout.Should().Be(expected);
        Random.Next(10000).Should().Be(replay.Next(10000), "the spin consumed the first draw and no other");
    }

    [Test]
    public void Spin_settles_the_stake_and_the_payout_in_one_command_assigned_to_the_player()
    {
        var service = ServiceWithPayout(500);

        service.Spin(PlayerObject, MachineObject);

        var assigned = _engine.Engine.CallsTo("AssignCommand");
        assigned.Select(call => (uint)call.Arguments[0]!).Should().Equal(PlayerObject, PlayerObject);
        RunAssignedCommands();
        _engine.Engine.CallsTo("TakeGoldFromCreature").Select(call => (call.Arguments[0], call.Arguments[1], call.Arguments[2]))
            .Should().Equal(((object?)100, (object?)PlayerObject, (object?)true));
        _engine.Engine.CallsTo("GiveGoldToCreature").Select(call => (call.Arguments[0], call.Arguments[1]))
            .Should().Equal(((object?)PlayerObject, (object?)500));
        _engine.Engine.CallsTo("ExportSingleCharacter").Select(call => call.Arguments[0]).Should().Equal(PlayerObject);
        _engine.Engine.CallsTo("PlaySound").Select(call => call.Arguments[0]).Should().Equal("gui_prompt");
    }

    [Test]
    public void Spin_gives_nothing_back_on_a_losing_roll()
    {
        var service = ServiceWithPayout(0);

        service.Spin(PlayerObject, MachineObject);
        RunAssignedCommands();

        _engine.Engine.CallsTo("TakeGoldFromCreature").Should().ContainSingle();
        _engine.Engine.CallsTo("GiveGoldToCreature").Should().BeEmpty();
    }

    [Test]
    public void Spin_makes_the_machine_announce_a_jackpot_and_no_other_payout()
    {
        var service = ServiceWithPayout(CasinoSlotService.MaximumPayout);

        service.Spin(PlayerObject, MachineObject);
        RunAssignedCommands();

        _engine.Engine.CallsTo("AssignCommand").Select(call => (uint)call.Arguments[0]!)
            .Should().Equal(PlayerObject, PlayerObject, MachineObject);
        _engine.Engine.CallsTo("SpeakString").Select(call => call.Arguments[0])
            .Should().Equal("Jackpot! This machine has paid out 10,000 credits!");
    }

    [Test]
    public void Spin_does_not_announce_a_payout_below_the_jackpot()
    {
        var service = ServiceWithPayout(1000);

        service.Spin(PlayerObject, MachineObject);
        RunAssignedCommands();

        _engine.Engine.CallsTo("AssignCommand").Should().HaveCount(2);
        _engine.Engine.CallsTo("SpeakString").Should().BeEmpty();
    }

    [Test]
    public void Spin_records_the_spin_and_starts_the_cooldown()
    {
        var service = ServiceWithPayout(500);
        var before = DateTime.UtcNow;
        var dayBefore = before.AddHours(-10).Date;

        service.Spin(PlayerObject, MachineObject);

        var play = _db.Saved.Should().ContainSingle().Which.Should().BeOfType<CasinoDailyPlay>().Subject;
        play.Id.Should().Be(AccountId);
        play.Spins.Should().Be(1);
        play.ProfitableWins.Should().Be(1, "500 credits is more than the stake");
        play.NextSpinUtc.Should().BeOnOrAfter(before.AddSeconds(3)).And.BeOnOrBefore(DateTime.UtcNow.AddSeconds(3));
        play.DayUtc.Should().BeOneOf(dayBefore, DateTime.UtcNow.AddHours(-10).Date);
    }

    [Test]
    public void Spin_counts_a_break_even_roll_as_a_spin_but_not_a_profitable_win()
    {
        var service = ServiceWithPayout(100);

        service.Spin(PlayerObject, MachineObject);

        var play = (CasinoDailyPlay)_db.Saved.Single();
        play.Spins.Should().Be(1);
        play.ProfitableWins.Should().Be(0);
    }

    [Test]
    public void Spin_continues_the_statistics_of_a_stored_account()
    {
        _db.With(new CasinoDailyPlay
        {
            Id = AccountId,
            DayUtc = DateTime.UtcNow.AddHours(-10).Date,
            Spins = 8,
            ProfitableWins = 2,
            NextSpinUtc = DateTime.UtcNow.AddSeconds(-1)
        });
        var service = ServiceWithPayout(1000);

        service.Spin(PlayerObject, MachineObject);

        var play = (CasinoDailyPlay)_db.Saved.Single();
        play.Spins.Should().Be(9);
        play.ProfitableWins.Should().Be(3);
    }

    [Test]
    public void Spin_starts_a_new_day_with_fresh_statistics()
    {
        _db.With(new CasinoDailyPlay
        {
            Id = AccountId,
            DayUtc = DateTime.UtcNow.AddHours(-10).Date.AddDays(-1),
            Spins = 8,
            ProfitableWins = 2,
            NextSpinUtc = DateTime.UtcNow.AddDays(-1)
        });
        var service = ServiceWithPayout(0);

        service.Spin(PlayerObject, MachineObject);

        var play = (CasinoDailyPlay)_db.Saved.Single();
        play.Spins.Should().Be(1);
        play.ProfitableWins.Should().Be(0);
    }

    [Test]
    public void Spin_saves_the_cooldown_before_it_settles_any_credits()
    {
        var engine = _engine.Engine;
        var order = new List<string>();
        var recordingDb = new SaveOrderDb(_db, () => order.Add($"saved after {engine.CountOf("AssignCommand")} commands"));
        var service = new TestCasinoSlotService(recordingDb);
        Random.SetSeed(SeedWhere(roll => service.GetPayout(roll) == 500));

        service.Spin(PlayerObject, MachineObject);

        order.Should().Equal("saved after 0 commands");
        engine.CountOf("AssignCommand").Should().BeGreaterThan(0);
    }

    [Test]
    public void Spin_that_cannot_save_the_cooldown_settles_no_credits()
    {
        var service = new TestCasinoSlotService(new ThrowingSaveDb(_db));
        Random.SetSeed(SeedWhere(roll => service.GetPayout(roll) == 500));

        var spin = () => service.Spin(PlayerObject, MachineObject);

        spin.Should().Throw<InvalidOperationException>();
        _engine.Engine.CountOf("AssignCommand").Should().Be(0, "a failed save cannot allow free retries");
    }

    // ---- fakes

    /// <summary>Answers the window call and records it instead of calling the GUI service.</summary>
    private sealed class TestCasinoSlotService : CasinoSlotService
    {
        public TestCasinoSlotService(IDBService db)
            : base(db)
        {
        }

        public List<(uint Player, GuiPayloadBase Payload, uint Machine)> OpenedWindows { get; } = new();

        public void OpenWithTheGuiService(uint player, GuiPayloadBase payload, uint machine)
        {
            base.TogglePlayerWindow(player, payload, machine);
        }

        protected override void TogglePlayerWindow(uint player, GuiPayloadBase payload, uint machine)
        {
            OpenedWindows.Add((player, payload, machine));
        }
    }

    private class SaveOrderDb : IDBService
    {
        private readonly IDBService _inner;
        private readonly Action _onSet;

        public SaveOrderDb(IDBService inner, Action onSet)
        {
            _inner = inner;
            _onSet = onSet;
        }

        public T Get<T>(string id) where T : EntityBase => _inner.Get<T>(id);

        public virtual void Set<T>(T entity) where T : EntityBase
        {
            _onSet();
            _inner.Set(entity);
        }

        public void Delete<T>(string id) where T : EntityBase => _inner.Delete<T>(id);

        public bool Exists<T>(string id) where T : EntityBase => _inner.Exists<T>(id);

        public IEnumerable<T> Search<T>(DBQuery<T> query) where T : EntityBase => _inner.Search(query);

        public long SearchCount<T>(DBQuery<T> query) where T : EntityBase => _inner.SearchCount(query);
    }

    private sealed class ThrowingSaveDb : SaveOrderDb
    {
        public ThrowingSaveDb(IDBService inner)
            : base(inner, () => { })
        {
        }

        public override void Set<T>(T entity) => throw new InvalidOperationException("The save failed.");
    }
}
