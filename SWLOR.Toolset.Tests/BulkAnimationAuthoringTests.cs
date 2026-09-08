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
            var saved = File.ReadAllBytes(Path.Combine(Root, "design/animations/bases/Pistolaimandrecoil.swlanim"));
            // A harmless trailing newline represents an animator-owned file revision.
            var edited = saved.Concat(new byte[] { (byte)'\n' }).ToArray();
            File.WriteAllBytes(basePath, edited);
            var input = Path.Combine(folder, "input.json");
            File.WriteAllText(input, JsonSerializer.Serialize(new[] { new ActiveMotion("NewShot", "sw_newshot", "Pistol", "Combat", "Deal damage.") }));
            BulkMotionAuthor.Generate(model, input, Path.Combine(folder, "output"), targetedReplacement,
                targetedReplacement ? new HashSet<string> { "NewShot" } : null);
            File.ReadAllBytes(basePath).Should().Equal(edited);
            File.Exists(Path.Combine(folder, "output/pistol/NewShot.swlanim")).Should().BeTrue();
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
