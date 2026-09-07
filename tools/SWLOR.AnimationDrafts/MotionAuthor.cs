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
    string Interpretation, bool Shield, Beat[] Beats, string AbilityDefinition, bool Loop = false, bool NaturalGrip = false);
internal sealed record Beat(float Time, string Label, JsonObject Pose, bool Through = false);

internal static class MotionAuthor
{
    public static AnimationProject Bake(AnimationProject rig, PosedNode[] neutral, Recipe recipe, Motion motion)
    {
        var beats = motion.Beats;
        if (!motion.Loop && beats.Length < 3)
            throw new InvalidDataException($"{motion.Id}: non-looping motions require at least three beats for entry and release.");
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
            var p = Pose.Interpolate(poses, beats, segment, fraction);
            var pose = PoseRig(rig, p, motion.Id, time, motion.NaturalGrip);
            // A one-shot must release its authored stance even when no walking/attack follows it.
            // Channels retain a closed guard loop; their installed exit phase releases the stance.
            if (!motion.Loop)
            {
                var weight = time <= beats[1].Time ? time / beats[1].Time :
                    time >= beats[^2].Time ? (result.Duration - time) / (result.Duration - beats[^2].Time) : 1f;
                weight = weight * weight * (3 - 2 * weight);
                var fromWorld = AnimationRig.World(rig.Joints, neutral);
                var toWorld = AnimationRig.World(rig.Joints, pose);
                for (var i = 0; i < pose.Length; i++)
                    pose[i] = new(Vector3.Lerp(neutral[i].Position, pose[i].Position, weight),
                        Quaternion.Slerp(neutral[i].Orientation, pose[i].Orientation, weight),
                        float.Lerp(neutral[i].Scale, pose[i].Scale, weight));
                // Rotation interpolation alone arcs the feet through the floor while stepping
                // between idle and the wider guard stance. Keep their world-space path grounded.
                foreach (var foot in new[] { "lfoot_g", "rfoot_g" })
                {
                    var i = Index(rig, foot);
                    var target = Vector3.Lerp(fromWorld[i].Translation, toWorld[i].Translation, weight);
                    pose = AnimationRig.SolveLimb(rig.Joints, pose, i, target, target + new Vector3(0, 1, .3f));
                    Matrix4x4.Decompose(fromWorld[i], out _, out var startRotation, out _);
                    Matrix4x4.Decompose(toWorld[i], out _, out var endRotation, out _);
                    Matrix4x4.Decompose(AnimationRig.World(rig.Joints, pose)[rig.Joints[i].Parent], out _, out var parent, out _);
                    pose[i] = pose[i] with { Orientation = Quaternion.Normalize(Quaternion.Inverse(parent) * Quaternion.Slerp(startRotation, endRotation, weight)) };
                }
                if (weight == 0) pose = (PosedNode[])neutral.Clone();
            }
            result.SetKey(time, pose);
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

    private static PosedNode[] PoseRig(AnimationProject rig, Pose p, string name, float time, bool naturalGrip)
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
        // Keep bent elbows below the hands and near the ribs. Wide, shoulder-height poles
        // produce an outward elbow flare even when the hand is in a low guard.
        Solve("lhand_g", p.LeftHand, new Vector3(-.60f, .35f, p.Root.Z + .12f) + centre);
        Solve("rhand_g", p.RightHand, (naturalGrip
            ? new Vector3(.52f, -.16f, p.RightHand.Z - .15f)
            : new Vector3(.36f, -.12f, p.Root.Z - .18f)) + centre);
        // Preserve the native hand's roll around the blade, rather than arbitrarily twisting
        // the wrist when the sword changes direction. Native +Z points toward the wrist/elbow.
        var world = AnimationRig.World(rig.Joints, pose);
        var blade = Vector3.Normalize(p.Blade);
        if (naturalGrip)
        {
            // Let forearm roll carry the grip, then use the smallest wrist swing to aim.
            // The offset elbow guide keeps this thrust clear of the straight-arm singularity.
            var rightHand = Index(rig, "rhand_g");
            var rightForearm = rig.Joints[rightHand].Parent;
            Matrix4x4.Decompose(world[rightForearm], out _, out var parentRotation, out _);
            var grip = Quaternion.Normalize(parentRotation * rig.Joints[rightHand].Rest.Orientation);
            var armAxis = Vector3.Normalize(world[rightHand].Translation - world[rightForearm].Translation);
            var gripForward = Vector3.Transform(Vector3.UnitY, grip);
            gripForward -= armAxis * Vector3.Dot(gripForward, armAxis);
            var aim = blade - armAxis * Vector3.Dot(blade, armAxis);
            if (gripForward.LengthSquared() < .01f || aim.LengthSquared() < .01f)
                throw new InvalidDataException($"{name} at {time:0.000}s: move the elbow off the blade line to retain a stable grip.");
            parentRotation = AnimationRig.Between(Vector3.Normalize(gripForward), Vector3.Normalize(aim)) * parentRotation;
            SetWorld("rforearm_g", parentRotation);
            grip = Quaternion.Normalize(parentRotation * rig.Joints[rightHand].Rest.Orientation);
            SetWorld("rhand_g", AnimationRig.Between(Vector3.Transform(Vector3.UnitY, grip), blade) * grip);
        }
        else
        {
            var towardElbow = world[Index(rig, "rforearm_g")].Translation - p.RightHand;
            var wrist = towardElbow - blade * Vector3.Dot(towardElbow, blade);
            if (wrist.LengthSquared() < .0001f) wrist = Vector3.UnitZ - blade * blade.Z;
            if (wrist.LengthSquared() < .0001f) wrist = Vector3.UnitX - blade * blade.X;
            wrist = Vector3.Normalize(wrist);
            var across = Vector3.Normalize(Vector3.Cross(blade, wrist));
            SetWorld("rhand_g", Quaternion.CreateFromRotationMatrix(new Matrix4x4(
                across.X, across.Y, across.Z, 0, blade.X, blade.Y, blade.Z, 0,
                wrist.X, wrist.Y, wrist.Z, 0, 0, 0, 0, 1)));
        }
        // NWN straps shields to lforearm, not lhand_g. Roll the forearm about its
        // elbow-to-wrist axis to turn the shield toward the target without moving the
        // hand, stretching a bone, or rotating the attachment independently of the arm.
        world = AnimationRig.World(rig.Joints, pose);
        var forearm = Index(rig, "lforearm_g");
        var hand = Index(rig, "lhand_g");
        var axis = Vector3.Normalize(world[hand].Translation - world[forearm].Translation);
        var normal = Vector3.TransformNormal(-Vector3.UnitX, world[Index(rig, "lforearm")]);
        normal = Vector3.Normalize(normal - axis * Vector3.Dot(normal, axis));
        var forward = Vector3.Transform(Vector3.UnitY, Rotation(p.Shield));
        forward -= axis * Vector3.Dot(forward, axis);
        if (forward.LengthSquared() < .01f)
            throw new InvalidDataException($"{name} at {time:0.000}s: shield arm points into the strike; bend the elbow across the guard.");
        Matrix4x4.Decompose(world[forearm], out _, out var forearmRotation, out _);
        SetWorld("lforearm_g", AnimationRig.Between(normal, Vector3.Normalize(forward)) * forearmRotation);
        // A neutral wrist follows the braced forearm. Rotating the hand to aim the
        // shield only twists the fingers in game, since the shield ignores that bone.
        pose[hand] = pose[hand] with { Orientation = rig.Joints[hand].Rest.Orientation };
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
        // A strike passes through contact poses without stopping. Shape-preserving
        // Hermite tangents keep that velocity continuous without overshooting limb
        // targets or dragging planted feet below the floor. Unmarked beats still rest.
        public static Pose Interpolate(Pose[] poses, Beat[] beats, int segment, float t)
        {
            Vector3 V(Func<Pose, Vector3> select)
            {
                var a = select(poses[segment]); var b = select(poses[segment + 1]);
                if (!beats[segment].Through && !beats[segment + 1].Through)
                    return Vector3.Lerp(a, b, t * t * (3 - 2 * t));
                var duration = beats[segment + 1].Time - beats[segment].Time;
                Vector3 Tangent(int i)
                {
                    if (i == 0 || i == poses.Length - 1 || !beats[i].Through) return Vector3.Zero;
                    var before = beats[i].Time - beats[i - 1].Time;
                    var after = beats[i + 1].Time - beats[i].Time;
                    var left = (select(poses[i]) - select(poses[i - 1])) / before;
                    var right = (select(poses[i + 1]) - select(poses[i])) / after;
                    float Slope(float l, float r)
                    {
                        if (l * r <= 0) return 0;
                        var w1 = 2 * after + before; var w2 = after + 2 * before;
                        return (w1 + w2) / (w1 / l + w2 / r);
                    }
                    return new(Slope(left.X, right.X), Slope(left.Y, right.Y), Slope(left.Z, right.Z));
                }
                var t2 = t * t; var t3 = t2 * t;
                return (2 * t3 - 3 * t2 + 1) * a + (t3 - 2 * t2 + t) * duration * Tangent(segment) +
                    (-2 * t3 + 3 * t2) * b + (t3 - t2) * duration * Tangent(segment + 1);
            }
            return new(V(p => p.Root), V(p => p.Chest), V(p => p.Hips), V(p => p.LeftHand), V(p => p.RightHand),
                V(p => p.Blade), V(p => p.LeftFoot), V(p => p.RightFoot), V(p => p.Shield));
        }
    }
}
