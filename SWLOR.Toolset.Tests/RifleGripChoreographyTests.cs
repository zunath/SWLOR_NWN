using System.Numerics;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.AnimationDrafts;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Animation;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests;

[TestFixture]
public class RifleGripChoreographyTests
{
    private string _root = null!;
    private MdlModel _source = null!;
    private AnimationProject _sourceRig = null!;
    private Choreography[] _recipes = null!;
    private AnimationRegistration[] _registry = null!;
    private readonly Dictionary<string, MdlModel> _models = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AnimationProject> _projects = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, InstalledMotionLibrary> _installed = new(StringComparer.OrdinalIgnoreCase);

    [OneTimeSetUp]
    public void LoadRifleResources()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln"))) directory = directory.Parent;
        _root = directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        _source = Load("a_ba")!;
        _sourceRig = AnimationProject.FromModel(_source);
        _recipes = ChoreographyAuthor.Read(File.ReadAllText(Path.Combine(_root, "design/animations/rifle/choreographies.json")));
        _recipes.Should().NotBeEmpty();
        _registry = JsonSerializer.Deserialize<AnimationRegistration[]>(File.ReadAllText(Path.Combine(_root, "design/animations/registry.json")))!;
        foreach (var recipe in _recipes)
            _projects.Add(recipe.Id, AnimationProject.Deserialize(File.ReadAllText(Path.Combine(_root, "design/animations/rifle", recipe.Id + ".swlanim"))));
    }

    [TestCase("project")]
    [TestCase("a_ba")]
    [TestCase("a_fa")]
    public void RifleHandContactMatchesTheEffectiveNativeGrip(string target)
    {
        var (rig, model) = Target(target);
        foreach (var recipe in _recipes)
        foreach (var beat in recipe.Beats.Skip(1).SkipLast(1))
        {
            // A hand deliberately visiting a control has no two-hand grip at that
            // moment. Every other native hold/recoil must preserve the working
            // auto-attack's hand position AND palm angle relative to its weapon.
            if (beat.LeftHand != null || beat.RightHand != null) continue;
            var expected = NativeOnTarget(beat.SourceAnimation, beat.SourceTime, rig, model);
            var actual = Sample(recipe.Id, beat.Time, target, rig, model);
            var nativeGrip = HandRelativeToWeapon(rig, expected);
            var authoredGrip = HandRelativeToWeapon(rig, actual);
            var context = $"{recipe.Id}/{target} at {beat.Time:0.###}s ({beat.Label})";
            Vector3.Distance(authoredGrip.Translation, nativeGrip.Translation).Should().BeLessThan(.001f, context);
            Matrix4x4.Decompose(authoredGrip, out _, out var actualRotation, out _);
            Matrix4x4.Decompose(nativeGrip, out _, out var expectedRotation, out _);
            RotationDegrees(actualRotation, expectedRotation).Should().BeLessThan(.2f, context);
        }
    }

    [TestCase("project")]
    [TestCase("a_ba")]
    [TestCase("a_fa")]
    public void RifleSupportHandStaysOnTheNativePathThroughoutRaisingAndLowering(string target)
    {
        var (rig, model) = Target(target);
        var checkedSpans = 0;
        foreach (var recipe in _recipes)
        for (var segment = 1; segment < recipe.Beats.Length - 2; segment++)
        {
            var first = recipe.Beats[segment]; var second = recipe.Beats[segment + 1];
            if (first.LeftHand != null || second.LeftHand != null || first.RightHand != null || second.RightHand != null) continue;
            for (var frame = 0; frame <= Math.Ceiling((second.Time - first.Time) * 120); frame++)
            {
                var time = Math.Min(first.Time + frame / 120f, second.Time);
                var fraction = (time - first.Time) / (second.Time - first.Time);
                // Native ready is the shot's initial carry pose. Comparing to
                // the complete shot path also catches mixing ready/shot clips
                // and interpolating their arm joints into a floating midpoint.
                var firstPhase = first.SourceAnimation == "xbowrdy" ? 0 : first.SourceTime;
                var secondPhase = second.SourceAnimation == "xbowrdy" ? 0 : second.SourceTime;
                var expected = HandRelativeToWeapon(rig, NativeOnTarget("xbowshot", float.Lerp(firstPhase, secondPhase, fraction), rig, model));
                var actual = HandRelativeToWeapon(rig, Sample(recipe.Id, time, target, rig, model));
                var context = $"{recipe.Id}/{target} between {first.Label} and {second.Label} at {time:0.###}s";
                Vector3.Distance(actual.Translation, expected.Translation).Should().BeLessThan(.003f, context);
                Matrix4x4.Decompose(actual, out _, out var actualRotation, out _);
                Matrix4x4.Decompose(expected, out _, out var expectedRotation, out _);
                // The 60 Hz bake approximates source controller corners between
                // keys: 1.25 degrees is under 3 mm across a 13 cm hand, while
                // exact authored beats retain the tighter .2-degree check.
                RotationDegrees(actualRotation, expectedRotation).Should().BeLessThan(1.25f, context);
            }
            checkedSpans++;
        }
        checkedSpans.Should().BeGreaterThan(0, "Rifle motions must exercise supported movement between held poses");
    }

    [TestCase("project")]
    [TestCase("a_ba")]
    [TestCase("a_fa")]
    public void SustainedRifleSightLinesDoNotHoldTheNativeReloadOrRecoil(string target)
    {
        var (rig, model) = Target(target);
        // The effective a_ba shot pauses its aim around 40%; the later 60-70%
        // recoil raises the barrel and releases the support grip. Measure the
        // real weapon hook against that native sight line, not an ideal zero
        // angle that would ignore the rifle model's attachment convention.
        var nativeAim = MuzzleAngles(rig, NativeOnTarget("xbowshot", .4f, rig, model));
        var checkedHolds = 0;
        foreach (var recipe in _recipes)
        for (var index = 0; index < recipe.Beats.Length - 1; index++)
        {
            var first = recipe.Beats[index]; var second = recipe.Beats[index + 1];
            if (first.SourceAnimation != "xbowshot" || second.SourceAnimation != "xbowshot" ||
                first.SourceTime != second.SourceTime || first.SourceTime <= 0 || first.SourceTime >= 1) continue;
            var (minimum, maximum) = recipe.Id switch
            {
                "Headshot" => (-1f, 8f),
                "CripplingShot" => (-20f, 1f),
                "PiercingRound" => (-10f, 3f),
                _ => (-8f, 5f)
            };
            for (var fraction = 0; fraction <= 12; fraction++)
            {
                var time = float.Lerp(first.Time, second.Time, fraction / 12f);
                var angles = MuzzleAngles(rig, Sample(recipe.Id, time, target, rig, model));
                var context = $"{recipe.Id}/{target} held sight line at {time:0.###}s";
                (angles.Pitch - nativeAim.Pitch).Should().BeInRange(minimum, maximum, context);
                Math.Abs(WrappedAngle(angles.Yaw - nativeAim.Yaw)).Should().BeLessThan(25f, context);
            }
            checkedHolds++;
        }
        checkedHolds.Should().BeGreaterThan(0, "Rifle should include deliberate aimed holds");
    }

    private MdlModel? Load(string name)
    {
        if (_models.TryGetValue(name, out var cached)) return cached;
        var path = AnimationInstall.FindTargetSource(_root, name);
        if (path == null) return null;
        var bytes = File.ReadAllBytes(path);
        AnimationBankSource.IsBinary(bytes).Should().BeTrue($"the installed {name} must be compiled");
        var model = new MdlReader().Parse(bytes);
        _models.Add(name, model);
        return model;
    }

    private (AnimationProject Rig, MdlModel Model) Target(string target)
    {
        var model = target == "project" ? _source : Load(target)!;
        return (target == "project" ? _sourceRig : AnimationProject.FromModel(model), model);
    }

    private PosedNode[] NativeOnTarget(string name, float phase, AnimationProject rig, MdlModel model)
    {
        // Resolving from a_ba deliberately follows the same winning source as an
        // ordinary attack. a_ba_med_weap also defines xbowshot but is shadowed;
        // copying that older definition was the actual floating-hand regression.
        var native = Library(_source).Resolve(name).Animation;
        var sampled = MdlAnimationPose.Sample(native, phase * native.Length, MdlAnimationPose.BindPose(_source));
        return rig.Joints.Select(destination =>
        {
            var source = destination.Parent < 0 ? _sourceRig.Joints[0] : _sourceRig.Joints.Single(j => j.Name.Equals(destination.Name, StringComparison.OrdinalIgnoreCase));
            var value = sampled.GetValueOrDefault(source.Name, source.Rest);
            return new PosedNode(destination.Rest.Position + value.Position - source.Rest.Position,
                Quaternion.Normalize(destination.Rest.Orientation * Quaternion.Inverse(source.Rest.Orientation) * value.Orientation),
                destination.Rest.Scale * value.Scale / source.Rest.Scale);
        }).ToArray();
    }

    private InstalledMotionLibrary Library(MdlModel model)
    {
        if (!_installed.TryGetValue(model.Name, out var library))
            _installed.Add(model.Name, library = new InstalledMotionLibrary(model, Load));
        return library;
    }

    private PosedNode[] Sample(string id, float time, string target, AnimationProject rig, MdlModel model)
    {
        if (target == "project") return _projects[id].Sample(time);
        var registered = _registry.Single(entry => entry.Name == id);
        var resolved = Library(model).Resolve(registered.AnimationName);
        var sampled = MdlAnimationPose.Sample(resolved.Animation, time, MdlAnimationPose.BindPose(model));
        return rig.Joints.Select(joint => sampled.TryGetValue(joint.Name, out var pose)
            ? pose with { Position = pose.Position * model.Scale } : joint.Rest).ToArray();
    }

    private static Matrix4x4 HandRelativeToWeapon(AnimationProject rig, PosedNode[] pose)
    {
        var world = AnimationRig.World(rig.Joints, pose);
        Matrix4x4.Invert(world[Joint(rig, "rhand")], out var inverse);
        return world[Joint(rig, "lhand_g")] * inverse;
    }

    private static (float Pitch, float Yaw) MuzzleAngles(AnimationProject rig, PosedNode[] pose)
    {
        var weapon = AnimationRig.World(rig.Joints, pose)[Joint(rig, "rhand")];
        var direction = Vector3.Normalize(Vector3.TransformNormal(-Vector3.UnitZ, weapon));
        return (MathF.Asin(direction.Z) * 180 / MathF.PI, MathF.Atan2(direction.X, direction.Y) * 180 / MathF.PI);
    }

    private static int Joint(AnimationProject rig, string name) => rig.Joints.FindIndex(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    private static float RotationDegrees(Quaternion first, Quaternion second) => 2 * MathF.Acos(Math.Clamp(Math.Abs(
        Quaternion.Dot(Quaternion.Normalize(first), Quaternion.Normalize(second))), 0, 1)) * 180 / MathF.PI;
    private static float WrappedAngle(float angle) => (angle + 540) % 360 - 180;
}
