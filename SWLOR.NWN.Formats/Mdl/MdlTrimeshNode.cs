// SPDX-License-Identifier: MIT

using System.Numerics;

namespace SWLOR.NWN.Formats.Mdl;

public class MdlTrimeshNode : MdlNode
{
    public bool Render { get; set; } = true;

    /// <summary>
    /// True for a collision node - ASCII <c>node aabb</c>/<c>pwk</c>/<c>dwk</c>, or a binary node
    /// carrying the AABB payload flag. Its triangles are the walkable surface, not artwork.
    /// </summary>
    /// <remarks>
    /// This has to be a node property rather than something a consumer infers, because nothing else
    /// in the node distinguishes it: a collision node carries ordinary vertices and faces, no bitmap,
    /// and - in ASCII, which never writes a <c>render</c> line for one - <see cref="Render"/> stays at
    /// its default of true. Drawing them put a flat grey slab over the ground of every tile that had
    /// one; ztd01 alone has 440.
    /// </remarks>
    public bool IsWalkmesh { get; set; }

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
