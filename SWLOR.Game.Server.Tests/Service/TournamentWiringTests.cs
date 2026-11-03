using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.TournamentService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The glue that the pure rules tests cannot reach: how the ring, the duel flow, the credit payments, property deletion and
/// login reach the tournament service, and that the new records are plain new types.
/// </summary>
public class TournamentWiringTests
{
    // Records

    [Test]
    public void TheNewRecords_AreEntitiesWithIndexedLookups()
    {
        typeof(CityTournament).BaseType.Should().Be(typeof(EntityBase));
        typeof(PendingCredit).BaseType.Should().Be(typeof(EntityBase));
        typeof(CreditPaymentReceipt).BaseType.Should().Be(typeof(EntityBase));

        IsIndexed(typeof(CityTournament), nameof(CityTournament.CityId)).Should().BeTrue();
        IsIndexed(typeof(CityTournament), nameof(CityTournament.RingPropertyId)).Should().BeTrue();
        IsIndexed(typeof(CityTournament), nameof(CityTournament.State)).Should().BeTrue();
        IsIndexed(typeof(PendingCredit), nameof(PendingCredit.PlayerId)).Should().BeTrue();
        IsIndexed(typeof(CreditPaymentReceipt), nameof(CreditPaymentReceipt.PlayerId)).Should().BeTrue();
    }

    [Test]
    public void ANewTournament_StartsEmptyAndInSignUp()
    {
        var tournament = new CityTournament();

        tournament.Entrants.Should().BeEmpty();
        tournament.Matches.Should().BeEmpty();
        tournament.BarredPlayerIds.Should().BeEmpty();
        tournament.CityId.Should().BeEmpty();
        tournament.OfficialPlayerId.Should().BeEmpty();
        tournament.ChampionPlayerId.Should().BeEmpty();
        tournament.Outcome.Should().Be(TournamentOutcome.None);
    }

    [Test]
    public void TheEnumValues_AreSavedWithTheRecordsAndNeverChange()
    {
        ((int)TournamentState.SignUp).Should().Be(1);
        ((int)TournamentState.Running).Should().Be(2);
        ((int)TournamentState.Settling).Should().Be(3);
        ((int)TournamentOutcome.Completed).Should().Be(1);
        ((int)TournamentOutcome.Cancelled).Should().Be(2);
        ((int)TournamentOutcome.CityRemoved).Should().Be(3);
        ((int)TournamentMatchState.Pending).Should().Be(1);
        ((int)TournamentMatchState.Called).Should().Be(2);
        ((int)TournamentMatchState.Active).Should().Be(3);
        ((int)TournamentMatchState.AwaitingOfficial).Should().Be(4);
        ((int)TournamentMatchState.Done).Should().Be(5);
    }

    // Signing up pays through a journaled credit payment

    [Test]
    public void TheEntryFee_IsAJournaledCreditPaymentThatRoundTrips()
    {
        ((int)CreditPaymentKind.TournamentEntry).Should().Be(6);

        var mark = new CreditPaymentMark { Id = "pay1", Kind = CreditPaymentKind.TournamentEntry, RecordId = "t1", Credits = 1_000 };

        CreditPaymentMark.TryParse(mark.Serialize(), out var parsed).Should().BeTrue();
        parsed.Kind.Should().Be(CreditPaymentKind.TournamentEntry);
        parsed.RecordId.Should().Be("t1");
        parsed.Credits.Should().Be(1_000);
    }

    [Test]
    public void ARestartMidSignUp_KeepsTheFeeOnlyWhenTheEntrantWasSaved()
    {
        var tournament = new CityTournament { Id = "t1" };
        tournament.Entrants.Add(new TournamentEntrant { PlayerId = "p1", PaymentId = "pay1" });
        var saved = new CreditPaymentMark { Id = "pay1", Kind = CreditPaymentKind.TournamentEntry, RecordId = "t1", Credits = 1_000 };
        var unsaved = new CreditPaymentMark { Id = "pay2", Kind = CreditPaymentKind.TournamentEntry, RecordId = "t1", Credits = 1_000 };

        saved.KeepsPayment(TournamentRules.FindEntryPaymentId(tournament, saved.Id)).Should().BeTrue();
        unsaved.KeepsPayment(TournamentRules.FindEntryPaymentId(tournament, unsaved.Id)).Should().BeFalse("the credits go back to the player");
        unsaved.KeepsPayment(TournamentRules.FindEntryPaymentId(null, unsaved.Id)).Should().BeFalse();
    }

