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
        // Run the real helper with a blocked creature action queue and observable client packets.
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        var root = directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
        var production = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(
            root, "SWLOR.Game.Server", "Feature", "UsePerkFeat.cs"))).GetRoot();
        var helper = production.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(method => method.Identifier.ValueText == "PlayAbilitySound");
        var source = $$"""
            using System;
            using System.Collections.Generic;
            using static World;

            public static class UsePerkFeat
            {
                {{helper}}
                public static void Play(uint caster, string sound) => PlayAbilitySound(caster, sound);
            }
            public static class World
            {
                public const uint OBJECT_INVALID = 0x7F000000;
                private static readonly Dictionary<uint, uint> Areas = new();
                private static readonly List<(uint Player, string Sound, uint Source)> Packets = new();
                private static readonly Queue<Action> Actions = new();
                private static int PlayerIndex;
                private static readonly uint[] Players = { 10, 20, 30 };

                public static bool GetIsObjectValid(uint obj) => obj != OBJECT_INVALID &&
                    (Areas.ContainsKey(obj) || obj is 1000 or 2000);
                public static uint GetArea(uint obj) => Areas[obj];
                public static uint GetFirstPC() { PlayerIndex = 0; return Players[0]; }
                public static uint GetNextPC() => ++PlayerIndex < Players.Length ? Players[PlayerIndex] : OBJECT_INVALID;
                public static void AssignCommand(uint obj, Action action) => Actions.Enqueue(action);
                public static void PlaySound(string sound) => throw new InvalidOperationException("Creature audio uses the action queue.");
                public static class PlayerPlugin
                {
                    public static void PlaySound(uint player, string sound, uint source) => Packets.Add((player, sound, source));
                }

                public static (uint Player, string Sound, uint Source)[] Play(uint caster, string sound, bool hasArea)
                {
                    Areas.Clear();
                    Areas[10] = Areas[20] = Areas[100] = 1000;
                    Areas[30] = 2000;
                    if (Areas.ContainsKey(caster) && !hasArea) Areas[caster] = OBJECT_INVALID;
                    Packets.Clear();
                    Actions.Clear();
                    UsePerkFeat.Play(caster, sound);
                    return Packets.ToArray();
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
            yield return new TestCaseData(ability.ImpactSound).SetName($"{feat}_SoundBypassesBusyCasterQueue");
    }

    [TestCaseSource(nameof(ReportedForceRanks))]
    public void ReportedForcePowers_PlayImmediatelyForCasterAndSameAreaObservers(string sound)
    {
        sound.Should().NotBeNullOrWhiteSpace();
        Play(10, sound).Should().BeEquivalentTo(new[] { (10u, sound, 10u), (20u, sound, 10u) },
            "the caster and observers should hear one positional cue even while the caster's action queue is blocked");
    }

    [Test]
    public void NpcCast_PlaysAtTheNpcForSameAreaPlayersOnly()
    {
        Play(100, "ksfx_frc_drain").Should().BeEquivalentTo(new[]
        {
            (10u, "ksfx_frc_drain", 100u), (20u, "ksfx_frc_drain", 100u)
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void UnconfiguredSound_SendsNoPackets(string sound)
    {
        Play(10, sound).Should().BeEmpty();
    }

    [TestCase(0x7F000000u, true)]
    [TestCase(999u, true)]
    [TestCase(10u, false)]
    public void MissingCasterOrArea_SendsNoPackets(uint caster, bool hasArea)
    {
        Play(caster, "ksfx_frc_drain", hasArea).Should().BeEmpty();
    }

    private (uint Player, string Sound, uint Source)[] Play(uint caster, string sound, bool hasArea = true) =>
        ((uint Player, string Sound, uint Source)[])_play.Invoke(null, [caster, sound, hasArea])!;
}
