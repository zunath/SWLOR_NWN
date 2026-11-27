using FluentAssertions;
using NUnit.Framework;
using SWLOR.EngineSeamGenerator;
using SWLOR.NWN.API.Tests.Support;

namespace SWLOR.NWN.API.Tests.Generator;

/// <summary>
/// The NWNX plugin part of the generated engine seam checked into the repository. <see cref="CheckedInOutputTests"/> runs the
/// generator in check mode over NWScript and the plugins together; these add the plugin-specific guarantees.
/// </summary>
[TestFixture]
public class PluginCheckedInOutputTests
{
    private static string PluginFolder => Path.Combine(RepositoryRoot.Path, "SWLOR.NWN.API", "NWNX");

    [Test]
    public void EveryPluginWrapperFile_HasASeam_AndEverySeamHasAWrapperFile()
    {
        var generator = SeamDefinition.Discover(new DiskFileSystem(RepositoryRoot.Path))
            .Skip(1)
            .Select(d => d.StaticClassName)
            .ToList();
        var files = Directory.GetFiles(PluginFolder, "*Plugin.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Order(StringComparer.Ordinal)
            .ToList();

        generator.Should().Equal(files);
        PluginSeam.All.Select(s => s.Name).Should().Equal(files, "the static classes found by reflection are the wrapper files the generator found");
    }

    [Test]
    public void PluginPassThroughParts_NeverCallTheirRuntime()
    {
        // A pass-through body that reached its runtime would call itself when the runtime still holds the pass-through.
        var parts = Directory.GetFiles(Path.Combine(RepositoryRoot.Path, "SWLOR.NWN.API", "Generated", "NWNX"), "NwnCore*Plugin.cs", SearchOption.AllDirectories);

        parts.Should().HaveCountGreaterThanOrEqualTo(16);
        parts.Where(p => File.ReadAllText(p).Contains(Path.GetFileNameWithoutExtension(p).Replace("NwnCore", string.Empty) + "Runtime", StringComparison.Ordinal))
            .Should().BeEmpty();
    }

    [Test]
    public void PluginWrappers_DoNotCallTheEngineDirectly()
    {
        var files = Directory.GetFiles(PluginFolder, "*.cs", SearchOption.TopDirectoryOnly);

        files.Should().NotBeEmpty();
        files.Where(f => File.ReadAllText(f).Contains("global::NWN.Core", StringComparison.Ordinal)).Should().BeEmpty(
            "plugin wrappers reach the engine only through their runtime's Current; new wrapper bodies are moved by the generator");
    }

    [Test]
    public void PluginWrappersThatUseAnImportedCorePlugin_AlsoForwardEveryCall()
    {
        // AdministrationPlugin reaches NWN.Core.NWNX.AdminPlugin through a using directive instead of a global:: name.
        var text = File.ReadAllText(Path.Combine(PluginFolder, "AdministrationPlugin.cs"));

        text.Should().NotContain("AdminPlugin.", "its calls moved into NwnCoreAdministrationPlugin");
        text.Should().Contain("AdministrationPluginRuntime.Current.");
    }

    [Test]
    public void NestedTypesLeftStatic_AreExactlyTheKnownSet()
    {
        var nested = PluginSeam.All
            .SelectMany(seam => seam.Wrapper.GetNestedTypes().Select(t => seam.Name + "." + t.Name))
            .Order(StringComparer.Ordinal)
            .ToList();

        nested.Should().Equal("ObjectPlugin.LocalVariable", "UtilPlugin.HighResTimestamp", "UtilPlugin.WorldTime", "WeaponPlugin.DevastatingCriticalData");
    }
}