    [Test]
    public void TheCreditPayments_ReadTheTournamentsEntrantsToSettleAMark()
    {
        ReadServerSource("Service", "CreditPayments.cs")
            .Should().Contain("CreditPaymentKind.TournamentEntry => TournamentRules.FindEntryPaymentId(DB.Get<CityTournament>(mark.RecordId), mark.Id)");
    }

    [Test]
    public void TheSettlement_WritesReceiptsForThePaidEntrants_BeforeItDeletesTheTournament()
    {
        var settle = ReadServerSource("Service", "Tournaments.Settlement.cs");
        var payouts = settle.IndexOf("PendingCredits.Add(payout.Id, payout.PlayerId, payout.Credits, payout.Reason);", StringComparison.Ordinal);
        var receipts = settle.IndexOf("CreditPayments.SaveReceipt(paymentId, playerId);", payouts, StringComparison.Ordinal);
        var city = settle.IndexOf("DB.Set(city);", receipts, StringComparison.Ordinal);
        var delete = settle.IndexOf("DB.Delete<CityTournament>(tournament.Id);", city, StringComparison.Ordinal);

        payouts.Should().BeGreaterThan(0);
        receipts.Should().BeGreaterThan(payouts);
        city.Should().BeGreaterThan(receipts);
        delete.Should().BeGreaterThan(city, "a restart before the delete repeats the settlement and its receipts under the same ids");
        settle.Should().Contain("TournamentSettlementRules.GetEntryReceipts(tournament)");
    }

    [Test]
    public void TheLogin_SettlesAMarkWithTheReceiptsOfAnEndedTournament_AndDeletesThemAfterTheMarkIsRetired()
    {
        var payments = ReadServerSource("Service", "CreditPayments.cs");
        var settle = payments.Substring(payments.IndexOf("public static void SettleInterruptedPayment()", StringComparison.Ordinal));
        settle = settle.Substring(0, settle.IndexOf("private static string GetRecordPaymentId(", StringComparison.Ordinal));

        var load = settle.IndexOf("var receipts = LoadReceipts(GetObjectUUID(player));", StringComparison.Ordinal);
        var keeps = settle.IndexOf("mark.KeepsPayment(GetRecordPaymentId(player, mark), receipts.Any(x => x.Id == mark.Id))", load, StringComparison.Ordinal);
        var refund = settle.IndexOf("GiveGoldToCreature(player, mark.Credits);", keeps, StringComparison.Ordinal);
        var retire = settle.IndexOf("Retire(player);", refund, StringComparison.Ordinal);
        var delete = settle.IndexOf("DB.Delete<CreditPaymentReceipt>(receipt.Id);", retire, StringComparison.Ordinal);

        load.Should().BeGreaterThan(0);
        keeps.Should().BeGreaterThan(load);
        refund.Should().BeGreaterThan(keeps);
        retire.Should().BeGreaterThan(refund);
        delete.Should().BeGreaterThan(retire, "a restart before the receipts go leaves them for a login that has no mark");
    }

    [Test]
    public void ASignUp_TakesTheFeeOnThePlayersQueue_AndSavesTheEntrantBeforeTheEscrow()
    {
        var source = ReadServerSource("Service", "Tournaments.cs");

        source.Should().Contain("AssignCommand(player, () => CompleteSignUp(player, tournamentId));");
        var take = source.IndexOf("CreditPayments.Take(player, mark);", StringComparison.Ordinal);
        var entrant = source.IndexOf("DB.Set(tournament);", take, StringComparison.Ordinal);
        var escrow = source.IndexOf("CityTreasury.AddEntryFee(", entrant, StringComparison.Ordinal);
        var retire = source.IndexOf("CreditPayments.Retire(player);", escrow, StringComparison.Ordinal);

        take.Should().BeGreaterThan(0);
        entrant.Should().BeGreaterThan(take);
        escrow.Should().BeGreaterThan(entrant);
        retire.Should().BeGreaterThan(escrow);
    }

    [Test]
    public void ThePrize_IsTakenFromTheOrdinaryTreasuryOnly()
    {
        var source = ReadServerSource("Service", "Tournaments.cs");

        source.Should().Contain("CityTreasury.PostEscrow(city, tournament.Id, prize, restrictedFirst: false)");
        source.Should().Contain("CityTreasuryRules.GetSpendable(city, restrictedFirst: false)");
    }

