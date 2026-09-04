// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Numerics;
using SWLOR.NWN.Formats.Internal;

namespace SWLOR.NWN.Formats.Mdl;

internal sealed class AsciiMdlReader
{
    private const int MaximumLines = 5_000_000;
    private const int MaximumLineLength = 1_000_000;
    private const int MaximumNodes = 100_000;
    private const int MaximumVertices = ushort.MaxValue;
    private const int MaximumFaces = 1_000_000;
    private const int MaximumControllerValues = 4_000_000;
    private const int MaximumDepth = 512;

    private readonly List<SourceLine> _lines = new();
    private int _index;
    private int _nodeCount;

    public MdlModel Parse(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        LoadLines(NwnTextEncoding.DecodeGeneral(data));
        var model = new MdlModel();
        var foundModel = false;
        var foundGeometry = false;

        while (TryRead(out var line))
        {
            var tokens = Tokens(line);
            if (tokens.Length == 0)
                continue;

            switch (tokens[0].ToLowerInvariant())
            {
                case "newmodel":
                    model.Name = RequiredToken(tokens, 1, line, "model name");
                    foundModel = true;
                    break;
                case "setsupermodel":
                    model.SuperModel = NullAsEmpty(RequiredToken(tokens, tokens.Length >= 3 ? 2 : 1, line, "supermodel name"));
                    break;
                case "setanimationscale":
                    model.Scale = tokens.Length >= 2 &&
                                  tokens[1].Equals("undefined", StringComparison.OrdinalIgnoreCase)
                        ? 1f
                        : FloatToken(tokens, 1, line, "animation scale");
                    break;
                case "beginmodelgeom":
                    model.GeometryRoot = ParseNodeBlock(
                        animationOnly: false,
                        "endmodelgeom",
                        model.Name);
                    foundGeometry = true;
                    break;
                case "newanim":
                    model.Animations.Add(ParseAnimation(tokens, line, model.Name));
                    break;
            }
        }

        if (!foundModel || string.IsNullOrWhiteSpace(model.Name))
            throw Error("ASCII MDL does not declare a model name.");
        if (!foundGeometry)
            throw Error($"ASCII MDL model '{model.Name}' does not contain a geometry block.");
        ComputeBounds(model);
        return model;
    }

    private MdlAnimation ParseAnimation(string[] declaration, SourceLine source, string modelName)
    {
        var animation = new MdlAnimation
        {
            Name = RequiredToken(declaration, 1, source, "animation name")
        };
        var pending = new List<PendingNode>();

        while (TryRead(out var line))
        {
            var tokens = Tokens(line);
            if (tokens.Length == 0)
                continue;

            switch (tokens[0].ToLowerInvariant())
            {
                case "length":
                    animation.Length = FloatToken(tokens, 1, line, "animation length");
                    break;
                case "transtime":
                    animation.TransitionTime = FloatToken(tokens, 1, line, "animation transition time");
                    break;
                case "node":
                    pending.Add(ParseNode(tokens, line, animationOnly: true));
                    break;
                case "doneanim":
                    animation.GeometryRoot = LinkNodes(pending, modelName);
                    return animation;
            }
        }

        throw Error($"ASCII MDL animation '{animation.Name}' is missing doneanim.", source);
    }

    private MdlNode? ParseNodeBlock(bool animationOnly, string terminator, string preferredRootName)
    {
        var pending = new List<PendingNode>();
        while (TryRead(out var line))
        {
            var tokens = Tokens(line);
            if (tokens.Length == 0)
                continue;
            if (tokens[0].Equals(terminator, StringComparison.OrdinalIgnoreCase))
                return LinkNodes(pending, preferredRootName);
            if (terminator.Equals("endmodelgeom", StringComparison.OrdinalIgnoreCase) &&
                tokens[0].Equals("donemodel", StringComparison.OrdinalIgnoreCase))
            {
                return LinkNodes(pending, preferredRootName);
            }
            if (tokens[0].Equals("node", StringComparison.OrdinalIgnoreCase))
                pending.Add(ParseNode(tokens, line, animationOnly));
        }

        // Real corpus files end truncated (sw_t_cepdesert/ztd01_o64_01.mdl stops mid-node with no
        // block terminator); tolerate a missing terminator when the block yielded nodes.
        if (pending.Count > 0)
            return LinkNodes(pending, preferredRootName);
        throw Error($"ASCII MDL block is missing {terminator}.");
    }

