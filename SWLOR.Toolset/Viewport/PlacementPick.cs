using System.Numerics;

namespace SWLOR.Toolset.Viewport
{
    /// <summary>
    /// Where a placement click resolved to, and - when the viewport chose one - the heading the new
    /// instance should take.
    /// </summary>
    /// <param name="Position">The world point the instance is created at.</param>
    /// <param name="Orientation">
    /// The heading, as the (x, y) unit vector an instance's orientation uses, or null to leave the
    /// blueprint's own default. Only a snapped placement supplies one: a door takes the heading of
    /// the doorway it landed in, because a door hung across its own frame is never what was meant.
    /// </param>
    public readonly record struct PlacementPick(Vector3 Position, Vector2? Orientation);
}
