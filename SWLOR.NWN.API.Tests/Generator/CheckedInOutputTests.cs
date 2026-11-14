using FluentAssertions;
using NUnit.Framework;
using SWLOR.EngineSeamGenerator;
using SWLOR.NWN.API.Tests.Support;

namespace SWLOR.NWN.API.Tests.Generator;

/// <summary>The generated engine seam checked into the repository is exactly what the generator produces from the wrappers.</summary>
[TestFixture]
public class CheckedInOutputTests
{
    [Test]
    public void GeneratorCheck_FindsNoDifference()
    {
        var log = new StringWriter();

        var exit = GeneratorRunner.Run(RepositoryRoot.Path, GeneratorMode.Check, null, log);

        exit.Should().Be(GeneratorRunner.Ok, "the checked-in output must be what the generator produces; run tools/Refactor/Apply-EngineSeam.ps1.\n" + log);
    }

    [Test]
    public void PassThroughParts_NeverCallTheRuntime()
    {
        // A pass-through body that reached NWScriptRuntime would call itself when the runtime still holds the pass-through.
        var parts = Directory.GetFiles(Path.Combine(RepositoryRoot.Path, "SWLOR.NWN.API", "Generated", "NwnCoreNWScript"), "*.cs", SearchOption.AllDirectories);

        parts.Should().NotBeEmpty();
        parts.Where(p => File.ReadAllText(p).Contains("NWScriptRuntime", StringComparison.Ordinal)).Should().BeEmpty();
    }

    [Test]
    public void StaticWrappers_DoNotCallTheEngineDirectly()
    {
        var wrapperFolders = new[] { "NWScript", "NWScriptCustom" }
            .Select(folder => Path.Combine(RepositoryRoot.Path, "SWLOR.NWN.API", folder))
            .ToList();
        var files = wrapperFolders.SelectMany(folder => Directory.GetFiles(folder, "*.cs", SearchOption.TopDirectoryOnly)).ToList();

        files.Should().NotBeEmpty();
        files.Where(f => File.ReadAllText(f).Contains("global::NWN.Core", StringComparison.Ordinal)).Should().BeEmpty(
            "wrappers reach the engine only through NWScriptRuntime.Current; new wrapper bodies are moved by the generator");
    }

    [Test]
    public void ConstantsLeftStatic_AreExactlyTheKnownSet()
    {
        var constants = typeof(SWLOR.NWN.API.NWScript.NWScript)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly)
            .Where(f => f.IsLiteral)
            .Select(f => f.Name)
            .ToList();

        constants.Should().HaveCount(31);
        constants.Should().Contain(new[] { "OBJECT_INVALID", "PI", "NumberOfInventorySlots", "EVENT_HEARTBEAT", "TILESET_RESREF_UNDERDARK" });
    }
}
