using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.PvpCombatService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The engine glue the pure rules tests cannot reach: where a fall is judged for bounties, where PvP damage holds the
/// combat window open, where the flag and identity changes are deferred, and that the records start with nothing accepted.
/// </summary>
public class BountyWiringTests
{
    // Claims

    [Test]
    public void ABountyIsJudged_AfterTheDuelCheck_AndBeforeTheDeathPanelOrTheSubdual()
    {
        var death = ReadServerSource("Service", "Death.cs");
        var space = death.IndexOf("Space.ApplyDeath(player)", StringComparison.Ordinal);
        var duel = death.IndexOf("var isDuelFall = Duels.TakeFall(player, hostile);", StringComparison.Ordinal);
        var claim = death.IndexOf("CityBounties.ClaimOnFall(player, hostile, isDuelFall);", StringComparison.Ordinal);
        var subdual = death.IndexOf("ApplySubdual(player, PvpCombat.GetControllingPlayer(hostile));", StringComparison.Ordinal);
        var stockSubdual = death.IndexOf("IsSubdualModeEnabled", StringComparison.Ordinal);
        var panel = death.IndexOf("PopUpDeathGUIPanel", StringComparison.Ordinal);

        space.Should().BeGreaterThan(0);
        duel.Should().BeGreaterThan(space, "a ship death returns before bounties are judged");
        claim.Should().BeGreaterThan(duel, "the duel check decides whether a fall can pay");
        claim.Should().BeLessThan(subdual);
        claim.Should().BeLessThan(stockSubdual, "a defeat and a subdual both collect");
        claim.Should().BeLessThan(panel);
        death.Split("CityBounties.ClaimOnFall(").Length.Should().Be(2, "the death handler is the one place a fall is judged");
    }

    [Test]
    public void TheClaim_RunsOnTheCreditedPlayer_NotTheLastHit_AndNeverThrowsIntoTheDeath()
    {
        var source = ReadServerSource("Service", "CityBounties.cs");
        var method = source.IndexOf("public static void ClaimOnFall(", StringComparison.Ordinal);
        var end = source.IndexOf("private static bool SharesParty(", method, StringComparison.Ordinal);
        var claim = source.Substring(method, end - method);

        claim.Should().Contain("BountyRules.TryResolveClaimant(hostile, PvpCombat.IsPlayer, GetMaster, out var claimant)");
        claim.Should().Contain("catch (Exception ex)");
        claim.Should().Contain("GetPCPublicCDKey(target)");
        claim.Should().Contain("GetPCPublicCDKey(claimant)");
        claim.Should().Contain("InDuel = inDuel");
        claim.Should().Contain("WindowOpen = PvpCombatWindow.IsOpen(dbTarget.Id)");
        claim.IndexOf("TryResolveClaimant", StringComparison.Ordinal)
            .Should().BeLessThan(claim.IndexOf("DB.Get<Player>", StringComparison.Ordinal), "a fall no player caused reads nothing");
    }

    [Test]
    public void AClaim_IsJournaledOnTheBounty_BeforeAnythingIsPaid()
    {
        var source = ReadServerSource("Service", "CityBounties.cs");
        var claim = source.IndexOf("private static void Claim(", StringComparison.Ordinal);
        var paying = source.IndexOf("bounty.State = BountyState.Paying;", claim, StringComparison.Ordinal);
        var save = source.IndexOf("DB.Set(bounty);", paying, StringComparison.Ordinal);
        var finish = source.IndexOf("FinishPayment(bounty, announce: true);", save, StringComparison.Ordinal);

        paying.Should().BeGreaterThan(claim);
        save.Should().BeGreaterThan(paying);
        finish.Should().BeGreaterThan(save, "a restart after the save finds the claim and finishes it");
    }

    [Test]
    public void ThePayment_OwesTheCredits_BeforeTheEscrowIsReleased_AndDeletesTheBountyLast()
    {
        var source = ReadServerSource("Service", "CityBounties.cs");
        var finish = source.IndexOf("private static void FinishPayment(", StringComparison.Ordinal);
        var mark = source.IndexOf("MarkClaimed(bounty);", finish, StringComparison.Ordinal);
        var owe = source.IndexOf("PendingCredits.Add(", finish, StringComparison.Ordinal);
        var release = source.IndexOf("CityTreasury.PayOutEscrow(city, bounty.Id);", finish, StringComparison.Ordinal);
        var delete = source.IndexOf("DB.Delete<CityBounty>(bounty.Id);", finish, StringComparison.Ordinal);
        var deliver = source.IndexOf("PendingCredits.DeliverIfOnline(", finish, StringComparison.Ordinal);

        mark.Should().BeGreaterThan(finish);
        owe.Should().BeGreaterThan(mark);
        release.Should().BeGreaterThan(owe);
        delete.Should().BeGreaterThan(release);
        deliver.Should().BeGreaterThan(delete, "credits reach the player only after the record that could repeat the payment is gone");
        source.Should().Contain("PaymentIdPrefix + bounty.Id", "the debt's id never changes, so repeating the step owes once");
    }

