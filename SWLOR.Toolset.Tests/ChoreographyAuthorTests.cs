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
        using var inventory = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "design/animations/active-abilities.json")));
        recipes.Select(recipe => recipe.Id).Should().BeEquivalentTo(inventory.RootElement.EnumerateArray()
            .Where(entry => entry.GetProperty("Category").GetString() is "Devices" or "Beast Mastery")
            .Select(entry => entry.GetProperty("Id").GetString()));
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
    public void ForceChoreographiesCoverEveryAbilityWithNativeGripsRecoveryAndDistinctArmPaths()
    {
        var path = Path.Combine(Root, "SWLOR_Haks/sw_cr_creature/a_ba.mdl");
        if (!File.Exists(path)) Assert.Ignore("Initialize native models.");
        var model = new MdlReader().Parse(File.ReadAllBytes(path));
        var recipes = ChoreographyAuthor.Read(File.ReadAllText(Path.Combine(Root, "design/animations/force/choreographies.json")));
        var inventory = JsonSerializer.Deserialize<ActiveMotion[]>(File.ReadAllText(Path.Combine(Root,
            "design/animations/active-abilities.json")), BulkMotionAuthor.Json)!;
        recipes.Should().HaveCount(25);
        recipes.Select(recipe => recipe.Id).Should().BeEquivalentTo(inventory.Where(entry => entry.Category == "Force").Select(entry => entry.Id));
        var sources = recipes.SelectMany(recipe => recipe.Beats).Where(beat => beat.SourceModel != null)
            .Select(beat => beat.SourceModel!).Distinct().ToDictionary(name => name,
                name => new MdlReader().Parse(File.ReadAllBytes(Path.Combine(Root, "SWLOR_Haks/sw_cr_creature", name + ".mdl"))));
        var trajectories = new Dictionary<string, Vector3[]>();
        foreach (var recipe in recipes)
        {
            var project = ChoreographyAuthor.Bake(model, recipe, sources);
            project.Keys.Count.Should().BeLessThanOrEqualTo((int)Math.Ceiling(recipe.Duration * 120) + recipe.Beats.Length,
                "adaptive floor correction may refine samples but must remain within the dense verification budget");
            var feet = new[] { "lfoot_g", "rfoot_g" }.Select(name => project.Joints.FindIndex(joint => joint.Name == name)).ToArray();
            var idleWorld = AnimationRig.World(project.Joints, project.Sample(0));
            var root = project.Joints.FindIndex(joint => joint.Name == "rootdummy");
            var highestRoot = idleWorld[root].Translation.Z;
            var floor = feet.Min(foot => idleWorld[foot].Translation.Z);
            for (var frame = 0; frame <= (int)Math.Ceiling(project.Duration * 120); frame++)
            {
                var world = AnimationRig.World(project.Joints, project.Sample(Math.Min(project.Duration, frame / 120f)));
                feet.Min(foot => world[foot].Translation.Z).Should().BeGreaterThanOrEqualTo(floor - .025f,
                    recipe.Id + " must not sink between authored keys or during native leap recovery");
                if (recipe.Id is "ForceLeap" or "ForceIntercept")
                {
                    Vector2.Distance(new(world[root].Translation.X, world[root].Translation.Y),
                        new(idleWorld[root].Translation.X, idleWorld[root].Translation.Y)).Should().BeLessThan(.001f,
                        "native leap translation must not send the tester offscreen or rewind the character");
                    highestRoot = Math.Max(highestRoot, world[root].Translation.Z);
                }
            }
            if (recipe.Id is "ForceLeap" or "ForceIntercept")
                highestRoot.Should().BeGreaterThan(idleWorld[root].Translation.Z + .15f,
                    "in-place playback must retain the airborne part of the native leap");
            var hands = new[] { "lhand_g", "rhand_g" }.Select(name => project.Joints.FindIndex(joint => joint.Name == name)).ToArray();
            foreach (var beat in recipe.Beats)
            {
                var owner = beat.SourceModel == null ? model : sources[beat.SourceModel];
                var clip = owner.Animations.Single(animation => animation.Name == beat.SourceAnimation);
                var native = SWLOR.Toolset.Domain.Render.MdlAnimationPose.Sample(clip, beat.SourceTime * clip.Length,
                    SWLOR.Toolset.Domain.Render.MdlAnimationPose.BindPose(model));
                foreach (var hand in hands)
                    Math.Abs(Quaternion.Dot(project.Sample(beat.Time)[hand].Orientation,
                        native[project.Joints[hand].Name].Orientation)).Should().BeGreaterThan(.99999f,
                        recipe.Id + " must preserve its native local wrist grip at " + beat.Label);
            }
            var first = project.Sample(0); var last = project.Sample(project.Duration);
            for (var i = 0; i < first.Length; i++)
            {
                Vector3.Distance(first[i].Position, last[i].Position).Should().BeLessThan(.00001f);
                Math.Abs(Quaternion.Dot(first[i].Orientation, last[i].Orientation)).Should().BeGreaterThan(.99999f);
            }
            // Normalize time so merely stretching a reused motion cannot satisfy distinctness.
            trajectories[recipe.Id] = Enumerable.Range(1, 19).SelectMany(frame =>
            {
                var world = AnimationRig.World(project.Joints, project.Sample(project.Duration * frame / 20f));
                return hands.Select(hand => world[hand].Translation);
            }).ToArray();
        }
        var ids = trajectories.Keys.ToArray();
        for (var i = 0; i < ids.Length; i++)
        for (var j = i + 1; j < ids.Length; j++)
        {
            var squaredDistance = trajectories[ids[i]].Zip(trajectories[ids[j]], Vector3.DistanceSquared).Average();
            Math.Sqrt(squaredDistance).Should().BeGreaterThan(.02,
                ids[i] + " and " + ids[j] + " need visibly distinct arm paths, not only different names or durations");
        }
    }

    [Test]
    public void InPlaceOptionOnlyChangesExplicitLeapRecipesAndDoesNotInvalidateOlderProvenance()
    {
        var force = ChoreographyAuthor.Read(File.ReadAllText(Path.Combine(Root, "design/animations/force/choreographies.json")));
        force.Where(recipe => recipe.InPlace).Select(recipe => recipe.Id).Should().BeEquivalentTo("ForceLeap", "ForceIntercept");
        foreach (var category in new[] { "devices", "beast-mastery", "first-aid", "espionage" })
        foreach (var recipe in ChoreographyAuthor.Read(File.ReadAllText(Path.Combine(Root,
            "design/animations", category, "choreographies.json"))))
        {
            recipe.InPlace.Should().BeFalse();
            JsonSerializer.Serialize(recipe, BulkMotionAuthor.Json).Should().NotContain("InPlace",
                "an omitted default option must not change the hash of previously authored recipes");
        }
    }

    [TestCase("ForceLightning", "a_ba_casts", "custom64start", 1f)]
    [TestCase("ForceLightning", "a_ba_casts", "custom64lp", 1f)]
    [TestCase("ForceLeap", "a_ba_casts", "custom65lp", 2f)]
    [TestCase("ThrowLightsaber", "a_ba_non_combat", "custom46start", 2f)]
    public void MasterForceMotionSpansRetainNativeInBetweenPoses(string id, string sourceModel, string sourceName, float speed)
    {
        var path = Path.Combine(Root, "SWLOR_Haks/sw_cr_creature/a_ba.mdl");
        if (!File.Exists(path)) Assert.Ignore("Initialize native models.");
        var model = new MdlReader().Parse(File.ReadAllBytes(path));
        var recipe = ChoreographyAuthor.Read(File.ReadAllText(Path.Combine(Root,
            "design/animations/force/choreographies.json"))).Single(recipe => recipe.Id == id);
        var sources = recipe.Beats.Where(beat => beat.SourceModel != null).Select(beat => beat.SourceModel!).Distinct()
            .ToDictionary(name => name, name => new MdlReader().Parse(File.ReadAllBytes(Path.Combine(Root,
                "SWLOR_Haks/sw_cr_creature", name + ".mdl"))));
        var project = ChoreographyAuthor.Bake(model, recipe, sources);
        var native = sources[sourceModel].Animations.Single(animation => animation.Name == sourceName);
        var spans = recipe.Beats.Zip(recipe.Beats.Skip(1)).Where(pair =>
            pair.First.SourceModel == sourceModel && pair.Second.SourceModel == sourceModel &&
            pair.First.SourceAnimation == sourceName && pair.Second.SourceAnimation == sourceName &&
            pair.First.SourceTime == 0 && pair.Second.SourceTime == 1).ToArray();
        spans.Should().HaveCount(sourceName == "custom64lp" ? 4 : 1);
        foreach (var span in spans)
        {
            (span.Second.Time - span.First.Time).Should().BeApproximately(native.Length / speed, .002f);
            // Root-only floor refinement inserts interpolated poses; compare the original
            // 30 Hz source samples rather than treating added clearance keys as new native samples.
            var frames = project.Keys.Where(key => key.Time > span.First.Time && key.Time < span.Second.Time &&
                Math.Abs(key.Time * 30 - MathF.Round(key.Time * 30)) < .0001f).ToArray();
            frames.Length.Should().BeGreaterThan(3);
            foreach (var key in frames)
            {
                var fraction = (key.Time - span.First.Time) / (span.Second.Time - span.First.Time);
                var pose = SWLOR.Toolset.Domain.Render.MdlAnimationPose.Sample(native, fraction * native.Length,
                    SWLOR.Toolset.Domain.Render.MdlAnimationPose.BindPose(model));
                var actual = project.Sample(key.Time);
                for (var i = 0; i < project.Joints.Count; i++)
                    Math.Abs(Quaternion.Dot(actual[i].Orientation, pose.GetValueOrDefault(project.Joints[i].Name, project.Joints[i].Rest).Orientation))
                        .Should().BeGreaterThan(.99999f, id + " must sample real native motion between authored bookends");
            }
        }
    }

    [Test]
    public void HeldNativePhaseOverlaysPreservePreviouslyReviewedProjectBytes()
    {
        var path = Path.Combine(Root, "SWLOR_Haks/sw_cr_creature/a_ba.mdl");
        if (!File.Exists(path)) Assert.Ignore("Initialize native models.");
        var model = new MdlReader().Parse(File.ReadAllBytes(path));
        foreach (var category in new[] { "devices", "beast-mastery", "first-aid", "espionage" })
        foreach (var recipe in ChoreographyAuthor.Read(File.ReadAllText(Path.Combine(Root, "design/animations", category, "choreographies.json"))))
        {
            if (recipe.Id == "Resuscitation") continue;
            // Archived projects with advancing native phases and upper-body edits
            // used sparse interpolation before continuous sampling was supported.
            // They remain untouched until explicitly regenerated. This golden check
            // protects held-phase compatibility; FullBodyChoreographyTests exercises
            // the deliberate change to advancing native motion under overlays.
            if (!recipe.Beats.Any(beat => beat.RootOffset != null) && recipe.Beats.Zip(recipe.Beats.Skip(1)).Any(pair =>
                    pair.First.SourceAnimation == pair.Second.SourceAnimation && pair.First.SourceModel == pair.Second.SourceModel &&
                    pair.First.SourceTime != pair.Second.SourceTime &&
                    (pair.First.LeftHand != null || pair.Second.LeftHand != null || pair.First.RightHand != null || pair.Second.RightHand != null ||
                     pair.First.TorsoDegrees != null || pair.Second.TorsoDegrees != null))) continue;
            var sourceModels = recipe.Beats.Where(b => b.SourceModel != null).Select(b => b.SourceModel!).Distinct()
                .ToDictionary(name => name, name => new MdlReader().Parse(File.ReadAllBytes(Path.Combine(Root, "SWLOR_Haks/sw_cr_creature", name + ".mdl"))));
            var project = ChoreographyAuthor.Bake(model, recipe, sourceModels);
            (project.Serialize() + "\n").Replace("\r\n", "\n").Should().Be(
                File.ReadAllText(Path.Combine(Root, "design/animations", category, recipe.Id + ".swlanim")).Replace("\r\n", "\n"),
                "held-phase compatibility and explicitly revised projects must remain reproducible: " + recipe.Id);
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
