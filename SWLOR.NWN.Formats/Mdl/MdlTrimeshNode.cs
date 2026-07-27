// SPDX-License-Identifier: MIT

using System.Numerics;

namespace SWLOR.NWN.Formats.Mdl;

public class MdlTrimeshNode : MdlNode
{
    public bool Render { get; set; } = true;

    public int TileFade { get; set; }

    public string Bitmap { get; set; } = string.Empty;

    /// <summary>
    /// The mesh's authored diffuse colour. Untextured marker meshes (waypoint flags on
    /// tcn01_white) are coloured entirely by this value, so consumers must not drop it.
    /// </summary>
    public Vector3 Diffuse { get; set; } = Vector3.One;

    public string Lightmap { get; set; } = string.Empty;

    public Vector3[] Vertices { get; set; } = Array.Empty<Vector3>();

    public Vector3[] Normals { get; set; } = Array.Empty<Vector3>();

    public Vector2[] TextureCoordinates { get; set; } = Array.Empty<Vector2>();

    public MdlFace[] Faces { get; set; } = Array.Empty<MdlFace>();
}