    [Test]
    public void ThePrizeRecord_IsSavedBeforeItsMoneyIsHeld_SoARestartCanFindAnUnfundedPost()
    {
        var source = ReadServerSource("Service", "Tournaments.cs");
        var save = source.IndexOf("DB.Set(tournament);", source.IndexOf("public static string Post(", StringComparison.Ordinal), StringComparison.Ordinal);
        var hold = source.IndexOf("CityTreasury.PostEscrow(", save, StringComparison.Ordinal);

        save.Should().BeGreaterThan(0);
        hold.Should().BeGreaterThan(save);
        ReadServerSource("Service", "Tournaments.Matches.cs")
            .Should().Contain("if (tournament.Entrants.Count == 0 && !city.Escrow.ContainsKey(tournament.Id))");
    }

    // The duel flow

    [Test]
    public void TheDuelFlow_ReportsInterruptionsAndResultsToTheTournament()
    {
        var duels = ReadServerSource("Service", "Duels.cs");

        duels.Should().Contain("Tournaments.OnDuelInterrupted(record, source);");
        duels.Should().Contain("Tournaments.OnDuelInterrupted(record, interrupter);");
        duels.Should().Contain("Tournaments.OnDuelFinished(record, outcome.Value);");
        duels.Should().Contain("public static DuelRecord StartTournamentDuel(");
    }

    [Test]
    public void ATournamentDuel_PostsNoGazetteLineOfItsOwn()
    {
        var duels = ReadServerSource("Service", "Duels.cs");
        var finish = duels.IndexOf("private static void Finish(DuelRecord record)", StringComparison.Ordinal);
        var handOver = duels.IndexOf("Tournaments.OnDuelFinished(record, outcome.Value);", finish, StringComparison.Ordinal);
        var post = duels.IndexOf("CityGazette.Post(", finish, StringComparison.Ordinal);

        handOver.Should().BeGreaterThan(finish);
        post.Should().BeGreaterThan(handOver, "the hand-over returns before the ordinary duel line is written");
    }

    [Test]
    public void TheRing_RefusesABarredPlayerAndOrdinaryChallengesDuringATournament()
    {
        var duels = ReadServerSource("Service", "Duels.cs");

        duels.Should().Contain("Tournaments.IsBarred(OBJECT_SELF, GetObjectUUID(player))");
        duels.Should().Contain("Tournaments.IsRunningAt(ring)");
        duels.Should().Contain("Tournaments.IsBarred(ring, GetObjectUUID(challenger))");
    }

    // Removal

    [Test]
    public void ATakenDownRing_AndARemovedCity_SettleTheirTournaments()
    {
        var property = ReadServerSource("Service", "Property.cs");

        property.Should().Contain("property.StructureType == StructureType.DuelingRing");
        property.Should().Contain("Tournaments.OnRingDeleted(property);");
        property.Should().Contain("Tournaments.OnCityDeleted(property);");
    }

    // The ring's menu

    [Test]
    public void TheRingMenu_HasTheTournamentPages()
    {
        var menu = new DuelingRingDialog().Build();

        menu.DefaultPageId.Should().Be("MAIN_PAGE");
        menu.Pages.Keys.Should().Contain(new[] { "MAIN_PAGE", "TOURNAMENT_PAGE", "TOURNAMENT_POST_PAGE" });
        menu.DataModel.Should().NotBeNull("the post wizard keeps its picks in the menu's data model");
    }

    // Plain ASCII, as every player-facing string must be

    [TestCase("Service", "Tournaments.cs")]
    [TestCase("Service", "Tournaments.Matches.cs")]
    [TestCase("Service", "Tournaments.Settlement.cs")]
    [TestCase("Service", "PendingCredits.cs")]
    [TestCase("Service", "TournamentService", "TournamentText.cs")]
    [TestCase("Service", "TournamentService", "TournamentRules.cs")]
    [TestCase("Service", "TournamentService", "TournamentSettlementRules.cs")]
    [TestCase("Service", "CreditPayments.cs")]
    [TestCase("Entity", "CreditPaymentReceipt.cs")]
    [TestCase("Feature", "TournamentNotices.cs")]
    [TestCase("Feature", "DialogDefinition", "DuelingRingDialog.Tournaments.cs")]
    public void TheSource_IsPlainAscii(params string[] path)
    {
        ReadServerSource(path).Where(c => c > 0x7E).Should().BeEmpty("player-facing punctuation is ASCII");
    }

    private static bool IsIndexed(Type type, string property)
    {
        return type.GetProperty(property)!.GetCustomAttribute<IndexedAttribute>() != null;
    }

    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        }
    }

    private static string ReadServerSource(params string[] path)
    {
        var full = Path.Combine(new[] { Root, "SWLOR.Game.Server" }.Concat(path).ToArray());
        return File.ReadAllText(full).Replace("\r\n", "\n");
    }
}
