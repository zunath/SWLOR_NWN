using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.CLI;
using System.Diagnostics;
using System.Text;

namespace SWLOR.CLI.Tests;

[TestFixture]
public sealed class ModulePaletteRefresherTests
{
    private string _moduleRoot = null!;
    private string _outputRoot = null!;

    [SetUp]
    public void SetUp()
    {
        var testRoot = Path.Combine(
            Path.GetTempPath(),
            "SWLOR.CLI.Tests",
            Guid.NewGuid().ToString("N"));
        _moduleRoot = Path.Combine(testRoot, "Module");
        _outputRoot = Path.Combine(testRoot, "refreshed");
        Directory.CreateDirectory(Path.Combine(_moduleRoot, "itp"));
    }

    [TearDown]
    public void TearDown()
    {
        var testRoot = Directory.GetParent(_moduleRoot)?.FullName;
        if (testRoot != null && Directory.Exists(testRoot))
            Directory.Delete(testRoot, recursive: true);
    }

    [Test]
    public void CliEntryPointsBuildAndRunTheCurrentSource()
    {
        var repositoryRoot = FindRepositoryRoot().FullName;
        var runner = File.ReadAllText(Path.Combine(repositoryRoot, "tools", "SWLOR.CLI", "RunCLI.cmd"));
        var packCommand = File.ReadAllText(Path.Combine(repositoryRoot, "Module", "PackModule.cmd"));
        var serverProject = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "SWLOR.Game.Server",
            "SWLOR.Game.Server.csproj"));
        var deployBuild = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "SWLOR.CLI",
            "DeployBuild.cs"));

        runner.Should().Contain("dotnet build");
        runner.Should().Contain("-c Release");
        runner.Should().Contain("-p:RunPostBuildEvent=Never");
        runner.Should().Contain("SWLOR.CLI.dll");
        deployBuild.Should().Contain("../SWLOR.Game.Server/bin/Release/net10.0/");
        deployBuild.Should().NotContain("../SWLOR.Game.Server/bin/Debug/net10.0/");
        packCommand.Should().Contain("RunCLI.cmd");
        serverProject.Should().Contain("RunCLI.cmd");
        serverProject.Should().NotContain("tools\\SWLOR.CLI\\SWLOR.CLI.exe");
        File.Exists(Path.Combine(repositoryRoot, "tools", "SWLOR.CLI", "SWLOR.CLI.exe"))
            .Should().BeFalse("the committed executable can silently fall behind the CLI source");
    }

    [Test]
    public void Refresh_PreservesCategoryTreeAndSourceWhileReconcilingItemDescriptors()
    {
        var palettePath = WritePalette(
            "itempalcus",
            Terminal(
                1,
                "Original category",
                NamedLeaf("existing", "Old name"),
                NamedLeaf("orphan", "Deleted blueprint"),
                NamedLeaf("moved", "Old category")),
            Terminal(2, "Destination category"));
        var originalPalette = File.ReadAllBytes(palettePath);

        WriteBlueprint("uti", "existing", "PaletteID", 1, "LocalizedName", LocString("New name"));
        WriteBlueprint("uti", "added_z", "PaletteID", 1, "LocalizedName", LocString("Zed"));
        WriteBlueprint("uti", "added_a", "PaletteID", 1, "LocalizedName", LocString(strRef: 4242));
        WriteBlueprint("uti", "moved", "PaletteID", 2, "LocalizedName", LocString("Moved"));
        WriteBlueprint("uti", "unknown", "PaletteID", 9, "LocalizedName", LocString("No category"));

        var refresh = ModulePaletteRefresher.Refresh(_moduleRoot, _outputRoot);

        File.ReadAllBytes(palettePath).Should().Equal(originalPalette,
            "packing refreshes temporary copies rather than dirtying the module source");
        var result = refresh.Results.Should().ContainSingle().Subject;
        result.Should().Be(new PaletteRefreshResult(
            "itempalcus",
            Included: 4,
            Added: 3,
            Removed: 2,
            Updated: 1,
            MissingCategory: 1));

        var output = LoadOutput("itempalcus");
        var categories = TerminalCategories(output);
        categories[1]["NAME"]!["value"]!.Value<string>().Should().Be("Original category");

        var first = Descriptors(categories[1]);
        first.Select(ResRef).Should().Equal("existing", "added_a", "added_z");
        first[0]["NAME"]!["value"]!.Value<string>().Should().Be("New name");
        first[1]["NAME"].Should().BeNull();
        first[1]["STRREF"]!["value"]!.Value<uint>().Should().Be(4242);

        Descriptors(categories[2]).Select(ResRef).Should().Equal("moved");
    }

    [Test]
    public void Refresh_ReconcilesDescriptorsInsideNestedIdCategories()
    {
        WritePalette(
            "placeablepalcus",
            Terminal(
                100,
                "Parent category",
                Terminal(
                    23,
                    "Nested source",
                    NamedLeaf("existing", "Old name"),
                    NamedLeaf("orphan", "Deleted blueprint"),
                    NamedLeaf("moved", "Old category")),
                Terminal(24, "Nested destination")));

        WriteBlueprint("utp", "existing", "PaletteID", 23, "LocName", LocString("New name"));
        WriteBlueprint("utp", "added", "PaletteID", 23, "LocName", LocString("Added"));
        WriteBlueprint("utp", "moved", "PaletteID", 24, "LocName", LocString("Moved"));

        var refresh = ModulePaletteRefresher.Refresh(_moduleRoot, _outputRoot);

        refresh.Results.Should().ContainSingle().Which.Should().Be(
            new PaletteRefreshResult(
                "placeablepalcus",
                Included: 3,
                Added: 2,
                Removed: 2,
                Updated: 1,
                MissingCategory: 0));
        var categories = TerminalCategories(LoadOutput("placeablepalcus"));
        var source = Descriptors(categories[23]);
        source.Select(ResRef).Should().Equal("existing", "added");
        source[0]["NAME"]!["value"]!.Value<string>().Should().Be("New name");
        Descriptors(categories[24]).Select(ResRef).Should().Equal("moved");
    }

    [Test]
    public void Refresh_DecodesWindows1252BlueprintNamesWithoutReplacementCharacters()
    {
        WritePalette("placeablepalcus", Terminal(7, "Custom"));
        var blueprint = Blueprint("PaletteID", 7, "LocName", LocString("Café’s Crate"));
        WriteNwnJson(
            Path.Combine(_moduleRoot, "utp", "encoded.utp.json"),
            blueprint);

        ModulePaletteRefresher.Refresh(_moduleRoot, _outputRoot);

        var descriptor = Descriptors(TerminalCategories(LoadOutput("placeablepalcus"))[7])
            .Should().ContainSingle().Subject;
        descriptor["NAME"]!["value"]!.Value<string>().Should().Be("Café’s Crate");
    }

    [TestCase("doorpalcus", "utd", "PaletteID", "LocName")]
    [TestCase("encounterpalcus", "ute", "PaletteID", "LocalizedName")]
    [TestCase("itempalcus", "uti", "PaletteID", "LocalizedName")]
    [TestCase("placeablepalcus", "utp", "PaletteID", "LocName")]
    [TestCase("soundpalcus", "uts", "PaletteID", "LocName")]
    [TestCase("storepalcus", "utm", "ID", "LocName")]
    [TestCase("triggerpalcus", "utt", "PaletteID", "LocalizedName")]
    [TestCase("waypointpalcus", "utw", "PaletteID", "LocalizedName")]
    public void Refresh_UsesTheAuroraCategoryAndNameFieldsForEveryNonCreaturePalette(
        string paletteName,
        string extension,
        string categoryField,
        string nameField)
    {
        WritePalette(paletteName, Terminal(7, "Custom"));
        WriteBlueprint(extension, "probe", categoryField, 7, nameField, LocString("Probe name"));

        var refresh = ModulePaletteRefresher.Refresh(_moduleRoot, _outputRoot);

        refresh.Results.Should().ContainSingle()
            .Which.Included.Should().Be(1);
        var descriptor = Descriptors(TerminalCategories(LoadOutput(paletteName))[7]).Should()
            .ContainSingle().Subject;
        ResRef(descriptor).Should().Be("probe");
        descriptor["NAME"]!["value"]!.Value<string>().Should().Be("Probe name");
    }

    [Test]
    public void Refresh_CreatureDescriptorIncludesFirstNameChallengeRatingAndFactionName()
    {
        WritePalette("creaturepalcus", Terminal(3, "Creatures"));
        WriteFactionFile((0, "PC"), (1, "Hostile"));

        var creature = Blueprint("PaletteID", 3, "FirstName", LocString("Banecaller Vex"));
        creature["LastName"] = LocString("Ignored by Aurora");
        creature["ChallengeRating"] = Field("float", 118.0);
        creature["FactionID"] = Field("word", 1);
        WriteJson(Path.Combine(_moduleRoot, "utc", "banecaller.utc.json"), creature);

        ModulePaletteRefresher.Refresh(_moduleRoot, _outputRoot);

        var descriptor = Descriptors(TerminalCategories(LoadOutput("creaturepalcus"))[3])
            .Should().ContainSingle().Subject;
        descriptor.Properties().Select(property => property.Name)
            .Should().Equal("__struct_id", "CR", "FACTION", "NAME", "RESREF");
        descriptor["CR"]!["value"]!.Value<double>().Should().Be(118.0);
        descriptor["FACTION"]!["value"]!.Value<string>().Should().Be("Hostile");
        descriptor["NAME"]!["value"]!.Value<string>().Should().Be("Banecaller Vex");
    }

    [Test]
    public void Refresh_RepositoryModuleProducesConvertiblePalettesWithoutChangingSources()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..",
            "..",
            "..",
            ".."));
        var moduleRoot = Path.Combine(repositoryRoot, "Module");
        var paletteDirectory = Path.Combine(moduleRoot, "itp");
        Directory.Exists(paletteDirectory).Should().BeTrue(
            "the repository module is the integration corpus for its packer");

        var sources = Directory.GetFiles(paletteDirectory, "*palcus.itp.json")
            .ToDictionary(path => path, File.ReadAllBytes);
        var refresh = ModulePaletteRefresher.Refresh(moduleRoot, _outputRoot);

        refresh.Results.Should().HaveCount(9);
        refresh.Replacements.Should().HaveCount(9);
        foreach (var result in refresh.Results)
        {
            TestContext.Progress.WriteLine(
                $"{result.PaletteName}: included={result.Included}, added={result.Added}, " +
                $"removed={result.Removed}, updated={result.Updated}, " +
                $"category-not-found={result.MissingCategory}");
        }

        foreach (var (source, originalContent) in sources)
            File.ReadAllBytes(source).Should().Equal(originalContent);

        var converter = Path.Combine(TestContext.CurrentContext.TestDirectory, "nwn_gff.exe");
        File.Exists(converter).Should().BeTrue();
        foreach (var replacement in refresh.Replacements.Values)
        {
            var binaryOutput = Path.Combine(
                _outputRoot,
                Path.GetFileNameWithoutExtension(replacement));
            var startInfo = new ProcessStartInfo(converter)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            foreach (var argument in new[]
                     {
                         "-l", "json",
                         "-i", replacement,
                         "-o", binaryOutput,
                         "-k", "gff"
                     })
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = Process.Start(startInfo)!;
            var standardOutput = process.StandardOutput.ReadToEnd();
            var standardError = process.StandardError.ReadToEnd();
            process.WaitForExit();
            process.ExitCode.Should().Be(
                0,
                $"nwn_gff must accept refreshed '{Path.GetFileName(replacement)}': " +
                standardOutput + standardError);
            File.Exists(binaryOutput).Should().BeTrue();
        }
    }

    [Test]
    [NonParallelizable]
    public void PackModule_EmbedsTheRefreshedPaletteAndCleansItsTemporaryWorkspace()
    {
        WritePalette("itempalcus", Terminal(1, "Custom", NamedLeaf("orphan", "Stale")));
        WriteBlueprint("uti", "probe", "PaletteID", 1, "LocalizedName", LocString("Packed probe"));
        Directory.CreateDirectory(Path.Combine(_moduleRoot, "ncs"));
        Directory.CreateDirectory(Path.Combine(_moduleRoot, "nss"));
        var canonicalScript = Path.Combine(_moduleRoot, "nss", "probe.nss");
        File.WriteAllText(canonicalScript, "void main() {}\n");
        File.WriteAllText(
            canonicalScript + "." + Guid.NewGuid().ToString("N") + ".save-backup",
            "stale backup");

        var previousDirectory = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = _moduleRoot;
            new ModulePacker().PackModule("palette-probe.mod", noPrompt: true);
        }
        finally
        {
            Environment.CurrentDirectory = previousDirectory;
        }

        var modulePath = Path.Combine(_moduleRoot, "palette-probe.mod");
        File.Exists(modulePath).Should().BeTrue();
        Directory.Exists(Path.Combine(_moduleRoot, "packing")).Should().BeFalse();
        Directory.Exists(Path.Combine(_moduleRoot, "palette-refresh")).Should().BeFalse();

        var extracted = Path.Combine(_outputRoot, "extracted");
        Directory.CreateDirectory(extracted);
        RunTool(
            "nwn_erf.exe",
            extracted,
            "-f", modulePath,
            "-x");

        var binaryPalette = Path.Combine(extracted, "itempalcus.itp");
        File.Exists(binaryPalette).Should().BeTrue();
        var unpackedJson = Path.Combine(extracted, "itempalcus.itp.json");
        RunTool(
            "nwn_gff.exe",
            extracted,
            "-i", binaryPalette,
            "-o", unpackedJson,
            "-p");

        var descriptors = Descriptors(TerminalCategories(
            JObject.Parse(File.ReadAllText(unpackedJson)))[1]);
        descriptors.Select(ResRef).Should().Equal("probe");
        descriptors[0]["NAME"]!["value"]!.Value<string>().Should().Be("Packed probe");
    }

    [Test]
    [NonParallelizable]
    public void PackModule_RejectsABackupWhoseCanonicalTargetIsMissing()
    {
        var scriptDirectory = Path.Combine(_moduleRoot, "nss");
        Directory.CreateDirectory(scriptDirectory);
        var backup = Path.Combine(
            scriptDirectory,
            "missing.nss." + Guid.NewGuid().ToString("N") + ".save-backup");
        File.WriteAllText(backup, "only surviving generation");

        var previousDirectory = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = _moduleRoot;
            var action = () => new ModulePacker().PackModule("blocked.mod", noPrompt: true);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*Interrupted toolset save*");
        }
        finally
        {
            Environment.CurrentDirectory = previousDirectory;
        }
    }

    [Test]
    [NonParallelizable]
    public void PackModule_RejectsAnInterruptedResourceDeleteManifest()
    {
        var manifest = Path.Combine(
            _moduleRoot,
            "." + Guid.NewGuid().ToString("N") + ".resource-delete-transaction.json");
        File.WriteAllText(manifest, "{}");

        var previousDirectory = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = _moduleRoot;
            var action = () => new ModulePacker().PackModule("blocked.mod", noPrompt: true);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*Interrupted toolset resource delete*");
        }
        finally
        {
            Environment.CurrentDirectory = previousDirectory;
        }
    }

    private string WritePalette(string paletteName, params JObject[] categories)
    {
        var palette = new JObject
        {
            ["__data_type"] = "ITP ",
            ["MAIN"] = ListField(
                new JObject
                {
                    ["__struct_id"] = 0,
                    ["LIST"] = ListField(categories)
                })
        };
        var path = Path.Combine(_moduleRoot, "itp", paletteName + ".itp.json");
        WriteJson(path, palette);
        return path;
    }

    private void WriteBlueprint(
        string extension,
        string resRef,
        string categoryField,
        int categoryId,
        string nameField,
        JObject localizedName)
    {
        var blueprint = Blueprint(categoryField, categoryId, nameField, localizedName);
        WriteJson(
            Path.Combine(_moduleRoot, extension, $"{resRef}.{extension}.json"),
            blueprint);
    }

    private static JObject Blueprint(
        string categoryField,
        int categoryId,
        string nameField,
        JObject localizedName)
    {
        return new JObject
        {
            ["__data_type"] = "TEST",
            [categoryField] = Field("byte", categoryId),
            [nameField] = localizedName
        };
    }

    private void WriteFactionFile(params (int Id, string Name)[] factions)
    {
        var entries = factions.Select(faction => new JObject
        {
            ["__struct_id"] = faction.Id,
            ["FactionName"] = Field("cexostring", faction.Name)
        });
        WriteJson(
            Path.Combine(_moduleRoot, "fac", "repute.fac.json"),
            new JObject
            {
                ["__data_type"] = "FAC ",
                ["FactionList"] = ListField(entries.ToArray())
            });
    }

    private static JObject Terminal(int id, string name, params JObject[] descriptors)
    {
        return new JObject
        {
            ["__struct_id"] = 0,
            ["ID"] = Field("byte", id),
            ["LIST"] = ListField(descriptors),
            ["NAME"] = Field("cexostring", name)
        };
    }

    private static JObject NamedLeaf(string resRef, string name)
    {
        return new JObject
        {
            ["__struct_id"] = 0,
            ["NAME"] = Field("cexostring", name),
            ["RESREF"] = Field("resref", resRef)
        };
    }

    private static JObject LocString(string? value = null, uint? strRef = null)
    {
        var result = new JObject
        {
            ["type"] = "cexolocstring",
            ["value"] = value == null
                ? new JObject()
                : new JObject { ["0"] = value }
        };
        if (strRef.HasValue)
            result["id"] = strRef.Value;
        return result;
    }

    private static JObject Field(string type, object value)
    {
        return new JObject
        {
            ["type"] = type,
            ["value"] = JToken.FromObject(value)
        };
    }

    private static JObject ListField(params JObject[] entries)
    {
        return new JObject
        {
            ["type"] = "list",
            ["value"] = new JArray(entries)
        };
    }

    private static void WriteJson(string path, JObject document)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, document.ToString(Formatting.Indented));
    }

    private static void WriteNwnJson(string path, JObject document)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(
            path,
            Encoding.GetEncoding(1252).GetBytes(document.ToString(Formatting.Indented)));
    }

    private JObject LoadOutput(string paletteName)
    {
        return JObject.Parse(File.ReadAllText(
            Path.Combine(_outputRoot, paletteName + ".itp.json")));
    }

    private static Dictionary<int, JObject> TerminalCategories(JObject palette)
    {
        var results = new Dictionary<int, JObject>();
        Walk((JArray)palette["MAIN"]!["value"]!, results);
        return results;

        static void Walk(JArray nodes, IDictionary<int, JObject> results)
        {
            foreach (var node in nodes.Cast<JObject>())
            {
                var id = node["ID"]?["value"]?.Value<int>();
                if (id.HasValue)
                    results[id.Value] = node;
                if (node["LIST"]?["value"] is JArray children)
                    Walk(children, results);
            }
        }
    }

    private static List<JObject> Descriptors(JObject terminal)
    {
        return ((JArray)terminal["LIST"]!["value"]!).Cast<JObject>()
            .Where(node => node["RESREF"] != null)
            .ToList();
    }

    private static string ResRef(JObject descriptor) =>
        descriptor["RESREF"]!["value"]!.Value<string>()!;

    private static void RunTool(string toolName, string workingDirectory, params string[] arguments)
    {
        var toolPath = Path.Combine(TestContext.CurrentContext.TestDirectory, toolName);
        var startInfo = new ProcessStartInfo(toolPath)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = Process.Start(startInfo)!;
        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();
        process.ExitCode.Should().Be(
            0,
            $"{toolName} failed: {standardOutput}{standardError}");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return directory ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