    [Test]
    public void ARestart_FinishesDecidedClaims_AndDropsUnfundedBounties()
    {
        var source = ReadServerSource("Service", "CityBounties.cs");

        source.Should().Contain("[NWNEventHandler(ScriptName.OnModuleLoad)]");
        source.Should().Contain("if (bounty.State == BountyState.Paying)");
        source.Should().Contain("!city.Escrow.ContainsKey(bounty.Id)");
        source.Should().Contain("Scheduler.ScheduleRepeating(ExpireDue, TimeSpan.FromMinutes(1))");
    }

    [Test]
    public void ARemovedCity_TakesItsBountiesWithIt()
    {
        ReadServerSource("Service", "Property.cs").Should().Contain("CityBounties.OnCityDeleted(property);");
    }

    // The combat window

    [Test]
    public void AnyPvpHit_HoldsBothPlayersWindowsOpen()
    {
        var source = ReadServerSource("Service", "PvpCombat.cs");
        var method = source.IndexOf("public static void RecordHit(", StringComparison.Ordinal);
        var end = source.IndexOf("public static int ApplyDamageIncrease(", method, StringComparison.Ordinal);
        var recordHit = source.Substring(method, end - method);

        recordHit.Should().Contain("PvpCombatWindow.Open(attackingPlayer);");
        recordHit.Should().Contain("PvpCombatWindow.Open(targetPlayer);");
    }

    [Test]
    public void AFlagTurnedOff_IsHeldBackBeforeItChanges_AndATurnedOnPvPFlagShowsTheNotice()
    {
        var source = ReadServerSource("Service", "PlayerList.cs");
        var method = source.IndexOf("public static void SetFlag(", StringComparison.Ordinal);
        var hold = source.IndexOf("PvpCombatWindow.HoldFlagChange(player, dbPlayer, isCurrentlyOn, isEnabled)", method, StringComparison.Ordinal);
        var change = source.IndexOf("flags.RemoveAll(existing => existing == flag);", method, StringComparison.Ordinal);
        var notice = source.IndexOf("BountyConsent.PromptIfNeeded(player);", method, StringComparison.Ordinal);

        hold.Should().BeGreaterThan(method);
        hold.Should().BeLessThan(change);
        notice.Should().BeGreaterThan(change);
    }

    [Test]
    public void AnIdentityChange_IsHeldBackBeforeItChanges()
    {
        var source = ReadServerSource("Service", "Disguise.cs");

        var activate = source.IndexOf("public static ActivateDisguiseResult Activate(", StringComparison.Ordinal);
        var delay = source.IndexOf("ValidateActivationDelay(player, playerId)", activate, StringComparison.Ordinal);
        var holdActivate = source.IndexOf("PvpCombatWindow.RequestIdentity(player, dbPlayer, disguise.Id, out var queuedMessage)", activate, StringComparison.Ordinal);
        var activeId = source.IndexOf("dbPlayer.ActiveDisguiseId = disguise.Id;", activate, StringComparison.Ordinal);
        var activateNotice = source.IndexOf("BountyConsent.PromptIfNeeded(player);", activate, StringComparison.Ordinal);
        delay.Should().BeGreaterThan(activate);
        holdActivate.Should().BeGreaterThan(delay, "an activation that is refused anyway says so first");
        holdActivate.Should().BeLessThan(activeId);
        activateNotice.Should().BeGreaterThan(activeId);

        var deactivate = source.IndexOf("public static bool Deactivate(", StringComparison.Ordinal);
        var holdDeactivate = source.IndexOf("PvpCombatWindow.RequestIdentity(player, dbPlayer, string.Empty, out var queuedMessage)", deactivate, StringComparison.Ordinal);
        var clearId = source.IndexOf("dbPlayer.ActiveDisguiseId = string.Empty;", deactivate, StringComparison.Ordinal);
        holdDeactivate.Should().BeGreaterThan(deactivate);
        holdDeactivate.Should().BeLessThan(clearId);

        var retire = source.IndexOf("public static bool Retire(", StringComparison.Ordinal);
        var guard = source.IndexOf("if (PvpCombatWindow.IsOpen(player))", retire, StringComparison.Ordinal);
        var deactivateCall = source.IndexOf("Deactivate(player);", retire, StringComparison.Ordinal);
        guard.Should().BeGreaterThan(retire);
        guard.Should().BeLessThan(deactivateCall, "retiring the disguise shown would change identity mid-fight");
    }

    [Test]
    public void AQueuedActivation_IsReportedToTheDisguiseWindowAsNotDoneYet()
    {
        var source = ReadServerSource("Service", "DisguiseService", "ActivateDisguiseResult.cs");

        source.Should().Contain("public static ActivateDisguiseResult Queued(string message)");
        source.Should().Contain("IsQueued = true");
    }

