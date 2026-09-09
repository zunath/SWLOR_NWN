using System.Numerics;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.AnimationDrafts;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Animation;

namespace SWLOR.Toolset.Tests;

public class ChoreographyAuthorTests
{
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
    public void DeviceAndCompanionChoreographiesBakeWithNeutralRecoveryAndGroundClearance()
    {
        var path = Path.Combine(Root, "SWLOR_Haks/sw_cr_creature/a_ba.mdl");
        if (!File.Exists(path)) Assert.Ignore("Initialize native models.");
        var model = new MdlReader().Parse(File.ReadAllBytes(path));
        foreach (var source in new[] { "pause1", "throwr", "castout", "castoutlp", "getlow" })
        {
            var clip = model.Animations.SingleOrDefault(a => a.Name == source);
            TestContext.Progress.WriteLine($"Native source {source}: {clip?.Length}");
        }
        var recipes = new[] { "devices", "beast-mastery" }.SelectMany(category => ChoreographyAuthor.Read(
            File.ReadAllText(Path.Combine(Root, "design/animations", category, "choreographies.json")))).ToArray();
        recipes.Should().HaveCount(30);
        foreach (var recipe in recipes)
        {
            TestContext.Progress.WriteLine("Validating " + recipe.Id);
            var sourceModels = recipe.Beats.Where(b => b.SourceModel != null).Select(b => b.SourceModel!).Distinct()
                .ToDictionary(name => name, name => new MdlReader().Parse(File.ReadAllBytes(Path.Combine(Root, "SWLOR_Haks/sw_cr_creature", name + ".mdl"))));
            var project = ChoreographyAuthor.Bake(model, recipe, sourceModels);
            project.Name.Should().Be(recipe.Id);
            project.Duration.Should().Be(recipe.Duration);
            project.Keys.Count.Should().BeLessThanOrEqualTo((int)Math.Ceiling(recipe.Duration * 30) + recipe.Beats.Length,
                "sampling must remain within the bank storage budget while preserving every authored beat");
            foreach (var beat in recipe.Beats)
            {
                var sourceModel = beat.SourceModel == null ? model : sourceModels[beat.SourceModel];
                var native = SWLOR.Toolset.Domain.Render.MdlAnimationPose.Sample(sourceModel.Animations.Single(a => a.Name == beat.SourceAnimation),
                    beat.SourceTime * sourceModel.Animations.Single(a => a.Name == beat.SourceAnimation).Length,
                    SWLOR.Toolset.Domain.Render.MdlAnimationPose.BindPose(model));
                foreach (var (hand, target) in new[] { ("lhand_g", beat.LeftHand), ("rhand_g", beat.RightHand) })
                {
                    if (target == null) continue;
                    var index = project.Joints.FindIndex(j => j.Name == hand);
                    Math.Abs(Quaternion.Dot(project.Sample(beat.Time)[index].Orientation, native[hand].Orientation)).Should().BeGreaterThan(.99999f,
                        recipe.Id + " must retain the native local hand grip at " + beat.Label);
                }
            }
            if (recipe.Id.EndsWith("Grenade") || recipe.Id == "ThermalDetonator")
            {
                var cock = recipe.Beats.First(b => b.Label.StartsWith("Cock"));
                var release = recipe.Beats.First(b => b.Label.StartsWith("Release"));
                var hand = project.Joints.FindIndex(j => j.Name == "rhand_g");
                var startHand = AnimationRig.World(project.Joints, project.Sample(cock.Time))[hand].Translation;
                var releaseHand = AnimationRig.World(project.Joints, project.Sample(release.Time))[hand].Translation;
                Vector3.Distance(startHand, releaseHand).Should().BeGreaterThan(.45f, recipe.Id + " requires a visible throw, not a static throwing-ready clip");
                releaseHand.Y.Should().BeGreaterThan(startHand.Y + .35f);
            }
            var first = project.Sample(0); var last = project.Sample(project.Duration);
            for (var i = 0; i < first.Length; i++)
                Math.Abs(Quaternion.Dot(first[i].Orientation, last[i].Orientation)).Should().BeGreaterThan(.99999f);
        }
    }

