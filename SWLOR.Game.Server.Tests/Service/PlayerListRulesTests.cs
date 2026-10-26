using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.PlayerListService;

namespace SWLOR.Game.Server.Tests.Service;

public class PlayerListRulesTests
{
    [Test]
    public void Search_EmptyMatchesEveryone()
    {
        PlayerListRules.MatchesSearch(null, "Kira Venn", "A friendly Twi'lek").Should().BeTrue();
        PlayerListRules.MatchesSearch("   ", string.Empty, "A hooded figure").Should().BeTrue();
    }

    [Test]
    public void Search_MatchesTheStartOfWordsInAnyField()
    {
        PlayerListRules.MatchesSearch("ki", "Kira Venn", "A friendly Twi'lek").Should().BeTrue();
        PlayerListRules.MatchesSearch("FRIEND", "Kira Venn", "A friendly Twi'lek").Should().BeTrue();
        PlayerListRules.MatchesSearch("ira", "Kira Venn", "A friendly Twi'lek").Should().BeFalse();
    }

    [Test]
    public void Search_RequiresEveryWordAcrossTheFields()
    {
        PlayerListRules.MatchesSearch("venn twi", "Kira Venn", "A friendly Twi'lek").Should().BeTrue();
        PlayerListRules.MatchesSearch("venn rodian", "Kira Venn", "A friendly Twi'lek").Should().BeFalse();
    }

    [Test]
    public void Search_KeepsApostrophesAndHyphensInsideWords()
    {
        PlayerListRules.MatchesSearch("twi'l", string.Empty, "A friendly Twi'lek").Should().BeTrue();
        PlayerListRules.MatchesSearch("kal-r", "Kal-Ria Halkov", string.Empty).Should().BeTrue();
        PlayerListRules.MatchesSearch("lek", string.Empty, "A friendly Twi'lek").Should().BeFalse();
    }

    [Test]
    public void Search_IgnoresMissingFields()
    {
        PlayerListRules.MatchesSearch("rodian", null, "A small Rodian").Should().BeTrue();
        PlayerListRules.MatchesSearch("rodian", null, null).Should().BeFalse();
    }

    [Test]
    public void DMs_AreVisibleOnlyToOtherDMs()
    {
        PlayerListRules.IsVisibleTo(viewerIsDM: false, subjectIsDM: true).Should().BeFalse();
        PlayerListRules.IsVisibleTo(viewerIsDM: true, subjectIsDM: true).Should().BeTrue();
        PlayerListRules.IsVisibleTo(viewerIsDM: false, subjectIsDM: false).Should().BeTrue();
        PlayerListRules.IsVisibleTo(viewerIsDM: true, subjectIsDM: false).Should().BeTrue();
    }

    [Test]
    public void Descriptors_FollowThePlayersSettingsExceptForStaffAndUnnamedPlayers()
    {
        PlayerListRules.ShowsDescriptor(viewerIsDM: false, isSelf: true, isNamed: true, showOwnDescriptor: false, showNamedDescriptors: true)
            .Should().BeFalse("players can hide their own descriptor");
        PlayerListRules.ShowsDescriptor(false, true, true, true, false).Should().BeTrue();
        PlayerListRules.ShowsDescriptor(false, false, true, true, false)
            .Should().BeFalse("players can hide descriptors beside names they assigned");
        PlayerListRules.ShowsDescriptor(false, false, false, true, false)
            .Should().BeTrue("an unnamed player is only identifiable by their descriptor");
        PlayerListRules.ShowsDescriptor(true, false, true, false, false).Should().BeTrue("staff always see descriptors");
    }

    [Test]
    public void RowNames_MarkSelfUnassignedAndAfk()
    {
        PlayerListRules.GetRowName("Taren Vos", isSelf: true, isAfk: false).Should().Be("Taren Vos (You)");
        PlayerListRules.GetRowName(string.Empty, isSelf: false, isAfk: false).Should().Be("Unassigned");
        PlayerListRules.GetRowName("Kira Venn", isSelf: false, isAfk: true).Should().Be("Kira Venn [AFK]");
        PlayerListRules.GetRowName("Kira Venn", false, false, maxLength: 17).Should().Be("Kira Venn");
    }

    [Test]
    public void RowNames_KeepTheAfkMarkerWhenShortened()
    {
        var shortened = PlayerListRules.GetRowName("Bartholomew Quintessence", isSelf: false, isAfk: true, maxLength: 17);

        shortened.Should().EndWith(" [AFK]");
        shortened.Length.Should().BeLessThanOrEqualTo(17);
        PlayerListRules.GetRowName("Bartholomew Quintessence", false, false, 17).Should().HaveLength(17).And.EndWith("...");
    }

