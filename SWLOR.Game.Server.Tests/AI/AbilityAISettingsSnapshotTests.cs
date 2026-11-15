using System.Runtime.CompilerServices;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Tests.Support;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.AI;

/// <summary>
/// Locks the AI settings of every registered ability. The snapshot was recorded from the members that
/// <see cref="AbilityDetail"/> held before they moved to Creatures (P5-1d), so it proves the move changed no
/// ability's target selector, score or hit point cost: each delegate is identified by the method it runs.
/// </summary>
public class AbilityAISettingsSnapshotTests
{
    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void InstallFacades() => _facadeScope = _facades.Install();

    [TearDown]
    public void RemoveFacades() => _facadeScope.Dispose();

    private static string SnapshotPath([CallerFilePath] string source = "") =>
        Path.Combine(Path.GetDirectoryName(source)!, "Snapshots", "AbilityAISettings.txt");

    private static string Describe(Delegate? value) =>
        value == null ? "-" : $"{value.Method.DeclaringType?.FullName}.{value.Method.Name}";

    [Test]
    public void EveryAbilityKeepsItsAISettings()
    {
        AbilityCache.Load();
        var text = new StringBuilder();
        foreach (var (feat, ability) in Ability.GetAllAbilityDetails().OrderBy(entry => (int)entry.Key))
        {
            text.Append((int)feat).Append(' ').Append(feat)
                .Append("\tselector=").Append(Describe(ability.GetAITargetSelector()))
                .Append("\tscore=").Append(Describe(ability.GetAIScore()))
                .Append("\tcost=").Append(Describe(ability.GetAIHitPointCostPercent()))
                .Append('\n');
        }

        if (Environment.GetEnvironmentVariable("SWLOR_UPDATE_SNAPSHOTS") == "1")
            File.WriteAllText(SnapshotPath(), text.ToString());

        text.ToString().Should().Be(File.ReadAllText(SnapshotPath()).Replace("\r\n", "\n"));
    }
}