    [Test]
    public void QueuedRequests_AreAppliedWhenTheWindowEnds_AndAtLogin()
    {
        var source = ReadServerSource("Service", "PvpCombatWindow.cs");

        source.Should().Contain("Scheduler.ScheduleRepeating(Tick, TimeSpan.FromSeconds(1))");
        source.Should().Contain("[NWNEventHandler(ScriptName.OnModuleEnter)]");

        var apply = source.IndexOf("public static void ApplyQueued(", StringComparison.Ordinal);
        var flagOff = source.IndexOf("PlayerList.SetFlag(player, PlayerListFlagType.PvP, false);", apply, StringComparison.Ordinal);
        var identity = source.IndexOf("Disguise.Deactivate(player)", apply, StringComparison.Ordinal);
        var clear = source.IndexOf("requests.Clear();", apply, StringComparison.Ordinal);
        clear.Should().BeLessThan(flagOff, "the queue is cleared and saved first so the changes find nothing waiting");
        flagOff.Should().BeLessThan(identity, "the flag goes off while the identity that asked is still shown");
    }

    [Test]
    public void ThePlayerListShowsTheRequestedState()
    {
        var source = ReadServerSource("Feature", "GuiDefinition", "ViewModel", "PlayerListViewModel.cs");

        source.Should().Contain("PlayerList.IsFlagOffQueued(Player, PlayerListFlagType.PvP)");
        source.Should().Contain("PlayerList.IsFlagOffQueued(Player, flag)");
    }

    // The notice

    [Test]
    public void TheNotice_ShowsAtLogin_AndTheMenuIsRegistered()
    {
        var consent = ReadServerSource("Service", "BountyConsent.cs");
        var notices = ReadServerSource("Service", "CityBounties.cs");

        consent.Should().Contain("[NWNEventHandler(ScriptName.OnModuleEnter)]");
        consent.Should().Contain("ConversationMenu.TryStart(player, player, nameof(BountyNoticeDialog))");
        notices.Should().Contain("public static void NotifyOnLogin()");
        typeof(BountyNoticeDialog).BaseType.Should().Be(typeof(ConversationMenuDefinition));
    }

    [Test]
    public void NobodyHasAccepted_AndNoOneIsWaitingOnARequest_ForRecordsMadeBeforeLaunch()
    {
        var player = new Player();
        var disguise = new PlayerDisguise();

        player.BountyNoticeAcceptedUtc.Should().BeNull();
        player.LastBountyClaimedUtc.Should().BeNull();
        disguise.BountyNoticeAcceptedUtc.Should().BeNull();
        disguise.LastBountyClaimedUtc.Should().BeNull();
        player.PvpCombatRequests.HasAny.Should().BeFalse();
    }

    [Test]
    public void APlayerSavedBeforeLaunch_LoadsWithNoAcceptanceAndAnEmptyQueue()
    {
        const string oldRecord = "{\"Id\":\"p1\",\"Name\":\"Old\"}";

        var player = Newtonsoft.Json.JsonConvert.DeserializeObject<Player>(oldRecord);

        player.BountyNoticeAcceptedUtc.Should().BeNull("the notice is unset for everyone at launch, so no migration step is needed");
        player.PvpCombatRequests.Should().NotBeNull();
        player.PvpCombatRequests.HasAny.Should().BeFalse();
    }

    // The window

    [Test]
    public void TheBountiesTab_IsOnTheCityAffairsWindow_AndTheBountyRecordIsANewType()
    {
        var definition = ReadServerSource("Feature", "GuiDefinition", "CityAffairsDefinition.cs");
        var viewModel = ReadServerSource("Feature", "GuiDefinition", "ViewModel", "CityAffairsViewModel.cs");

        definition.Should().Contain(".AddOption(\"Bounties\")");
        definition.Should().Contain("DefinePartialView(CityAffairsViewModel.BountiesTabPartial, AddBountiesTab)");
        viewModel.Should().Contain(".AddTab(BountiesTabId, BountiesTabPartial, model => model.RefreshBounties())");
        viewModel.Should().Contain("new(SituationsTabId, OfficersTabId, StoryTabId, BountiesTabId)");
        viewModel.Should().Contain("InitializeBounties();");

        typeof(CityBounty).BaseType.Should().Be(typeof(EntityBase));
        new CityBounty().State.Should().Be(SWLOR.Game.Server.Service.CityBountyService.BountyState.Invalid);
    }

    [Test]
    public void ThePostingTab_RequiresThePostRequestsPower()
    {
        var source = ReadServerSource("Service", "CityBounties.cs");

        source.Should().Contain("CityPowers.Can(city, playerId, CityPower.PostRequests)");
        source.Should().Contain("restrictedFirst: false");
        source.Should().NotContain("restrictedFirst: true");
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