    [Test]
    public void NoSelection_DisablesTargetedActionsButKeepsLeaveParty()
    {
        var actions = PlayerListRules.Evaluate(new PlayerListRelation { ViewerIsInParty = true });

        foreach (var state in new[] { actions.SendTell, actions.ToggleHostile, actions.Invite, actions.Remove, actions.MakeLeader })
        {
            state.IsEnabled.Should().BeFalse();
            state.DisabledReason.Should().Be(PlayerListRules.SelectPlayerFirst);
        }

        actions.LeaveParty.IsEnabled.Should().BeTrue();
        PlayerListRules.Evaluate(new PlayerListRelation()).LeaveParty.DisabledReason.Should().Be("You are not in a party.");
    }

    [Test]
    public void SelfSelected_OnlyAllowsLeavingTheParty()
    {
        var actions = PlayerListRules.Evaluate(new PlayerListRelation
        {
            HasTarget = true,
            TargetIsSelf = true,
            ViewerIsInParty = true,
            ViewerIsPartyLeader = true,
        });

        actions.SendTell.DisabledReason.Should().Be("You cannot send a tell to yourself.");
        actions.ToggleHostile.IsEnabled.Should().BeFalse();
        actions.Invite.IsEnabled.Should().BeFalse();
        actions.Remove.DisabledReason.Should().Be("Use Leave Party to leave your party.");
        actions.MakeLeader.DisabledReason.Should().Be("You already lead your party.");
        actions.LeaveParty.IsEnabled.Should().BeTrue();
    }

    [Test]
    public void LeaderSelectingMember_CanRemoveAndPassLeadership()
    {
        var actions = PlayerListRules.Evaluate(new PlayerListRelation
        {
            HasTarget = true,
            ViewerIsInParty = true,
            ViewerIsPartyLeader = true,
            TargetIsInViewerParty = true,
        });

        actions.SendTell.IsEnabled.Should().BeTrue();
        actions.Remove.IsEnabled.Should().BeTrue();
        actions.MakeLeader.IsEnabled.Should().BeTrue();
        actions.Invite.DisabledReason.Should().Be("Already in your party.");
        actions.ToggleHostile.DisabledReason.Should().Be("You cannot mark members of your party hostile.");
    }

    [Test]
    public void MemberWhoIsNotLeader_CannotManageTheParty()
    {
        var member = PlayerListRules.Evaluate(new PlayerListRelation
        {
            HasTarget = true,
            ViewerIsInParty = true,
            TargetIsInViewerParty = true,
        });
        member.Remove.DisabledReason.Should().Be("Only your party leader can remove members.");
        member.MakeLeader.DisabledReason.Should().Be("Only your party leader can choose a new leader.");
    }

    [Test]
    public void MemberWhoIsNotLeader_CanInvitePlayersOutsideAnyParty()
    {
        var outsider = PlayerListRules.Evaluate(new PlayerListRelation
        {
            HasTarget = true,
            ViewerIsInParty = true,
        });

        outsider.Invite.IsEnabled.Should().BeTrue();
        outsider.Invite.DisabledReason.Should().BeNullOrEmpty();
    }

    [Test]
    public void SoloPlayers_CanInviteAndMarkHostile()
    {
        var actions = PlayerListRules.Evaluate(new PlayerListRelation { HasTarget = true });

        actions.Invite.IsEnabled.Should().BeTrue();
        actions.ToggleHostile.IsEnabled.Should().BeTrue();
        actions.ToggleHostileText.Should().Be("Mark Hostile");
        actions.Remove.DisabledReason.Should().Be("Not in your party.");
        actions.LeaveParty.IsEnabled.Should().BeFalse();

        PlayerListRules.Evaluate(new PlayerListRelation { HasTarget = true, ViewerDislikesTarget = true })
            .ToggleHostileText.Should().Be("Clear Hostile");
    }

