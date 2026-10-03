using System.Globalization;
using System.Net;
using System.Text.Json;
using Discord;
using NUnit.Framework;
using SWLOR.DiscordBot.Core;
using SWLOR.DiscordBot.Discord;
using SWLOR.DiscordBot.Hosting;

namespace SWLOR.DiscordBot.Tests;

[TestFixture]
public sealed class DiscordAdapterTests
{
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
    public void ClosedRequesterVisibility_IsExplicitAndAllOrdinaryWritersFreeze(bool requesterRead)
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

    [Test]
    public void Privacy_FailsClosedWhenExplicitOverwriteCountExceedsDiscordLimit()
    {
        var inherited = Enumerable.Range(10, 100).Select(x => new Overwrite((ulong)x, PermissionTarget.Role, new OverwritePermissions(viewChannel: PermValue.Allow)));
        Assert.Throws<InvalidOperationException>(() => DiscordOperations.BuildOverwrites(1, 2, 3, new ulong[] { 4 }, inherited, true, true, true));
    }

    [Test]
    public void SelfSelectedFactionRoles_RejectNativeStaffPermissions()
    {
        Assert.That(DiscordOperations.HasStaffPermissions(new GuildPermissions(administrator: true)), Is.True);
        Assert.That(DiscordOperations.HasStaffPermissions(new GuildPermissions(manageGuild: true)), Is.True);
        Assert.That(DiscordOperations.HasStaffPermissions(new GuildPermissions(manageRoles: true)), Is.True);
        Assert.That(DiscordOperations.HasStaffPermissions(new GuildPermissions(manageChannels: true)), Is.True);
        Assert.That(DiscordOperations.HasStaffPermissions(new GuildPermissions(kickMembers: true)), Is.True);
        Assert.That(DiscordOperations.HasStaffPermissions(new GuildPermissions(banMembers: true)), Is.True);
        Assert.That(DiscordOperations.HasStaffPermissions(new GuildPermissions(moderateMembers: true)), Is.True);
        Assert.That(DiscordOperations.HasStaffPermissions(new GuildPermissions(manageWebhooks: true)), Is.True);
        Assert.That(DiscordOperations.HasStaffPermissions(new GuildPermissions(viewChannel: true, sendMessages: true)), Is.False);
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
    private static OverwritePermissions Find(Overwrite[] overwrites, ulong id, PermissionTarget target) =>
        overwrites.Single(x => x.TargetId == id && x.TargetType == target).Permissions;

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
