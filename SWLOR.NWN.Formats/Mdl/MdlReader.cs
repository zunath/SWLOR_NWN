// SPDX-License-Identifier: MIT

using System.Numerics;
using SWLOR.NWN.Formats.Internal;

namespace SWLOR.NWN.Formats.Mdl;

/// <summary>
/// Reads binary and ASCII Neverwinter Nights MDL resources.
/// </summary>
public sealed class MdlReader
{
    private const int FileHeaderSize = 12;
    private const int GeometryHeaderSize = 112;
    private const int ModelHeaderSize = 232;
    private const int AnimationHeaderSize = 196;
    private const int NodeHeaderSize = 112;
    private const int MeshHeaderSize = 512;
    private const int SkinHeaderSize = 100;
    private const int EmitterHeaderSize = 216;
    private const int MaximumNodes = 100_000;
    private const int MaximumAnimations = 10_000;
    private const int MaximumVertices = ushort.MaxValue;
    private const int MaximumFaces = 1_000_000;
    private const int MaximumControllerValues = 4_000_000;
    private const int MaximumDepth = 512;

    private GuardedBinaryReader _reader = null!;
    private long _modelBase;
    private long _modelDataEnd;
    private long _mdxBase;
    private long _mdxEnd;
    private AllocationBudget _allocationBudget = null!;
    private readonly Dictionary<(uint Pointer, bool HasGeometryPayload), MdlNode> _nodes = new();
    private readonly HashSet<(uint Pointer, bool HasGeometryPayload)> _activeNodes = new();

    public MdlModel Parse(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length >= sizeof(uint) && BitConverter.ToUInt32(data, 0) != 0)
            return new AsciiMdlReader().Parse(data);

