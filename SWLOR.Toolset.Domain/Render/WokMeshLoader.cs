using System.Buffers.Binary;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// One tile's walkmesh, in tile-LOCAL space (the same frame as the tile MDL - apply
    /// <see cref="TilePlacement.Transform"/> to reach world space).
    /// </summary>
    public sealed class WalkMesh
    {
        public required IReadOnlyList<Vector3> Vertices { get; init; }
        public required IReadOnlyList<WalkFace> Faces { get; init; }
    }

    /// <summary>One walkmesh triangle: three vertex indices, a surfacemat.2da row, and whether that row is walkable.</summary>
    public readonly struct WalkFace
    {
        /// <summary>Index into the owning <see cref="WalkMesh.Vertices"/>.</summary>
        public required int A { get; init; }

        /// <summary>Index into the owning <see cref="WalkMesh.Vertices"/>.</summary>
        public required int B { get; init; }

        /// <summary>Index into the owning <see cref="WalkMesh.Vertices"/>.</summary>
        public required int C { get; init; }

        /// <summary>surfacemat.2da row id for this face.</summary>
        public required int Material { get; init; }

        /// <summary>Resolved via the caller-supplied <c>isWalkable</c> predicate at parse time.</summary>
        public required bool Walkable { get; init; }
    }

    /// <summary>
    /// Parses a tile's <c>.wok</c> walkmesh resource into a <see cref="WalkMesh"/>.
    ///
    /// <para>
    /// EMPIRICAL FINDING: every real <c>.wok</c> resource actually present in this
    /// project's asset corpus - both the SWLOR custom hak-source folders under
    /// <c>SWLOR_Haks\sw_t_*</c> (e.g. <c>tfb01_p05_01.wok</c>, <c>ttd02_f04_01.wok</c>) and the
    /// retail base game's <c>nwn_base.key</c>/<c>.bif</c> archives (verified directly, e.g.
    /// <c>dag01_a01_01.wok</c>) - is plain ASCII "NWMax walkmesh" export text (header line
    /// <c>#NWmax WALKMESH  ASCII</c>, <c>#MAXWALKMESH  ASCII</c>, or <c>#MAXMODEL ASCII</c>
    /// depending on exporter version; the reliable marker across every variant is the
    /// <c>beginwalkmeshgeom</c> keyword). None of the dozens of real files sampled (including a
    /// brute-force byte search for the literal text "BWM V1.0" across every tileset .bif in a
    /// local NWN:EE install) contained the binary "Aurora Binary Walkmesh" layout originally
    /// assumed for this implementation. <see cref="ParseAscii"/> is therefore the path every real
    /// resource in this project takes. <see cref="ParseBinary"/> is kept as a defensive/forward
    /// compatible fallback matching that original binary spec exactly (self-consistency guarded),
    /// in case a binary-format .wok is ever encountered, but it has not been exercised against a
    /// real sample because none exists in this project's corpus.
    /// </para>
    /// </summary>
    public static class WokMeshLoader
    {
        private const int BwmHeaderSize = 112;
        private static readonly byte[] BwmMagic = Encoding.ASCII.GetBytes("BWM V1.0");

        /// <summary>
        /// Parse raw .wok bytes (either the binary "BWM V1.0" layout or - what every real
        /// resource in this project actually is - ASCII NWMax walkmesh export text).
        /// <paramref name="isWalkable"/> classifies each face's surfacemat.2da row id. Never
        /// throws - returns null for null/empty/malformed input or a null predicate.
        /// </summary>
        public static WalkMesh? Parse(ReadOnlySpan<byte> bytes, Func<int, bool> isWalkable)
        {
            if (bytes.IsEmpty || isWalkable == null)
                return null;

            try
            {
                if (bytes.Length >= BwmMagic.Length && bytes[..BwmMagic.Length].SequenceEqual(BwmMagic))
                    return ParseBinary(bytes, isWalkable);

                return ParseAscii(bytes, isWalkable);
            }
            catch (Exception)
            {
                // Malformed/truncated/unexpected input must never abort area assembly or the
                // height-snap raycast - the caller treats a null result as "no walkmesh for this
                // tile" and keeps going (falls back to whatever ground-plane heuristic it used
                // before this feature existed).
                return null;
            }
        }

        // ------------------------------------------------------------------------------------
        // ASCII NWMax walkmesh export text - the format every real .wok in this project's corpus
        // actually uses. Grammar (confirmed against real corpus + base-game samples):
        //
        //   # comment lines (variable count/wording across exporter versions) - ignored
        //   beginwalkmeshgeom <name>
        //   node aabb <nodename>
        //     parent <name>
        //     position x y z
        //     orientation ox oy oz oangle           (every real sample found has angle 0 - ignored)
        //     wirecolor r g b
        //     [multimaterial N \n name1 \n ... \n nameN]   (optional legend, informational only)
        //     [ambient/diffuse/specular/shininess/bitmap lines]   (optional, informational only)
        //     verts V
        //       x y z                                (repeated V times)
        //     faces F
        //       v1 v2 v3  smoothgroup  t1 t2 t3  surfacematId   (repeated F times; only the
        //                                                        first 3 and last tokens matter)
        //     [tverts T \n u v w ...]                (optional, ignored)
        //     aabb <bounding-volume-tree lines>       (ignored - spatial index, not geometry)
        //   endnode
        //   [additional sibling "node aabb" blocks - not seen in any real sample, but merged
        //    defensively if present]
        //   endwalkmeshgeom <name>
        //
        // Multiple "node aabb" blocks (if ever present) are merged into one flat WalkMesh: each
        // node's own vertices are offset by that node's "position" line, and each node's face
        // indices are offset by the vertex count already accumulated from prior nodes.
        // ------------------------------------------------------------------------------------

        private static WalkMesh? ParseAscii(ReadOnlySpan<byte> bytes, Func<int, bool> isWalkable)
        {
            var text = Encoding.UTF8.GetString(bytes);
            if (text.IndexOf("beginwalkmeshgeom", StringComparison.OrdinalIgnoreCase) < 0)
                return null; // Not the binary format (checked by the caller) and not recognizable ASCII either.

            var lines = text.Split('\n');
            var vertices = new List<Vector3>();
            var faces = new List<WalkFace>();
            var sawAnyNode = false;

            var i = 0;
            while (i < lines.Length)
            {
                var line = lines[i].Trim();
                if (IsNodeAabbLine(line))
                {
                    i++;
                    if (!ParseNode(lines, ref i, vertices, faces, isWalkable))
                        return null; // Internally inconsistent node data - treat the whole file as malformed.

                    sawAnyNode = true;
                    continue;
                }

                i++;
            }

            if (!sawAnyNode || vertices.Count == 0 || faces.Count == 0)
                return null;

            return new WalkMesh { Vertices = vertices, Faces = faces };
        }

        private static bool IsNodeAabbLine(string line)
        {
            if (!line.StartsWith("node", StringComparison.OrdinalIgnoreCase))
                return false;

            var tokens = Tokenize(line);
            return tokens.Length >= 2 && string.Equals(tokens[1], "aabb", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Consumes one "node aabb ... endnode" block starting at <paramref name="i"/> (the line
        /// right after the "node aabb NAME" line itself), appending its geometry into the shared
        /// <paramref name="vertices"/>/<paramref name="faces"/> lists. Advances <paramref name="i"/>
        /// past the "endnode" line. Returns false when the block's verts/faces data is internally
        /// inconsistent (declared count doesn't match available/parseable lines, or a face
        /// references an out-of-range vertex index) - the caller treats that as a malformed file.
        /// </summary>
        private static bool ParseNode(
            string[] lines, ref int i, List<Vector3> vertices, List<WalkFace> faces, Func<int, bool> isWalkable)
        {
            var position = Vector3.Zero;
            var localVerts = new List<Vector3>();
            var localFaces = new List<(int A, int B, int C, int Material)>();
            var vertexBase = vertices.Count;

            while (i < lines.Length)
            {
                var line = lines[i].Trim();

                if (line.StartsWith("endnode", StringComparison.OrdinalIgnoreCase))
                {
                    i++;
                    break;
                }

                if (line.StartsWith("position", StringComparison.OrdinalIgnoreCase))
                {
                    var tokens = Tokenize(line);
                    if (tokens.Length >= 4 &&
                        TryFloat(tokens[1], out var px) && TryFloat(tokens[2], out var py) && TryFloat(tokens[3], out var pz))
                    {
                        position = new Vector3(px, py, pz);
                    }

                    i++;
                    continue;
                }

                // "verts" (vertex block) vs "tverts" (texture-coordinate block, irrelevant here) -
                // must match the bare keyword, not a suffix match, so "tverts" doesn't trigger this.
                if (line.StartsWith("verts", StringComparison.OrdinalIgnoreCase))
                {
                    var tokens = Tokenize(line);
                    if (tokens.Length < 2 || !int.TryParse(tokens[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) || count < 0)
                        return false;

                    i++;
                    for (var v = 0; v < count; v++)
                    {
                        if (i >= lines.Length)
                            return false;

                        var vTokens = Tokenize(lines[i]);
                        if (vTokens.Length < 3 ||
                            !TryFloat(vTokens[0], out var x) || !TryFloat(vTokens[1], out var y) || !TryFloat(vTokens[2], out var z))
                        {
                            return false;
                        }

                        localVerts.Add(new Vector3(x, y, z));
                        i++;
                    }

                    continue;
                }

                if (line.StartsWith("faces", StringComparison.OrdinalIgnoreCase))
                {
                    var tokens = Tokenize(line);
                    if (tokens.Length < 2 || !int.TryParse(tokens[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) || count < 0)
                        return false;

                    i++;
                    for (var f = 0; f < count; f++)
                    {
                        if (i >= lines.Length)
                            return false;

                        var fTokens = Tokenize(lines[i]);
                        // At minimum: v1 v2 v3 ... materialId (>= 4 tokens). Real samples carry 8
                        // (v1 v2 v3 smoothgroup t1 t2 t3 materialId) but only the first 3 and the
                        // last are meaningful to a WalkFace, so this tolerates narrower variants.
                        if (fTokens.Length < 4 ||
                            !int.TryParse(fTokens[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var a) ||
                            !int.TryParse(fTokens[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var b) ||
                            !int.TryParse(fTokens[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var c) ||
                            !int.TryParse(fTokens[^1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var material))
                        {
                            return false;
                        }

                        localFaces.Add((a, b, c, material));
                        i++;
                    }

                    continue;
                }

                // Everything else (orientation, wirecolor, multimaterial + its name lines,
                // ambient/diffuse/specular/shininess/bitmap, tverts + its data lines, the "aabb"
                // bounding-volume-tree lines) carries no walkmesh geometry - skip one line at a
                // time regardless of how many lines a given section spans.
                i++;
            }

            foreach (var v in localVerts)
                vertices.Add(v + position);

            foreach (var (a, b, c, material) in localFaces)
            {
                if (a < 0 || b < 0 || c < 0 || a >= localVerts.Count || b >= localVerts.Count || c >= localVerts.Count)
                    return false;

                faces.Add(new WalkFace
                {
                    A = vertexBase + a,
                    B = vertexBase + b,
                    C = vertexBase + c,
                    Material = material,
                    Walkable = isWalkable(material)
                });
            }

            return true;
        }

        private static string[] Tokenize(string line) =>
            line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        private static bool TryFloat(string token, out float value) =>
            float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

        // ------------------------------------------------------------------------------------
        // Binary "BWM V1.0" Aurora walkmesh layout, per the original format specification. Kept as a
        // defensive/forward-compatible fallback (see the class doc comment) - not exercised by
        // any real sample in this project's corpus, so every offset below is guarded by a
        // self-consistency check rather than trusted blindly.
        // ------------------------------------------------------------------------------------

        private static WalkMesh? ParseBinary(ReadOnlySpan<byte> bytes, Func<int, bool> isWalkable)
        {
            if (bytes.Length < BwmHeaderSize)
                return null;

            var walkmeshType = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(0x08, 4));
            if (walkmeshType != 0 && walkmeshType != 1)
                return null; // Neither documented walkmeshType value - not confident this is really BWM.

            var position = new Vector3(
                BinaryPrimitives.ReadSingleLittleEndian(bytes.Slice(0x24, 4)),
                BinaryPrimitives.ReadSingleLittleEndian(bytes.Slice(0x28, 4)),
                BinaryPrimitives.ReadSingleLittleEndian(bytes.Slice(0x2C, 4)));

            var vertexCount = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(0x30, 4));
            var vertexOffset = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(0x34, 4));
            var faceCount = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(0x38, 4));
            var faceIndicesOffset = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(0x3C, 4));
            var faceMaterialsOffset = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(0x40, 4));
            var faceNormalsOffset = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(0x44, 4));

            // Self-consistency guards - if these don't hold, the
            // offset/order assumption is wrong for whatever produced these bytes and this is not
            // a safe binary BWM to trust.
            if (faceCount > 0)
            {
                var indicesSpan = (long)faceMaterialsOffset - faceIndicesOffset;
                if (indicesSpan != (long)faceCount * 12)
                    return null; // Would be faceCount * 6 if indices were uint16 - not handled (never observed).

                var materialsSpan = (long)faceNormalsOffset - faceMaterialsOffset;
                if (materialsSpan != (long)faceCount * 4)
                    return null;
            }

            if (!TryReadBounds(vertexOffset, (long)vertexCount * 12, bytes.Length, out var vertexEnd) ||
                !TryReadBounds(faceIndicesOffset, (long)faceCount * 12, bytes.Length, out _) ||
                !TryReadBounds(faceMaterialsOffset, (long)faceCount * 4, bytes.Length, out _))
            {
                return null;
            }

            _ = vertexEnd;

            var vertices = new Vector3[vertexCount];
            for (var v = 0; v < vertexCount; v++)
            {
                var offset = checked((int)(vertexOffset + v * 12));
                var raw = new Vector3(
                    BinaryPrimitives.ReadSingleLittleEndian(bytes.Slice(offset, 4)),
                    BinaryPrimitives.ReadSingleLittleEndian(bytes.Slice(offset + 4, 4)),
                    BinaryPrimitives.ReadSingleLittleEndian(bytes.Slice(offset + 8, 4)));

                vertices[v] = position == Vector3.Zero ? raw : raw + position;
            }

            var faces = new WalkFace[faceCount];
            for (var f = 0; f < faceCount; f++)
            {
                var indexOffset = checked((int)(faceIndicesOffset + f * 12));
                var a = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(indexOffset, 4));
                var b = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(indexOffset + 4, 4));
                var c = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(indexOffset + 8, 4));

                if (a < 0 || b < 0 || c < 0 || a >= vertexCount || b >= vertexCount || c >= vertexCount)
                    return null;

                var materialOffset = checked((int)(faceMaterialsOffset + f * 4));
                var material = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(materialOffset, 4));

                faces[f] = new WalkFace { A = a, B = b, C = c, Material = material, Walkable = isWalkable(material) };
            }

            return new WalkMesh { Vertices = vertices, Faces = faces };
        }

        private static bool TryReadBounds(uint offset, long length, int totalLength, out long end)
        {
            end = offset + length;
            return length >= 0 && offset <= totalLength && end >= 0 && end <= totalLength;
        }
    }
}
