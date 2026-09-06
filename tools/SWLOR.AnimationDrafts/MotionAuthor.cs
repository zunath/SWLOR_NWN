using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using SWLOR.Toolset.Domain.Animation;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.AnimationDrafts;

internal sealed record Recipe(string Workbook, string Model, JsonObject ReadyShield, JsonObject ReadyBlade, Motion[] Motions)
{
    public static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };
}
internal sealed record Motion(string Id, string Name, int BibleRow, string Reference, string Observation,
    string Interpretation, bool Shield, Beat[] Beats);
internal sealed record Beat(float Time, string Label, JsonObject Pose);

internal static class MotionAuthor
{
    public static AnimationProject Bake(AnimationProject rig, Recipe recipe, Motion motion)
    {
        var beats = motion.Beats;
        if (beats.Length < 2 || beats[0].Time != 0 || beats[^1].Time is <= 0 or > 10 ||
            beats.Zip(beats.Skip(1)).Any(p => !float.IsFinite(p.Second.Time) || p.Second.Time <= p.First.Time))
            throw new InvalidDataException($"{motion.Id}: beats must increase from zero to a duration of at most ten seconds.");
        var ready = motion.Shield ? recipe.ReadyShield : recipe.ReadyBlade;
        var poses = beats.Select(beat => Read(ready, beat.Pose)).ToArray();
        var result = rig.Clone(); result.Keys.Clear(); result.Events.Clear(); result.Name = motion.Id;
        result.Duration = beats[^1].Time; result.Transition = .15f;
        var times = new SortedSet<float>(beats.Select(b => b.Time));
        for (var i = 0; i / 20f < result.Duration; i++) times.Add(i / 20f);
        var segment = 0;
        foreach (var time in times)
        {
            while (segment < beats.Length - 2 && time > beats[segment + 1].Time) segment++;
            var fraction = Math.Clamp((time - beats[segment].Time) / (beats[segment + 1].Time - beats[segment].Time), 0, 1);
            var eased = fraction * fraction * (3 - 2 * fraction);
            var p = Pose.Lerp(poses[segment], poses[segment + 1], eased);
            result.SetKey(time, PoseRig(rig, p, motion.Id, time, motion.Shield));
        }
        result.Validate();
        var start = result.Sample(0); var finish = result.Sample(result.Duration);
        for (var i = 0; i < start.Length; i++)
            if (Vector3.Distance(start[i].Position, finish[i].Position) > .0001f ||
                Math.Abs(Quaternion.Dot(start[i].Orientation, finish[i].Orientation)) < .99999f)
                throw new InvalidDataException($"{motion.Id}: last pose must return to its ready pose.");
        for (var t = 0f; t <= result.Duration; t += 1f / 120)
        {
            var world = AnimationRig.World(rig.Joints, result.Sample(t));
            foreach (var name in new[] { "lfoot_g", "rfoot_g" })
                if (world[Index(rig, name)].Translation.Z < .125f)
                    throw new InvalidDataException($"{motion.Id}: foot penetrates the floor at {t:0.000}s.");
        }
        return result;
    }

    private static Pose Read(JsonObject ready, JsonObject patch)
    {
        Vector3 V(string name)
        {
            var a = (patch[name] ?? ready[name])?.AsArray() ?? throw new InvalidDataException("Missing pose field: " + name);
            if (a.Count != 3) throw new InvalidDataException("Pose vectors need three components: " + name);
            var v = new Vector3(a[0]!.GetValue<float>(), a[1]!.GetValue<float>(), a[2]!.GetValue<float>());
            if (!AnimationProject.Finite(v)) throw new InvalidDataException("Nonfinite pose: " + name);
            return v;
        }
        var blade = V("blade");
        if (blade.LengthSquared() < .01f) throw new InvalidDataException("Blade direction is zero.");
        return new(V("root"), V("chest"), V("hips"), V("leftHand"), V("rightHand"), blade,
            V("leftFoot"), V("rightFoot"), V("shield"));
    }

