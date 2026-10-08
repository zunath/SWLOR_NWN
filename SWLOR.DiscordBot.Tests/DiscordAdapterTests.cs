using System.Globalization;
using System.Net;
using System.Text.Json;
using Discord;
using Discord.Net;
using NUnit.Framework;
using SWLOR.DiscordBot.Configuration;
using SWLOR.DiscordBot.Core;
using SWLOR.DiscordBot.Discord;
using SWLOR.DiscordBot.Hosting;

namespace SWLOR.DiscordBot.Tests;

[TestFixture]
public sealed class DiscordAdapterTests
{
    [TestCase(TicketState.Creating)]
    [TestCase(TicketState.Open)]
    [TestCase(TicketState.Closing)]
    [TestCase(TicketState.Closed)]
    [TestCase(TicketState.Reopening)]
    public void DisabledIntakePreflightUsesOnlyRetainedPanelDestinationsAndLog(TicketState state)
    {
        var config = new BotConfiguration
        {
            Tickets = new TicketOptions
            {
                Enabled = false, ClosedCategoryId = 30, LogChannelId = 40,
                Panels =
                [
                    new TicketPanelOptions { Id = "support", ChannelId = 0, Label = "", PanelMessage = "{obsolete}", OpenCategoryIds = [20] },
                    new TicketPanelOptions { Id = "SUPPORT", ChannelId = 999, OpenCategoryIds = [998] }
                ]
            }
        };
        var tickets = new[] { new Ticket(Guid.NewGuid(), "support", 5, 10, state, 1, DateTimeOffset.UnixEpoch) };
        var permissions = new ChannelPermissions(viewChannel: true, sendMessages: true, manageChannel: true,
            manageRoles: true, readMessageHistory: true, attachFiles: true);
        (ulong Id, ChannelType Type, ChannelPermissions Permissions)[] channels =
            [(20, ChannelType.Category, permissions), (30, ChannelType.Category, permissions), (40, ChannelType.Text, permissions)];
        Assert.DoesNotThrow(() => DiscordOperations.ValidateTicketChannels(config, tickets, channels));
        Assert.That(TicketMaintenanceRequirements.RequiredPanels(config, tickets).Single().Id, Is.EqualTo("support"));
        Assert.Throws<DiscordValidationException>(() => DiscordOperations.ValidateTicketChannels(config, tickets,
            channels.Where(channel => channel.Id != 20).ToArray()));
        config.Tickets.Enabled = true;
        Assert.Throws<DiscordValidationException>(() => DiscordOperations.ValidateTicketChannels(config, tickets, channels));
    }

