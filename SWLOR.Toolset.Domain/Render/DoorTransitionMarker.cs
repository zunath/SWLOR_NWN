using System.Numerics;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// Fallback editor shape for an invisible area-transition door whose MDL cannot be resolved.
    /// Door models span local +X along the wall, with their stored position at the centre of the
    /// doorway; the standard Aurora transition plane is two metres wide and three metres high.
    /// </summary>
    public static class DoorTransitionMarker
    {
        public const float HalfWidth = 1f;
        public const float HalfDepth = 0.05f;
        public const float HalfHeight = 1.5f;

        public static readonly Vector3 LocalMinimum =
            new(-HalfWidth, -HalfDepth, -HalfHeight);

        public static readonly Vector3 LocalMaximum =
            new(HalfWidth, HalfDepth, HalfHeight);
    }
}
