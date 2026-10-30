using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class EquipmentAnimationPlaybackTests
{
    [Test]
    public void EquipmentChangeReleasesAnActiveRestrictedPoseWithoutCancellingTheCast()
    {
        var runtime = new Runtime();
        var playback = new NamedAnimationPlayback(runtime);
        var request = playback.ReserveEquipmentPlayback(1);
        var token = playback.Begin(1, AuthoredAnimation.BastionStance, 2, true, equipmentRequest: request);
        runtime.Maps[NamedAnimationPlayback.LoopSource].Should().Be(AuthoredAnimation.BastionStance.Name);
        playback.InvalidateEquipmentPlayback(1).Should().BeTrue();
        playback.IsCurrent(1, token).Should().BeFalse();
        runtime.Maps.Values.Should().OnlyContain(value => value == "");
        runtime.ActionsCleared.Should().Be(0, "equipment changes must not cancel an ability or other queued gameplay actions");
    }

    [Test]
    public void EquipmentChangeInvalidatesARequestBeforeItsDeferredBegin()
    {
        var runtime = new Runtime();
        var playback = new NamedAnimationPlayback(runtime);
        var request = playback.ReserveEquipmentPlayback(1);
        playback.InvalidateEquipmentPlayback(1).Should().BeFalse("no pose started yet");
        playback.CanStartEquipmentPlayback(1, request).Should().BeFalse();
        playback.Begin(1, AuthoredAnimation.SoulDevourerStance, 2, equipmentRequest: request).Should().BeNull();
        runtime.Maps.Should().BeEmpty();
    }

    [Test]
    public void ANewPendingRequestDoesNotHideAnOlderRestrictedPoseFromEquipmentChanges()
    {
        var runtime = new Runtime();
        var playback = new NamedAnimationPlayback(runtime);
        playback.Begin(1, AuthoredAnimation.BastionStance, 2, equipmentRequest: playback.ReserveEquipmentPlayback(1));
        var pending = playback.ReserveEquipmentPlayback(1);
        playback.InvalidateEquipmentPlayback(1).Should().BeTrue();
        playback.CanStartEquipmentPlayback(1, pending).Should().BeFalse();
        runtime.Maps.Values.Should().OnlyContain(value => value == "");
    }

    [Test]
    public void EquipmentChangesReleaseTheRestrictedExitWithoutWaitingForGraceTimeout()
    {
        var runtime = new Runtime();
        var playback = new NamedAnimationPlayback(runtime);
        var token = playback.Begin(1, AuthoredAnimation.BastionStance, 2, equipmentRequest: playback.ReserveEquipmentPlayback(1));
        playback.Complete(1, token);
        playback.InvalidateEquipmentPlayback(1).Should().BeTrue();
        runtime.Maps.Values.Should().OnlyContain(value => value == "");
    }

    [Test]
    public void UnrestrictedPlaybackAndNewerMappingsSurviveOldEquipmentOwnership()
    {
        var runtime = new Runtime();
        var playback = new NamedAnimationPlayback(runtime);
        var request = playback.ReserveEquipmentPlayback(1);
        playback.Begin(1, AuthoredAnimation.BastionStance, 2, true, equipmentRequest: request);
        var oldCallbacks = runtime.Callbacks.ToArray();
        var newer = playback.Begin(1, AuthoredAnimation.Provoke, 1);
        playback.InvalidateEquipmentPlayback(1).Should().BeFalse();
        foreach (var callback in oldCallbacks) callback();
        playback.IsCurrent(1, newer).Should().BeTrue();
        runtime.Maps[NamedAnimationPlayback.LoopSource].Should().Be(AuthoredAnimation.Provoke.Name);
        playback.CanStartEquipmentPlayback(1, request).Should().BeFalse();
    }

    [TestCase(AnimationWeaponHold.Rifle, false)] [TestCase(AnimationWeaponHold.Rifle, true)]
    [TestCase(AnimationWeaponHold.Shoulder, false)] [TestCase(AnimationWeaponHold.Shoulder, true)]
    public void EquipmentChangesReleaseUnrestrictedHoldMappingsForTheOldWeapon(AnimationWeaponHold hold, bool alreadyEnding)
    {
        var runtime = new Runtime();
        var playback = new NamedAnimationPlayback(runtime);
        var token = playback.Begin(1, AuthoredAnimation.ConduitStance, 2, true, weaponHold: hold);
        if (alreadyEnding) playback.Complete(1, token);

        playback.InvalidateEquipmentPlayback(1).Should().BeTrue("the hold aliases would rewrite the new weapon's hold layer");
        runtime.Maps.Values.Should().OnlyContain(value => value == "");
        playback.IsCurrent(1, token).Should().BeFalse();
        playback.InvalidateEquipmentPlayback(1).Should().BeFalse();
    }

    [Test]
    public void EquipmentChangesKeepAnUnheldClipThatReplacedAHeldOne()
    {
        var runtime = new Runtime();
        var playback = new NamedAnimationPlayback(runtime);
        playback.Begin(1, AuthoredAnimation.ConduitStance, 2, true, weaponHold: AnimationWeaponHold.Shoulder);
        var unheld = playback.Begin(1, AuthoredAnimation.Provoke, 1);

        playback.InvalidateEquipmentPlayback(1).Should().BeFalse();
        playback.IsCurrent(1, unheld).Should().BeTrue();
        runtime.Maps[NamedAnimationPlayback.LoopSource].Should().Be(AuthoredAnimation.Provoke.Name);
    }

    [Test]
    public void NativePlaybackAndDeathInvalidatePendingRequests()
    {
        var runtime = new Runtime();
        var playback = new NamedAnimationPlayback(runtime);
        var request = playback.ReserveEquipmentPlayback(1);
        playback.BeginNative(1);
        playback.CanStartEquipmentPlayback(1, request).Should().BeFalse();
        request = playback.ReserveEquipmentPlayback(1);
        playback.ClearOnDeath(1);
        playback.CanStartEquipmentPlayback(1, request).Should().BeFalse();
    }

    [Test]
    public void GameplayExecutionPathsCannotBypassEquipmentAwareBindingOrPlaybackOwnership()
    {
        var activation = Read("SWLOR.Game.Server/Feature/UsePerkFeat.cs");
        var activationBody = activation.DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single(method => method.Identifier.Text == "PlayActivationAnimation");
        AssertCreatureBinding(activationBody.DescendantNodes().OfType<InvocationExpressionSyntax>(), "ActivationClip");
        AssertCreatureBinding(activationBody.DescendantNodes().OfType<InvocationExpressionSyntax>(), "ActivationType");
        AssertEquipmentPlayback(activationBody.DescendantNodes().OfType<InvocationExpressionSyntax>(), "NamedAnimation.Play", "ability.AnimationEquipmentRequirement");
        AssertEquipmentPlayback(activationBody.DescendantNodes().OfType<InvocationExpressionSyntax>(), "NamedAnimation.Queue", "ability.AnimationEquipmentRequirement");
        var queued = activation.DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.Text == "QueueWeaponAbility");
        AssertCreatureBinding(queued.DescendantNodes().OfType<InvocationExpressionSyntax>(), "QueuedClip");
        var impact = Read("SWLOR.Game.Server/Service/Ability.cs").DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(method => method.Identifier.Text == "PlayCombatImpactAnimation");
        AssertCreatureBinding(impact.DescendantNodes().OfType<InvocationExpressionSyntax>(), "ImpactClip");
        AssertEquipmentPlayback(impact.DescendantNodes().OfType<InvocationExpressionSyntax>(), "NamedAnimation.Play", "trackedAbility.AnimationEquipmentRequirement");
        var playback = Read("SWLOR.Game.Server/Service/NamedAnimation.cs");
        var handler = playback.DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.Text == "OnEquipmentChanging");
        handler.AttributeLists.ToString().Should().Contain("OnSWLORItemEquipValidBefore").And.Contain("OnItemUnequipBefore");
        handler.Body!.ToString().Should().Contain("InvalidateEquipmentPlayback").And.Contain("PlayNativePreview");
    }

    [Test]
    public void QueuedPlaybackChoosesTheHoldAndCarrierWhenItStarts()
    {
        var queue = Read("SWLOR.Game.Server/Service/NamedAnimation.cs").DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(method => method.Identifier.Text == "Queue");
        var started = queue.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Single(call => call.Expression.ToString() == "ActionDoCommand" && call.ToString().Contains("Playback.Begin"));
        var startedCalls = started.DescendantNodes().OfType<InvocationExpressionSyntax>().Select(call => call.Expression.ToString()).ToArray();
        startedCalls.Should().Contain("AbilityAnimationEquipment.ReadHold",
            "equipment can change while the clip waits behind earlier actions");
        startedCalls.Should().Contain("PlayAnimation", "the carrier must match the mappings installed at start");
        queue.DescendantNodes().OfType<InvocationExpressionSyntax>().Select(call => call.Expression.ToString())
            .Should().NotContain("ActionPlayAnimation", "a carrier queued at selection cannot follow a later weapon change")
            .And.Contain("ActionWait");
    }

    private static void AssertCreatureBinding(IEnumerable<InvocationExpressionSyntax> calls, string method)
    {
        var matches = calls.Where(call => call.Expression.ToString().EndsWith("AbilityAnimationBinding." + method)).ToArray();
        matches.Should().NotBeEmpty(method);
        foreach (var call in matches)
            call.ArgumentList.Arguments[1].Expression.ToString().Should().Be("activator",
                "passing GetIsPC(activator) selects the boolean overload and silently bypasses equipment validation");
    }

    private static void AssertEquipmentPlayback(IEnumerable<InvocationExpressionSyntax> calls, string method, string requirement)
    {
        var matches = calls.Where(call => call.Expression.ToString() == method).ToArray();
        matches.Should().NotBeEmpty(method);
        foreach (var call in matches)
            call.ArgumentList.Arguments.Should().Contain(arg => arg.Expression.ToString() == requirement,
                "restricted playback must retain ownership until completion or equipment change");
    }

    private static CompilationUnitSyntax Read(string path)
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory != null; directory = directory.Parent)
        {
            var file = Path.Combine(directory.FullName, path);
            if (File.Exists(file)) return CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetCompilationUnitRoot();
        }
        throw new FileNotFoundException(path);
    }

    private sealed class Runtime : INamedAnimationRuntime
    {
        public string Token = "";
        public int ActionsCleared;
        public Dictionary<string, string> Maps = new();
        public List<Action> Callbacks = new();
        public bool IsValid(uint creature) => true;
        public string GetToken(uint creature) => Token;
        public void SetToken(uint creature, string token) => Token = token;
        public void Replace(uint creature, string source, string replacement) => Maps[source] = replacement;
        public ActionType CurrentAction(uint creature) => ActionType.Invalid;
        public void ClearActions(uint creature) => ActionsCleared++;
        public void Schedule(float seconds, Action callback) => Callbacks.Add(callback);
    }
}
