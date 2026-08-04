using System.Numerics;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// Corrects the axis the toolset's waypoint flag artwork is authored along.
    /// </summary>
    /// <remarks>
    /// The area transform treats model <c>+X</c> as forward and applies the instance's heading as a
    /// plain rotation about Z. That is the correct convention for doors: across the
    /// corpus, 515 doors that sit in a doorway their tile declares carry a Bearing equal to that
    /// doorway's orientation to within 0 or 180 degrees, never 90. Since a door's leaf spans model
    /// +X (TTR_UDoor_01 measures 3.32m across X against 0.44m across Y), rotating it by its bearing
    /// is what lays it along the wall.
    /// <para>
    /// The waypoint flags do not follow it. All four base-game markers - gi_waypoint01 through 04 -
    /// put their ground direction arrow at model <c>+Y</c>: the arrow tip measures (0.00, 1.20) with
    /// the flag above it. Rotated by the heading like everything else, the arrow ends up pointing a
    /// quarter turn anticlockwise of the facing the waypoint actually carries, so a waypoint set to
    /// face east drew pointing north. This turns the artwork onto the transform's convention first, and
    /// leaves everything downstream - rendering, picking, the gizmo - unchanged.
    /// </para>
    /// <para>
    /// The custom <c>zi_waypoint*</c> markers are symmetric discs and letters with no arrow at all,
    /// so the correction neither helps nor harms them; it is applied to every waypoint marker rather
    /// than to a list of resrefs, because a list would be one more thing to keep in step with
    /// waypoint.2da.
    /// </para>
    /// </remarks>
    public static class WaypointMarkerModel
    {
        /// <summary>
        /// Aurora draws placed stores with the yellow waypoint flag rather than with a generic
        /// object marker. Stores have no appearance field of their own, so this is their fixed
        /// editor-only model.
        /// </summary>
        public const string MerchantModelResRef = "gi_waypoint04";

        /// <summary>
        /// Turns the flag artwork's <c>+Y</c> arrow onto the transform's <c>+X</c> forward.
        /// Composed in model space, before the instance's own heading.
        /// </summary>
        public static readonly Matrix4x4 ForwardCorrection = Matrix4x4.CreateRotationZ(-MathF.PI / 2f);
    }
}