    [Test]
    public void Call_RequiresAnAvailablePlayerAndNoCallInProgress()
    {
        PlayerListRules.Evaluate(new PlayerListRelation { HasTarget = true }).Call.IsEnabled.Should().BeTrue();
        PlayerListRules.Evaluate(new PlayerListRelation()).Call.DisabledReason.Should().Be(PlayerListRules.SelectPlayerFirst);
        PlayerListRules.Evaluate(new PlayerListRelation { HasTarget = true, TargetIsSelf = true })
            .Call.DisabledReason.Should().Be("You cannot call yourself.");
        PlayerListRules.Evaluate(new PlayerListRelation { HasTarget = true, ViewerIsInSpace = true })
            .Call.DisabledReason.Should().Be("HoloCom cannot be used in space.");
        PlayerListRules.Evaluate(new PlayerListRelation { HasTarget = true, TargetIsInSpace = true })
            .Call.IsEnabled.Should().BeFalse();
        PlayerListRules.Evaluate(new PlayerListRelation { HasTarget = true, TargetIsInCall = true })
            .Call.DisabledReason.Should().Be("Line busy.");
        PlayerListRules.Evaluate(new PlayerListRelation { HasTarget = true, ViewerCallState = PlayerListCallState.Outgoing })
            .Call.IsEnabled.Should().BeFalse();
        PlayerListRules.Evaluate(new PlayerListRelation { HasTarget = true, ViewerIsDM = true })
            .Call.DisabledReason.Should().Be(PlayerListRules.DMsCannotUseHoloCom);

        var dmViewer = PlayerListRules.Evaluate(new PlayerListRelation
            { HasTarget = true, ViewerIsDM = true, TargetIsDM = true, ViewerCallState = PlayerListCallState.Incoming });
        dmViewer.Call.DisabledReason.Should().Be(PlayerListRules.DMsCannotUseHoloCom);
        dmViewer.AnswerCall.DisabledReason.Should().Be(PlayerListRules.DMsCannotUseHoloCom);
        dmViewer.DeclineCall.DisabledReason.Should().Be(PlayerListRules.DMsCannotUseHoloCom);
    }

    [Test]
    public void Answer_IsRefusedInSpaceButDeclineIsNot()
    {
        var actions = PlayerListRules.Evaluate(new PlayerListRelation
        {
            ViewerCallState = PlayerListCallState.Incoming,
            ViewerIsInSpace = true,
        });

        actions.AnswerCall.DisabledReason.Should().Be("HoloCom cannot be used in space.");
        actions.DeclineCall.IsEnabled.Should().BeTrue();
    }

    [TestCase(PlayerListCallState.None, false, false, "End Call")]
    [TestCase(PlayerListCallState.Incoming, true, false, "End Call")]
    [TestCase(PlayerListCallState.Outgoing, false, true, "Cancel Call")]
    [TestCase(PlayerListCallState.Connected, false, true, "End Call")]
    public void CallControls_FollowTheViewersCallState(PlayerListCallState state, bool canAnswer, bool canEnd, string endText)
    {
        var actions = PlayerListRules.Evaluate(new PlayerListRelation { ViewerCallState = state });

        actions.AnswerCall.IsEnabled.Should().Be(canAnswer);
        actions.DeclineCall.IsEnabled.Should().Be(canAnswer);
        actions.EndCall.IsEnabled.Should().Be(canEnd);
        actions.EndCallText.Should().Be(endText);
    }

    [Test]
    public void TargetInAnotherParty_CannotBeInvited()
    {
        PlayerListRules.Evaluate(new PlayerListRelation { HasTarget = true, TargetIsInOtherParty = true })
            .Invite.DisabledReason.Should().Be("Already in another party.");
    }

    [Test]
    public void DMViewer_CanSendTellsButNotUsePartyOrHostility()
    {
        var actions = PlayerListRules.Evaluate(new PlayerListRelation { HasTarget = true, ViewerIsDM = true });

        actions.SendTell.IsEnabled.Should().BeTrue();
        actions.ToggleHostile.IsEnabled.Should().BeFalse();
        actions.Invite.DisabledReason.Should().Be(PlayerListRules.DMsCannotUsePartyControls);
        actions.Remove.DisabledReason.Should().Be(PlayerListRules.DMsCannotUsePartyControls);
        actions.MakeLeader.DisabledReason.Should().Be(PlayerListRules.DMsCannotUsePartyControls);
        actions.LeaveParty.DisabledReason.Should().Be(PlayerListRules.DMsCannotUsePartyControls);
    }