    private PendingNode ParseNode(string[] declaration, SourceLine source, bool animationOnly)
    {
        if (++_nodeCount > MaximumNodes)
            throw Error($"ASCII MDL node count exceeds {MaximumNodes}.", source);

        var type = RequiredToken(declaration, 1, source, "node type").ToLowerInvariant();
        var name = declaration.Length >= 3 ? declaration[2] : $"{type}_{_nodeCount}";
        MdlNode node = animationOnly
            ? new MdlNode()
            : type switch
            {
                "skin" => new MdlSkinmeshNode(),
                "trimesh" or "animmesh" or "danglymesh" => new MdlTrimeshNode(),
                // Collision nodes carry the same vertex/face payload as a trimesh, so they are read
                // as one - but flagged, because ASCII never writes a "render" line for them and a
                // consumer would otherwise draw the walkable surface as untextured artwork.
                "aabb" or "pwk" or "dwk" => new MdlTrimeshNode { IsWalkmesh = true },
                "emitter" => new MdlEmitterNode(),
                _ => new MdlNode()
            };
        node.Name = name;

        string? parentName = null;
        var vertices = Array.Empty<Vector3>();
        var normals = Array.Empty<Vector3>();
        var textureCoordinates = Array.Empty<Vector2>();
        var faces = Array.Empty<AsciiFace>();
        var influences = Array.Empty<MdlSkinInfluence[]>();
        var bitmapSeen = false;
        string? texture0 = null;

        void ApplyTexture0Fallback()
        {
            if (node is MdlTrimeshNode fallbackMesh && !bitmapSeen && texture0 != null)
                fallbackMesh.Bitmap = texture0;
        }

        while (TryRead(out var line))
        {
            var tokens = Tokens(line);
            if (tokens.Length == 0)
                continue;
            var directive = tokens[0].ToLowerInvariant();
            if (directive == "endnode")
            {
                if (node is MdlTrimeshNode mesh)
                {
                    ApplyTexture0Fallback();
                    FinalizeMesh(mesh, vertices, normals, textureCoordinates, faces, influences, line);
                }
                return new PendingNode(node, NullAsEmpty(parentName ?? string.Empty), source);
            }
            if (directive is "endmodelgeom" or "doneanim" or "donemodel" or "node")
            {
                _index--;
                if (node is MdlTrimeshNode mesh)
                {
                    ApplyTexture0Fallback();
                    FinalizeMesh(mesh, vertices, normals, textureCoordinates, faces, influences, line);
                }
                return new PendingNode(node, NullAsEmpty(parentName ?? string.Empty), source);
            }

            switch (directive)
            {
                case "parent":
                    parentName = RequiredToken(tokens, 1, line, "node parent");
                    break;
                case "position":
                    node.Position = Vector3Tokens(tokens, 1, line, "node position");
                    break;
                case "orientation":
                    node.Orientation = AxisAngleTokens(tokens, 1, line, "node orientation");
                    break;
                case "scale":
                    node.Scale = FloatToken(tokens, 1, line, "node scale");
                    break;
                case "positionkey":
                    ReadPositionKeys(node, tokens, line);
                    break;
                case "orientationkey":
                    ReadOrientationKeys(node, tokens, line);
                    break;
                case "scalekey":
                    ReadScaleKeys(node, tokens, line);
                    break;
                case "verts" when node is MdlTrimeshNode:
                    vertices = ReadVector3Array(tokens, line, "vertices");
                    break;
                case "normals" when node is MdlTrimeshNode:
                    normals = ReadVector3Array(tokens, line, "normals");
                    break;
                case "tverts" when node is MdlTrimeshNode:
                    textureCoordinates = ReadVector2Array(tokens, line, "texture coordinates");
                    break;
                case "faces" when node is MdlTrimeshNode:
                    faces = ReadFaces(tokens, line);
                    break;
                case "weights" when node is MdlSkinmeshNode:
                    influences = ReadWeights(tokens, line);
                    break;
                case "render" when node is MdlTrimeshNode mesh:
                    mesh.Render = BoolToken(tokens, 1, line, "mesh render flag");
                    break;
                case "tilefade" when node is MdlTrimeshNode mesh:
                    mesh.TileFade = IntToken(tokens, 1, line, "mesh tile-fade flag");
                    break;
                case "bitmap" when node is MdlTrimeshNode mesh:
                    bitmapSeen = true;
                    mesh.Bitmap = NullAsEmpty(RequiredToken(tokens, 1, line, "mesh bitmap"));
                    break;
                case "materialname" when node is MdlTrimeshNode mesh:
                    mesh.MaterialName = NullAsEmpty(RequiredToken(tokens, 1, line, "mesh material name"));
                    break;
                case "texture0" when node is MdlTrimeshNode:
                    texture0 = NullAsEmpty(RequiredToken(tokens, 1, line, "mesh texture0"));
                    break;
                case "lightmap" when node is MdlTrimeshNode mesh:
                    mesh.Lightmap = NullAsEmpty(RequiredToken(tokens, 1, line, "mesh lightmap"));
                    break;
                case "diffuse" when node is MdlTrimeshNode mesh:
                    mesh.Diffuse = Vector3Tokens(tokens, 1, line, "mesh diffuse");
                    break;
                case "deadspace" when node is MdlEmitterNode emitter:
                    emitter.DeadSpace = FloatToken(tokens, 1, line, "emitter dead space");
                    break;
                case "blastradius" when node is MdlEmitterNode emitter:
                    emitter.BlastRadius = FloatToken(tokens, 1, line, "emitter blast radius");
                    break;
                case "blastlength" when node is MdlEmitterNode emitter:
                    emitter.BlastLength = FloatToken(tokens, 1, line, "emitter blast length");
                    break;
                case "xgrid" when node is MdlEmitterNode emitter:
                    emitter.XGrid = PositiveIntToken(tokens, 1, line, "emitter X grid");
                    break;
                case "ygrid" when node is MdlEmitterNode emitter:
                    emitter.YGrid = PositiveIntToken(tokens, 1, line, "emitter Y grid");
                    break;
                case "update" when node is MdlEmitterNode emitter:
                    emitter.Update = RequiredToken(tokens, 1, line, "emitter update mode");
                    break;
                case "render" when node is MdlEmitterNode emitter:
                    emitter.RenderMode = RequiredToken(tokens, 1, line, "emitter render mode");
                    break;
                case "blend" when node is MdlEmitterNode emitter:
                    emitter.Blend = RequiredToken(tokens, 1, line, "emitter blend mode");
                    break;
                case "texture" when node is MdlEmitterNode emitter:
                    emitter.Texture = NullAsEmpty(RequiredToken(tokens, 1, line, "emitter texture"));
                    break;
                case "chunk" when node is MdlEmitterNode emitter:
                    emitter.Chunk = NullAsEmpty(RequiredToken(tokens, 1, line, "emitter chunk"));
                    break;
                case "twosidedtex" when node is MdlEmitterNode emitter:
                    emitter.TextureIsTwoSided = BoolToken(tokens, 1, line, "emitter two-sided texture flag");
                    break;
                case "loop" when node is MdlEmitterNode emitter:
                    emitter.Loop = BoolToken(tokens, 1, line, "emitter loop flag");
                    break;
                case "renderorder" when node is MdlEmitterNode emitter:
                    emitter.RenderOrder = checked((ushort)Math.Clamp(
                        IntToken(tokens, 1, line, "emitter render order"),
                        ushort.MinValue,
                        ushort.MaxValue));
                    break;
            }
        }

        // Tolerate a node cut off by end-of-input the same way (finalize what was read).
        if (node is MdlTrimeshNode finalMesh)
        {
            ApplyTexture0Fallback();
            FinalizeMesh(finalMesh, vertices, normals, textureCoordinates, faces, influences, source);
        }
        return new PendingNode(node, NullAsEmpty(parentName ?? string.Empty), source);
    }

