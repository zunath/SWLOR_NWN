using System.Numerics;
using SWLOR.NWN.Formats.Mdl;

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
        /// Placeables use a different animation vocabulary from creatures. <c>default</c> is the
        /// authored ambient loop; <c>on</c> is the next-best active state for older effects that do
        /// not declare one. The remaining states stay selectable, but are never guessed as the
        /// initial preview while an active state exists.
        /// </summary>
        private static readonly string[] PlaceableDefaultNames = { "default", "on" };

        private static readonly string[] AttackNames =
        {
            "1hslashl", "nwslashl", "2hslashl", "plslashl", "2wslashl", "bowshot", "xbowshot"
        };

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
        /// Every distinct animation a placeable declares, preserving file order. Blank and duplicate
        /// names are ignored because a state picker cannot address them unambiguously.
        /// </summary>
        public static IReadOnlyList<MdlAnimation> PlaceableAnimations(MdlModel? model)
        {
            if (model == null || model.Animations.Count == 0)
                return Array.Empty<MdlAnimation>();

            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            return model.Animations
                .Where(animation =>
                    !string.IsNullOrWhiteSpace(animation.Name) &&
                    names.Add(animation.Name.Trim()))
                .ToList();
        }

        /// <summary>
        /// The state a placeable preview starts in. This rule is deliberately placeable-specific:
        /// exact <c>default</c>, then exact <c>on</c>, then the first state that actually has time to
        /// play, and finally the first declaration. Creature pause names are not considered here.
        /// </summary>
        public static MdlAnimation? FindPlaceableDefault(MdlModel? model)
        {
            var animations = PlaceableAnimations(model);
            foreach (var name in PlaceableDefaultNames)
            {
                var exact = animations.FirstOrDefault(
                    animation => string.Equals(animation.Name, name, StringComparison.OrdinalIgnoreCase));
                if (exact != null)
                    return exact;
            }

            return animations.FirstOrDefault(animation => animation.Length > 0f) ??
                   animations.FirstOrDefault();
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
            var bindPose = BindPose(model);

            for (var depth = 0; current != null && depth < maxDepth; depth++)
            {
                var posed = Sample(FindIdle(current), seconds, bindPose);
                if (posed.Count > 0)
                    return depth == 0
                        ? posed
                        : ScaleTranslations(posed, AnimationScale(model));

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

        /// <summary>One sampled frame of an idle: the pose, and how far into the animation it is.</summary>
        public readonly record struct IdleFrame(IReadOnlyDictionary<string, PosedNode> Pose, float Seconds);

        /// <summary>One sampled frame of any named animation.</summary>
        public readonly record struct AnimationFrame(IReadOnlyDictionary<string, PosedNode> Pose, float Seconds);

        /// <summary>A named creature animation sampled into the bounded pose frames the renderer consumes.</summary>
        public readonly record struct SampledAnimation(
            string Name,
            float Length,
            IReadOnlyList<IReadOnlyDictionary<string, PosedNode>> Frames);

        /// <summary>
        /// Samples an animation from beginning to end. The frame count is bounded because placeable
        /// models are retained by the shared render cache, while the matrices are interpolated
        /// densely enough for the preview to read as motion.
        /// </summary>
        public static IReadOnlyList<AnimationFrame> SampleFrames(
            MdlAnimation? animation,
            int framesPerSecond = 20,
            int maxFrames = 60,
            IReadOnlyDictionary<string, MdlNode>? bindPose = null)
        {
            if (animation == null)
                return Array.Empty<AnimationFrame>();

            var length = animation.Length;
            if (length <= 0f || framesPerSecond <= 0)
                return new[] { new AnimationFrame(Sample(animation, 0f, bindPose), 0f) };

            var count = Math.Clamp(
                (int)MathF.Ceiling(length * framesPerSecond) + 1,
                2,
                Math.Max(2, maxFrames));
            var frames = new List<AnimationFrame>(count);

            for (var i = 0; i < count; i++)
            {
                var seconds = length * i / (count - 1);
                frames.Add(new AnimationFrame(Sample(animation, seconds, bindPose), seconds));
            }

            return frames;
        }

        /// <summary>
        /// Resolves the builder's Idle, Walk, and Attack preview clips through a creature's
        /// supermodel chain, in that order, and samples only those three clips.
        /// </summary>
        public static IReadOnlyList<SampledAnimation> SampleCreaturePreviewAnimations(
            MdlModel? model,
            Func<string, MdlModel?> loadSuperModel,
            IReadOnlyDictionary<string, MdlNode>? bindPose = null,
            int framesPerSecond = 20,
            int maxFrames = 60)
        {
            ArgumentNullException.ThrowIfNull(loadSuperModel);
            if (model == null)
                return Array.Empty<SampledAnimation>();

            bindPose ??= BindPose(model);
            var clips = new List<SampledAnimation>(3);
            foreach (var selector in new Func<MdlModel?, MdlAnimation?>[] { FindIdle, FindWalk, FindAttack })
            {
                var (animation, owner) = FindAnimationInChain(model, loadSuperModel, selector);
                if (animation == null || owner == null ||
                    clips.Any(clip => string.Equals(clip.Name, animation.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var inheritedScale = ReferenceEquals(owner, model) ? 1f : AnimationScale(model);
                var frames = SampleFrames(animation, framesPerSecond, maxFrames, bindPose)
                    .Select(frame => (IReadOnlyDictionary<string, PosedNode>)ScaleTranslations(
                        frame.Pose,
                        inheritedScale))
                    .ToList();
                if (frames.Count > 0)
                    clips.Add(new SampledAnimation(animation.Name, animation.Length, frames));
            }

            return clips;
        }

        /// <summary>
        /// The idle sampled across its whole length, for playing it through once.
        /// </summary>
        /// <remarks>
        /// Aurora plays a creature's idle briefly when it appears and then leaves it standing in the
        /// pose it finished on - so the frames are what gets played, and the last of them is the
        /// resting pose everything else (picking, bounds, a still thumbnail) should use.
        /// <para>
        /// Pre-sampled rather than evaluated per frame because NWN's bodies are rigid parts bolted to
        /// bones, not skinned meshes: a pose changes each mesh's transform and never touches a vertex.
        /// So a frame costs one matrix per mesh, the geometry is uploaded once, and playing it back is
        /// swapping which matrix is bound. <paramref name="maxFrames"/> bounds the memory for a very
        /// long idle; the sampler interpolates, so a coarse set still plays smoothly.
        /// </para>
        /// </remarks>
        public static IReadOnlyList<IdleFrame> SampleIdleFrames(
            MdlModel? model,
            Func<string, MdlModel?> loadSuperModel,
            int framesPerSecond = 20,
            int maxFrames = 60)
        {
            return SampleIdleFrames(
                model,
                loadSuperModel,
                BindPose(model),
                framesPerSecond,
                maxFrames);
        }

        /// <summary>
        /// The idle sampled against an explicit geometry bind pose. This is used for Aurora
        /// animation overlays such as <c>a_ba_coat</c>: their animation owns coat-helper tracks,
        /// while untracked channels on shared body bones must retain the wearer's authored
        /// translations rather than the garment visual's zeroed skin skeleton.
        /// </summary>
        public static IReadOnlyList<IdleFrame> SampleIdleFrames(
            MdlModel? model,
            Func<string, MdlModel?> loadSuperModel,
            IReadOnlyDictionary<string, MdlNode> bindPose,
            int framesPerSecond = 20,
            int maxFrames = 60)
        {
            ArgumentNullException.ThrowIfNull(loadSuperModel);
            ArgumentNullException.ThrowIfNull(bindPose);

            var (animation, owner) = FindIdleInChain(model, loadSuperModel);
            if (animation == null || owner == null)
                return Array.Empty<IdleFrame>();

            var inheritedScale = ReferenceEquals(owner, model)
                ? 1f
                : AnimationScale(model);
            return SampleFrames(animation, framesPerSecond, maxFrames, bindPose)
                .Select(frame => new IdleFrame(
                    ScaleTranslations(frame.Pose, inheritedScale),
                    frame.Seconds))
                .ToList();
        }

        /// <summary>The first idle in the supermodel chain that actually poses something, with the model it came from.</summary>
        private static (MdlAnimation? Animation, MdlModel? Owner) FindIdleInChain(
            MdlModel? model, Func<string, MdlModel?> loadSuperModel, int maxDepth = 8)
            => FindAnimationInChain(model, loadSuperModel, FindIdle, maxDepth);

        private static (MdlAnimation? Animation, MdlModel? Owner) FindAnimationInChain(
            MdlModel? model,
            Func<string, MdlModel?> loadSuperModel,
            Func<MdlModel?, MdlAnimation?> select,
            int maxDepth = 8)
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var current = model;

            for (var depth = 0; current != null && depth < maxDepth; depth++)
            {
                var animation = select(current);
                if (animation != null && Sample(animation, 0f).Count > 0)
                    return (animation, current);

                var superModel = current.SuperModel;
                if (string.IsNullOrWhiteSpace(superModel) ||
                    string.Equals(superModel, "NULL", StringComparison.OrdinalIgnoreCase) ||
                    !visited.Add(superModel))
                {
                    break;
                }

                current = loadSuperModel(superModel);
            }

            return (null, null);
        }

        private static MdlAnimation? FindWalk(MdlModel? model)
        {
            if (model == null)
                return null;
            return model.Animations.FirstOrDefault(animation =>
                       string.Equals(animation.Name, "walk", StringComparison.OrdinalIgnoreCase)) ??
                   model.Animations.FirstOrDefault(animation =>
                       animation.Name.StartsWith("walk", StringComparison.OrdinalIgnoreCase) &&
                       !animation.Name.Contains("dead", StringComparison.OrdinalIgnoreCase) &&
                       !animation.Name.Contains("inj", StringComparison.OrdinalIgnoreCase));
        }

        private static MdlAnimation? FindAttack(MdlModel? model)
        {
            if (model == null)
                return null;
            foreach (var name in AttackNames)
            {
                var exact = model.Animations.FirstOrDefault(animation =>
                    string.Equals(animation.Name, name, StringComparison.OrdinalIgnoreCase));
                if (exact != null)
                    return exact;
            }

            return model.Animations.FirstOrDefault(animation =>
                animation.Name.Contains("attack", StringComparison.OrdinalIgnoreCase) ||
                animation.Name.Contains("slash", StringComparison.OrdinalIgnoreCase) ||
                animation.Name.Contains("stab", StringComparison.OrdinalIgnoreCase) ||
                animation.Name.Contains("kick", StringComparison.OrdinalIgnoreCase) ||
                animation.Name.EndsWith("shot", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Every animated node's local transform at <paramref name="seconds"/>, keyed by node name.
        /// Nodes the animation does not touch are absent, and the caller keeps their static values.
        /// </summary>
        public static IReadOnlyDictionary<string, PosedNode> Sample(
            MdlAnimation? animation,
            float seconds,
            IReadOnlyDictionary<string, MdlNode>? bindPose = null)
        {
            var posed = new Dictionary<string, PosedNode>(StringComparer.OrdinalIgnoreCase);
            if (animation?.GeometryRoot == null)
                return posed;

            Walk(animation.GeometryRoot, seconds, posed, bindPose);
            return posed;
        }

        /// <summary>
        /// Every geometry node of a model by name - the bind pose an animation samples against.
        /// </summary>
        /// <remarks>
        /// Needed because an animation's nodes are stubs: they carry the tracks they animate and
        /// nothing else, so an untracked channel read off the stub is a zero, not a value. Sampling
        /// <c>a_ba</c>'s pause1 against nothing gave every bone position &lt;0,0,0&gt; and folded the
        /// whole skeleton onto the origin - a body rendered as one blob at its own navel.
        /// </remarks>
        public static IReadOnlyDictionary<string, MdlNode> BindPose(MdlModel? model)
        {
            var bind = new Dictionary<string, MdlNode>(StringComparer.OrdinalIgnoreCase);
            Collect(model?.GeometryRoot, bind);
            return bind;

            static void Collect(MdlNode? node, Dictionary<string, MdlNode> into)
            {
                if (node == null)
                    return;
                if (!string.IsNullOrEmpty(node.Name))
                    into.TryAdd(node.Name, node);
                foreach (var child in node.Children)
                    Collect(child, into);
            }
        }

        private static void Walk(
            MdlNode node,
            float seconds,
            Dictionary<string, PosedNode> posed,
            IReadOnlyDictionary<string, MdlNode>? bindPose)
        {
            var animated =
                node.PositionTimes.Length > 0 || node.OrientationTimes.Length > 0 || node.ScaleTimes.Length > 0;

            if (animated && !string.IsNullOrEmpty(node.Name))
            {
                // An animation node is a stub carrying only the tracks it animates - its own
                // Position/Orientation are almost always zero and identity. The value for an
                // untracked channel is the SKELETON's authored bind pose, not the stub's blank.
                var bind = bindPose != null && bindPose.TryGetValue(node.Name, out var bound) ? bound : node;
                posed[node.Name] = new PosedNode(
                    SamplePosition(node, seconds, bind),
                    SampleOrientation(node, seconds, bind),
                    SampleScale(node, seconds, bind));
            }

            foreach (var child in node.Children)
                Walk(child, seconds, posed, bindPose);
        }

        private static Vector3 SamplePosition(MdlNode node, float seconds, MdlNode bind)
        {
            if (node.PositionTimes.Length == 0 || node.PositionValues.Length == 0)
                return bind.Position;

            var (before, after, blend) = Bracket(node.PositionTimes, seconds, node.PositionValues.Length);
            return Vector3.Lerp(node.PositionValues[before], node.PositionValues[after], blend);
        }

        private static Quaternion SampleOrientation(MdlNode node, float seconds, MdlNode bind)
        {
            if (node.OrientationTimes.Length == 0 || node.OrientationValues.Length == 0)
                return bind.Orientation;

            var (before, after, blend) = Bracket(node.OrientationTimes, seconds, node.OrientationValues.Length);

            // Slerp, not Lerp: a linear blend between quaternions shortens the arc and makes a limb
            // dip through the pose rather than swing round it.
            return Quaternion.Slerp(node.OrientationValues[before], node.OrientationValues[after], blend);
        }

        private static float SampleScale(MdlNode node, float seconds, MdlNode bind)
        {
            if (node.ScaleTimes.Length == 0 || node.ScaleValues.Length == 0)
                return bind.Scale;

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

        private static float AnimationScale(MdlModel? model) =>
            model != null && float.IsFinite(model.Scale) && model.Scale > 0f
                ? model.Scale
                : 1f;

        private static IReadOnlyDictionary<string, PosedNode> ScaleTranslations(
            IReadOnlyDictionary<string, PosedNode> pose,
            float scale)
        {
            if (scale == 1f || pose.Count == 0)
                return pose;

            return pose.ToDictionary(
                pair => pair.Key,
                pair => pair.Value with { Position = pair.Value.Position * scale },
                StringComparer.OrdinalIgnoreCase);
        }
    }
}