    [Test]
    public void DeletingOnlyDisabledIntakePreflightNeedsNoPanelsOrCategoryDestinations()
    {
        var config = new BotConfiguration { Tickets = new TicketOptions { Enabled = false, LogChannelId = 40, Panels = [] } };
        var tickets = new[] { new Ticket(Guid.NewGuid(), "obsolete", 5, 10, TicketState.Deleting, 1, DateTimeOffset.UnixEpoch) };
        Assert.DoesNotThrow(() => DiscordOperations.ValidateTicketChannels(config, tickets,
            [(40, ChannelType.Text, new ChannelPermissions(viewChannel: true, sendMessages: true))]));
        Assert.That(TicketMaintenanceRequirements.RequiredCategoryIds(config, tickets), Is.Empty);
        Assert.Throws<DiscordValidationException>(() => DiscordOperations.ValidateTicketChannels(config, tickets, []));
    }
    [Test]
    public void Privacy_DeniesForeignInheritedVisibilityAndPreservesOnlyConfiguredAccess()
    {
        var overwrites = DiscordOperations.BuildOverwrites(1, 2, 3, new ulong[] { 4 }, new[]
        {
            new Overwrite(5, PermissionTarget.Role, new OverwritePermissions(viewChannel: PermValue.Allow)),
            new Overwrite(6, PermissionTarget.User, new OverwritePermissions(viewChannel: PermValue.Allow))
        }, true, true, true);
        Assert.That(Find(overwrites, 1, PermissionTarget.Role).ViewChannel, Is.EqualTo(PermValue.Deny));
        Assert.That(Find(overwrites, 5, PermissionTarget.Role).ViewChannel, Is.EqualTo(PermValue.Deny));
        Assert.That(overwrites.Any(x => x.TargetId == 6), Is.False);
        Assert.That(Find(overwrites, 4, PermissionTarget.Role).ViewChannel, Is.EqualTo(PermValue.Allow));
        Assert.That(Find(overwrites, 4, PermissionTarget.Role).SendMessages, Is.EqualTo(PermValue.Allow));
        Assert.That(Find(overwrites, 3, PermissionTarget.User).SendMessages, Is.EqualTo(PermValue.Allow));
        Assert.That(Find(overwrites, 2, PermissionTarget.User).ManageChannel, Is.EqualTo(PermValue.Allow));
        Assert.That(Find(overwrites, 2, PermissionTarget.User).ReadMessageHistory, Is.EqualTo(PermValue.Allow));
        Assert.That(Find(overwrites, 4, PermissionTarget.Role).CreatePrivateThreads, Is.EqualTo(PermValue.Deny));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void RequesterVisibility_IsExplicitAndMessageSendingCanBeDenied(bool requesterRead)
    {
        var overwrites = DiscordOperations.BuildOverwrites(1, 2, 3, new ulong[] { 4 }, [], requesterRead, false, false);
        var requester = Find(overwrites, 3, PermissionTarget.User);
        Assert.That(requester.ViewChannel, Is.EqualTo(requesterRead ? PermValue.Allow : PermValue.Deny));
        Assert.That(requester.ReadMessageHistory, Is.EqualTo(requesterRead ? PermValue.Allow : PermValue.Deny));
        Assert.That(requester.SendMessages, Is.EqualTo(PermValue.Deny));
        Assert.That(requester.AddReactions, Is.EqualTo(PermValue.Deny));
        var support = Find(overwrites, 4, PermissionTarget.Role);
        Assert.That(support.ViewChannel, Is.EqualTo(PermValue.Allow));
        Assert.That(support.SendMessages, Is.EqualTo(PermValue.Deny));
        Assert.That(support.SendMessagesInThreads, Is.EqualTo(PermValue.Deny));
        Assert.That(support.AttachFiles, Is.EqualTo(PermValue.Deny));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task DeletionFreeze_HidesRequesterRegardlessOfClosedVisibilityAndRemovesForeignGrants(bool closedRequesterCanRead)
    {
        var ticket = new Ticket(Guid.NewGuid(), "support", 3, 10, TicketState.Deleting, 1, DateTimeOffset.UnixEpoch);
        var existing = DiscordOperations.BuildOverwrites(1, 2, 3, [4], [], closedRequesterCanRead, false, true)
            .Append(new Overwrite(5, PermissionTarget.Role, new OverwritePermissions(viewChannel: PermValue.Allow)))
            .Append(new Overwrite(6, PermissionTarget.User, new OverwritePermissions(viewChannel: PermValue.Allow))).ToArray();
        var expected = DiscordOperations.BuildFrozenOverwrites(ticket, 1, 2, [4], existing);
        IReadOnlyCollection<Overwrite> actual = existing;
        var verified = false;
        await DiscordOperations.SynchronizeTicketOverwritesAsync(10, expected, actual,
            value => { actual = value; return Task.CompletedTask; },
            () => { verified = true; return Task.FromResult(actual); });

        var frozen = actual.ToArray();
        Assert.That(verified, Is.True);
        Assert.That(Find(frozen, 3, PermissionTarget.User).ViewChannel, Is.EqualTo(PermValue.Deny));
        Assert.That(Find(frozen, 3, PermissionTarget.User).ReadMessageHistory, Is.EqualTo(PermValue.Deny));
        Assert.That(Find(frozen, 3, PermissionTarget.User).SendMessages, Is.EqualTo(PermValue.Deny));
        Assert.That(Find(frozen, 5, PermissionTarget.Role).ViewChannel, Is.EqualTo(PermValue.Deny));
        Assert.That(frozen.Any(item => item.TargetId == 6), Is.False);
        Assert.That(Find(frozen, 4, PermissionTarget.Role).ViewChannel, Is.EqualTo(PermValue.Allow));
        Assert.That(Find(frozen, 4, PermissionTarget.Role).ReadMessageHistory, Is.EqualTo(PermValue.Allow));
        Assert.That(Find(frozen, 4, PermissionTarget.Role).SendMessages, Is.EqualTo(PermValue.Deny));
        Assert.That(Find(frozen, 2, PermissionTarget.User).ViewChannel, Is.EqualTo(PermValue.Allow));
    }

    [Test]
    public void DeletionFreeze_RequesterMemberDenialOverridesSupportRoleVisibilityGrant()
    {
        var ticket = new Ticket(Guid.NewGuid(), "support", 3, 10, TicketState.Deleting, 1, DateTimeOffset.UnixEpoch);
        var frozen = DiscordOperations.BuildFrozenOverwrites(ticket, 1, 2, [4], []);
        // Discord applies everyone, combined role overwrites, then the member overwrite last.
        var permissions = new GuildPermissions(viewChannel: true, readMessageHistory: true, manageMessages: true).RawValue;
        foreach (var overwrite in new[]
        {
            Find(frozen, 1, PermissionTarget.Role),
            Find(frozen, 4, PermissionTarget.Role),
            Find(frozen, 3, PermissionTarget.User)
        }) permissions = (permissions & ~overwrite.DenyValue) | overwrite.AllowValue;
        Assert.That(new ChannelPermissions(permissions).ViewChannel, Is.False,
            "The requester must remain hidden even when also holding a support role that allows channel visibility.");
    }

    [Test]
    public void DeletionFreeze_UnappliedRequesterVisibilityDenialFailsReadBack()
    {
        var ticket = new Ticket(Guid.NewGuid(), "support", 3, 10, TicketState.Deleting, 1, DateTimeOffset.UnixEpoch);
        var stale = DiscordOperations.BuildOverwrites(1, 2, 3, [4], [], true, false, false);
        var frozen = DiscordOperations.BuildFrozenOverwrites(ticket, 1, 2, [4], stale);
        Assert.ThrowsAsync<DiscordValidationException>(() => DiscordOperations.SynchronizeTicketOverwritesAsync(10, frozen, stale,
            _ => Task.CompletedTask, () => Task.FromResult<IReadOnlyCollection<Overwrite>>(stale)));
    }

    [TestCase(TicketState.Open)]
    [TestCase(TicketState.Closed)]
    [TestCase(TicketState.Deleted)]
    public void DeletionFreeze_RequiresDeletingState(TicketState state)
    {
        var ticket = new Ticket(Guid.NewGuid(), "support", 3, 10, state, 1, DateTimeOffset.UnixEpoch);
        Assert.Throws<InvalidOperationException>(() => DiscordOperations.BuildFrozenOverwrites(ticket, 1, 2, [4], []));
    }

    [Test]
    public void Privacy_FailsClosedWhenExplicitOverwriteCountExceedsDiscordLimit()
    {
        var inherited = Enumerable.Range(10, 100).Select(x => new Overwrite((ulong)x, PermissionTarget.Role, new OverwritePermissions(viewChannel: PermValue.Allow)));
        Assert.Throws<InvalidOperationException>(() => DiscordOperations.BuildOverwrites(1, 2, 3, new ulong[] { 4 }, inherited, true, true, true));
    }

    [TestCase(TicketState.Open, false)]
    [TestCase(TicketState.Open, true)]
    [TestCase(TicketState.Closed, false)]
    [TestCase(TicketState.Closed, true)]
    public async Task RetainedPermissions_ReplaceOldStaffAndApplyCurrentRequesterVisibility(TicketState state, bool requesterCanRead)
    {
        var ticket = new Ticket(Guid.NewGuid(), "support", 3, 10, state, 1, DateTimeOffset.UnixEpoch);
        IReadOnlyCollection<Overwrite> actual = DiscordOperations.BuildOverwrites(1, 2, 3, [4], [],
            requesterRead: !requesterCanRead, requesterWrite: state == TicketState.Open, supportWrite: true);
        var expected = DiscordOperations.BuildRetainedOverwrites(ticket, 1, 2, [7], actual, requesterCanRead);
        var updates = 0;

        await DiscordOperations.SynchronizeTicketOverwritesAsync(10, expected, actual,
            value => { updates++; actual = value; return Task.CompletedTask; },
            () => Task.FromResult(actual));

        var updated = actual.ToArray();
        Assert.That(updates, Is.EqualTo(1));
        Assert.That(Find(updated, 4, PermissionTarget.Role).ViewChannel, Is.EqualTo(PermValue.Deny));
        Assert.That(Find(updated, 7, PermissionTarget.Role).ViewChannel, Is.EqualTo(PermValue.Allow));
        Assert.That(Find(updated, 7, PermissionTarget.Role).SendMessages, Is.EqualTo(PermValue.Allow));
        Assert.That(Find(updated, 3, PermissionTarget.User).ViewChannel,
            Is.EqualTo(state == TicketState.Open || requesterCanRead ? PermValue.Allow : PermValue.Deny));
        Assert.That(Find(updated, 3, PermissionTarget.User).SendMessages,
            Is.EqualTo(state == TicketState.Open ? PermValue.Allow : PermValue.Deny));
        Assert.That(Find(updated, 1, PermissionTarget.Role).ViewChannel, Is.EqualTo(PermValue.Deny));
    }

    [Test]
    public async Task RetainedPermissions_AlreadyMatchingPolicyIsVerifiedWithoutAnotherMutation()
    {
        var expected = DiscordOperations.BuildOverwrites(1, 2, 3, [4], [], false, false, true);
        var readBack = 0;
        await DiscordOperations.SynchronizeTicketOverwritesAsync(10, expected, expected.Reverse().ToArray(),
            _ => throw new AssertionException("Already matching overwrites must not be rewritten."),
            () => { readBack++; return Task.FromResult<IReadOnlyCollection<Overwrite>>(expected); });
        Assert.That(readBack, Is.EqualTo(1));
    }

    [Test]
    public void RetainedPermissions_RejectedOrUnappliedUpdatesFailVerification()
    {
        var stale = DiscordOperations.BuildOverwrites(1, 2, 3, [4], [], true, true, true);
        var expected = DiscordOperations.BuildOverwrites(1, 2, 3, [7], stale, false, false, true);
        var failure = Assert.ThrowsAsync<DiscordValidationException>(() => DiscordOperations.SynchronizeTicketOverwritesAsync(10, expected, stale,
            _ => Task.CompletedTask, () => Task.FromResult<IReadOnlyCollection<Overwrite>>(stale)));
        Assert.That(failure!.Message, Does.Contain("10"));
        Assert.ThrowsAsync<InvalidOperationException>(() => DiscordOperations.SynchronizeTicketOverwritesAsync(10, expected, stale,
            _ => Task.FromException(new InvalidOperationException("Discord denied the permission mutation.")),
            () => throw new AssertionException("Failed updates must propagate immediately.")));
    }

    [TestCase(TicketState.Deleting)]
    [TestCase(TicketState.Closing)]
    [TestCase(TicketState.Reopening)]
    [TestCase(TicketState.Deleted)]
    public void RetainedPermissions_NeverRestoreWritesForUnstableOrDeletedTickets(TicketState state)
    {
        var ticket = new Ticket(Guid.NewGuid(), "support", 3, 10, state, 1, DateTimeOffset.UnixEpoch);
        Assert.Throws<InvalidOperationException>(() => DiscordOperations.BuildRetainedOverwrites(ticket, 1, 2, [4], [], true));
    }
    [TestCase(5UL)]
    [TestCase(null)]
    public async Task Close_FullArchiveCategoryCompletesWithPrivateUncategorizedChannel(ulong? currentCategory)
    {
        var events = new List<string>();
        var overwrites = DiscordOperations.BuildOverwrites(1, 2, 3, [4], [], false, false, true);
        await DiscordOperations.CloseChannelPlacementAsync(77, currentCategory,
            () => Task.FromResult(50),
            () =>
            {
                Assert.That(Find(overwrites, 3, PermissionTarget.User).SendMessages, Is.EqualTo(PermValue.Deny));
                Assert.That(Find(overwrites, 3, PermissionTarget.User).ViewChannel, Is.EqualTo(PermValue.Deny));
                Assert.That(Find(overwrites, 1, PermissionTarget.Role).ViewChannel, Is.EqualTo(PermValue.Deny));
                events.Add("closed");
                return Task.CompletedTask;
            },
            destination =>
            {
                Assert.That(destination, Is.Null);
                Assert.That(events[0], Is.EqualTo("closed"));
                events.Add("uncategorized");
                return Task.CompletedTask;
            });
        Assert.That(events, Is.EqualTo(currentCategory.HasValue ? new[] { "closed", "uncategorized" } : new[] { "closed" }));
    }

    [Test]
    public async Task Close_AlreadyInFullArchiveCategoryDoesNotCountOrMoveAgain()
    {
        var closed = false;
        await DiscordOperations.CloseChannelPlacementAsync(77, 77,
            () => throw new AssertionException("A repeated close must not allocate a new category slot."),
            () => { closed = true; return Task.CompletedTask; },
            _ => throw new AssertionException("A repeated close must not move the channel."));
        Assert.That(closed, Is.True);
    }

    [Test]
    public async Task Close_CategoryFillingDuringMoveFallsBackAfterAccessIsRestricted()
    {
        var counts = 0;
        var destinations = new List<ulong?>();
        var closed = false;
        await DiscordOperations.CloseChannelPlacementAsync(77, 5,
            () => Task.FromResult(++counts == 1 ? 49 : 50),
            () => { closed = true; return Task.CompletedTask; },
            destination =>
            {
                Assert.That(closed, Is.True);
                destinations.Add(destination);
                return destination.HasValue ? Task.FromException(PlacementError()) : Task.CompletedTask;
            });
        Assert.That(counts, Is.EqualTo(2));
        Assert.That(destinations, Is.EqualTo(new ulong?[] { 77, null }));
    }

    [TestCase(HttpStatusCode.Forbidden, "parent_id", 50)]
    [TestCase(HttpStatusCode.BadRequest, "permission_overwrites", 50)]
    [TestCase(HttpStatusCode.BadRequest, "parent_id", 49)]
    public void Close_UnrelatedApiFailuresPropagateWithoutFallback(HttpStatusCode status, string errorPath, int freshCount)
    {
        var counts = 0;
        var destinations = new List<ulong?>();
        var failure = PlacementError(status, errorPath);
        var actual = Assert.ThrowsAsync<HttpException>(() => DiscordOperations.CloseChannelPlacementAsync(77, 5,
            () => Task.FromResult(++counts == 1 ? 49 : freshCount),
            () => Task.CompletedTask,
            destination => { destinations.Add(destination); return Task.FromException(failure); }));
        Assert.That(actual, Is.SameAs(failure));
        Assert.That(destinations, Is.EqualTo(new ulong?[] { 77 }));
    }

    [Test]
    public void Close_AccessFailurePreventsCategoryMovement()
    {
        Assert.ThrowsAsync<InvalidOperationException>(() => DiscordOperations.CloseChannelPlacementAsync(77, 5,
            () => throw new AssertionException("Capacity is checked only after access is restricted."),
            () => Task.FromException(new InvalidOperationException("Permission update rejected.")),
            _ => throw new AssertionException("Do not move while access remains open.")));
    }

    [TestCase(GuildPermission.Administrator)]
    [TestCase(GuildPermission.ManageGuild)]
    [TestCase(GuildPermission.ManageRoles)]
    [TestCase(GuildPermission.ManageChannels)]
    [TestCase(GuildPermission.KickMembers)]
    [TestCase(GuildPermission.BanMembers)]
    [TestCase(GuildPermission.ModerateMembers)]
    [TestCase(GuildPermission.ManageWebhooks)]
    [TestCase(GuildPermission.ManageMessages)]
    [TestCase(GuildPermission.ManageThreads)]
    [TestCase(GuildPermission.ViewAuditLog)]
    [TestCase(GuildPermission.MentionEveryone)]
    [TestCase(GuildPermission.ManageNicknames)]
    [TestCase(GuildPermission.ManageEmojisAndStickers)]
    [TestCase(GuildPermission.ManageEvents)]
    [TestCase(GuildPermission.ViewGuildInsights)]
    [TestCase(GuildPermission.MuteMembers)]
    [TestCase(GuildPermission.DeafenMembers)]
    [TestCase(GuildPermission.MoveMembers)]
    [TestCase(GuildPermission.PrioritySpeaker)]
    public void SelfSelectedFactionRoles_RejectNativeStaffPermissions(GuildPermission permission)
    {
        var ordinary = (ulong)(GuildPermission.ViewChannel | GuildPermission.SendMessages);
        Assert.That(DiscordOperations.HasStaffPermissions(new GuildPermissions(ordinary | (ulong)permission)), Is.True);
    }

    [Test]
    public void SelfSelectedFactionRoles_AllowOrdinaryCapabilitiesButRejectUnknownBits()
    {
        Assert.That(DiscordOperations.HasStaffPermissions(new GuildPermissions(0)), Is.False);
        Assert.That(DiscordOperations.HasStaffPermissions(new GuildPermissions(
            viewChannel: true, sendMessages: true, connect: true, speak: true, addReactions: true,
            attachFiles: true, embedLinks: true, readMessageHistory: true, changeNickname: true)), Is.False);
        Assert.That(DiscordOperations.HasStaffPermissions(new GuildPermissions(1UL << 63)), Is.True);
    }

    [TestCase(GuildPermission.ManageMessages)]
    [TestCase(GuildPermission.ManageThreads)]
    [TestCase(GuildPermission.ManageChannels)]
    [TestCase(GuildPermission.ManageRoles)]
    [TestCase(GuildPermission.ManageWebhooks)]
    [TestCase(GuildPermission.MentionEveryone)]
    [TestCase(GuildPermission.MuteMembers)]
    [TestCase(GuildPermission.MoveMembers)]
    public void SelfSelectedFactionRoles_RejectPrivilegedChannelOrCategoryOverwriteGrants(GuildPermission permission)
    {
        var ordinary = new OverwritePermissions(viewChannel: PermValue.Allow, sendMessages: PermValue.Allow);
        var privileged = new OverwritePermissions((ulong)permission | ordinary.AllowValue, 0);
        var overwrites = new (ulong, Overwrite)[]
        {
            (101, new Overwrite(42, PermissionTarget.Role, ordinary)),
            (102, new Overwrite(42, PermissionTarget.Role, privileged))
        };

        Assert.That(() => DiscordOperations.ValidateFactionRoleOverwrites(42, overwrites),
            Throws.TypeOf<DiscordValidationException>().With.Message.Contains("42").And.Message.Contains("102"));
        // A grant on the category itself remains unsafe even before any child inherits it.
        Assert.That(() => DiscordOperations.ValidateFactionRoleOverwrites(42, [(101UL, overwrites[1].Item2)]),
            Throws.TypeOf<DiscordValidationException>().With.Message.Contains("101"));
    }

    [Test]
    public void SelfSelectedFactionRoles_OverwriteValidationPreservesOrdinaryAccessAndIgnoresUnrelatedTargets()
    {
        var ordinary = new OverwritePermissions(viewChannel: PermValue.Allow, sendMessages: PermValue.Allow,
            readMessageHistory: PermValue.Allow, attachFiles: PermValue.Allow,
            createPublicThreads: PermValue.Allow, sendMessagesInThreads: PermValue.Allow,
            manageMessages: PermValue.Deny, manageThreads: PermValue.Deny);
        var privileged = new OverwritePermissions(manageMessages: PermValue.Allow, manageThreads: PermValue.Allow);

        Assert.DoesNotThrow(() => DiscordOperations.ValidateFactionRoleOverwrites(42,
        [
            (101UL, new Overwrite(42, PermissionTarget.Role, ordinary)),
            (102UL, new Overwrite(43, PermissionTarget.Role, privileged)),
            (103UL, new Overwrite(42, PermissionTarget.User, privileged))
        ]));
    }

    [Test]
    public void SelfSelectedFactionRoles_OverwriteValidationRejectsUnknownAllowBitsButNotDenials()
    {
        Assert.That(() => DiscordOperations.ValidateFactionRoleOverwrites(42,
            [(101UL, new Overwrite(42, PermissionTarget.Role, new OverwritePermissions(1UL << 63, 0)))]),
            Throws.TypeOf<DiscordValidationException>());
        Assert.DoesNotThrow(() => DiscordOperations.ValidateFactionRoleOverwrites(42,
            [(101UL, new Overwrite(42, PermissionTarget.Role, new OverwritePermissions(0, 1UL << 63)))]));
    }

    [Test]
    public void SelfSelectedFactionRoles_FreshOverwriteValidationRejectsGrantsAddedAfterStartup()
    {
        var current = new List<(ulong, Overwrite)>
        {
            (101, new Overwrite(42, PermissionTarget.Role, new OverwritePermissions(viewChannel: PermValue.Allow)))
        };
        Assert.DoesNotThrow(() => DiscordOperations.ValidateFactionRoleOverwrites(42, current));
        current.Add((102, new Overwrite(42, PermissionTarget.Role, new OverwritePermissions(manageThreads: PermValue.Allow))));
        Assert.That(() => DiscordOperations.ValidateFactionRoleOverwrites(42, current),
            Throws.TypeOf<DiscordValidationException>());
        current.RemoveAt(1);
        Assert.DoesNotThrow(() => DiscordOperations.ValidateFactionRoleOverwrites(42, current));
    }
    [TestCase(false)]
    [TestCase(true)]
    public void Welcome_RejectsMissingOrForeignMentionChannelsEvenForDirectMessages(bool directMessage)
    {
        var configuration = new BotConfiguration
        {
            Welcome = new WelcomeOptions
            {
                Enabled = true, DirectMessage = directMessage, ChannelId = 10,
                ChannelMentions = new() { ["rules"] = 99 }
            }
        };
        var channels = new[] { (10UL, ChannelType.Text, new ChannelPermissions(viewChannel: true, sendMessages: true)) };
        Assert.That(() => DiscordOperations.ValidateCommunityChannels(configuration, channels),
            Throws.TypeOf<DiscordValidationException>().With.Message.Contains("99"));
        configuration.Welcome.ChannelMentions["rules"] = 10;
        Assert.DoesNotThrow(() => DiscordOperations.ValidateCommunityChannels(configuration, channels));
    }

    [Test]
    public void Welcome_MentionsNeedGuildExistenceWithoutBotPostingPermissions()
    {
        var configuration = new BotConfiguration
        {
            Welcome = new WelcomeOptions
            {
                Enabled = true, DirectMessage = true,
                ChannelMentions = new() { ["rules"] = 11 }
            }
        };
        Assert.DoesNotThrow(() => DiscordOperations.ValidateCommunityChannels(configuration,
            [(11UL, ChannelType.Text, new ChannelPermissions(0))]));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Answers_CommandDeletionRequiresEffectivePermissionsThroughoutItsScope(bool restricted)
    {
        var answer = new QuickAnswerOptions { DeleteCommand = true, AllowedChannelIds = restricted ? [10, 11] : [] };
        var configuration = new BotConfiguration { Answers = [answer] };
        var ordinary = new ChannelPermissions(viewChannel: true, sendMessages: true);
        var moderation = new ChannelPermissions(viewChannel: true, sendMessages: true, manageMessages: true);
        var channels = new[] { (10UL, ChannelType.Text, moderation), (11UL, ChannelType.Text, ordinary) };
        Assert.That(() => DiscordOperations.ValidateCommunityChannels(configuration, channels),
            Throws.TypeOf<DiscordValidationException>().With.Message.Contains("11"));
        channels[1] = (11UL, ChannelType.Text, moderation);
        Assert.DoesNotThrow(() => DiscordOperations.ValidateCommunityChannels(configuration, channels));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Answers_OwnResponseDeletionDoesNotRequireModerationPermissions(bool restricted)
    {
        var configuration = new BotConfiguration
        {
            Answers = [new QuickAnswerOptions { DeleteResponseAfter = TimeSpan.FromSeconds(10), AllowedChannelIds = restricted ? [10] : [] }]
        };
        Assert.DoesNotThrow(() => DiscordOperations.ValidateCommunityChannels(configuration,
            [(10UL, ChannelType.Text, new ChannelPermissions(viewChannel: true, sendMessages: true))]));
    }

    [Test]
    public void Answers_UnrestrictedScopeExcludesUnreadableReadOnlyAndUnsupportedChannels()
    {
        var configuration = new BotConfiguration { Answers = [new QuickAnswerOptions { DeleteCommand = true }] };
        Assert.DoesNotThrow(() => DiscordOperations.ValidateCommunityChannels(configuration,
        [
            (10UL, ChannelType.Text, new ChannelPermissions(viewChannel: true, sendMessages: true, manageMessages: true)),
            (11UL, ChannelType.Text, new ChannelPermissions(sendMessages: true)),
            (12UL, ChannelType.Text, new ChannelPermissions(viewChannel: true)),
            (13UL, ChannelType.PublicThread, new ChannelPermissions(viewChannel: true, sendMessagesInThreads: true))
        ]));
        configuration.Answers[0].AllowedChannelIds = [12];
        Assert.That(() => DiscordOperations.ValidateCommunityChannels(configuration,
            [(12UL, ChannelType.Text, new ChannelPermissions(viewChannel: true))]), Throws.TypeOf<DiscordValidationException>());
    }

    [Test]
    public void Answers_EmbedPermissionsAreRequiredWhenResponseHasEmbeds()
    {
        var answer = new QuickAnswerOptions { Embeds = [new AnswerEmbed { Description = "Help" }] };
        var configuration = new BotConfiguration { Answers = [answer] };
        var channels = new[] { (10UL, ChannelType.Text, new ChannelPermissions(viewChannel: true, sendMessages: true)) };
        Assert.That(() => DiscordOperations.ValidateCommunityChannels(configuration, channels), Throws.TypeOf<DiscordValidationException>());
        channels[0] = (10UL, ChannelType.Text, new ChannelPermissions(viewChannel: true, sendMessages: true, embedLinks: true));
        Assert.DoesNotThrow(() => DiscordOperations.ValidateCommunityChannels(configuration, channels));
        answer.Enabled = false;
        Assert.DoesNotThrow(() => DiscordOperations.ValidateCommunityChannels(configuration,
            [(10UL, ChannelType.Text, new ChannelPermissions(viewChannel: true, sendMessages: true))]));
    }

    [Test]
    public void Factions_CommandDeletionRequiresEffectivePermissionsAcrossUsableTextChannels()
    {
        var configuration = new BotConfiguration { Factions = new FactionOptions { Enabled = true, DeleteCommand = true } };
        var moderation = new ChannelPermissions(viewChannel: true, sendMessages: true, manageMessages: true);
        var channels = new[]
        {
            (10UL, ChannelType.Text, moderation),
            (11UL, ChannelType.Text, new ChannelPermissions(viewChannel: true, sendMessages: true)),
            (12UL, ChannelType.Text, new ChannelPermissions(viewChannel: true)),
            (13UL, ChannelType.PublicThread, new ChannelPermissions(viewChannel: true, sendMessagesInThreads: true))
        };
        Assert.That(() => DiscordOperations.ValidateCommunityChannels(configuration, channels),
            Throws.TypeOf<DiscordValidationException>().With.Message.EqualTo("Command deletion requires Manage Messages in text channel 11."));
        channels[1] = (11UL, ChannelType.Text, moderation);
        Assert.DoesNotThrow(() => DiscordOperations.ValidateCommunityChannels(configuration, channels));
        configuration.Factions.Enabled = false;
        channels[1] = (11UL, ChannelType.Text, new ChannelPermissions(viewChannel: true, sendMessages: true));
        Assert.DoesNotThrow(() => DiscordOperations.ValidateCommunityChannels(configuration, channels));
    }

    [Test]
    public void Factions_OwnResponseDeletionDoesNotRequireModerationPermissions()
    {
        var configuration = new BotConfiguration { Factions = new FactionOptions { Enabled = true, DeleteResponse = true } };
        Assert.DoesNotThrow(() => DiscordOperations.ValidateCommunityChannels(configuration,
            [(10UL, ChannelType.Text, new ChannelPermissions(viewChannel: true, sendMessages: true))]));
    }

    [Test]
    public void TicketTextChannels_PlainMessagesAndControlsDoNotRequireEmbedLinks()
    {
        Assert.DoesNotThrow(() => DiscordOperations.ValidateTextChannelPermissions(10,
            new ChannelPermissions(viewChannel: true, sendMessages: true)));
    }

    [TestCase(false, true)]
    [TestCase(true, false)]
    public void TicketTextChannels_RequireVisibilityAndSendPermissions(bool view, bool send)
    {
        Assert.That(() => DiscordOperations.ValidateTextChannelPermissions(10,
            new ChannelPermissions(viewChannel: view, sendMessages: send)),
            Throws.TypeOf<DiscordValidationException>().With.Message.Contains("View Channel and Send Messages"));
    }

    [TestCase(TicketState.Creating)]
    [TestCase(TicketState.Open)]
    [TestCase(TicketState.Closing)]
    [TestCase(TicketState.Closed)]
    [TestCase(TicketState.Reopening)]
    [TestCase(TicketState.Deleting)]
    public void DisabledTicketing_RetainedChannelStatesStillRequireDiscordTicketCapabilities(TicketState state)
    {
        var configuration = new BotConfiguration { Tickets = new TicketOptions { Enabled = false } };
        var retained = new Ticket(Guid.NewGuid(), "retained", 42, 123, state, 1, DateTimeOffset.UnixEpoch);
        Assert.That(DiscordOperations.RequiresTicketCapabilities(configuration, [retained]), Is.True);
    }

    [Test]
    public void DisabledTicketing_DeletedArchiveOnlyRecordsAndEmptyStoreNeedNoTicketCapabilities()
    {
        var configuration = new BotConfiguration { Tickets = new TicketOptions { Enabled = false } };
        var retained = new Ticket(Guid.NewGuid(), "old-panel", 42, 123, TicketState.Deleted, 1, DateTimeOffset.UnixEpoch,
            ArchivePath: "/archives/retained/transcript.html");
        Assert.That(DiscordOperations.RequiresTicketCapabilities(configuration, [retained]), Is.False);
        Assert.That(DiscordOperations.RequiresTicketCapabilities(configuration, []), Is.False);
        configuration.Tickets.Enabled = true;
        Assert.That(DiscordOperations.RequiresTicketCapabilities(configuration, []), Is.True, "New ticketing requires capabilities before any ticket exists.");
    }

    [Test]
    public void DisabledTicketing_DeletingOnlyMaintenanceDoesNotRequireBypassRole()
    {
        var configuration = new BotConfiguration { Tickets = new TicketOptions { Enabled = false } };
        var deleting = new Ticket(Guid.NewGuid(), "old-panel", 42, 123, TicketState.Deleting, 1, DateTimeOffset.UnixEpoch);
        var closed = deleting with { State = TicketState.Closed };

        Assert.That(TicketMaintenanceRequirements.RequiresBypassRoles(configuration, [deleting]), Is.False);
        Assert.That(TicketMaintenanceRequirements.RequiresBypassRoles(configuration, [closed]), Is.True);
        configuration.Tickets.Enabled = true;
        Assert.That(TicketMaintenanceRequirements.RequiresBypassRoles(configuration, []), Is.True);
    }

    [Test]
    public void DisabledTicketing_CapabilityPolicyUsesCurrentSnapshotAfterRetainedTicketDeletion()
    {
        var configuration = new BotConfiguration { Tickets = new TicketOptions { Enabled = false } };
        var ticket = new Ticket(Guid.NewGuid(), "retained", 42, 123, TicketState.Closed, 1, DateTimeOffset.UnixEpoch);
        var snapshot = new List<Ticket> { ticket };
        Assert.That(DiscordOperations.RequiresTicketCapabilities(configuration, snapshot), Is.True);
        snapshot[0] = ticket with { State = TicketState.Deleted, ArchivePath = "/archives/retained/transcript.html" };
        Assert.That(DiscordOperations.RequiresTicketCapabilities(configuration, snapshot), Is.False);
    }
    [TestCase(ApplicationFlags.GatewayMessageContent)]
    [TestCase(ApplicationFlags.GatewayMessageContentLimited)]
    public void TicketTranscripts_RequireApplicationMessageContentCapability(ApplicationFlags flags)
    {
        Assert.DoesNotThrow(() => DiscordOperations.ValidateTranscriptCapability(flags));
        Assert.DoesNotThrow(() => DiscordOperations.ValidateTranscriptCapability(flags | ApplicationFlags.GatewayGuildMembersLimited));
    }

    [Test]
    public void TicketTranscripts_RejectMissingApplicationMessageContentCapability()
    {
        Assert.That(() => DiscordOperations.ValidateTranscriptCapability(ApplicationFlags.GatewayGuildMembersLimited),
            Throws.TypeOf<DiscordValidationException>().With.Message.Contains("Message Content Intent"));
    }

    [Test]
    public void DeliveryNonce_IsDeterministicAndFitsDiscordsLimit()
    {
        var nonce = DiscordCommunityPoster.Nonce("welcome:3:1234");
        Assert.That(nonce, Is.EqualTo(DiscordCommunityPoster.Nonce("welcome:3:1234")));
        Assert.That(nonce.Length, Is.EqualTo(24));
        Assert.That(nonce, Does.Match("^[A-F0-9]{24}$"));
        Assert.That(DiscordCommunityPoster.Nonce("welcome:3:1235"), Is.Not.EqualTo(nonce));
    }

    [Test]
    public async Task CommunityPoster_RetriesRateLimitWithSameEnforcedNonceAndNoMentions()
    {
        var handler = new RecordingHandler();
        using var http = new HttpClient(handler);
        using var poster = new DiscordCommunityPoster(http, new BotSecrets("fake-test-token", "Host=unused"), TimeProvider.System);
        var message = new CommunityMessage("Welcome <@3>", [], DeliveryKey: "welcome:3:1234");
        var id = await poster.SendAsync(10, message, CancellationToken.None);
        Assert.That(id, Is.EqualTo(123UL));
        Assert.That(handler.Bodies.Count, Is.EqualTo(2));
        foreach (var body in handler.Bodies)
        {
            using var json = JsonDocument.Parse(body);
            Assert.That(json.RootElement.GetProperty("nonce").GetString(), Is.EqualTo(DiscordCommunityPoster.Nonce(message.DeliveryKey!)));
            Assert.That(json.RootElement.GetProperty("enforce_nonce").GetBoolean(), Is.True);
            Assert.That(json.RootElement.GetProperty("allowed_mentions").GetProperty("parse").GetArrayLength(), Is.Zero);
            Assert.That(json.RootElement.GetProperty("content").GetString(), Is.EqualTo(message.Content));
        }
    }

    [TestCase(50007)]
    [TestCase(50013)]
    [TestCase(50001)]
    [TestCase(50278)]
    public void CommunityPosterPreservesNumericErrorCodeWithoutResponseOrToken(int code)
    {
        using var handler = new ErrorResponseHandler(() => new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent(JsonSerializer.Serialize(new { code, message = "private-response-marker" }))
        });
        using var http = new HttpClient(handler);
        using var poster = new DiscordCommunityPoster(http, new BotSecrets("private-token-marker", "Host=unused"), TimeProvider.System);
        var exception = Assert.ThrowsAsync<HttpException>(() => poster.SendAsync(10, new("welcome", []), default))!;
        Assert.Multiple(() =>
        {
            Assert.That(exception.HttpCode, Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That(exception.DiscordCode, Is.EqualTo((DiscordErrorCode)code));
            Assert.That(exception.ToString(), Does.Not.Contain("private-response-marker").And.Not.Contain("private-token-marker"));
            Assert.That(exception.InnerException, Is.Null);
            Assert.That(DiscordGateway.SafeError(exception), Is.EqualTo("Discord HTTP 403"));
            Assert.That(handler.Requests, Is.EqualTo(1));
        });
    }

    [TestCase("")]
    [TestCase("<html>private-response-marker</html>")]
    [TestCase("{}")]
    [TestCase("null")]
    [TestCase("[]")]
    [TestCase("{\"code\":\"50007\"}")]
    [TestCase("{\"code\":50007.5}")]
    [TestCase("{\"code\":2147483648}")]
    [TestCase("{\"code\":-1}")]
    [TestCase("{\"code\":0}")]
    [TestCase("{\"code\":true}")]
    [TestCase("{\"code\":50007")]
    public void CommunityPosterInvalidErrorCodeKeepsRetryableStatusWithoutResponseText(string body)
    {
        using var handler = new ErrorResponseHandler(() => new HttpResponseMessage(HttpStatusCode.Forbidden)
            { Content = new StringContent(body) });
        using var http = new HttpClient(handler);
        using var poster = new DiscordCommunityPoster(http, new BotSecrets("private-token-marker", "Host=unused"), TimeProvider.System);
        var exception = Assert.ThrowsAsync<HttpRequestException>(() => poster.SendAsync(10, new("welcome", []), default))!;
        Assert.That(exception.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That(exception.Message, Is.EqualTo("Discord message creation failed."));
        Assert.That(exception.ToString(), Does.Not.Contain("private-token-marker").And.Not.Contain("private-response-marker"));
        Assert.That(handler.Requests, Is.EqualTo(1));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CommunityPosterBoundsErrorBodyWithOrWithoutContentLength(bool declaredLength)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes("{\"code\":50007,\"message\":\"" + new string('x', 100000) + "\"}");
        using var stream = new CountedErrorStream(bytes);
        using var handler = new ErrorResponseHandler(() =>
        {
            var content = new StreamContent(stream);
            if (declaredLength) content.Headers.ContentLength = bytes.Length;
            return new HttpResponseMessage(HttpStatusCode.Forbidden) { Content = content };
        });
        using var http = new HttpClient(handler);
        using var poster = new DiscordCommunityPoster(http, new BotSecrets("fake-test-token", "Host=unused"), TimeProvider.System);
        var exception = Assert.ThrowsAsync<HttpRequestException>(() => poster.SendAsync(10, new("welcome", []), default))!;
        Assert.That(exception.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That(stream.BytesRead, declaredLength ? Is.Zero : Is.LessThanOrEqualTo(8193));
        Assert.That(stream.BytesRead, Is.LessThan(bytes.Length));
        Assert.That(handler.Requests, Is.EqualTo(1));
    }

    [Test]
    public async Task CommunityPosterErrorBodyCancellationPropagatesAndReleasesGate()
    {
        using var cancellation = new CancellationTokenSource();
        using var stream = new CountedErrorStream(System.Text.Encoding.UTF8.GetBytes("{\"code\":50007}"))
            { BeforeRead = () => cancellation.Cancel() };
        var refused = true;
        using var handler = new ErrorResponseHandler(() => refused
            ? new HttpResponseMessage(HttpStatusCode.Forbidden) { Content = new StreamContent(stream) }
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"id\":\"123\"}") });
        using var http = new HttpClient(handler);
        using var poster = new DiscordCommunityPoster(http, new BotSecrets("fake-test-token", "Host=unused"), TimeProvider.System);
        Assert.CatchAsync<OperationCanceledException>(() => poster.SendAsync(10, new("welcome", []), cancellation.Token));
        refused = false;
        Assert.That(await poster.SendAsync(10, new("welcome", []), default), Is.EqualTo(123UL));
        Assert.That(handler.Requests, Is.EqualTo(2));
    }

    [Test]
    public void Readiness_RequiresFreshNonfutureTimestamp()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".ready");
        var now = new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);
        try
        {
            Assert.That(ReadinessMarker.IsHealthy(path, now), Is.False);
            File.WriteAllText(path, (now - TimeSpan.FromSeconds(30)).ToString("O", CultureInfo.InvariantCulture));
            Assert.That(ReadinessMarker.IsHealthy(path, now), Is.True);
            File.WriteAllText(path, (now - TimeSpan.FromSeconds(91)).ToString("O", CultureInfo.InvariantCulture));
            Assert.That(ReadinessMarker.IsHealthy(path, now), Is.False);
            File.WriteAllText(path, (now + TimeSpan.FromSeconds(1)).ToString("O", CultureInfo.InvariantCulture));
            Assert.That(ReadinessMarker.IsHealthy(path, now), Is.False);
            File.WriteAllText(path, "invalid");
            Assert.That(ReadinessMarker.IsHealthy(path, now), Is.False);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Test]
    public async Task ValidateCommand_DoesNotRequireCredentialsOrConnect()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            File.WriteAllText(path, "{\"guildId\":\"1\",\"tickets\":{\"enabled\":false},\"welcome\":{\"enabled\":false},\"factions\":{\"enabled\":false},\"answers\":[]}");
            Assert.That(await Program.Main(["--config", path, "--validate"]), Is.Zero);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Test]
    public void GatewaySession_InitialHeartbeatCannotBypassStartupValidation()
    {
        var state = new GatewaySessionState();
        Assert.That(state.ObserveHeartbeat(true), Is.Null);
        Assert.That(state.IsReady(true), Is.False);
        var validation = state.BeginValidation();
        Assert.That(state.ObserveHeartbeat(true), Is.Null);
        Assert.That(state.IsReady(true), Is.False);
        Assert.That(state.CompleteValidation(validation, true), Is.True);
        Assert.That(state.IsReady(true), Is.True);
    }

    [Test]
    public void GatewaySession_ResumeHeartbeatRestoresAnAlreadyValidatedSession()
    {
        var state = new GatewaySessionState();
        state.CompleteValidation(state.BeginValidation(), true);
        state.Disconnect();
        Assert.That(state.IsReady(true), Is.False);
        Assert.That(state.ObserveHeartbeat(false), Is.Null);
        Assert.That(state.IsReady(false), Is.False);
        Assert.That(state.ObserveHeartbeat(true), Is.Null);
        Assert.That(state.IsReady(true), Is.True);
        Assert.That(state.IsReady(false), Is.False);
    }

    [Test]
    public void GatewaySession_FreshReadyRequiresValidationAgain()
    {
        var state = new GatewaySessionState();
        state.CompleteValidation(state.BeginValidation(), true);
        state.Disconnect();
        var fresh = state.BeginValidation();
        Assert.That(state.ObserveHeartbeat(true), Is.Null);
        Assert.That(state.IsReady(true), Is.False);
        Assert.That(state.CompleteValidation(fresh, true), Is.True);
        Assert.That(state.IsReady(true), Is.True);
    }

    [Test]
    public void GatewaySession_DisconnectInvalidatesInFlightValidationAndResumeRechecksIt()
    {
        var state = new GatewaySessionState();
        var interrupted = state.BeginValidation();
        state.Disconnect();
        Assert.That(state.IsCurrent(interrupted), Is.False);
        Assert.That(state.CompleteValidation(interrupted, true), Is.False);
        Assert.That(state.IsReady(true), Is.False);
        var resumed = state.ObserveHeartbeat(true);
        Assert.That(resumed, Is.Not.Null);
        Assert.That(state.IsReady(true), Is.False);
        Assert.That(state.ObserveHeartbeat(true), Is.Null);
        Assert.That(state.CompleteValidation(resumed!.Value, true), Is.True);
        Assert.That(state.IsReady(true), Is.True);
    }

    [Test]
    public void GatewaySession_OldValidationCannotEnableANewerOrStoppedSession()
    {
        var state = new GatewaySessionState();
        var old = state.BeginValidation();
        var fresh = state.BeginValidation();
        Assert.That(state.CompleteValidation(old, true), Is.False);
        Assert.That(state.IsReady(true), Is.False);
        state.Stop();
        Assert.That(state.CompleteValidation(fresh, true), Is.False);
        Assert.That(state.ObserveHeartbeat(true), Is.Null);
        Assert.That(state.IsReady(true), Is.False);
    }
    [Test]
    public void GatewaySession_ReadinessNotificationRequiresSuccessfulValidationAndFiresOncePerRecovery()
    {
        var state = new GatewaySessionState();
        var notifications = 0;
        state.ReadinessEstablished += () =>
        {
            Assert.That(state.IsReady(true), Is.True, "Maintenance notifications follow the ready state transition.");
            notifications++;
        };
        state.ObserveHeartbeat(true);
        Assert.That(notifications, Is.Zero);
        var initial = state.BeginValidation();
        state.ObserveHeartbeat(true);
        Assert.That(notifications, Is.Zero);
        Assert.That(state.CompleteValidation(initial, true), Is.True);
        Assert.That(notifications, Is.EqualTo(1));
        Assert.That(state.CompleteValidation(initial, true), Is.False);
        Parallel.For(0, 10, _ => state.ObserveHeartbeat(true));
        Assert.That(notifications, Is.EqualTo(1), "Ordinary heartbeat ACKs cannot start duplicate maintenance sweeps.");

        state.Disconnect();
        state.ObserveHeartbeat(false);
        Assert.That(notifications, Is.EqualTo(1));
        Parallel.For(0, 10, _ => state.ObserveHeartbeat(true));
        Assert.That(notifications, Is.EqualTo(2), "A validated RESUMED session must wake maintenance exactly once.");

        state.Disconnect();
        var fresh = state.BeginValidation();
        state.ObserveHeartbeat(true);
        Assert.That(notifications, Is.EqualTo(2));
        Assert.That(state.CompleteValidation(fresh, true), Is.True);
        Assert.That(notifications, Is.EqualTo(3), "A fresh READY must validate before waking maintenance.");
        state.Stop();
        state.ObserveHeartbeat(true);
        Assert.That(state.CompleteValidation(fresh, true), Is.False);
        Assert.That(notifications, Is.EqualTo(3));
    }

    [Test]
    public void GatewaySession_StaleOrDisconnectedValidationCannotNotifyMaintenance()
    {
        var state = new GatewaySessionState();
        var notifications = 0;
        state.ReadinessEstablished += () => notifications++;
        var interrupted = state.BeginValidation();
        state.Disconnect();
        Assert.That(state.CompleteValidation(interrupted, true), Is.False);
        Assert.That(notifications, Is.Zero);
        var retry = state.ObserveHeartbeat(true);
        Assert.That(retry, Is.Not.Null);
        Assert.That(state.CompleteValidation(retry!.Value, false), Is.False);
        Assert.That(notifications, Is.Zero);
        retry = state.ObserveHeartbeat(true);
        Assert.That(retry, Is.Not.Null);
        Assert.That(state.CompleteValidation(retry!.Value, true), Is.True);
        Assert.That(notifications, Is.EqualTo(1));
    }
    private static HttpException PlacementError(HttpStatusCode status = HttpStatusCode.BadRequest, string path = "parent_id")
    {
        // Discord.Net exposes structured errors read-only and constructs them internally.
        var error = (DiscordJsonError)Activator.CreateInstance(typeof(DiscordJsonError),
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null,
            [path, Array.Empty<DiscordError>()], null)!;
        return new HttpException(status, null!, DiscordErrorCode.InvalidFormBody, "Test placement failure.", [error]);
    }

    private static OverwritePermissions Find(Overwrite[] overwrites, ulong id, PermissionTarget target) =>
        overwrites.Single(x => x.TargetId == id && x.TargetType == target).Permissions;

    private sealed class ErrorResponseHandler(Func<HttpResponseMessage> response) : HttpMessageHandler
    {
        public int Requests { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Requests++;
            return Task.FromResult(response());
        }
    }

    private sealed class CountedErrorStream(byte[] bytes) : MemoryStream(bytes)
    {
        public override bool CanSeek => false;
        public int BytesRead { get; private set; }
        public Action? BeforeRead { get; init; }
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            BeforeRead?.Invoke();
            var read = await base.ReadAsync(buffer, ct);
            BytesRead += read;
            return read;
        }
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<string> Bodies { get; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Assert.That(request.RequestUri?.AbsoluteUri, Is.EqualTo("https://discord.com/api/v10/channels/10/messages"));
            Bodies.Add(await request.Content!.ReadAsStringAsync(ct));
            return Bodies.Count == 1
                ? new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = new StringContent("{\"retry_after\":0,\"global\":false}") }
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"id\":\"123\"}") };
        }
    }
}
