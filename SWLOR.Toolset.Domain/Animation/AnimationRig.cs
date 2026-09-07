using System.Numerics;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Domain.Animation;

/// <summary>Hierarchy transforms and analytic two-bone IK, independent of a rendering engine.</summary>
public static class AnimationRig
{
    public static PosedNode[] SetJoint(IReadOnlyList<AnimationJoint> joints, PosedNode[] pose,
        int selected, PosedNode value, bool anchorLimbs)
    {
        var next = (PosedNode[])pose.Clone(); next[selected] = value;
        if (!anchorLimbs || !joints[selected].Name.Equals("pelvis_g", StringComparison.OrdinalIgnoreCase) &&
            !joints[selected].Name.Equals("rootdummy", StringComparison.OrdinalIgnoreCase)) return next;
        var before = World(joints, pose);
        for (var i = 0; i < joints.Count; i++)
        {
            var name = joints[i].Name;
            if (!(name.Equals("lhand_g", StringComparison.OrdinalIgnoreCase) || name.Equals("rhand_g", StringComparison.OrdinalIgnoreCase) ||
                  name.Equals("lfoot_g", StringComparison.OrdinalIgnoreCase) || name.Equals("rfoot_g", StringComparison.OrdinalIgnoreCase)) || joints[i].Parent < 0) continue;
            var ancestor = joints[i].Parent;
            while (ancestor >= 0 && ancestor != selected) ancestor = joints[ancestor].Parent;
            if (ancestor < 0) continue;
            next = SolveLimb(joints, next, i, before[i].Translation, before[joints[i].Parent].Translation);
            var world = World(joints, next);
            Matrix4x4.Decompose(before[i], out _, out var originalRotation, out _);
            Matrix4x4.Decompose(world[joints[i].Parent], out _, out var parentRotation, out _);
            next[i] = next[i] with { Orientation = Quaternion.Normalize(Quaternion.Inverse(parentRotation) * originalRotation) };
        }
        return next;
    }
    public static Matrix4x4 Local(PosedNode p) => Matrix4x4.CreateScale(p.Scale) *
        Matrix4x4.CreateFromQuaternion(p.Orientation) * Matrix4x4.CreateTranslation(p.Position);

    public static Matrix4x4[] World(IReadOnlyList<AnimationJoint> joints, IReadOnlyList<PosedNode> pose)
    {
        if (joints.Count != pose.Count) throw new ArgumentException("Pose does not match rig.");
        var world = new Matrix4x4[joints.Count];
        for (var i = 0; i < joints.Count; i++)
            world[i] = joints[i].Parent < 0 ? Local(pose[i]) : Local(pose[i]) * world[joints[i].Parent];
        return world;
    }

    public static PosedNode[] SolveLimb(IReadOnlyList<AnimationJoint> joints, PosedNode[] pose,
        int end, Vector3 target, Vector3 pole)
    {
        if ((uint)end >= joints.Count || joints[end].Parent < 0 || joints[joints[end].Parent].Parent < 0)
            throw new InvalidDataException("IK needs an end joint with two ancestors.");
        if (!AnimationProject.Finite(target) || !AnimationProject.Finite(pole)) throw new InvalidDataException("Invalid IK target.");
        var result = (PosedNode[])pose.Clone();
        var mid = joints[end].Parent; var root = joints[mid].Parent;
        var world = World(joints, result);
        var a = world[root].Translation; var b = world[mid].Translation; var c = world[end].Translation;
        var l1 = Vector3.Distance(a, b); var l2 = Vector3.Distance(b, c);
        if (l1 < 0.00001f || l2 < 0.00001f) throw new InvalidDataException("IK joints have zero-length bones.");
        var direction = target - a;
        if (direction.LengthSquared() < 1e-10f) direction = c - a;
        if (direction.LengthSquared() < 1e-10f) direction = Vector3.UnitZ;
        direction = Vector3.Normalize(direction);
        var distance = Math.Clamp(Vector3.Distance(a, target), Math.Abs(l1 - l2) + 0.000001f, l1 + l2 - 0.000001f);
        var bend = pole - a - Vector3.Dot(pole - a, direction) * direction;
        if (bend.LengthSquared() < 1e-10f)
            bend = Vector3.Cross(direction, Math.Abs(direction.Z) < 0.9f ? Vector3.UnitZ : Vector3.UnitX);
        bend = Vector3.Normalize(bend);
        var along = (l1 * l1 + distance * distance - l2 * l2) / (2 * distance);
        var elbow = a + direction * along + bend * MathF.Sqrt(Math.Max(0, l1 * l1 - along * along));
        RotateWorld(root, b - a, elbow - a);
        world = World(joints, result);
        RotateWorld(mid, world[end].Translation - world[mid].Translation, a + direction * distance - world[mid].Translation);
        // Keep the hand/foot's original world orientation while the limb moves.
        var originalWorld = World(joints, pose);
        world = World(joints, result);
        Matrix4x4.Decompose(originalWorld[end], out _, out var endRotation, out _);
        Matrix4x4.Decompose(world[mid], out _, out var parentRotation, out _);
        result[end] = result[end] with { Orientation = Quaternion.Normalize(Quaternion.Inverse(parentRotation) * endRotation) };
        return result;

        void RotateWorld(int index, Vector3 from, Vector3 to)
        {
            Matrix4x4.Decompose(world[index], out _, out var rotation, out _);
            var changed = Between(from, to) * rotation;
            var parent = joints[index].Parent;
            var parentRotation = Quaternion.Identity;
            if (parent >= 0) Matrix4x4.Decompose(world[parent], out _, out parentRotation, out _);
            result[index] = result[index] with { Orientation = Quaternion.Normalize(Quaternion.Inverse(parentRotation) * changed) };
        }
    }

    public static Quaternion Between(Vector3 from, Vector3 to)
    {
        from = Vector3.Normalize(from); to = Vector3.Normalize(to);
        var dot = Math.Clamp(Vector3.Dot(from, to), -1, 1);
        if (dot > 0.999999f) return Quaternion.Identity;
        if (dot < -0.999999f)
            return Quaternion.CreateFromAxisAngle(Vector3.Normalize(Vector3.Cross(from,
                Math.Abs(from.Z) < 0.9f ? Vector3.UnitZ : Vector3.UnitX)), MathF.PI);
        return Quaternion.Normalize(new Quaternion(Vector3.Cross(from, to), 1 + dot));
    }
}
