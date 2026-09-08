using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Vibroblade;
using SWLOR.Game.Server.Feature.ChatCommandDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class AuthoredCombatAnimationTests
{
    [TestCase(typeof(ShieldBashAbilityDefinition), "ShieldBash", true)]
    [TestCase(typeof(RiotBladeAbilityDefinition), "RiotBlade", true)]
    [TestCase(typeof(ShieldWallAbilityDefinition), "ShieldWall", false)]
    [TestCase(typeof(CoveringStrikeAbilityDefinition), "CoveringStrike", false)]
    [TestCase(typeof(InvincibleAbilityDefinition), "Invincible", false)]
    [TestCase(typeof(RendingStrikeAbilityDefinition), "RendingStrike", false)]
    [TestCase(typeof(SavageCleaveAbilityDefinition), "SavageCleave", false)]
    public void EveryRankUsesItsInstalledClipAtTheCorrectCombatStage(Type definition, string name, bool queued)
    {
        var clip = AnimationPreviewChatCommand.Clips[name];
        var abilities = ((IAbilityListDefinition)Activator.CreateInstance(definition)!).BuildAbilities();
        abilities.Should().NotBeEmpty();
        foreach (var ability in abilities.Values)
        {
            (queued ? ability.QueuedAttackAnimation : ability.AuthoredAnimation).Should().BeSameAs(clip);
            ability.ActivationType.Should().Be(queued ? AbilityActivationType.Weapon : AbilityActivationType.Casted);
            ability.ImpactAnimationType.Should().Be(Animation.Invalid, "a landed or area hit must not start another swing");
        }
    }

    [Test]
    public void NativeAnimationAndOverwriteCallsReplaceAuthoredActivationMetadata()
    {
        var builder = new AbilityBuilder().Create(FeatType.ShieldWall1, SWLOR.Game.Server.Service.PerkService.PerkType.ShieldWall);
        builder.UsesAnimation(AuthoredAnimation.ShieldWall).UsesAnimation(Animation.LoopingPause);
        builder.Build().Values.Single().AuthoredAnimation.Should().BeNull();
        builder.UsesAnimation(AuthoredAnimation.ShieldWall).UsesAnimationOverwrite("pause2");
        builder.Build().Values.Single().AuthoredAnimation.Should().BeNull();
    }

    [Test]
    public void PolearmSwingsPlayQueuedClipsAndRestoreWithoutChangingReadyOrParry()
    {
        var runtime = new Runtime();
        runtime.Maps["plreadyr"] = "native_ready";
        runtime.Maps["plparryl"] = "native_parry";
        var playback = new QueuedAttackAnimationPlayback(runtime);
        var token = playback.Begin(1, AuthoredAnimation.ShieldBash);
        var swings = new[] { "plslashl", "plslashr", "plslasho", "plstab", "plcloseh", "plclosel", "plreach" };
        foreach (var swing in swings) runtime.Maps[swing].Should().Be(AuthoredAnimation.ShieldBash.Name);
        playback.Complete(1, token);
        foreach (var swing in swings) runtime.Maps[swing].Should().BeEmpty();
        runtime.Maps["plreadyr"].Should().Be("native_ready");
        runtime.Maps["plparryl"].Should().Be("native_parry");
    }

    [Test]
    public void QueuedSwingsRestoreOnConsumptionAndIgnoreSupersededTimeouts()
    {
        var runtime = new Runtime(); var playback = new QueuedAttackAnimationPlayback(runtime);
        var first = playback.Begin(1, AuthoredAnimation.ShieldBash);
        var second = playback.Begin(1, AuthoredAnimation.RiotBlade);
        playback.Complete(1, first); runtime.Timeouts[0]();
        runtime.Maps.Values.Should().OnlyContain(value => value == AuthoredAnimation.RiotBlade.Name);
        runtime.Maps.Keys.Should().NotContain(key => key.Contains("ready") || key.Contains("parry") || key == "throwr");
        playback.Complete(1, second);
        runtime.Maps.Values.Should().OnlyContain(value => value == "");
        runtime.Token.Should().BeEmpty();
    }

    [Test]
    public void InterruptedQueueStillRestoresItsSwingKeys()
    {
        var runtime = new Runtime(); var playback = new QueuedAttackAnimationPlayback(runtime);
        playback.Begin(1, AuthoredAnimation.ShieldBash);
        runtime.Timeouts.Single()();
        runtime.Maps.Values.Should().OnlyContain(value => value == "");
        runtime.Token.Should().BeEmpty();
    }

    [Test]
    public void PreviewIncludesAllCatalogMovesWithCurrentAbilities()
    {
        AnimationPreviewChatCommand.Clips.Keys.Should().BeEquivalentTo(
            ActiveAbilityAnimationCatalog.Entries.Select(entry => entry.Id));
    }

    [Test]
    public void DeathReleasesAuthoredMappingsBeforeSubdualResurrection()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln"))) root = root.Parent;
        root.Should().NotBeNull();
        var death = File.ReadAllText(Path.Combine(root!.FullName, "SWLOR.Game.Server", "Service", "Death.cs"));
        var stop = death.IndexOf("NamedAnimation.ClearOnDeath(player);", StringComparison.Ordinal);
        stop.Should().BeGreaterThan(death.IndexOf("var player = GetLastPlayerDied();", StringComparison.Ordinal));
        stop.Should().BeLessThan(death.IndexOf("EffectResurrection()", StringComparison.Ordinal),
            "subdual revives the same object immediately, so its old custom mappings must already be gone");
        var npc = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "CreatureDeathAnimation.cs"));
        npc.Should().Contain("NamedAnimation.ClearOnDeath(creature);").And.Contain("UsePerkFeat.ClearQueuedAbility(creature);");
        var clearQueue = death.IndexOf("UsePerkFeat.ClearQueuedAbility(player);", StringComparison.Ordinal);
        clearQueue.Should().BeGreaterThan(stop).And.BeLessThan(death.IndexOf("EffectResurrection()", StringComparison.Ordinal));
    }

    private sealed class Runtime : INamedAnimationRuntime
    {
        public string Token = "";
        public Dictionary<string, string> Maps = new();
        public List<Action> Timeouts = new();
        public bool IsValid(uint creature) => true;
        public string GetToken(uint creature) => Token;
        public void SetToken(uint creature, string token) => Token = token;
        public void Replace(uint creature, string source, string replacement) => Maps[source] = replacement;
        public ActionType CurrentAction(uint creature) => ActionType.Invalid;
        public void ClearActions(uint creature) => throw new InvalidOperationException("Swing remapping must not clear the action queue.");
        public void Schedule(float seconds, Action callback) => Timeouts.Add(callback);
    }
}