    [Test]
    public void JumpTo_IsOnlyForDMsWithAnotherPlayerSelected()
    {
        PlayerListRules.Evaluate(new PlayerListRelation { HasTarget = true }).JumpTo.DisabledReason
            .Should().Be("Only DMs can jump to players.");
        PlayerListRules.Evaluate(new PlayerListRelation { ViewerIsDM = true }).JumpTo.DisabledReason
            .Should().Be(PlayerListRules.SelectPlayerFirst);
        PlayerListRules.Evaluate(new PlayerListRelation { ViewerIsDM = true, HasTarget = true, TargetIsSelf = true }).JumpTo.DisabledReason
            .Should().Be("You cannot jump to yourself.");
        PlayerListRules.Evaluate(new PlayerListRelation { ViewerIsDM = true, HasTarget = true }).JumpTo.IsEnabled
            .Should().BeTrue();
        PlayerListRules.Evaluate(new PlayerListRelation { ViewerIsDM = true, HasTarget = true, TargetIsDM = true }).JumpTo.IsEnabled
            .Should().BeTrue();
    }

    [Test]
    public void DMPlayerActions_AreOnlyForDMsWithAnotherPlayerSelected()
    {
        static PlayerListActionState[] DMActions(PlayerListRelation relation)
        {
            var actions = PlayerListRules.Evaluate(relation);
            return new[] { actions.JumpHere, actions.Examine, actions.Boot };
        }

        DMActions(new PlayerListRelation { HasTarget = true }).Select(state => state.DisabledReason)
            .Should().Equal("Only DMs can summon players.", "Only DMs can examine players.", "Only DMs can boot players.");
        DMActions(new PlayerListRelation { ViewerIsDM = true }).Select(state => state.DisabledReason)
            .Should().AllBe(PlayerListRules.SelectPlayerFirst);
        DMActions(new PlayerListRelation { ViewerIsDM = true, HasTarget = true, TargetIsSelf = true }).Select(state => state.DisabledReason)
            .Should().Equal("You cannot summon yourself.", "You cannot examine yourself.", "You cannot boot yourself.");
        DMActions(new PlayerListRelation { ViewerIsDM = true, HasTarget = true, TargetIsDM = true }).Select(state => state.DisabledReason)
            .Should().Equal("DMs cannot be summoned.", "Only player characters can be examined here.", "DMs cannot be booted.");
        DMActions(new PlayerListRelation { ViewerIsDM = true, HasTarget = true }).Should().OnlyContain(state => state.IsEnabled);
    }

    [Test]
    public void Party_DescribesLeadersMembersAndSoloPlayers()
    {
        PlayerListRules.DescribeParty(true, "Kira", 1).Should().Be("None");
        PlayerListRules.DescribeParty(true, "Kira", 3).Should().Be("Leader (3 players)");
        PlayerListRules.DescribeParty(false, "Kira", 2).Should().Be("Led by Kira (2 players)");
    }

    [Test]
    public void Flags_DescribeInDisplayOrder()
    {
        PlayerListRules.DescribeFlags(Array.Empty<PlayerListFlagType>()).Should().Be("None");
        PlayerListRules.DescribeFlags(new[] { PlayerListFlagType.Crafting, PlayerListFlagType.GeneralRP })
            .Should().Be("General RP, Crafting");
    }

    [Test]
    public void Status_MarksPartyHostilityAndDMs()
    {
        PlayerListRules.GetStatus(new PlayerListRelation { TargetIsSelf = true })
            .Should().Be(PlayerListStatus.None);
        PlayerListRules.GetStatus(new PlayerListRelation { TargetIsSelf = true, ViewerIsInParty = true, ViewerIsPartyLeader = true })
            .Should().Be(PlayerListStatus.Leader);
        PlayerListRules.GetStatus(new PlayerListRelation { TargetIsSelf = true, ViewerIsInParty = true })
            .Should().Be(PlayerListStatus.Party);
        PlayerListRules.GetStatus(new PlayerListRelation { TargetIsInViewerParty = true, TargetIsPartyLeader = true })
            .Should().Be(PlayerListStatus.Leader);
        PlayerListRules.GetStatus(new PlayerListRelation { TargetIsInViewerParty = true })
            .Should().Be(PlayerListStatus.Party);
        PlayerListRules.GetStatus(new PlayerListRelation { TargetIsInOtherParty = true })
            .Should().Be(PlayerListStatus.Grouped);
        PlayerListRules.GetStatus(new PlayerListRelation { TargetIsInOtherParty = true, ViewerDislikesTarget = true })
            .Should().Be(PlayerListStatus.Hostile);
        PlayerListRules.GetStatus(new PlayerListRelation { TargetIsDM = true })
            .Should().Be(PlayerListStatus.DM);
    }