    [Test]
    public void FirstAidAndGhostProtocolChoreographiesPreserveGripAndRecovery()
    {
        var path = Path.Combine(Root, "SWLOR_Haks/sw_cr_creature/a_ba.mdl");
        if (!File.Exists(path)) Assert.Ignore("Initialize native models.");
        var model = new MdlReader().Parse(File.ReadAllBytes(path));
        var firstAid = ChoreographyAuthor.Read(File.ReadAllText(Path.Combine(Root, "design/animations/first-aid/choreographies.json")));
        var inventory = JsonSerializer.Deserialize<ActiveMotion[]>(File.ReadAllText(Path.Combine(Root, "design/animations/active-abilities.json")), BulkMotionAuthor.Json)!;
        firstAid.Select(r => r.Id).Should().BeEquivalentTo(inventory.Where(e => e.Category == "First Aid").Select(e => e.Id));
        var ghost = ChoreographyAuthor.Read(File.ReadAllText(Path.Combine(Root, "design/animations/espionage/choreographies.json"))).Single(r => r.Id == "GhostProtocol");
        ghost.Duration.Should().BeGreaterThanOrEqualTo(2.5f);
        foreach (var recipe in firstAid.Append(ghost))
        {
            TestContext.Progress.WriteLine("Validating medical/escape choreography " + recipe.Id);
            var project = ChoreographyAuthor.Bake(model, recipe);
            project.Keys.Count.Should().BeLessThanOrEqualTo((int)Math.Ceiling(recipe.Duration * 30) + recipe.Beats.Length);
            foreach (var beat in recipe.Beats)
            {
                var clip = model.Animations.Single(a => a.Name == beat.SourceAnimation);
                var native = SWLOR.Toolset.Domain.Render.MdlAnimationPose.Sample(clip, beat.SourceTime * clip.Length,
                    SWLOR.Toolset.Domain.Render.MdlAnimationPose.BindPose(model));
                foreach (var (name, target) in new[] { ("lhand_g", beat.LeftHand), ("rhand_g", beat.RightHand) })
                {
                    if (target == null) continue;
                    var index = project.Joints.FindIndex(j => j.Name == name);
                    Math.Abs(Quaternion.Dot(project.Sample(beat.Time)[index].Orientation, native[name].Orientation)).Should().BeGreaterThan(.99999f,
                        recipe.Id + " must not twist the native wrist grip during " + beat.Label);
                }
            }
            if (recipe.Id == "Resuscitation")
            {
                var root = project.Joints.FindIndex(j => j.Name == "rootdummy");
                var hand = project.Joints.FindIndex(j => j.Name == "rhand_g");
                var compressed = AnimationRig.World(project.Joints, project.Sample(1.30f));
                var released = AnimationRig.World(project.Joints, project.Sample(1.63f));
                compressed[root].Translation.Z.Should().BeLessThan(project.Sample(0)[root].Position.Z - .15f);
                released[hand].Translation.Z.Should().BeGreaterThan(compressed[hand].Translation.Z + .05f,
                    "resuscitation needs visible compression release before the second push");
            }
        }
    }