        return ParseBinary(data);
    }

    private MdlModel ParseBinary(byte[] data)
    {
        _reader = new GuardedBinaryReader(data);
        _allocationBudget = new AllocationBudget("Binary MDL");
        _nodes.Clear();
        _activeNodes.Clear();

        if (data.Length < FileHeaderSize + ModelHeaderSize)
            throw new NwnFormatException("Binary MDL is shorter than its file and model headers.");
        var mdxOffset = _reader.ReadUInt32(4);
        var mdxSize = _reader.ReadUInt32(8);
        _modelBase = FileHeaderSize;
        _modelDataEnd = CheckedAdd(_modelBase, mdxOffset, "MDL model-data boundary");
        _mdxBase = _modelDataEnd;
        _mdxEnd = CheckedAdd(_mdxBase, mdxSize, "MDL MDX boundary");
        if (_modelDataEnd > data.LongLength || _mdxEnd > data.LongLength)
            throw new NwnFormatException("MDL header points beyond the end of the resource.");

        ValidateModelAbsoluteRange(_modelBase, ModelHeaderSize, "MDL model header");

        var model = new MdlModel
        {
            Name = FixedString(_modelBase + 8, 64, "MDL model name"),
            SuperModel = FixedString(_modelBase + 168, 64, "MDL supermodel name"),
            ModelType = _reader.ReadByte(_modelBase + 108),
            BoundsMinimum = Vector3At(_modelBase + 136, "MDL minimum bounds"),
            BoundsMaximum = Vector3At(_modelBase + 148, "MDL maximum bounds"),
            Radius = Finite(_reader.ReadSingle(_modelBase + 160), "MDL radius"),
            Scale = Finite(_reader.ReadSingle(_modelBase + 164), "MDL scale")
        };

        var rootPointer = _reader.ReadUInt32(_modelBase + 72);
        if (rootPointer != 0)
            model.GeometryRoot = ParseNode(rootPointer, null, 0, hasGeometryPayload: true);

        ParseAnimations(model, _modelBase + 120);
        return model;
    }

    private void ParseAnimations(MdlModel model, long definitionOffset)
    {
        var pointer = _reader.ReadUInt32(definitionOffset);
        var count = CheckedCount(_reader.ReadUInt32(definitionOffset + 4), MaximumAnimations, "MDL animation");
        ValidateModelRange(pointer, checked(count * 4L), "MDL animation pointer array");
        _allocationBudget.ReserveElements(count, 128, "MDL animations");

        for (var index = 0; index < count; index++)
        {
            var animationPointer = _reader.ReadUInt32(ModelOffset(pointer) + index * 4L);
            if (animationPointer == 0)
                throw new NwnFormatException($"MDL animation pointer {index} is null.");

            var offset = ModelOffset(animationPointer);
            ValidateModelAbsoluteRange(offset, AnimationHeaderSize, $"MDL animation {index}");
            var animation = new MdlAnimation
            {
                Name = FixedString(offset + 8, 64, $"MDL animation {index} name"),
                Length = Finite(_reader.ReadSingle(offset + GeometryHeaderSize), "MDL animation length"),
                TransitionTime = Finite(
                    _reader.ReadSingle(offset + GeometryHeaderSize + 4),
                    "MDL animation transition time")
            };

            var rootPointer = _reader.ReadUInt32(offset + 72);
            if (rootPointer != 0)
                animation.GeometryRoot = ParseNode(rootPointer, null, 0, hasGeometryPayload: false);
            model.Animations.Add(animation);
        }
    }

    private MdlNode ParseNode(uint pointer, MdlNode? parent, int depth, bool hasGeometryPayload)
    {
        var nodeKey = (pointer, hasGeometryPayload);
        if (depth > MaximumDepth)
            throw new NwnFormatException($"MDL node depth exceeds {MaximumDepth}.");
        if (_activeNodes.Contains(nodeKey))
            throw new NwnFormatException($"MDL node graph contains a cycle at pointer 0x{pointer:X8}.");
        if (_nodes.TryGetValue(nodeKey, out var existing))
        {
            if (existing.Parent != parent)
                throw new NwnFormatException($"MDL node pointer 0x{pointer:X8} has more than one parent.");
            return existing;
        }
        if (_nodes.Count >= MaximumNodes)
            throw new NwnFormatException($"MDL node count exceeds {MaximumNodes}.");

        var offset = ModelOffset(pointer);
        ValidateModelAbsoluteRange(offset, NodeHeaderSize, "MDL node header");
        var content = _reader.ReadUInt32(offset + 108);
        var hasEmitter = (content & 0x04) != 0;
        var hasMesh = (content & 0x20) != 0;
        var hasSkin = (content & 0x40) != 0;
        ValidateNodeFlags(content);
        _allocationBudget.Reserve(1_024, "MDL node");

        MdlNode node = !hasGeometryPayload
            ? new MdlNode()
            : hasSkin
            ? new MdlSkinmeshNode()
            : hasMesh
                ? new MdlTrimeshNode()
                : hasEmitter
                    ? new MdlEmitterNode()
                    : new MdlNode();
        node.Name = FixedString(offset + 32, 32, "MDL node name");
        node.Parent = parent;
        _nodes.Add(nodeKey, node);
        _activeNodes.Add(nodeKey);

        try
        {
            if (hasGeometryPayload)
            {
                var extraOffset = offset + NodeHeaderSize;
                if ((content & 0x02) != 0)
                    extraOffset = CheckedSection(extraOffset, 92, "MDL light header");
                if (hasEmitter)
                {
                    ParseEmitter((MdlEmitterNode)node, extraOffset);
                    extraOffset = CheckedSection(extraOffset, EmitterHeaderSize, "MDL emitter header");
                }
                if ((content & 0x10) != 0)
                    extraOffset = CheckedSection(extraOffset, 68, "MDL reference header");
                var mdxStride = 0;
                if (hasMesh)
                {
                    mdxStride = ParseMesh((MdlTrimeshNode)node, extraOffset);
                    extraOffset = CheckedSection(extraOffset, MeshHeaderSize, "MDL mesh header");
                }
                if (hasSkin)
                {
                    ParseSkin((MdlSkinmeshNode)node, extraOffset, mdxStride);
                    extraOffset = CheckedSection(extraOffset, SkinHeaderSize, "MDL skin header");
                }
                if ((content & 0x80) != 0)
                    extraOffset = CheckedSection(extraOffset, 52, "MDL animmesh header");
                if ((content & 0x100) != 0)
                    extraOffset = CheckedSection(extraOffset, 24, "MDL danglymesh header");
                if ((content & 0x200) != 0)
                {
                    // The payload itself is not read, but the node has to be marked: its triangles
                    // are the walkable surface, not artwork. See MdlTrimeshNode.IsWalkmesh.
                    if (node is MdlTrimeshNode aabbMesh)
                        aabbMesh.IsWalkmesh = true;
                    _ = CheckedSection(extraOffset, 4, "MDL AABB header");
                }
            }

            ParseControllers(node, offset + 84, offset + 96);
            ParseChildren(node, offset + 72, depth, hasGeometryPayload);
            return node;
        }
        finally
        {
            _activeNodes.Remove(nodeKey);
        }
    }

    private void ParseChildren(MdlNode node, long definitionOffset, int depth, bool hasGeometryPayload)
    {
        var pointer = _reader.ReadUInt32(definitionOffset);
        var count = CheckedCount(_reader.ReadUInt32(definitionOffset + 4), MaximumNodes, "MDL child node");
        ValidateModelRange(pointer, checked(count * 4L), "MDL child pointer array");
        _allocationBudget.ReserveElements(count, IntPtr.Size, "MDL child references");
        var arrayOffset = ModelOffset(pointer);
        for (var index = 0; index < count; index++)
        {
            var childPointer = _reader.ReadUInt32(arrayOffset + index * 4L);
            if (childPointer == 0)
                throw new NwnFormatException($"MDL child pointer {index} of node '{node.Name}' is null.");
            node.Children.Add(ParseNode(childPointer, node, depth + 1, hasGeometryPayload));
        }
    }

    private void ParseEmitter(MdlEmitterNode emitter, long offset)
    {
        ValidateModelAbsoluteRange(offset, EmitterHeaderSize, "MDL emitter header");
        emitter.DeadSpace = Finite(_reader.ReadSingle(offset), "MDL emitter dead space");
        emitter.BlastRadius = Finite(_reader.ReadSingle(offset + 4), "MDL emitter blast radius");
        emitter.BlastLength = Finite(_reader.ReadSingle(offset + 8), "MDL emitter blast length");
        emitter.XGrid = PositiveGrid(_reader.ReadUInt32(offset + 12), "MDL emitter X grid");
        emitter.YGrid = PositiveGrid(_reader.ReadUInt32(offset + 16), "MDL emitter Y grid");
        emitter.Update = FixedString(offset + 24, 32, "MDL emitter update");
        emitter.RenderMode = FixedString(offset + 56, 32, "MDL emitter render mode");
        emitter.Blend = FixedString(offset + 88, 32, "MDL emitter blend");
        emitter.Texture = FixedString(offset + 120, 64, "MDL emitter texture");
        emitter.Chunk = FixedString(offset + 184, 16, "MDL emitter chunk");
        emitter.TextureIsTwoSided = _reader.ReadUInt32(offset + 200) != 0;
        emitter.Loop = _reader.ReadUInt32(offset + 204) != 0;
        emitter.RenderOrder = _reader.ReadUInt16(offset + 208);
    }

    private int ParseMesh(MdlTrimeshNode mesh, long offset)
    {
        ValidateModelAbsoluteRange(offset, MeshHeaderSize, "MDL mesh header");
        mesh.Diffuse = new Vector3(
            Finite(_reader.ReadSingle(offset + 60), "MDL mesh diffuse"),
            Finite(_reader.ReadSingle(offset + 64), "MDL mesh diffuse"),
            Finite(_reader.ReadSingle(offset + 68), "MDL mesh diffuse"));
        mesh.Render = _reader.ReadUInt32(offset + 108) != 0;
        mesh.Bitmap = FixedString(offset + 120, 64, "MDL mesh bitmap");
        mesh.Lightmap = FixedString(offset + 184, 64, "MDL mesh lightmap");
        mesh.TileFade = _reader.ReadInt32(offset + 376);

        var facePointer = _reader.ReadUInt32(offset + 8);
        var faceCount = CheckedCount(_reader.ReadUInt32(offset + 12), MaximumFaces, "MDL face");
        ValidateModelRange(facePointer, checked(faceCount * 32L), "MDL face array");
        var faceOffset = ModelOffset(facePointer);
        // Faces are reference-type records: charge conservatively for the array slot, object,
        // alignment, and parsing overhead rather than only the 32-byte on-disk record.
        _allocationBudget.ReserveElements(faceCount, 256, "MDL faces");
        var faces = new MdlFace[faceCount];
        for (var index = 0; index < faceCount; index++)
        {
            var current = faceOffset + index * 32L;
            faces[index] = new MdlFace
            {
                Normal = Vector3At(current, "MDL face normal"),
                Distance = Finite(_reader.ReadSingle(current + 12), "MDL face distance"),
                SurfaceId = _reader.ReadInt32(current + 16),
                VertexIndex0 = _reader.ReadUInt16(current + 26),
                VertexIndex1 = _reader.ReadUInt16(current + 28),
                VertexIndex2 = _reader.ReadUInt16(current + 30)
            };
        }
        mesh.Faces = faces;

        var vertexCount = _reader.ReadUInt16(offset + 448);
        if (vertexCount > MaximumVertices)
            throw new NwnFormatException($"MDL vertex count {vertexCount} exceeds {MaximumVertices}.");
        var mdxStride = CheckedMdxStride(_reader.ReadUInt32(offset + 440), vertexCount);
        mesh.Vertices = ReadMdxVector3Array(
            _reader.ReadInt32(offset + 444),
            vertexCount,
            mdxStride,
            "MDL vertices");
        mesh.TextureCoordinates = ReadMdxVector2Array(
            _reader.ReadInt32(offset + 452),
            vertexCount,
            mdxStride,
            "MDL texture coordinates");
        mesh.Normals = ReadMdxVector3Array(
            _reader.ReadInt32(offset + 468),
            vertexCount,
            mdxStride,
            "MDL normals");
        return mdxStride;
    }

    private void ParseSkin(MdlSkinmeshNode skin, long offset, int mdxStride)
    {
        ValidateModelAbsoluteRange(offset, SkinHeaderSize, "MDL skin header");
        var vertexCount = skin.Vertices.Length;

        var mappingPointer = _reader.ReadInt32(offset + 20);
        var mappingCount = _reader.ReadInt32(offset + 24);
        if (mappingCount < 0 || mappingCount > MaximumNodes)
            throw new NwnFormatException($"MDL bone mapping count {mappingCount} is invalid.");
        if (mappingCount > 0)
        {
            if (mappingPointer <= 0)
                throw new NwnFormatException("MDL bone mapping has entries but no pointer.");
            ValidateModelRange((uint)mappingPointer, checked(mappingCount * 2L), "MDL bone mapping");
            _allocationBudget.ReserveElements(mappingCount, sizeof(short), "MDL bone mapping");
            var mapping = new short[mappingCount];
            var mappingOffset = ModelOffset((uint)mappingPointer);
            for (var index = 0; index < mapping.Length; index++)
                mapping[index] = _reader.ReadInt16(mappingOffset + index * 2L);
            skin.BoneMapping = mapping;
        }

        skin.BoneQuaternions = ReadModelQuaternionArray(offset + 28, "MDL bone quaternions");
        skin.BoneTranslations = ReadModelVector3Array(offset + 40, "MDL bone translations");

        var weightPointer = _reader.ReadInt32(offset + 12);
        if (weightPointer >= 0 && vertexCount > 0)
        {
            var weightStride = EffectiveMdxStride(mdxStride, 16);
            var absolute = MdxOffset(
                weightPointer,
                MdxWindowSize(vertexCount, weightStride, 16, "MDL bone weights"),
                "MDL bone weights");
            _allocationBudget.ReserveElements(vertexCount, 16, "MDL bone weights");
            var weights = new Vector4[vertexCount];
            for (var index = 0; index < weights.Length; index++)
            {
                var current = absolute + index * (long)weightStride;
                weights[index] = new Vector4(
                    Finite(_reader.ReadSingle(current), "MDL bone weight"),
                    Finite(_reader.ReadSingle(current + 4), "MDL bone weight"),
                    Finite(_reader.ReadSingle(current + 8), "MDL bone weight"),
                    Finite(_reader.ReadSingle(current + 12), "MDL bone weight"));
            }
            skin.BoneWeights = weights;
        }

        var indexPointer = _reader.ReadInt32(offset + 16);
        if (indexPointer >= 0 && vertexCount > 0)
        {
            var indexStride = EffectiveMdxStride(mdxStride, 8);
            var absolute = MdxOffset(
                indexPointer,
                MdxWindowSize(vertexCount, indexStride, 8, "MDL bone indices"),
                "MDL bone indices");
            _allocationBudget.ReserveElements(vertexCount, 8, "MDL bone indices");
            var indices = new MdlBoneIndices[vertexCount];
            for (var index = 0; index < indices.Length; index++)
            {
                var current = absolute + index * (long)indexStride;
                indices[index] = new MdlBoneIndices(
                    _reader.ReadInt16(current),
                    _reader.ReadInt16(current + 2),
                    _reader.ReadInt16(current + 4),
                    _reader.ReadInt16(current + 6));
            }
            skin.BoneIndices = indices;
        }
    }

    private void ParseControllers(MdlNode node, long keyDefinitionOffset, long dataDefinitionOffset)
    {
        var keyPointer = _reader.ReadUInt32(keyDefinitionOffset);
        var keyCount = CheckedCount(
            _reader.ReadUInt32(keyDefinitionOffset + 4),
            MaximumControllerValues,
            "MDL controller key");
        var dataPointer = _reader.ReadUInt32(dataDefinitionOffset);
        var dataCount = CheckedCount(
            _reader.ReadUInt32(dataDefinitionOffset + 4),
            MaximumControllerValues,
            "MDL controller data");
        ValidateModelRange(keyPointer, checked(keyCount * 12L), "MDL controller key array");
        ValidateModelRange(dataPointer, checked(dataCount * 4L), "MDL controller data array");
        var keyOffset = ModelOffset(keyPointer);
        var dataOffset = ModelOffset(dataPointer);

        for (var index = 0; index < keyCount; index++)
        {
            var current = keyOffset + index * 12L;
            var type = _reader.ReadUInt32(current);
            var rowCount = _reader.ReadUInt16(current + 4);
            var timeStart = _reader.ReadUInt16(current + 6);
            var valueStart = _reader.ReadUInt16(current + 8);
            var columns = _reader.ReadByte(current + 10);
            if (rowCount == 0)
                continue;

            ValidateFloatWindow(timeStart, rowCount, dataCount, "MDL controller times");
            ValidateFloatWindow(valueStart, checked(rowCount * columns), dataCount, "MDL controller values");
            var times = ReadFloatArray(dataOffset + timeStart * 4L, rowCount, "MDL controller times");

            switch (type)
            {
                case 8 when columns >= 3:
                    node.PositionTimes = times;
                    node.PositionValues = ReadControllerVector3(dataOffset, valueStart, rowCount, columns);
                    node.Position = node.PositionValues[0];
                    break;
                case 20 when columns >= 4:
                    node.OrientationTimes = times;
                    node.OrientationValues = ReadControllerQuaternions(dataOffset, valueStart, rowCount, columns);
                    node.Orientation = node.OrientationValues[0];
                    break;
                case 36 when columns >= 1:
                    node.ScaleTimes = times;
                    node.ScaleValues = ReadControllerScalars(dataOffset, valueStart, rowCount, columns);
                    node.Scale = node.ScaleValues[0];
                    break;
            }
        }
    }

    private Vector3[] ReadControllerVector3(long dataOffset, int start, int rows, int columns)
    {
        _allocationBudget.ReserveElements(rows, 12, "MDL position controller values");
        var values = new Vector3[rows];
        for (var index = 0; index < rows; index++)
        {
            var current = dataOffset + checked((start + index * columns) * 4L);
            values[index] = Vector3At(current, "MDL position controller");
        }
        return values;
    }

    private Quaternion[] ReadControllerQuaternions(long dataOffset, int start, int rows, int columns)
    {
        _allocationBudget.ReserveElements(rows, 16, "MDL orientation controller values");
        var values = new Quaternion[rows];
        for (var index = 0; index < rows; index++)
        {
            var current = dataOffset + checked((start + index * columns) * 4L);
            values[index] = QuaternionAt(current, "MDL orientation controller");
        }
        return values;
    }

    private float[] ReadControllerScalars(long dataOffset, int start, int rows, int columns)
    {
        _allocationBudget.ReserveElements(rows, sizeof(float), "MDL scale controller values");
        var values = new float[rows];
        for (var index = 0; index < rows; index++)
            values[index] = Finite(
                _reader.ReadSingle(dataOffset + checked((start + index * columns) * 4L)),
                "MDL scale controller");
        return values;
    }

    private Vector3[] ReadMdxVector3Array(int pointer, int count, int stride, string context)
    {
        if (pointer < 0 || count == 0)
            return Array.Empty<Vector3>();
        stride = EffectiveMdxStride(stride, 12);
        var offset = MdxOffset(pointer, MdxWindowSize(count, stride, 12, context), context);
        _allocationBudget.ReserveElements(count, 12, context);
        var values = new Vector3[count];
        for (var index = 0; index < values.Length; index++)
            values[index] = Vector3At(offset + index * (long)stride, context);
        return values;
    }

    private Vector2[] ReadMdxVector2Array(int pointer, int count, int stride, string context)
    {
        if (pointer < 0 || count == 0)
            return Array.Empty<Vector2>();
        stride = EffectiveMdxStride(stride, 8);
        var offset = MdxOffset(pointer, MdxWindowSize(count, stride, 8, context), context);
        _allocationBudget.ReserveElements(count, 8, context);
        var values = new Vector2[count];
        for (var index = 0; index < values.Length; index++)
        {
            var current = offset + index * (long)stride;
            values[index] = new Vector2(
                Finite(_reader.ReadSingle(current), context),
                Finite(_reader.ReadSingle(current + 4), context));
        }
        return values;
    }

    private static int CheckedMdxStride(uint value, int vertexCount)
    {
        if (vertexCount == 0)
            return 0;
        if (value > int.MaxValue)
            throw new NwnFormatException($"MDL MDX vertex stride {value} is invalid.");
        // A stored stride of 0 is real corpus data (base-game dag_*/ptm_* placeables): it means
        // the MDX arrays are tightly packed, so each consumer substitutes its own element size.
        return (int)value;
    }

    private static int EffectiveMdxStride(int stride, int elementSize) =>
        stride == 0 ? elementSize : stride;

    private static long MdxWindowSize(int count, int stride, int elementSize, string context)
    {
        if (stride < elementSize)
        {
            throw new NwnFormatException(
                $"{context} element size {elementSize} exceeds MDX vertex stride {stride}.");
        }

        return checked((count - 1L) * stride + elementSize);
    }

    private Quaternion[] ReadModelQuaternionArray(long definitionOffset, string context)
    {
        var pointer = _reader.ReadUInt32(definitionOffset);
        var count = CheckedCount(_reader.ReadUInt32(definitionOffset + 4), MaximumNodes, context);
        ValidateModelRange(pointer, checked(count * 16L), context);
        var offset = ModelOffset(pointer);
        _allocationBudget.ReserveElements(count, 16, context);
        var values = new Quaternion[count];
        for (var index = 0; index < values.Length; index++)
            values[index] = QuaternionAt(offset + index * 16L, context);
        return values;
    }

    private Vector3[] ReadModelVector3Array(long definitionOffset, string context)
    {
        var pointer = _reader.ReadUInt32(definitionOffset);
        var count = CheckedCount(_reader.ReadUInt32(definitionOffset + 4), MaximumNodes, context);
        ValidateModelRange(pointer, checked(count * 12L), context);
        var offset = ModelOffset(pointer);
        _allocationBudget.ReserveElements(count, 12, context);
        var values = new Vector3[count];
        for (var index = 0; index < values.Length; index++)
            values[index] = Vector3At(offset + index * 12L, context);
        return values;
    }

    private float[] ReadFloatArray(long offset, int count, string context)
    {
        _allocationBudget.ReserveElements(count, sizeof(float), context);
        var values = new float[count];
        for (var index = 0; index < values.Length; index++)
            values[index] = Finite(_reader.ReadSingle(offset + index * 4L), context);
        return values;
    }

    private Vector3 Vector3At(long offset, string context) =>
        new(
            Finite(_reader.ReadSingle(offset), context),
            Finite(_reader.ReadSingle(offset + 4), context),
            Finite(_reader.ReadSingle(offset + 8), context));

    private Quaternion QuaternionAt(long offset, string context)
    {
        var x = Finite(_reader.ReadSingle(offset), context);
        var y = Finite(_reader.ReadSingle(offset + 4), context);
        var z = Finite(_reader.ReadSingle(offset + 8), context);
        var w = Finite(_reader.ReadSingle(offset + 12), context);
        return new Quaternion(x, y, z, w);
    }

    private long CheckedSection(long offset, int size, string context)
    {
        ValidateModelAbsoluteRange(offset, size, context);
        return offset + size;
    }

    private long ModelOffset(uint pointer)
    {
        var offset = CheckedAdd(_modelBase, pointer, "MDL model pointer");
        if (offset > _modelDataEnd)
            throw new NwnFormatException($"MDL model pointer 0x{pointer:X8} lies outside model data.");
        return offset;
    }

    private long MdxOffset(int pointer, long size, string context)
    {
        if (pointer < 0)
            throw new NwnFormatException($"{context} has a negative MDX pointer.");
        var offset = CheckedAdd(_mdxBase, (uint)pointer, context);
        if (offset > _mdxEnd || size < 0 || size > _mdxEnd - offset)
            throw new NwnFormatException($"{context} lies outside MDX data.");
        return offset;
    }

    private void ValidateModelRange(uint pointer, long size, string context)
    {
        if (size == 0 && pointer == 0)
            return;
        var offset = ModelOffset(pointer);
        ValidateModelAbsoluteRange(offset, size, context);
    }

    private void ValidateModelAbsoluteRange(long offset, long size, string context)
    {
        if (offset < _modelBase || size < 0 || offset > _modelDataEnd || size > _modelDataEnd - offset)
            throw new NwnFormatException($"{context} lies outside MDL model data.");
        _reader.ValidateRange(offset, size, context);
    }

    private string FixedString(long offset, int length, string context)
    {
        var value = _reader.ReadAscii(offset, length, context);
        var terminator = value.IndexOf('\0');
        return (terminator >= 0 ? value[..terminator] : value).TrimEnd();
    }

    private static int CheckedCount(uint value, int maximum, string context)
    {
        if (value > maximum)
            throw new NwnFormatException($"{context} count {value} exceeds {maximum}.");
        return checked((int)value);
    }

    private static void ValidateNodeFlags(uint content)
    {
        const uint light = 0x02;
        const uint emitter = 0x04;
        const uint camera = 0x08;
        const uint reference = 0x10;
        const uint mesh = 0x20;
        const uint meshSubtypes = 0x40 | 0x80 | 0x100 | 0x200;

        var primaryTypes = content & (light | emitter | camera | reference | mesh);
        if (BitOperations.PopCount(primaryTypes) > 1)
            throw new NwnFormatException($"MDL node flags 0x{content:X8} combine incompatible node types.");

        var subtypes = content & meshSubtypes;
        if (subtypes != 0 && (content & mesh) == 0)
            throw new NwnFormatException($"MDL node flags 0x{content:X8} declare a mesh subtype without mesh data.");
        if (BitOperations.PopCount(subtypes) > 1)
            throw new NwnFormatException($"MDL node flags 0x{content:X8} combine incompatible mesh subtypes.");
    }

    private static long CheckedAdd(long left, uint right, string context)
    {
        try
        {
            return checked(left + right);
        }
        catch (OverflowException ex)
        {
            throw new NwnFormatException($"{context} overflows.", ex);
        }
    }

    private static float Finite(float value, string context)
    {
        if (!float.IsFinite(value))
            throw new NwnFormatException($"{context} contains a non-finite value.");
        return value;
    }

    private static int PositiveGrid(uint value, string context)
    {
        if (value > int.MaxValue)
            throw new NwnFormatException($"{context} value {value} is too large.");
        return value == 0 ? 1 : (int)value;
    }

    private static void ValidateFloatWindow(int start, int count, int available, string context)
    {
        if (start < 0 || count < 0 || start > available || count > available - start)
            throw new NwnFormatException($"{context} range [{start}, {start + Math.Max(count, 0)}) exceeds {available} floats.");
    }
}