    [Test]
    public void Flags_KeepTheirPersistedValuesAndDescribeThemselves()
    {
        ((int)PlayerListFlagType.GeneralRP).Should().Be(1);
        ((int)PlayerListFlagType.PvP).Should().Be(2);
        ((int)PlayerListFlagType.DM).Should().Be(3);
        ((int)PlayerListFlagType.Crafting).Should().Be(4);

        foreach (var flag in Enum.GetValues<PlayerListFlagType>())
        {
            var definition = flag.GetAttribute<PlayerListFlagType, PlayerListFlagAttribute>();
            definition.Name.Should().NotBeNullOrWhiteSpace();
            definition.Description.Should().StartWith(definition.Name + ":");
            definition.IconResref.Should().MatchRegex("^[a-z0-9_]{1,16}$");
        }
    }

    [Test]
    public void Flags_ShipTheirIconsInTheUIHak()
    {
        var ui = Path.Combine(FindRepositoryRoot(), "SWLOR_Haks", "sw_ui");

        foreach (var flag in Enum.GetValues<PlayerListFlagType>())
        {
            var path = Path.Combine(ui, flag.GetAttribute<PlayerListFlagType, PlayerListFlagAttribute>().IconResref + ".tga");
            File.Exists(path).Should().BeTrue(path);

            // NWN ignores the TGA origin bit and reads rows bottom-up.
            var header = File.ReadAllBytes(path).Take(18).ToArray();
            (header[12] | header[13] << 8).Should().Be(32, path);
            (header[14] | header[15] << 8).Should().Be(32, path);
            (header[17] & 0x20).Should().Be(0, $"{path} must store rows bottom-up");
        }
    }

    [Test]
    public void PartyLeaders_SoloPlayersLeadThemselves()
    {
        var leaders = PlayerListRules.GetPartyLeaders(new[]
        {
            Member(1, factionId: 10, factionLeader: 1),
            Member(2, factionId: 11, factionLeader: 2),
        });

        leaders.Should().Equal(new Dictionary<uint, uint> { [1] = 1, [2] = 2 });
    }

    [Test]
    public void PartyLeaders_ASharedNPCFactionLeaderIsNotAParty()
    {
        // DM faction tools leave both players in an NPC faction whose leader is the same creature.
        var leaders = PlayerListRules.GetPartyLeaders(new[]
        {
            Member(1, factionId: null, factionLeader: 900),
            Member(2, factionId: null, factionLeader: 900),
        });

        leaders.Should().Equal(new Dictionary<uint, uint> { [1] = 1, [2] = 2 });
    }

    [Test]
    public void PartyLeaders_AFactionLeaderOutsideTheFactionIsNotAParty()
    {
        // A solo player whose GetFactionLeader names another player's character shares no party with them.
        var leaders = PlayerListRules.GetPartyLeaders(new[]
        {
            Member(1, factionId: 10, factionLeader: 1),
            Member(2, factionId: 11, factionLeader: 1),
        });

        leaders.Should().Equal(new Dictionary<uint, uint> { [1] = 1, [2] = 2 });
    }

    [Test]
    public void PartyLeaders_APartyFollowsTheEngineLeader()
    {
        var leaders = PlayerListRules.GetPartyLeaders(new[]
        {
            Member(1, factionId: 10, factionLeader: 2),
            Member(2, factionId: 10, factionLeader: 2),
            Member(3, factionId: 12, factionLeader: 3),
        });

        leaders.Should().Equal(new Dictionary<uint, uint> { [1] = 2, [2] = 2, [3] = 3 });
    }

    [Test]
    public void PartyLeaders_ADMCanLeadButIsNotListed()
    {
        var leaders = PlayerListRules.GetPartyLeaders(new[]
        {
            Member(1, factionId: 10, factionLeader: 9),
            Member(2, factionId: 10, factionLeader: 9),
            Member(9, factionId: 10, factionLeader: 9, isDM: true),
        });

        leaders.Should().Equal(new Dictionary<uint, uint> { [1] = 9, [2] = 9 });
    }

    [Test]
    public void PartyLeaders_AnUnlistedEngineLeaderFallsBackToAMember()
    {
        var leaders = PlayerListRules.GetPartyLeaders(new[]
        {
            Member(1, factionId: 10, factionLeader: 7),
            Member(2, factionId: 10, factionLeader: 7),
        });

        leaders.Should().Equal(new Dictionary<uint, uint> { [1] = 1, [2] = 1 });
    }

    private static PlayerListPartyMembership Member(uint player, int? factionId, uint factionLeader, bool isDM = false)
    {
        return new PlayerListPartyMembership
        {
            Player = player,
            IsDM = isDM,
            PartyFactionId = factionId,
            FactionLeader = factionLeader,
        };
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
