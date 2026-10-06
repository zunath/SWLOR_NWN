using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Force;
using SWLOR.Game.Server.Service.AbilityService;

namespace SWLOR.Game.Server.Tests.Feature;

public class AbilitySoundPlaybackTests
{
    private MethodInfo _play = null!;

    [OneTimeSetUp]
    public void CompilePlaybackHarness()
    {
        // Run the real helpers with observable script context and a slow creature action queue.
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        var root = directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
        var production = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(
            root, "SWLOR.Game.Server", "Feature", "UsePerkFeat.cs"))).GetRoot();
        var helpers = production.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(method => method.Identifier.ValueText is "PlayAbilitySound" or "ResumeAttackAfterImpact")
            .Select(method => method.ToFullString());
        var source = $$"""
            using System;
            using System.Collections.Generic;
            using static World;

            public static class UsePerkFeat
            {
                {{string.Join(Environment.NewLine, helpers)}}
                public static void Play(uint caster, string sound) => PlayAbilitySound(caster, sound);
                public static void Resume(uint caster, string sound) =>
                    ResumeAttackAfterImpact(caster, 50, new AbilityDetail { ImpactSound = sound });
            }
            public class AbilityDetail
            {
                public string ImpactSound;
            }
            public static class AbilityAnimationBinding
            {
                public static object ActivationClip(AbilityDetail ability, uint caster) => null;
            }
            public static class World
            {
                public static uint OBJECT_SELF => Self;
                private static readonly List<string> Events = new();
                private static readonly Queue<Action> Actions = new();
                private static readonly Queue<Action> Timers = new();
                private static uint Self;

                public static bool GetIsObjectValid(uint obj) => obj is 10 or 100;
                public static bool GetIsPC(uint obj) => obj == 10;
                public static void AssignCommand(uint obj, Action action) =>
                    throw new InvalidOperationException("Audio must not defer another script command.");
                public static class ServerManager
                {
                    public static readonly ScriptExecutor Executor = new();
                }
                public class ScriptExecutor
                {
                    public void ExecuteInScriptContext(Action action, uint caster)
                    {
                        var previous = Self;
                        Self = caster;
                        try { action(); }
                        finally { Self = previous; }
                    }
                }
                public static void ActionDoCommand(Action action) => Actions.Enqueue(action);
                public static void PlaySound(string sound)
                {
                    var caster = Self;
                    Actions.Enqueue(() => Events.Add($"Sound:{caster}:{sound}"));
                }
                public static void ResumeAttackAfterDelay(uint caster, uint target, float delay) => Timers.Enqueue(() =>
                {
                    if (!GetIsPC(caster)) Actions.Clear();
                    Events.Add($"Attack:{caster}:{target}");
                });

                public static string[] Play(uint caster, string sound, bool resumeAttack, bool casterContext)
                {
                    Events.Clear();
                    Actions.Clear();
                    Timers.Clear();
                    var originalContext = Self = casterContext ? caster : 20;
                    UsePerkFeat.Play(caster, sound);
                    if (resumeAttack) UsePerkFeat.Resume(caster, sound);
                    if (Self != originalContext) throw new InvalidOperationException("Caller context was not restored.");
                    // The resume timer may fire before the engine processes the queued sound.
                    while (Timers.TryDequeue(out var timer)) timer();
                    while (Actions.TryDequeue(out var action)) action();
                    while (Timers.TryDequeue(out var timer)) timer();
                    return Events.ToArray();
                }
            }
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator).Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("AbilitySoundPlaybackHarness",
            [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        _play = Assembly.Load(stream.ToArray()).GetType("World")!.GetMethod("Play")!;
    }

    public static IEnumerable<TestCaseData> ReportedForceRanks()
    {
        IAbilityListDefinition[] definitions =
        [
            new ForceDrainAbilityDefinition(),
            new ForceSparkAbilityDefinition(),
            new CreepingTerrorAbilityDefinition()
        ];
        foreach (var definition in definitions)
        foreach (var (feat, ability) in definition.BuildAbilities())
            yield return new TestCaseData(ability.ImpactSound).SetName($"{feat}_SoundDispatchesBeforeCombatResumes");
    }

    [TestCaseSource(nameof(ReportedForceRanks))]
    public void ReportedForcePowers_DispatchNativeSoundBeforeCombatResumes(string sound)
    {
        sound.Should().NotBeNullOrWhiteSpace();
        Play(10, sound, resumeAttack: true).Should().Equal(new[] { $"Sound:10:{sound}", "Attack:10:50" },
            "resumed combat must not overtake the native sound action, even when the action queue is slow");
    }

    [Test]
    public void NpcCast_DispatchesSoundBeforeTheCombatResetClearsActions()
    {
        Play(100, "ksfx_frc_drain", resumeAttack: true).Should().Equal("Sound:100:ksfx_frc_drain", "Attack:100:50");
    }

    [Test]
    public void CompanionAbilityFromOwnerContext_PlaysAtTheCompanionAndRestoresCallerContext()
    {
        Play(100, "ksfx_frc_drain", resumeAttack: true, casterContext: false)
            .Should().Equal("Sound:100:ksfx_frc_drain", "Attack:100:50");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void UnconfiguredSound_DoesNotQueueAudio(string sound)
    {
        Play(10, sound).Should().BeEmpty();
    }

    [TestCase(0x7F000000u)]
    [TestCase(999u)]
    public void MissingCaster_DoesNotQueueAudio(uint caster)
    {
        Play(caster, "ksfx_frc_drain").Should().BeEmpty();
    }

    [TestCase(10u)]
    [TestCase(100u)]
    public void AbilityWithoutImpactSound_ResumesCombatNormally(uint caster)
    {
        Play(caster, "", resumeAttack: true).Should().Equal($"Attack:{caster}:50");
    }

    private string[] Play(uint caster, string sound, bool resumeAttack = false, bool casterContext = true) =>
        (string[])_play.Invoke(null, [caster, sound, resumeAttack, casterContext])!;
}