    private Vector3[] ReadVector3Array(string[] declaration, SourceLine source, string context)
    {
        var count = ArrayCount(declaration, source, context, MaximumVertices);
        var values = new Vector3[count];
        for (var index = 0; index < values.Length; index++)
        {
            var line = RequiredDataLine(context);
            var tokens = Tokens(line);
            if (tokens.Length < 2)
                throw Error($"ASCII MDL {context} row requires at least two values.", line);
            values[index] = new Vector3(
                FloatToken(tokens, 0, line, context),
                FloatToken(tokens, 1, line, context),
                tokens.Length >= 3 ? FloatToken(tokens, 2, line, context) : 0f);
        }
        return values;
    }

    private Vector2[] ReadVector2Array(string[] declaration, SourceLine source, string context)
    {
        var count = ArrayCount(declaration, source, context, MaximumVertices);
        var values = new List<Vector2>(count);
        for (var index = 0; index < count; index++)
        {
            if (!TryRead(out var line))
                break;
            var tokens = Tokens(line);
            values.Add(new Vector2(
                FloatToken(tokens, 0, line, context),
                FloatToken(tokens, 1, line, context)));
        }
        return values.ToArray();
    }

    private AsciiFace[] ReadFaces(string[] declaration, SourceLine source)
    {
        var count = ArrayCount(declaration, source, "faces", MaximumFaces);
        var values = new AsciiFace[count];
        for (var index = 0; index < values.Length; index++)
        {
            var line = RequiredDataLine("faces");
            var tokens = Tokens(line);
            if (tokens.Length < 7)
                throw Error("ASCII MDL face requires three vertex indices, a surface, and three texture indices.", line);
            // Column layout: v1 v2 v3 smoothgroup tv1 tv2 tv3 material. SurfaceId comes from the
            // material column (index 7), matching the binary reader; smoothgroup (index 3) is not
            // a surface/material id. Older/malformed lines without a material column default to 0.
            values[index] = new AsciiFace(
                IntToken(tokens, 0, line, "face vertex"),
                IntToken(tokens, 1, line, "face vertex"),
                IntToken(tokens, 2, line, "face vertex"),
                IntToken(tokens, 3, line, "face smoothing group"),
                tokens.Length > 7 ? IntToken(tokens, 7, line, "face surface") : 0,
                IntToken(tokens, 4, line, "face texture vertex"),
                IntToken(tokens, 5, line, "face texture vertex"),
                IntToken(tokens, 6, line, "face texture vertex"));
        }
        return values;
    }

