using System.Numerics;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Domain.Animation;

public sealed record RetargetBinding(string Target, string Source);

/// <summary>Captures world orientation offsets at a posed calibration frame, then bakes local NWN transforms.</summary>
public sealed class AnimationRetarget
{
    private readonly AnimationProject _rig;
    private readonly PosedNode[] _referencePose;
    private readonly Dictionary<int, (int Source, Quaternion Offset, Vector3 SourcePosition, Quaternion MotionRotation)> _bindings;
    private readonly SourceJoint[] _sourceJoints;
    private readonly Matrix4x4[] _targetWorld;

    public AnimationRetarget(AnimationProject rig, PosedNode[] pose, GltfAnimationSource source,
        int clip, float time, IEnumerable<RetargetBinding> bindings)
    {
        _rig = rig.Clone(); _referencePose = (PosedNode[])pose.Clone();
        _targetWorld = AnimationRig.World(rig.Joints, pose);
        var sourceWorld = source.Sample(clip, time);
        _sourceJoints = source.Joints.ToArray();
        _bindings = [];
        foreach (var binding in bindings)
        {
            var target = rig.Joints.FindIndex(j => j.Name == binding.Target);
            var matches = source.Joints.Select((joint, index) => (joint, index)).Where(p => p.joint.Name == binding.Source).ToArray();
            if (target < 0 || matches.Length != 1 || _bindings.ContainsKey(target))
                throw new InvalidDataException($"Mapping '{binding.Target}' to '{binding.Source}' is missing or ambiguous.");
            var index = matches[0].index;
            if (!Matrix4x4.Decompose(sourceWorld[index], out _, out var sourceRotation, out _) ||
                !Matrix4x4.Decompose(_targetWorld[target], out _, out var targetRotation, out _))
                throw new InvalidDataException("Calibration transform cannot be decomposed.");
            _bindings.Add(target, (index, Quaternion.Normalize(Quaternion.Inverse(sourceRotation) * targetRotation), sourceWorld[index].Translation,
                Quaternion.Normalize(targetRotation * Quaternion.Inverse(sourceRotation))));
        }
        if (_bindings.Count == 0) throw new InvalidDataException("Map at least one joint before locking calibration.");
    }

    public AnimationProject Bake(GltfAnimationSource source, int clip, int framesPerSecond, float rootScale)
    {
        if (framesPerSecond is < 1 or > 60 || !float.IsFinite(rootScale) || rootScale <= 0 || rootScale > 1000)
            throw new InvalidDataException("Bake rate must be 1–60 FPS and root scale must be positive (at most 1000).");
        if (!_sourceJoints.SequenceEqual(source.Joints))
            throw new InvalidDataException("Source skeleton changed. Lock calibration again.");
        var result = _rig.Clone(); result.Keys.Clear(); result.Events.Clear();
        result.Duration = source.GetPlaybackDuration(clip);
        var frames = source.Animations[clip].Duration == 0 ? 0 : (int)Math.Ceiling(result.Duration * framesPerSecond);
        if (frames + 1 > AnimationProject.MaxKeyframes)
            throw new InvalidDataException($"Bake exceeds {AnimationProject.MaxKeyframes:N0} keyframes. Choose a lower bake rate.");
        if ((long)(frames + 1) * (result.Joints.Count + source.Joints.Count) > 2_000_000)
            throw new InvalidDataException("Bake exceeds the source and target transform budget. Choose a shorter clip or a lower bake rate.");
        AnimationProject.ValidateSizeBudget(frames + 1, result.Joints.Count);
        var root = result.Joints.FindIndex(j => j.Name.Equals(result.AnimationRoot, StringComparison.OrdinalIgnoreCase));
        for (var frame = 0; frame <= frames; frame++)
        {
            var time = Math.Min(result.Duration, (float)frame / framesPerSecond);
            var sourceWorld = source.Sample(clip, time);
            var pose = (PosedNode[])_referencePose.Clone();
            var world = new Matrix4x4[pose.Length];
            for (var i = 0; i < pose.Length; i++)
            {
                var parent = result.Joints[i].Parent;
                var parentWorld = parent < 0 ? Matrix4x4.Identity : world[parent];
                if (_bindings.TryGetValue(i, out var binding))
                {
                    if (!Matrix4x4.Decompose(sourceWorld[binding.Source], out _, out var sourceRotation, out _) ||
                        !Matrix4x4.Decompose(parentWorld, out _, out var parentRotation, out _))
                        throw new InvalidDataException("Source animation contains a singular transform.");
                    pose[i] = pose[i] with { Orientation = Quaternion.Normalize(Quaternion.Inverse(parentRotation) * sourceRotation * binding.Offset) };
                    if (i == root)
                    {
                        if (!Matrix4x4.Invert(parentWorld, out var inverse)) throw new InvalidDataException("Target parent is singular.");
                        var delta = sourceWorld[binding.Source].Translation - binding.SourcePosition;
                        var position = _targetWorld[i].Translation + Vector3.Transform(delta, binding.MotionRotation) * rootScale;
                        pose[i] = pose[i] with { Position = Vector3.Transform(position, inverse) };
                    }
                }
                world[i] = AnimationRig.Local(pose[i]) * parentWorld;
            }
            // Baking already produces ascending times and fresh pose arrays. Append directly;
            // interactive SetKey searches and sorts the timeline and would make dense bakes quadratic.
            var key = new AnimationKey(time, pose);
            if (result.Keys.Count > 0 && result.Keys[^1].Time == time) result.Keys[^1] = key;
            else result.Keys.Add(key);
        }
        _ = result.Serialize(); // Check the exact serialized size before returning a publishable bake.
        return result;
    }
}
