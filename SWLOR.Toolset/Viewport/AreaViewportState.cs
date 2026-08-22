using System.Numerics;

namespace SWLOR.Toolset.Viewport
{
    /// <summary>
    /// The complete orbit-camera state that belongs to an open area document. The GL control is a
    /// transient view and can be recreated as dock tabs change; this snapshot lets the document put
    /// a replacement control back exactly where the builder left it.
    /// </summary>
    public readonly record struct AreaViewportState(
        Vector3 Target,
        float Distance,
        float InitialDistance,
        float Azimuth,
        float Elevation);
}