    private MdlSkinInfluence[][] ReadWeights(string[] declaration, SourceLine source)
    {
        var count = ArrayCount(declaration, source, "skin weights", MaximumVertices);
        var values = new MdlSkinInfluence[count][];
        for (var index = 0; index < values.Length; index++)
        {
            var line = RequiredDataLine("skin weights");
            var tokens = Tokens(line);
            if (tokens.Length == 0 || tokens.Length % 2 != 0)
                throw Error("ASCII MDL skin weight line must contain bone/weight pairs.", line);
            if (tokens.Length > 8)
                throw Error("ASCII MDL skin vertex has more than four bone influences.", line);

            var row = new MdlSkinInfluence[tokens.Length / 2];
            var total = 0f;
            for (var pair = 0; pair < row.Length; pair++)
            {
                var weight = FloatToken(tokens, pair * 2 + 1, line, "skin weight");
                if (weight < 0)
                    throw Error("ASCII MDL skin weight cannot be negative.", line);
                row[pair] = new MdlSkinInfluence(tokens[pair * 2], weight);
                total += weight;
            }
            if (!float.IsFinite(total))
                throw Error("ASCII MDL skin weights are not finite.", line);
            values[index] = row;
        }
        return values;
    }

    private void ReadPositionKeys(MdlNode node, string[] declaration, SourceLine source)
    {
        var rows = ReadKeyRows(declaration, source, "position keys", 4);
        node.PositionTimes = rows.Select(row => FloatToken(row.Tokens, 0, row.Line, "position key time")).ToArray();
        node.PositionValues = rows.Select(row => Vector3Tokens(row.Tokens, 1, row.Line, "position key")).ToArray();
        if (node.PositionValues.Length > 0)
            node.Position = node.PositionValues[0];
    }

    private void ReadOrientationKeys(MdlNode node, string[] declaration, SourceLine source)
    {
        var rows = ReadKeyRows(declaration, source, "orientation keys", 5);
        node.OrientationTimes = rows.Select(row => FloatToken(row.Tokens, 0, row.Line, "orientation key time")).ToArray();
        node.OrientationValues = rows.Select(row => AxisAngleTokens(row.Tokens, 1, row.Line, "orientation key")).ToArray();
        if (node.OrientationValues.Length > 0)
            node.Orientation = node.OrientationValues[0];
    }

    private void ReadScaleKeys(MdlNode node, string[] declaration, SourceLine source)
    {
        var rows = ReadKeyRows(declaration, source, "scale keys", 2);
        node.ScaleTimes = rows.Select(row => FloatToken(row.Tokens, 0, row.Line, "scale key time")).ToArray();
        node.ScaleValues = rows.Select(row => FloatToken(row.Tokens, 1, row.Line, "scale key value")).ToArray();
        if (node.ScaleValues.Length > 0)
            node.Scale = node.ScaleValues[0];
    }

    private List<KeyRow> ReadKeyRows(string[] declaration, SourceLine source, string context, int minimumColumns)
    {
        var result = new List<KeyRow>();
        if (declaration.Length >= 2 && int.TryParse(
                declaration[1],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var declaredCount))
        {
            if (declaredCount < 0 || declaredCount > MaximumControllerValues)
                throw Error($"ASCII MDL {context} count {declaredCount} is invalid.", source);
            for (var index = 0; index < declaredCount; index++)
                result.Add(ParseKeyRow(RequiredDataLine(context), context, minimumColumns));
            return result;
        }

        while (TryRead(out var line))
        {
            var tokens = Tokens(line);
            if (tokens.Length > 0 && tokens[0].Equals("endlist", StringComparison.OrdinalIgnoreCase))
                return result;
            result.Add(ParseKeyRow(line, context, minimumColumns));
            if (result.Count > MaximumControllerValues)
                throw Error($"ASCII MDL {context} count exceeds {MaximumControllerValues}.", source);
        }

        throw Error($"ASCII MDL {context} block is missing endlist.", source);
    }

