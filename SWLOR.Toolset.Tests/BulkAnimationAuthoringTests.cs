using System.Numerics;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.AnimationDrafts;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Animation;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests;

public class BulkAnimationAuthoringTests
{
    private static byte[] ExistingProjectFixture(string name)
    {
        var rest = new PosedNode(Vector3.Zero, Quaternion.Identity, 1);
        var project = new AnimationProject
        {
            Name = name, ModelName = "a_ba", AnimationRoot = "a_ba", Duration = 1,
            Joints = [new("a_ba", -1, rest)]
        };
        project.SetKey(0, [rest]);
        project.SetKey(1, [rest]);
        return System.Text.Encoding.UTF8.GetBytes(project.Serialize() + "\n");
    }

    [Test]
    public void RecipesDoNotRepeatTheSameNativePoseSequenceWithDifferentTiming()
    {
        var duplicates = new List<string>();
        var signatures = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var path in Directory.EnumerateFiles(Path.Combine(Root, "design/animations"), "choreographies.json", SearchOption.AllDirectories))
        {
            var recipes = ChoreographyAuthor.Read(File.ReadAllText(path));
            foreach (var recipe in recipes)
            {
                var signature = JsonSerializer.Serialize(new
                {
                    recipe.InPlace,
                    Beats = recipe.Beats.Select(beat => new
                    {
                        beat.SourceAnimation, beat.SourceModel, beat.SourceTime,
                        beat.LeftHand, beat.RightHand, beat.TorsoDegrees
                    })
                });
                if (signatures.TryGetValue(signature, out var previous))
                    duplicates.Add($"{Path.GetFileName(Path.GetDirectoryName(path))}: {previous} and {recipe.Id}");
                else signatures.Add(signature, recipe.Id);
            }
        }
        duplicates.Should().BeEmpty("changing duration or beat labels does not create a distinct motion");
    }

    [Test]
    public void ProjectProvenanceIgnoresCheckoutLineEndingsButDetectsContentChanges()
    {
        const string project = "{\n  \"Name\": \"Example\"\n}\n";
        BulkMotionAuthor.ProjectHash(project.Replace("\n", "\r\n")).Should().Be(BulkMotionAuthor.ProjectHash(project));
        BulkMotionAuthor.ProjectHash(project.Replace("Example", "Changed")).Should().NotBe(BulkMotionAuthor.ProjectHash(project));
    }

    [Test]
    public void NumericFeatValuesAreRejectedBeforeGeneratingInvalidCatalogMembers()
    {
        var folder = Path.Combine(Path.GetTempPath(), "swlor-bulk-feat-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var numeric = Convert.ToInt32(Enum.GetValues<SWLOR.NWN.API.NWScript.Enum.FeatType>()[0]).ToString(System.Globalization.CultureInfo.InvariantCulture);
            var input = Path.Combine(folder, "input.json");
            File.WriteAllText(input, JsonSerializer.Serialize(new[] { new ActiveMotion("Test", "sw_test", "Force", "Combat", "", Feats: [numeric]) }));
            Action generate = () => BulkMotionAuthor.Generate("missing-model.mdl", input, Path.Combine(folder, "output"), false);
            generate.Should().Throw<InvalidDataException>().WithMessage("Unknown feat:*");
            Directory.Exists(Path.Combine(folder, "output")).Should().BeFalse();
        }
        finally { Directory.Delete(folder, true); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void GeneratingANewAbilityDoesNotOverwriteAnExistingManuallyEditedBase(bool targetedReplacement)
    {
        var model = Path.Combine(Root, "SWLOR_Haks/sw_cr_creature/a_ba.mdl");
        if (!File.Exists(model)) Assert.Ignore("Initialize HAK sources.");
        var folder = Path.Combine(Path.GetTempPath(), "swlor-bulk-base-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(folder, "output/bases"));
        try
        {
            var basePath = Path.Combine(folder, "output/bases/Pistolaimandrecoil.swlanim");
            var saved = ExistingProjectFixture("Pistolaimandrecoil");
            // A harmless trailing newline represents an animator-owned file revision.
            var edited = saved.Concat(new byte[] { (byte)'\n' }).ToArray();
            File.WriteAllBytes(basePath, edited);
            var input = Path.Combine(folder, "input.json");
            File.WriteAllText(input, JsonSerializer.Serialize(new[] { new ActiveMotion("NewShot", "sw_newshot", "Pistol", "Combat", "Deal damage.", SourceAnimation: "1hreadyr", Profile: "Pistol aim and recoil") }));
            BulkMotionAuthor.Generate(model, input, Path.Combine(folder, "output"), targetedReplacement,
                targetedReplacement ? new HashSet<string> { "NewShot" } : null);
            File.ReadAllBytes(basePath).Should().Equal(edited);
            File.Exists(Path.Combine(folder, "output/pistol/NewShot.swlanim")).Should().BeTrue();
        }
        finally { Directory.Delete(folder, true); }
    }

    [Test]
    public void NewOrReplacedMotionRequiresIndividualDirectionButExistingMotionCanBePreserved()
    {
        var model = Path.Combine(Root, "SWLOR_Haks/sw_cr_creature/a_ba.mdl");
        if (!File.Exists(model)) Assert.Ignore("Initialize HAK sources.");
        var folder = Path.Combine(Path.GetTempPath(), "swlor-authored-direction-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var input = Path.Combine(folder, "input.json");
            var output = Path.Combine(folder, "output");
            var motion = new ActiveMotion("Example", "sw_example", "Force", "Combat", "Blast an enemy.");
            File.WriteAllText(input, JsonSerializer.Serialize(new[] { motion }));
            Action missing = () => BulkMotionAuthor.Generate(model, input, output, false);
            missing.Should().Throw<InvalidDataException>().WithMessage("*individual choreography*SourceAnimation*");
            Directory.Exists(output).Should().BeFalse();
            File.WriteAllText(input, JsonSerializer.Serialize(new[] { motion with { SourceAnimation = "castout" } }));
            BulkMotionAuthor.Generate(model, input, output, false);
            var path = Path.Combine(output, "force/Example.swlanim");
            var authored = File.ReadAllBytes(path);
            File.WriteAllText(input, JsonSerializer.Serialize(new[] { motion }));
            BulkMotionAuthor.Generate(model, input, output, false);
            File.ReadAllBytes(path).Should().Equal(authored);
            Action overwrite = () => BulkMotionAuthor.Generate(model, input, output, true);
            overwrite.Should().Throw<InvalidDataException>().WithMessage("*individual choreography*SourceAnimation*");
            File.ReadAllBytes(path).Should().Equal(authored);
        }
        finally { Directory.Delete(folder, true); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void DuplicateFeatAssignmentsAreRejectedBeforeAnyGeneration(bool acrossEntries)
    {
        var feat = Enum.GetNames<SWLOR.NWN.API.NWScript.Enum.FeatType>()[0];
        var entries = acrossEntries
            ? new[] { new ActiveMotion("First", "sw_first", "Force", "Combat", "", Feats: [feat]),
                new ActiveMotion("Second", "sw_second", "Force", "Combat", "", Feats: [feat]) }
            : new[] { new ActiveMotion("First", "sw_first", "Force", "Combat", "", Feats: [feat, feat]) };
        Action validate = () => BulkMotionAuthor.ValidateEntries(entries);
        validate.Should().Throw<InvalidDataException>().WithMessage("Duplicate feat assignment:*");
    }

    [Test]
    public void LateCatalogPublicationFailureRestoresTheEntireGeneratedLibrary()
    {
        if (!OperatingSystem.IsWindows()) Assert.Ignore("Windows sharing modes enforce the publication lock.");
        var model = Path.Combine(Root, "SWLOR_Haks/sw_cr_creature/a_ba.mdl");
        if (!File.Exists(model)) Assert.Ignore("Initialize HAK sources.");
        var folder = Path.Combine(Path.GetTempPath(), "swlor-bulk-transaction-" + Guid.NewGuid().ToString("N"));
        var output = Path.Combine(folder, "design/animations");
        var catalog = Path.Combine(folder, "SWLOR.Game.Server/Service/AnimationService/ActiveAbilityAnimationCatalog.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(catalog)!);
        Directory.CreateDirectory(Path.Combine(output, "pistol"));
        try
        {
            var projectPath = Path.Combine(output, "pistol/NewShot.swlanim");
            var original = ExistingProjectFixture("NewShot");
            File.WriteAllBytes(projectPath, original);
            var manifest = Path.Combine(output, "active-manifest.json");
            const string manifestText = "{\"Animations\":[]}";
            File.WriteAllText(manifest, manifestText);
            File.WriteAllText(catalog, "// Original catalog");
            var input = Path.Combine(folder, "input.json");
            File.WriteAllText(input, JsonSerializer.Serialize(new[] { new ActiveMotion("NewShot", "sw_newshot", "Pistol", "Combat", "Deal damage.", SourceAnimation: "1hreadyr", Profile: "Pistol aim and recoil") }));
            using (var locked = new FileStream(catalog, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Action generate = () => BulkMotionAuthor.Generate(model, input, output, true);
                generate.Should().Throw<IOException>();
            }
            File.ReadAllBytes(projectPath).Should().Equal(original);
            File.ReadAllText(manifest).Should().Be(manifestText);
            File.ReadAllText(catalog).Should().Be("// Original catalog");
            File.Exists(Path.Combine(output, "bases/Pistolaimandrecoil.swlanim")).Should().BeFalse();
            Directory.GetFiles(folder, "*.tmp", SearchOption.AllDirectories).Should().BeEmpty();
        }
        finally { Directory.Delete(folder, true); }
    }

    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln"))) directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException();
        }
    }

    [Test]
    public void EveryGeneratedMotionReleasesItsPoseAndClearsTheFloorBetweenKeys()
    {
        var entries = JsonSerializer.Deserialize<ActiveMotion[]>(File.ReadAllText(Path.Combine(Root, "design/animations/active-abilities.json")), BulkMotionAuthor.Json)!;
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "design/animations/active-manifest.json")));
        var reports = manifest.RootElement.GetProperty("Animations").EnumerateArray().ToDictionary(a => a.GetProperty("Id").GetString()!);
        reports.Keys.Should().BeEquivalentTo(entries.Select(e => e.Id));
        foreach (var entry in entries)
        {
            var report = reports[entry.Id];
            if (report.GetProperty("SourceModel").GetString() == "Existing authored project") continue;
            var project = AnimationProject.Deserialize(File.ReadAllText(Path.Combine(Root, "design/animations", report.GetProperty("Project").GetString()!)));
            var neutral = project.Sample(0);
            var world = AnimationRig.World(project.Joints, neutral);
            var feet = new[] { "lfoot_g", "rfoot_g" }.Select(name => project.Joints.FindIndex(j => j.Name == name)).ToArray();
            BulkMotionAuthor.ValidateMotion(project, neutral, feet.Min(i => world[i].Translation.Z));
            project.Name.Should().Be(entry.Id);
            project.Duration.Should().Be(report.GetProperty("Duration").GetSingle());
        }
    }

    [TestCase("Pistol", "1hreadyr")]
    [TestCase("Rifle", "xbowrdy")]
    public void ProceduralRecoilPreservesNativeWristGrip(string category, string expectedBase)
    {
        var path = Path.Combine(Root, "SWLOR_Haks/sw_cr_creature/a_ba.mdl");
        if (!File.Exists(path)) Assert.Ignore("Initialize HAK sources to verify native grips.");
        var model = new MdlReader().Parse(File.ReadAllBytes(path));
        var motion = new ActiveMotion("TestShot", "sw_testshot", category, "Combat", "Deals damage in a rapid burst.");
        var profile = BulkMotionAuthor.Select(motion);
        profile.Source.Should().Be(expectedBase);
        var result = BulkMotionAuthor.Bake(model, motion, profile);
        var source = MdlAnimationPose.Sample(model.Animations.Single(a => a.Name == expectedBase), 0, MdlAnimationPose.BindPose(model));
        foreach (var name in new[] { "rhand_g", "lhand_g" })
        {
            var index = result.Joints.FindIndex(j => j.Name == name);
            foreach (var time in new[] { .4f, .5f, .6f })
                Math.Abs(Quaternion.Dot(result.Sample(time * result.Duration)[index].Orientation, source[name].Orientation)).Should().BeGreaterThan(.99999f,
                    "recoil must come from the shoulder rather than twisting the hand");
        }
    }

    [Test]
    public void MissingNativeBaseFailsRatherThanSubstitutingAnUnrelatedAttack()
    {
        var path = Path.Combine(Root, "SWLOR_Haks/sw_cr_creature/a_ba.mdl");
        if (!File.Exists(path)) Assert.Ignore("Initialize HAK sources.");
        var model = new MdlReader().Parse(File.ReadAllBytes(path));
        Action bake = () => BulkMotionAuthor.Bake(model, new("Missing", "sw_missing", "Force", "Combat", ""), new("Missing", "does_not_exist", 1));
        bake.Should().Throw<InvalidDataException>().WithMessage("*required base animation*");
    }
}