    [Test]
    public void CrouchAwareElbowPolesPreservePreviouslyReviewedStandingProjects()
    {
        var path = Path.Combine(Root, "SWLOR_Haks/sw_cr_creature/a_ba.mdl");
        if (!File.Exists(path)) Assert.Ignore("Initialize native models.");
        var model = new MdlReader().Parse(File.ReadAllBytes(path));
        foreach (var category in new[] { "devices", "beast-mastery", "first-aid", "espionage" })
        foreach (var recipe in ChoreographyAuthor.Read(File.ReadAllText(Path.Combine(Root, "design/animations", category, "choreographies.json"))))
        {
            if (recipe.Id == "Resuscitation") continue;
            var sourceModels = recipe.Beats.Where(b => b.SourceModel != null).Select(b => b.SourceModel!).Distinct()
                .ToDictionary(name => name, name => new MdlReader().Parse(File.ReadAllBytes(Path.Combine(Root, "SWLOR_Haks/sw_cr_creature", name + ".mdl"))));
            var project = ChoreographyAuthor.Bake(model, recipe, sourceModels);
            (project.Serialize() + "\n").Replace("\r\n", "\n").Should().Be(
                File.ReadAllText(Path.Combine(Root, "design/animations", category, recipe.Id + ".swlanim")).Replace("\r\n", "\n"),
                "the crouched elbow fix must not alter the previously reviewed standing choreography " + recipe.Id);
        }
    }

    [Test]
    public void EditingOneRecipeInvalidatesOnlyItsOwnPreservedProvenance()
    {
        var model = Path.Combine(Root, "SWLOR_Haks/sw_cr_creature/a_ba.mdl");
        if (!File.Exists(model)) Assert.Ignore("Initialize native models.");
        var folder = Path.Combine(Path.GetTempPath(), "swlor-choreography-provenance-" + Guid.NewGuid().ToString("N"));
        var output = Path.Combine(folder, "output");
        Directory.CreateDirectory(Path.Combine(output, "devices"));
        try
        {
            var recipePath = Path.Combine(output, "devices/choreographies.json");
            var recipes = ChoreographyAuthor.Read(File.ReadAllText(Path.Combine(Root, "design/animations/devices/choreographies.json"))).Take(2).ToArray();
            File.WriteAllText(recipePath, JsonSerializer.Serialize(recipes, BulkMotionAuthor.Json));
            var input = Path.Combine(folder, "input.json");
            File.WriteAllText(input, JsonSerializer.Serialize(recipes.Select((r, i) => new ActiveMotion(r.Id, "sw_test" + i, "Devices", "Combat", r.Description))));
            BulkMotionAuthor.Generate(model, input, output, false);
            var preservedPath = Path.Combine(output, "devices", recipes[1].Id + ".swlanim");
            var bytes = File.ReadAllBytes(preservedPath);
            recipes[0] = recipes[0] with { Description = "Revised first motion direction" };
            File.WriteAllText(recipePath, JsonSerializer.Serialize(recipes, BulkMotionAuthor.Json));
            BulkMotionAuthor.Generate(model, input, output, false);
            using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "active-manifest.json")));
            var reports = manifest.RootElement.GetProperty("Animations").EnumerateArray().ToArray();
            reports[0].GetProperty("ChoreographySha256").ValueKind.Should().Be(JsonValueKind.Null);
            reports[1].GetProperty("Profile").GetString().Should().StartWith("Authored choreography:");
            reports[1].GetProperty("ChoreographySha256").GetString().Should().NotBeNullOrEmpty();
            File.ReadAllBytes(preservedPath).Should().Equal(bytes);
        }
        finally { Directory.Delete(folder, true); }
    }

    [Test]
    public void MalformedChoreographyCannotIntroduceNonneutralEndpointsOrTwistedTorso()
    {
        ChoreographyBeat[] beats = [new(0, "Start", "pause1"), new(.5f, "Gesture", "pause1", TorsoDegrees: [0, 0, 180]), new(1, "Finish", "pause1")];
        Action read = () => ChoreographyAuthor.Read(System.Text.Json.JsonSerializer.Serialize(new[] { new Choreography("Bad", "Test", 1, beats) }));
        read.Should().Throw<InvalidDataException>().WithMessage("*35 degrees*");
        beats[1] = new(.5f, "Gesture", "pause1"); beats[2] = new(1, "Finish", "throwr");
        read.Should().Throw<InvalidDataException>().WithMessage("*endpoints*");
    }
}