    private static KeyRow ParseKeyRow(SourceLine line, string context, int minimumColumns)
    {
        var tokens = Tokens(line);
        if (tokens.Length < minimumColumns)
            throw Error($"ASCII MDL {context} row requires at least {minimumColumns} values.", line);
        return new KeyRow(line, tokens);
    }

    private static void FinalizeMesh(
        MdlTrimeshNode mesh,
        Vector3[] vertices,
        Vector3[] normals,
        Vector2[] textureCoordinates,
        AsciiFace[] sourceFaces,
        MdlSkinInfluence[][] influences,
        SourceLine source)
    {
        if (normals.Length != 0 && normals.Length != vertices.Length)
            throw Error($"ASCII MDL mesh '{mesh.Name}' has {normals.Length} normals for {vertices.Length} vertices.", source);
        if (mesh is MdlSkinmeshNode && influences.Length != 0 && influences.Length != vertices.Length)
            throw Error($"ASCII MDL skin '{mesh.Name}' has {influences.Length} weight rows for {vertices.Length} vertices.", source);

        if (sourceFaces.Length == 0)
        {
            mesh.Vertices = vertices;
            mesh.Normals = normals;
            mesh.TextureCoordinates = textureCoordinates;
            mesh.Faces = Array.Empty<MdlFace>();
            if (mesh is MdlSkinmeshNode skin)
                skin.VertexInfluences = influences;
            return;
        }

        ValidateFaceIndices();
        var generatedNormals = normals.Length == 0
            ? GenerateVertexNormals(vertices, sourceFaces)
            : null;
        var hasTextureCoordinates = textureCoordinates.Length != 0;
        var remap = new Dictionary<(int Vertex, int Texture, Vector3 Normal), ushort>();
        var expandedVertices = new List<Vector3>();
        var expandedNormals = new List<Vector3>();
        var expandedTextureCoordinates = new List<Vector2>();
        var expandedInfluences = new List<MdlSkinInfluence[]>();
        var faces = new MdlFace[sourceFaces.Length];
        for (var index = 0; index < sourceFaces.Length; index++)
        {
            var face = sourceFaces[index];
            var first = RemapVertex(face.Vertex0, face.Texture0, VertexNormal(index, 0, face.Vertex0));
            var second = RemapVertex(face.Vertex1, face.Texture1, VertexNormal(index, 1, face.Vertex1));
            var third = RemapVertex(face.Vertex2, face.Texture2, VertexNormal(index, 2, face.Vertex2));
            var normal = FaceNormal(expandedVertices[first], expandedVertices[second], expandedVertices[third]);
            faces[index] = new MdlFace
            {
                VertexIndex0 = first,
                VertexIndex1 = second,
                VertexIndex2 = third,
                SurfaceId = face.Surface,
                Normal = normal,
                Distance = Vector3.Dot(normal, expandedVertices[first])
            };
        }

        mesh.Vertices = expandedVertices.ToArray();
        mesh.Normals = expandedNormals.ToArray();
        mesh.TextureCoordinates = hasTextureCoordinates
            ? expandedTextureCoordinates.ToArray()
            : Array.Empty<Vector2>();
        mesh.Faces = faces;
        if (mesh is MdlSkinmeshNode skinmesh)
            skinmesh.VertexInfluences = influences.Length == 0
                ? Array.Empty<MdlSkinInfluence[]>()
                : expandedInfluences.ToArray();
        return;

        Vector3 VertexNormal(int faceIndex, int cornerIndex, int vertexIndex) =>
            generatedNormals == null
                ? normals[vertexIndex]
                : generatedNormals.Normals[generatedNormals.FaceNormalIndices[faceIndex, cornerIndex]];

        ushort RemapVertex(int vertexIndex, int textureIndex, Vector3 normal)
        {
            var mappedTextureIndex = hasTextureCoordinates ? textureIndex : -1;
            if (hasTextureCoordinates && (uint)textureIndex >= (uint)textureCoordinates.Length)
                throw Error($"ASCII MDL mesh '{mesh.Name}' references texture vertex {textureIndex} outside {textureCoordinates.Length}.", source);
            if (remap.TryGetValue((vertexIndex, mappedTextureIndex, normal), out var existing))
                return existing;
            if (expandedVertices.Count > ushort.MaxValue)
                throw Error($"ASCII MDL mesh '{mesh.Name}' expands beyond {ushort.MaxValue + 1} vertices.", source);

            var created = checked((ushort)expandedVertices.Count);
            remap.Add((vertexIndex, mappedTextureIndex, normal), created);
            expandedVertices.Add(vertices[vertexIndex]);
            expandedNormals.Add(normal);
            if (hasTextureCoordinates)
                expandedTextureCoordinates.Add(textureCoordinates[textureIndex]);
            if (influences.Length != 0)
                expandedInfluences.Add(influences[vertexIndex]);
            return created;
        }

        void ValidateFaceIndices()
        {
            foreach (var face in sourceFaces)
            {
                ValidateVertexIndex(face.Vertex0);
                ValidateVertexIndex(face.Vertex1);
                ValidateVertexIndex(face.Vertex2);
            }

            return;

            void ValidateVertexIndex(int index)
            {
                if ((uint)index >= (uint)vertices.Length)
                    throw Error($"ASCII MDL face references vertex {index} outside {vertices.Length}.", source);
            }
        }
    }

