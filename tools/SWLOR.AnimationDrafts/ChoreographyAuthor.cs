using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Animation;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.AnimationDrafts;

internal sealed record Choreography(string Id, string Description, float Duration, ChoreographyBeat[] Beats,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool InPlace = false);
internal sealed record ChoreographyBeat(float Time, string Label, string SourceAnimation, float SourceTime = 0,
    float[]? LeftHand = null, float[]? RightHand = null, float[]? TorsoDegrees = null, string? SourceModel = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] float[]? RootOffset = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool FollowSourceMotion = false);

/// <summary>Native pose choreography with explicit hand paths and native local grips.
/// Recipes are editable motion direction, not inferred motion capture.</summary>
internal static class ChoreographyAuthor
{
    internal static Choreography[] Read(string text)
    {
        var recipes = JsonSerializer.Deserialize<Choreography[]>(text, BulkMotionAuthor.Json)
            ?? throw new InvalidDataException("Empty choreography document.");
        if (recipes.Select(r => r.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != recipes.Length)
            throw new InvalidDataException("Duplicate choreography identity.");
        foreach (var recipe in recipes) Validate(recipe);
        return recipes;
    }

    private static void Validate(Choreography recipe)
    {
        if (string.IsNullOrWhiteSpace(recipe.Id) || string.IsNullOrWhiteSpace(recipe.Description) ||
            !float.IsFinite(recipe.Duration) || recipe.Duration is < .4f or > 10 || recipe.Beats == null || recipe.Beats.Length < 3 ||
            recipe.Beats[0].Time != 0 || recipe.Beats[^1].Time != recipe.Duration)
            throw new InvalidDataException("Choreography requires an identity, description and timed entry/recovery: " + recipe.Id);
        for (int i = 0; i < recipe.Beats.Length; i++)
        {
            var beat = recipe.Beats[i];
            if (!float.IsFinite(beat.Time) || i > 0 && beat.Time <= recipe.Beats[i - 1].Time ||
                string.IsNullOrWhiteSpace(beat.Label) || string.IsNullOrWhiteSpace(beat.SourceAnimation) ||
                !float.IsFinite(beat.SourceTime) || beat.SourceTime is < 0 or > 1)
                throw new InvalidDataException("Invalid choreography beat: " + recipe.Id);
            if (beat.SourceModel != null && !System.Text.RegularExpressions.Regex.IsMatch(beat.SourceModel, "^[a-z0-9_]{1,16}$"))
                throw new InvalidDataException("Source model must be a native resource name.");
            foreach (var vector in new[] { beat.LeftHand, beat.RightHand, beat.TorsoDegrees, beat.RootOffset })
                if (vector != null && (vector.Length != 3 || vector.Any(v => !float.IsFinite(v))))
                    throw new InvalidDataException("Choreography vectors require three finite coordinates.");
            if (beat.TorsoDegrees?.Any(v => Math.Abs(v) > 35) == true)
                throw new InvalidDataException("Choreography torso adjustments must stay within 35 degrees.");
            if (beat.RootOffset is { } offset && (Math.Abs(offset[0]) > .15f || Math.Abs(offset[1]) > .15f || Math.Abs(offset[2]) > .1f))
                throw new InvalidDataException("Choreography root offsets must stay within 15 cm horizontally and 10 cm vertically.");
            if (beat.FollowSourceMotion && (i + 1 == recipe.Beats.Length ||
                recipe.Beats[i + 1].SourceAnimation != beat.SourceAnimation || recipe.Beats[i + 1].SourceModel != beat.SourceModel))
                throw new InvalidDataException("Following source motion requires the next beat to use the same source animation and model.");
        }
        foreach (var beat in new[] { recipe.Beats[0], recipe.Beats[^1] })
            if (beat.SourceAnimation != "pause1" || beat.SourceModel != null || beat.SourceTime != 0 || beat.LeftHand != null || beat.RightHand != null || beat.TorsoDegrees != null || beat.RootOffset != null || beat.FollowSourceMotion)
                throw new InvalidDataException("Choreography endpoints must use unmodified native idle.");
    }

    internal static AnimationProject Bake(MdlModel model, Choreography recipe, IReadOnlyDictionary<string, MdlModel>? sourceModels = null)
    {
        Validate(recipe);
        var rig = AnimationProject.FromModel(model);
        int Joint(string name) => rig.Joints.FindIndex(j => j.Name == name) is var i && i >= 0 ? i : throw new InvalidDataException("Missing joint: " + name);
        var bind = MdlAnimationPose.BindPose(model);
        MdlAnimation Source(string name, string? sourceModel)
        {
            var owner = sourceModel == null ? model : sourceModels != null && sourceModels.TryGetValue(sourceModel, out var supplied)
                ? supplied : throw new InvalidDataException("Missing choreography source model: " + sourceModel);
            return owner.Animations.SingleOrDefault(a => a.Name == name) ?? throw new InvalidDataException("Missing choreography source: " + name);
        }
        foreach (var beat in recipe.Beats) Source(beat.SourceAnimation, beat.SourceModel);
        PosedNode[] Native(string name, float fraction, string? sourceModel = null)
        {
            var clip = Source(name, sourceModel);
            var sampled = MdlAnimationPose.Sample(clip, fraction * clip.Length, bind);
            return rig.Joints.Select(j => sampled.TryGetValue(j.Name, out var node) ? node : j.Rest).ToArray();
        }
        static Vector3 V(float[] v) => new(v[0], v[1], v[2]);
        PosedNode[] Overlays(PosedNode[] pose, ChoreographyBeat first, ChoreographyBeat second, float blend)
        {
            if (first.TorsoDegrees != null || second.TorsoDegrees != null)
            {
                var angle = Vector3.Lerp(first.TorsoDegrees == null ? Vector3.Zero : V(first.TorsoDegrees),
                    second.TorsoDegrees == null ? Vector3.Zero : V(second.TorsoDegrees), blend) * (MathF.PI / 180);
                var torso = Joint("torso_g");
                pose[torso] = pose[torso] with { Orientation = Quaternion.Normalize(pose[torso].Orientation *
                    Quaternion.CreateFromYawPitchRoll(angle.Y, angle.X, angle.Z)) };
            }
            foreach (var (name, start, finish, side) in new[] { ("lhand_g", first.LeftHand, second.LeftHand, -1f), ("rhand_g", first.RightHand, second.RightHand, 1f) })
            {
                if (start == null && finish == null || blend == 0 && start == null || blend == 1 && finish == null) continue;
                var hand = Joint(name);
                var grip = pose[hand].Orientation;
                var world = AnimationRig.World(rig.Joints, pose);
                var influence = start == null ? blend : finish == null ? 1 - blend : 1;
                // A missing target means native arm motion. Solve the full authored
                // target, then fade the arm rotations as a layer; moving the target
                // first would ease it twice and still snap the elbow's bend plane.
                var position = start == null ? V(finish!) : finish == null ? V(start) : Vector3.Lerp(V(start), V(finish), blend);
                // Elbows remain below the wrist, with a stable lateral bend plane.
                var elbow = rig.Joints[hand].Parent;
                var shoulder = rig.Joints[elbow].Parent;
                var shoulderHeight = world[shoulder].Translation.Z;
                var minimumPoleHeight = Math.Min(.85f, shoulderHeight - .18f);
                var pole = new Vector3(side * .48f, .06f, Math.Max(minimumPoleHeight, position.Z - .28f));
                var nativeShoulder = pose[shoulder].Orientation;
                var nativeElbow = pose[elbow].Orientation;
                pose = AnimationRig.SolveLimb(rig.Joints, pose, hand, position, pole);
                if (influence < 1)
                {
                    pose[shoulder] = pose[shoulder] with { Orientation = Quaternion.Slerp(nativeShoulder, pose[shoulder].Orientation, influence) };
                    pose[elbow] = pose[elbow] with { Orientation = Quaternion.Slerp(nativeElbow, pose[elbow].Orientation, influence) };
                }
                // SolveLimb normally compensates wrist rotation to preserve world orientation.
                // For equipped characters preserve the sampled native LOCAL grip instead.
                pose[hand] = pose[hand] with { Orientation = grip };
            }
            return pose;
        }
        var nativePoses = recipe.Beats.Select(beat => Native(beat.SourceAnimation, beat.SourceTime, beat.SourceModel)).ToArray();
        var poses = recipe.Beats.Select((beat, index) => Overlays((PosedNode[])nativePoses[index].Clone(), beat, beat, 0)).ToArray();
        var neutral = Native("pause1", 0);
        var root = Joint("rootdummy");
        var feet = new[] { Joint("lfoot_g"), Joint("rfoot_g") };
        PosedNode[] ShiftWeight(PosedNode[] pose, Vector3 offset)
        {
            if (offset.LengthSquared() < 1e-12f) return pose;
            var world = AnimationRig.World(rig.Joints, pose);
            // A requested shift must not pull a foot beyond the leg's reach. Restrict
            // the body displacement, rather than accepting the IK solver's foot slide.
            bool Reachable(float amount)
            {
                foreach (var foot in feet)
                {
                    var knee = rig.Joints[foot].Parent; var hip = rig.Joints[knee].Parent;
                    var upper = Vector3.Distance(world[hip].Translation, world[knee].Translation);
                    var lower = Vector3.Distance(world[knee].Translation, world[foot].Translation);
                    var distance = Vector3.Distance(world[hip].Translation + offset * amount, world[foot].Translation);
                    if (distance > upper + lower + .000002f || distance < Math.Abs(upper - lower) - .000002f) return false;
                }
                return true;
            }
            if (!Reachable(1))
            {
                var low = 0f; var high = 1f;
                for (var i = 0; i < 20; i++)
                {
                    var middle = (low + high) / 2;
                    if (Reachable(middle)) low = middle; else high = middle;
                }
                offset *= low;
            }
            if (offset.LengthSquared() < 1e-12f) return pose;
            var localOffset = offset;
            if (rig.Joints[root].Parent >= 0)
            {
                if (!Matrix4x4.Invert(world[rig.Joints[root].Parent], out var inverse))
                    throw new InvalidDataException("Choreography root parent transform is singular.");
                localOffset = Vector3.TransformNormal(offset, inverse);
            }
            pose[root] = pose[root] with { Position = pose[root].Position + localOffset };
            foreach (var foot in feet)
            {
                var knee = rig.Joints[foot].Parent;
                // Move the native knee pole with the body. This retains the source's
                // bend plane while the foot follows its original sampled trajectory.
                pose = AnimationRig.SolveLimb(rig.Joints, pose, foot, world[foot].Translation,
                    world[knee].Translation + offset);
            }
            return pose;
        }
        var floor = feet.Min(i => AnimationRig.World(rig.Joints, neutral)[i].Translation.Z);
        var result = rig.Clone(); result.Name = recipe.Id; result.Duration = recipe.Duration; result.Transition = .12f;
        result.Keys.Clear(); result.Events.Clear();
        var times = new SortedSet<float>(recipe.Beats.Select(b => b.Time));
        for (int frame = 0; frame / 30f < result.Duration; frame++) times.Add(frame / 30f);
        // Coupled native grips can drift between sparse shoulder/elbow keys.
        // Refine only explicitly followed paths; other recipe bytes are stable.
        for (var index = 0; index < recipe.Beats.Length - 1; index++)
        {
            if (!recipe.Beats[index].FollowSourceMotion) continue;
            var start = recipe.Beats[index].Time; var end = recipe.Beats[index + 1].Time;
            for (var frame = (int)MathF.Ceiling(start * 60); frame / 60f < end; frame++)
                times.Add(frame / 60f);
        }
        var segment = 0;
        foreach (var time in times)
        {
            while (segment < recipe.Beats.Length - 2 && time > recipe.Beats[segment + 1].Time) segment++;
            var first = recipe.Beats[segment]; var second = recipe.Beats[segment + 1];
            var fraction = Math.Clamp((time - first.Time) / (second.Time - first.Time), 0, 1);
            var smooth = fraction * fraction * (3 - 2 * fraction);
            var pose = new PosedNode[neutral.Length];
            // Continuous native spans retain the source's real in-between motion (especially throws).
            var weightShift = first.RootOffset != null || second.RootOffset != null;
            // A held source phase contains no native in-between motion. Preserve the
            // previously reviewed interpolation unless a weight shift was authored.
            // Directed decreasing phases reset to an earlier pose for recovery or
            // another action. Blend those poses, retaining legacy native reverse
            // playback only when neither endpoint supplies hand/torso direction.
            // An explicit source path can instead retrace a known safe interval,
            // such as lowering a two-handed weapon without losing its grip.
            var directed = first.LeftHand != null || second.LeftHand != null || first.RightHand != null || second.RightHand != null ||
                first.TorsoDegrees != null || second.TorsoDegrees != null;
            var nativeSpan = first.SourceAnimation == second.SourceAnimation && first.SourceModel == second.SourceModel &&
                (first.FollowSourceMotion || !directed || second.SourceTime >= first.SourceTime && (weightShift || first.SourceTime != second.SourceTime));
            if (nativeSpan) pose = Native(first.SourceAnimation, float.Lerp(first.SourceTime, second.SourceTime, fraction), first.SourceModel);
            else
            {
                var frames = weightShift ? nativePoses : poses;
                for (int i = 0; i < pose.Length; i++)
                    pose[i] = new(Vector3.Lerp(frames[segment][i].Position, frames[segment + 1][i].Position, smooth),
                        Quaternion.Slerp(frames[segment][i].Orientation, frames[segment + 1][i].Orientation, smooth),
                        float.Lerp(frames[segment][i].Scale, frames[segment + 1][i].Scale, smooth));
            }
            if (recipe.InPlace)
                pose[root] = pose[root] with { Position = new Vector3(neutral[root].Position.X,
                    neutral[root].Position.Y, pose[root].Position.Z) };
            if (weightShift)
                pose = ShiftWeight(pose, Vector3.Lerp(first.RootOffset == null ? Vector3.Zero : V(first.RootOffset),
                    second.RootOffset == null ? Vector3.Zero : V(second.RootOffset), smooth));
            if (nativeSpan || weightShift) pose = Overlays(pose, first, second, smooth);
            var world = AnimationRig.World(rig.Joints, pose);
            var penetration = floor - feet.Min(i => world[i].Translation.Z);
            if (penetration > 0 && time > 0 && time < result.Duration)
                pose[root] = pose[root] with { Position = pose[root].Position + Vector3.UnitZ * penetration };
            result.SetKey(time, pose);
        }
        // Fast native leg rotations can arc below the floor between otherwise safe
        // 30 Hz keys. Refine only clips that fail the existing clearance tolerance,
        // preserving the bytes of previously reviewed choreography.
        float Penetration(PosedNode[] pose)
        {
            var world = AnimationRig.World(rig.Joints, pose);
            return floor - feet.Min(i => world[i].Translation.Z);
        }
        var sampleCount = (int)Math.Ceiling(result.Duration * 120);
        if (Enumerable.Range(1, sampleCount - 1).Any(frame => Penetration(result.Sample(frame / 120f)) > .025f))
        {
            for (int pass = 0; pass < 3; pass++)
            for (int frame = 1; frame < sampleCount; frame++)
            {
                var time = frame / 120f;
                var pose = result.Sample(time);
                var penetration = Penetration(pose);
                if (penetration <= .002f) continue;
                pose[root] = pose[root] with { Position = pose[root].Position + Vector3.UnitZ * penetration };
                result.SetKey(time, pose);
            }
        }
        result.Validate();
        BulkMotionAuthor.ValidateMotion(result, neutral, floor);
        return result;
    }

}