    private static PosedNode[] PoseRig(AnimationProject rig, Pose p, string name, float time, bool shield)
    {
        var pose = rig.Joints.Select(j => j.Rest).ToArray();
        var root = Index(rig, "rootdummy");
        pose[root] = pose[root] with { Position = p.Root, Orientation = Quaternion.Identity };
        SetLocal("torso_g", Rotation(p.Chest)); SetLocal("pelvis_g", Rotation(p.Hips));
        SetWorld("head_g", Quaternion.CreateFromAxisAngle(Vector3.UnitZ, Radians(p.Chest.Y * .2f)));
        var centre = new Vector3(p.Root.X, p.Root.Y, 0);
        Solve("lfoot_g", p.LeftFoot, new Vector3(-.42f, 1.1f, .4f) + centre);
        Solve("rfoot_g", p.RightFoot, new Vector3(.42f, 1.1f, .4f) + centre);
        SetWorld("lfoot_g", Quaternion.CreateFromAxisAngle(Vector3.UnitZ, Radians(8)));
        SetWorld("rfoot_g", Quaternion.CreateFromAxisAngle(Vector3.UnitZ, Radians(-12)));
        Solve("lhand_g", p.LeftHand, new Vector3(-.85f, -.20f, p.Root.Z + .12f) + centre);
        Solve("rhand_g", p.RightHand, new Vector3(.85f, -.20f, p.Root.Z + .12f) + centre);
        SetWorld("rhand_g", AnimationRig.Between(Vector3.UnitY, Vector3.Normalize(p.Blade)));
        // Native swords extend along +Y; native shields face -X at the hand attachment.
        SetWorld("lhand_g", Rotation(p.Shield) * (shield
            ? Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -MathF.PI / 2) : Quaternion.Identity));
        return pose;

        void Solve(string joint, Vector3 target, Vector3 pole)
        {
            var i = Index(rig, joint);
            pose = AnimationRig.SolveLimb(rig.Joints, pose, i, target, pole);
            var error = Vector3.Distance(AnimationRig.World(rig.Joints, pose)[i].Translation, target);
            if (error > .012f) throw new InvalidDataException($"{name} at {time:0.000}s: {joint} target exceeds reach by {error:0.000}m.");
        }
        void SetLocal(string joint, Quaternion orientation)
        {
            var i = Index(rig, joint); pose[i] = pose[i] with { Orientation = Quaternion.Normalize(orientation) };
        }
        void SetWorld(string joint, Quaternion orientation)
        {
            var i = Index(rig, joint);
            var world = AnimationRig.World(rig.Joints, pose);
            Matrix4x4.Decompose(world[rig.Joints[i].Parent], out _, out var parent, out _);
            pose[i] = pose[i] with { Orientation = Quaternion.Normalize(Quaternion.Inverse(parent) * orientation) };
        }
    }

    private static int Index(AnimationProject rig, string name)
    {
        var i = rig.Joints.FindIndex(j => j.Name == name);
        return i >= 0 ? i : throw new InvalidDataException("Rig is missing " + name);
    }
    private static float Radians(float degrees) => degrees * MathF.PI / 180;
    private static Quaternion Rotation(Vector3 degrees) =>
        Quaternion.CreateFromAxisAngle(Vector3.UnitZ, Radians(degrees.Y)) *
        Quaternion.CreateFromAxisAngle(Vector3.UnitY, Radians(degrees.Z)) *
        Quaternion.CreateFromAxisAngle(Vector3.UnitX, Radians(-degrees.X));

    private sealed record Pose(Vector3 Root, Vector3 Chest, Vector3 Hips, Vector3 LeftHand, Vector3 RightHand,
        Vector3 Blade, Vector3 LeftFoot, Vector3 RightFoot, Vector3 Shield)
    {
        public static Pose Lerp(Pose a, Pose b, float t) => new(
            Vector3.Lerp(a.Root, b.Root, t), Vector3.Lerp(a.Chest, b.Chest, t), Vector3.Lerp(a.Hips, b.Hips, t),
            Vector3.Lerp(a.LeftHand, b.LeftHand, t), Vector3.Lerp(a.RightHand, b.RightHand, t), Vector3.Lerp(a.Blade, b.Blade, t),
            Vector3.Lerp(a.LeftFoot, b.LeftFoot, t), Vector3.Lerp(a.RightFoot, b.RightFoot, t), Vector3.Lerp(a.Shield, b.Shield, t));
    }
}