    /// <summary>
    /// Reproduces Aurora's ASCII model compiler smoothing pass. A smoothing-group value is a bit
    /// mask: faces share a vertex normal when their masks overlap; group zero remains flat. The
    /// compiler assigns each connected group of face corners one normal before it expands vertices
    /// at UV and hard-normal seams. Connectivity matters for masks such as 1, 3, 2: the middle face
    /// overlaps both neighbors, so all three belong to one group regardless of face order.
    /// </summary>
    private static GeneratedVertexNormals GenerateVertexNormals(
        IReadOnlyList<Vector3> vertices,
        IReadOnlyList<AsciiFace> faces)
    {
        var faceNormalIndices = new int[faces.Count, 3];
        for (var faceIndex = 0; faceIndex < faces.Count; faceIndex++)
        {
            for (var cornerIndex = 0; cornerIndex < 3; cornerIndex++)
                faceNormalIndices[faceIndex, cornerIndex] = -1;
        }

        var faceNormals = new Vector3[faces.Count];
        var cornersByVertex = new Dictionary<int, List<int>>();
        for (var faceIndex = 0; faceIndex < faces.Count; faceIndex++)
        {
            var face = faces[faceIndex];
            faceNormals[faceIndex] = Vector3.Cross(
                vertices[face.Vertex1] - vertices[face.Vertex0],
                vertices[face.Vertex2] - vertices[face.Vertex1]);
            for (var cornerIndex = 0; cornerIndex < 3; cornerIndex++)
            {
                var vertexIndex = VertexIndex(face, cornerIndex);
                if (!cornersByVertex.TryGetValue(vertexIndex, out var corners))
                {
                    corners = new List<int>();
                    cornersByVertex.Add(vertexIndex, corners);
                }

                corners.Add(faceIndex * 3 + cornerIndex);
            }
        }

        var normals = new List<Vector3>();
        foreach (var corners in cornersByVertex.Values)
        {
            var assigned = new HashSet<int>();
            foreach (var seed in corners)
            {
                if (!assigned.Add(seed))
                    continue;

                var component = new List<int>();
                var pending = new Queue<int>();
                pending.Enqueue(seed);
                while (pending.Count > 0)
                {
                    var current = pending.Dequeue();
                    component.Add(current);
                    var currentFaceIndex = current / 3;
                    foreach (var candidate in corners)
                    {
                        if (assigned.Contains(candidate))
                            continue;

                        var candidateFaceIndex = candidate / 3;
                        if (currentFaceIndex != candidateFaceIndex &&
                            (faces[currentFaceIndex].SmoothingGroup &
                             faces[candidateFaceIndex].SmoothingGroup) == 0)
                        {
                            continue;
                        }

                        assigned.Add(candidate);
                        pending.Enqueue(candidate);
                    }
                }

                // A stable face ordering also makes the floating-point sum independent of the
                // source's face row order, rather than merely giving it the same membership.
                var componentFaces = component
                    .Select(corner => corner / 3)
                    .Distinct()
                    .OrderBy(index => faces[index].SmoothingGroup)
                    .ThenBy(index => faces[index].Vertex0)
                    .ThenBy(index => faces[index].Vertex1)
                    .ThenBy(index => faces[index].Vertex2)
                    .ThenBy(index => faceNormals[index].X)
                    .ThenBy(index => faceNormals[index].Y)
                    .ThenBy(index => faceNormals[index].Z)
                    .ToList();
                var sum = componentFaces.Aggregate(
                    Vector3.Zero,
                    (current, faceIndex) => current + faceNormals[faceIndex]);
                var normalIndex = normals.Count;
                normals.Add(sum.LengthSquared() <= float.Epsilon
                    ? Vector3.Zero
                    : Vector3.Normalize(sum));
                foreach (var corner in component)
                {
                    faceNormalIndices[corner / 3, corner % 3] = normalIndex;
                }
            }
        }

        return new GeneratedVertexNormals(normals.ToArray(), faceNormalIndices);

        static int VertexIndex(AsciiFace face, int cornerIndex) => cornerIndex switch
        {
            0 => face.Vertex0,
            1 => face.Vertex1,
            _ => face.Vertex2
        };
    }

