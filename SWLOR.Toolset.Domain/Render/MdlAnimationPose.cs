using System.Numerics;
using Radoub.Formats.Mdl;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>One node's local transform at a moment in an animation.</summary>
    public readonly record struct PosedNode(Vector3 Position, Quaternion Orientation, float Scale);

    /// <summary>
    /// Samples an MDL animation into per-node local transforms, so a model can be drawn standing
    /// the way the game stands it rather than in the bind pose its geometry is stored in.
    /// </summary>
    /// <remarks>
    /// A creature's mesh is authored in whatever pose the artist modelled it in - for NWN's segmented
    /// bodies that is arms out and legs apart, which is what a placed NPC looked like: recognisable,
    /// but not how it ever appears in game. Every creature model carries an idle animation, and its
    /// first frame is the standing pose.
    /// <para>
    /// Sampling is by node <em>name</em>, because an animation's node tree is a parallel skeleton to
    /// the geometry's: the same names, carrying keyframes instead of meshes. That is also why this
    /// works for a composed body - the parts are attached to the skeleton's bones by name, so posing
    /// the bones poses everything hanging off them.
    /// </para>
    /// </remarks>
    public static class MdlAnimationPose
    {
        /// <summary>
        /// NWN's idle animations, best first. <c>pause1</c> is the plain standing idle; the others are
        /// the stances a model may carry instead when it has no neutral one.
        /// </summary>
        private static readonly string[] IdleNames = { "pause1", "pause2", "pause3", "pausesh", "cpause1" };

        /// <summary>
        /// The animation to stand a model in, or null when it carries none. Prefers a plain idle by
        /// name, then anything whose name begins "pause", then gives up rather than guessing - an
        /// arbitrary animation is worse than the bind pose, because a walk or an attack frame reads as
        /// a broken model rather than an unposed one.
        /// </summary>
        public static MdlAnimation? FindIdle(MdlModel? model)
        {
            if (model == null || model.Animations.Count == 0)
                return null;

            foreach (var name in IdleNames)
            {
                var exact = model.Animations.FirstOrDefault(
                    a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));
                if (exact != null)
                    return exact;
            }

            return model.Animations.FirstOrDefault(
                a => a.Name.StartsWith("pause", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// The idle pose for <paramref name="model"/>, following its supermodel chain until one
        /// actually supplies keyframes. Empty when nothing in the chain does.
        /// </summary>
        /// <remarks>
        /// Creature models carry no animations of their own - measured across the corpus, every one is
        /// zero. The animations live in a shared supermodel: <c>pmh0</c> and <c>pfh0</c> declare
        /// <c>a_ba</c> and <c>a_fa</c>, which hold 156 and 49 animations respectively. So an idle can
        /// only be found by following <see cref="MdlModel.SuperModel"/>.
        /// <para>
        /// One link is not always enough either. <c>a_ba</c>'s pause1 poses 46 nodes, but
        /// <c>a_fa</c>'s is an empty declaration that defers to <c>a_fa_int</c> - so the walk
        /// continues past an idle that yields nothing rather than stopping at the first one it finds.
        /// It stops at <paramref name="maxDepth"/> and on any resref already visited, because a
        /// malformed pair of models can name each other.
        /// </para>
        /// </remarks>
        public static IReadOnlyDictionary<string, PosedNode> SampleIdle(
            MdlModel? model,
            Func<string, MdlModel?> loadSuperModel,
            float seconds = 0f,
            int maxDepth = 8)
        {
            ArgumentNullException.ThrowIfNull(loadSuperModel);

            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var current = model;

            for (var depth = 0; current != null && depth < maxDepth; depth++)
            {
                var posed = Sample(FindIdle(current), seconds);
                if (posed.Count > 0)
                    return posed;

                var superModel = current.SuperModel;
                if (string.IsNullOrWhiteSpace(superModel) ||
                    string.Equals(superModel, "NULL", StringComparison.OrdinalIgnoreCase) ||
                    !visited.Add(superModel))
                {
                    break;
                }

                current = loadSuperModel(superModel);
            }

            return new Dictionary<string, PosedNode>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Every animated node's local transform at <paramref name="seconds"/>, keyed by node name.
        /// Nodes the animation does not touch are absent, and the caller keeps their static values.
        /// </summary>
        public static IReadOnlyDictionary<string, PosedNode> Sample(MdlAnimation? animation, float seconds)
        {
            var posed = new Dictionary<string, PosedNode>(StringComparer.OrdinalIgnoreCase);
            if (animation?.GeometryRoot == null)
                return posed;

            Walk(animation.GeometryRoot, seconds, posed);
            return posed;
        }

        private static void Walk(MdlNode node, float seconds, Dictionary<string, PosedNode> posed)
        {
            var animated =
                node.PositionTimes.Length > 0 || node.OrientationTimes.Length > 0 || node.ScaleTimes.Length > 0;

            if (animated && !string.IsNullOrEmpty(node.Name))
            {
                posed[node.Name] = new PosedNode(
                    SamplePosition(node, seconds),
                    SampleOrientation(node, seconds),
                    SampleScale(node, seconds));
            }

            foreach (var child in node.Children)
                Walk(child, seconds, posed);
        }

        private static Vector3 SamplePosition(MdlNode node, float seconds)
        {
            if (node.PositionTimes.Length == 0 || node.PositionValues.Length == 0)
                return node.Position;

            var (before, after, blend) = Bracket(node.PositionTimes, seconds, node.PositionValues.Length);
            return Vector3.Lerp(node.PositionValues[before], node.PositionValues[after], blend);
        }

        private static Quaternion SampleOrientation(MdlNode node, float seconds)
        {
            if (node.OrientationTimes.Length == 0 || node.OrientationValues.Length == 0)
                return node.Orientation;

            var (before, after, blend) = Bracket(node.OrientationTimes, seconds, node.OrientationValues.Length);

            // Slerp, not Lerp: a linear blend between quaternions shortens the arc and makes a limb
            // dip through the pose rather than swing round it.
            return Quaternion.Slerp(node.OrientationValues[before], node.OrientationValues[after], blend);
        }

        private static float SampleScale(MdlNode node, float seconds)
        {
            if (node.ScaleTimes.Length == 0 || node.ScaleValues.Length == 0)
                return node.Scale;

            var (before, after, blend) = Bracket(node.ScaleTimes, seconds, node.ScaleValues.Length);
            return node.ScaleValues[before] + (node.ScaleValues[after] - node.ScaleValues[before]) * blend;
        }

        /// <summary>
        /// The two keyframes either side of <paramref name="seconds"/> and how far between them it
        /// falls. Clamps at both ends rather than wrapping: a caller asking past the end of a track
        /// wants the final pose held, not the first one snapped back to.
        /// </summary>
        private static (int Before, int After, float Blend) Bracket(float[] times, float seconds, int valueCount)
        {
            var last = Math.Min(times.Length, valueCount) - 1;
            if (last <= 0)
                return (0, 0, 0f);

            if (seconds <= times[0])
                return (0, 0, 0f);

            if (seconds >= times[last])
                return (last, last, 0f);

            for (var i = 0; i < last; i++)
            {
                if (seconds > times[i + 1])
                    continue;

                var span = times[i + 1] - times[i];
                return (i, i + 1, span <= 0f ? 0f : (seconds - times[i]) / span);
            }

            return (last, last, 0f);
        }
    }
}
