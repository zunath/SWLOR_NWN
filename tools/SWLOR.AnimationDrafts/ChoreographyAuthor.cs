using System.Numerics;
using System.Text.Json;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Animation;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.AnimationDrafts;

internal sealed record Choreography(string Id, string Description, float Duration, ChoreographyBeat[] Beats);
internal sealed record ChoreographyBeat(float Time, string Label, string SourceAnimation, float SourceTime = 0,
    float[]? LeftHand = null, float[]? RightHand = null, float[]? TorsoDegrees = null, string? SourceModel = null);

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
            foreach (var vector in new[] { beat.LeftHand, beat.RightHand, beat.TorsoDegrees })
                if (vector != null && (vector.Length != 3 || vector.Any(v => !float.IsFinite(v))))
                    throw new InvalidDataException("Choreography vectors require three finite coordinates.");
            if (beat.TorsoDegrees?.Any(v => Math.Abs(v) > 35) == true)
                throw new InvalidDataException("Choreography torso adjustments must stay within 35 degrees.");
        }
        foreach (var beat in new[] { recipe.Beats[0], recipe.Beats[^1] })
            if (beat.SourceAnimation != "pause1" || beat.SourceModel != null || beat.SourceTime != 0 || beat.LeftHand != null || beat.RightHand != null || beat.TorsoDegrees != null)
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
        PosedNode[] Pose(ChoreographyBeat beat)
        {
            var pose = Native(beat.SourceAnimation, beat.SourceTime, beat.SourceModel);
            if (beat.TorsoDegrees != null)
            {
                var angle = V(beat.TorsoDegrees) * (MathF.PI / 180);
                var torso = Joint("torso_g");
                pose[torso] = pose[torso] with { Orientation = Quaternion.Normalize(pose[torso].Orientation *
                    Quaternion.CreateFromYawPitchRoll(angle.Y, angle.X, angle.Z)) };
            }
            foreach (var (name, target, side) in new[] { ("lhand_g", beat.LeftHand, -1f), ("rhand_g", beat.RightHand, 1f) })
            {
                if (target == null) continue;
                var hand = Joint(name);
                var grip = pose[hand].Orientation;
                var position = V(target);
                // Elbows remain below the wrist, with a stable lateral bend plane.
                pose = AnimationRig.SolveLimb(rig.Joints, pose, hand, position,
                    new Vector3(side * .48f, .06f, Math.Max(.85f, position.Z - .28f)));
                // SolveLimb normally compensates wrist rotation to preserve world orientation.
                // For equipped characters preserve the sampled native LOCAL grip instead.
                pose[hand] = pose[hand] with { Orientation = grip };
            }
            return pose;
        }
        var poses = recipe.Beats.Select(Pose).ToArray();
        var neutral = Native("pause1", 0);
        var root = Joint("rootdummy");
        var feet = new[] { Joint("lfoot_g"), Joint("rfoot_g") };
        var floor = feet.Min(i => AnimationRig.World(rig.Joints, neutral)[i].Translation.Z);
        var result = rig.Clone(); result.Name = recipe.Id; result.Duration = recipe.Duration; result.Transition = .12f;
        result.Keys.Clear(); result.Events.Clear();
        var times = new SortedSet<float>(recipe.Beats.Select(b => b.Time));
        for (int frame = 0; frame / 30f < result.Duration; frame++) times.Add(frame / 30f);
        var segment = 0;
        foreach (var time in times)
        {
            while (segment < recipe.Beats.Length - 2 && time > recipe.Beats[segment + 1].Time) segment++;
            var first = recipe.Beats[segment]; var second = recipe.Beats[segment + 1];
            var fraction = Math.Clamp((time - first.Time) / (second.Time - first.Time), 0, 1);
            var smooth = fraction * fraction * (3 - 2 * fraction);
            var pose = new PosedNode[neutral.Length];
            // Continuous native spans retain the source's real in-between motion (especially throws).
            var nativeSpan = first.SourceAnimation == second.SourceAnimation && first.SourceModel == second.SourceModel && first.LeftHand == null && second.LeftHand == null &&
                first.RightHand == null && second.RightHand == null && first.TorsoDegrees == null && second.TorsoDegrees == null;
            if (nativeSpan) pose = Native(first.SourceAnimation, float.Lerp(first.SourceTime, second.SourceTime, fraction), first.SourceModel);
            else for (int i = 0; i < pose.Length; i++)
                pose[i] = new(Vector3.Lerp(poses[segment][i].Position, poses[segment + 1][i].Position, smooth),
                    Quaternion.Slerp(poses[segment][i].Orientation, poses[segment + 1][i].Orientation, smooth),
                    float.Lerp(poses[segment][i].Scale, poses[segment + 1][i].Scale, smooth));
            var world = AnimationRig.World(rig.Joints, pose);
            var penetration = floor - feet.Min(i => world[i].Translation.Z);
            if (penetration > 0 && time > 0 && time < result.Duration)
                pose[root] = pose[root] with { Position = pose[root].Position + Vector3.UnitZ * penetration };
            result.SetKey(time, pose);
        }
        result.Validate();
        BulkMotionAuthor.ValidateMotion(result, neutral, floor);
        return result;
    }
}