    private static MdlNode? LinkNodes(IReadOnlyList<PendingNode> pending, string preferredRootName)
    {
        if (pending.Count == 0)
            return null;

        var byName = new Dictionary<string, MdlNode>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in pending)
            byName.TryAdd(item.Node.Name, item.Node);

        foreach (var item in pending)
        {
            if (item.ParentName.Length == 0)
                continue;
            if (!byName.TryGetValue(item.ParentName, out var parent) || ReferenceEquals(parent, item.Node))
                continue;
            item.Node.Parent = parent;
            parent.Children.Add(item.Node);
        }

        ValidateTree(pending);
        var roots = pending.Select(item => item.Node).Where(node => node.Parent == null).ToList();
        if (roots.Count == 1)
            return roots[0];

        var preferred = roots.FirstOrDefault(root =>
            root.Name.Equals(preferredRootName, StringComparison.OrdinalIgnoreCase));
        if (preferred != null)
        {
            foreach (var root in roots.Where(root => !ReferenceEquals(root, preferred)))
            {
                root.Parent = preferred;
                preferred.Children.Add(root);
            }
            return preferred;
        }

        var synthetic = new MdlNode { Name = preferredRootName };
        foreach (var root in roots)
        {
            root.Parent = synthetic;
            synthetic.Children.Add(root);
        }
        return synthetic;
    }

    private static void ValidateTree(IReadOnlyList<PendingNode> pending)
    {
        var states = new Dictionary<MdlNode, byte>();
        foreach (var item in pending)
            Visit(item.Node, 0);
        return;

        void Visit(MdlNode node, int depth)
        {
            if (depth > MaximumDepth)
                throw Error($"ASCII MDL node depth exceeds {MaximumDepth}.");
            if (states.TryGetValue(node, out var state))
            {
                if (state == 1)
                    throw Error($"ASCII MDL node graph contains a cycle at '{node.Name}'.");
                return;
            }
            states[node] = 1;
            foreach (var child in node.Children)
                Visit(child, depth + 1);
            states[node] = 2;
        }
    }

    private static void ComputeBounds(MdlModel model)
    {
        var meshes = model.GetMeshNodes().Where(mesh => mesh.Vertices.Length > 0).ToArray();
        if (meshes.Length == 0)
            return;
        var minimum = new Vector3(float.PositiveInfinity);
        var maximum = new Vector3(float.NegativeInfinity);
        var radiusSquared = 0f;
        foreach (var mesh in meshes)
        {
            // Bounds describe the model, not each mesh's local space: a positioned, rotated, or
            // scaled node (or ancestor) moves its vertices, and consumers compose transforms the
            // same way (scale, then rotation, then translation, accumulated root-ward).
            var transform = ComposeNodeToModelTransform(mesh);
            foreach (var vertex in mesh.Vertices)
            {
                var transformed = Vector3.Transform(vertex, transform);
                minimum = Vector3.Min(minimum, transformed);
                maximum = Vector3.Max(maximum, transformed);
                radiusSquared = MathF.Max(radiusSquared, transformed.LengthSquared());
            }
        }
        model.BoundsMinimum = minimum;
        model.BoundsMaximum = maximum;
        model.Radius = MathF.Sqrt(radiusSquared);
    }

    private static Matrix4x4 ComposeNodeToModelTransform(MdlNode node)
    {
        var transform = Matrix4x4.Identity;
        for (var current = node; current != null; current = current.Parent)
        {
            var local = Matrix4x4.CreateScale(current.Scale) *
                        Matrix4x4.CreateFromQuaternion(current.Orientation) *
                        Matrix4x4.CreateTranslation(current.Position);
            transform *= local;
        }
        return transform;
    }

    private void LoadLines(string text)
    {
        using var reader = new StringReader(text);
        var number = 0;
        while (reader.ReadLine() is { } raw)
        {
            number++;
            if (number > MaximumLines)
                throw Error($"ASCII MDL line count exceeds {MaximumLines}.");
            if (raw.Length > MaximumLineLength)
                throw Error($"ASCII MDL line {number} exceeds {MaximumLineLength} characters.");
            var value = raw.Trim();
            if (value.Length > 0 && value[0] != '#')
                _lines.Add(new SourceLine(number, NormalizeConcatenatedDirective(value)));
        }
    }

    private static string NormalizeConcatenatedDirective(string value)
    {
        foreach (var directive in new[]
                 {
                     "setsupermodel",
                     "beginmodelgeom",
                     "endmodelgeom",
                     "donemodel",
                     "newmodel",
                     "parent"
                 })
        {
            if (value.Length > directive.Length &&
                value.StartsWith(directive, StringComparison.OrdinalIgnoreCase) &&
                !char.IsWhiteSpace(value[directive.Length]))
            {
                return $"{value[..directive.Length]} {value[directive.Length..]}";
            }
        }

        if (!value.StartsWith("node ", StringComparison.OrdinalIgnoreCase))
            return value;
        var tokens = value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length != 2)
            return value;
        foreach (var type in new[]
                 {
                     "danglymesh",
                     "animmesh",
                     "trimesh",
                     "emitter",
                     "reference",
                     "dummy",
                     "light",
                     "skin",
                     "aabb",
                     "pwk"
                 })
        {
            if (tokens[1].Length > type.Length &&
                tokens[1].StartsWith(type, StringComparison.OrdinalIgnoreCase))
            {
                return $"node {tokens[1][..type.Length]} {tokens[1][type.Length..]}";
            }
        }
        return value;
    }

    private bool TryRead(out SourceLine line)
    {
        if (_index >= _lines.Count)
        {
            line = default;
            return false;
        }
        line = _lines[_index++];
        return true;
    }

    private SourceLine RequiredDataLine(string context)
    {
        if (!TryRead(out var line))
            throw Error($"ASCII MDL ended while reading {context}.");
        return line;
    }

    private static int ArrayCount(string[] tokens, SourceLine source, string context, int maximum)
    {
        var count = IntToken(tokens, 1, source, $"{context} count");
        if (count < 0 || count > maximum)
            throw Error($"ASCII MDL {context} count {count} is outside 0..{maximum}.", source);
        return count;
    }

    private static string[] Tokens(SourceLine line) =>
        line.Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

    private static string RequiredToken(string[] tokens, int index, SourceLine source, string context)
    {
        if ((uint)index >= (uint)tokens.Length)
            throw Error($"ASCII MDL {context} is missing.", source);
        return tokens[index];
    }

    private static int IntToken(string[] tokens, int index, SourceLine source, string context)
    {
        var token = RequiredToken(tokens, index, source, context);
        if (!int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            throw Error($"ASCII MDL {context} value '{token}' is not an integer.", source);
        return value;
    }

    private static int PositiveIntToken(string[] tokens, int index, SourceLine source, string context)
    {
        var value = IntToken(tokens, index, source, context);
        return value <= 0 ? 1 : value;
    }

    private static float FloatToken(string[] tokens, int index, SourceLine source, string context)
    {
        var token = RequiredToken(tokens, index, source, context);
        if (!float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
            !float.IsFinite(value))
            throw Error($"ASCII MDL {context} value '{token}' is not a finite number.", source);
        return value;
    }

    private static bool BoolToken(string[] tokens, int index, SourceLine source, string context)
    {
        var token = RequiredToken(tokens, index, source, context);
        if (token.Equals("true", StringComparison.OrdinalIgnoreCase))
            return true;
        if (token.Equals("false", StringComparison.OrdinalIgnoreCase))
            return false;
        return IntToken(tokens, index, source, context) != 0;
    }

    private static Vector3 Vector3Tokens(string[] tokens, int start, SourceLine source, string context) =>
        new(
            FloatToken(tokens, start, source, context),
            FloatToken(tokens, start + 1, source, context),
            FloatToken(tokens, start + 2, source, context));

    private static Quaternion AxisAngleTokens(string[] tokens, int start, SourceLine source, string context)
    {
        var axis = Vector3Tokens(tokens, start, source, context);
        var angle = FloatToken(tokens, start + 3, source, context);
        if (axis.LengthSquared() <= float.Epsilon || MathF.Abs(angle) <= float.Epsilon)
            return Quaternion.Identity;
        return Quaternion.Normalize(Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), angle));
    }

    private static Vector3 FaceNormal(Vector3 first, Vector3 second, Vector3 third)
    {
        var cross = Vector3.Cross(second - first, third - first);
        return cross.LengthSquared() <= float.Epsilon ? Vector3.Zero : Vector3.Normalize(cross);
    }

    private static string NullAsEmpty(string value) =>
        value.Equals("NULL", StringComparison.OrdinalIgnoreCase) ? string.Empty : value;

    private static NwnFormatException Error(string message, SourceLine? source = null) =>
        new(source.HasValue ? $"{message} (line {source.Value.Number})" : message);

    private readonly record struct SourceLine(int Number, string Text);
    private readonly record struct PendingNode(MdlNode Node, string ParentName, SourceLine Source);
    private readonly record struct KeyRow(SourceLine Line, string[] Tokens);
    private readonly record struct AsciiFace(
        int Vertex0,
        int Vertex1,
        int Vertex2,
        int SmoothingGroup,
        int Surface,
        int Texture0,
        int Texture1,
        int Texture2);
    private sealed record GeneratedVertexNormals(Vector3[] Normals, int[,] FaceNormalIndices);
}
