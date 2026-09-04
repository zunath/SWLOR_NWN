// SPDX-License-Identifier: MIT

using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Mdl;

namespace SWLOR.NWN.Formats.Tests;

public sealed class MdlReaderTests
{
    [Test]
    public void ReadsBinaryMeshAndMdxStreams()
    {
        const int modelDataSize = 888;
        var bytes = new byte[12 + modelDataSize + 96];
        WriteUInt32(bytes, 4, modelDataSize);
        WriteUInt32(bytes, 8, 96);
        WriteFixed(bytes, 20, 64, "sample");
        WriteUInt32(bytes, 84, 232);
        WriteFixed(bytes, 180, 64, "base_model");

        var node = 12 + 232;
        WriteFixed(bytes, node + 32, 32, "panel");
        WriteUInt32(bytes, node + 108, 0x20);

        var mesh = node + 112;
        WriteUInt32(bytes, mesh + 8, 856);
        WriteUInt32(bytes, mesh + 12, 1);
        WriteUInt32(bytes, mesh + 16, 1);
        WriteUInt32(bytes, mesh + 108, 1);
        WriteFixed(bytes, mesh + 120, 64, "panel_texture");
        WriteFixed(bytes, mesh + 312, 64, "panel_material");
        WriteUInt32(bytes, mesh + 376, 0x8000_0007);
        WriteUInt32(bytes, mesh + 440, 32);
        WriteInt32(bytes, mesh + 444, 0);
        WriteUInt16(bytes, mesh + 448, 3);
        WriteUInt16(bytes, mesh + 450, 1);
        WriteInt32(bytes, mesh + 452, 12);
        WriteInt32(bytes, mesh + 456, -1);
        WriteInt32(bytes, mesh + 460, -1);
        WriteInt32(bytes, mesh + 464, -1);
        WriteInt32(bytes, mesh + 468, 20);
        WriteInt32(bytes, mesh + 472, -1);
        for (var offset = 476; offset <= 496; offset += 4)
            WriteInt32(bytes, mesh + offset, -1);

        var face = 12 + 856;
        WriteSingle(bytes, face + 8, 1f);
        WriteUInt16(bytes, face + 26, 0);
        WriteUInt16(bytes, face + 28, 1);
        WriteUInt16(bytes, face + 30, 2);

        var mdx = 12 + modelDataSize;
        var positions = new[] { Vector3.Zero, Vector3.UnitX, Vector3.UnitY };
        var textureCoordinates = new[] { Vector2.Zero, Vector2.UnitX, Vector2.UnitY };
        for (var index = 0; index < positions.Length; index++)
        {
            var vertex = mdx + index * 32;
            WriteVector3(bytes, vertex, positions[index]);
            WriteVector2(bytes, vertex + 12, textureCoordinates[index]);
            WriteVector3(bytes, vertex + 20, Vector3.UnitZ);
        }

        var model = new MdlReader().Parse(bytes);

        model.Name.Should().Be("sample");
        model.SuperModel.Should().Be("base_model");
        var parsed = model.GetMeshNodes().Should().ContainSingle().Subject;
        parsed.Name.Should().Be("panel");
        parsed.Render.Should().BeTrue();
        parsed.TileFade.Should().Be(unchecked((int)0x8000_0007));
        parsed.Bitmap.Should().Be("panel_texture");
        parsed.MaterialName.Should().Be("panel_material");
        parsed.Vertices.Should().Equal(Vector3.Zero, Vector3.UnitX, Vector3.UnitY);
        parsed.TextureCoordinates.Should().Equal(Vector2.Zero, Vector2.UnitX, Vector2.UnitY);
        parsed.Normals.Should().OnlyContain(normal => normal == Vector3.UnitZ);
        parsed.Faces.Should().ContainSingle()
            .Which.Should().Match<MdlFace>(item =>
                item.VertexIndex0 == 0 && item.VertexIndex1 == 1 && item.VertexIndex2 == 2);
    }

    [Test]
    public void ReadsBinarySkinAttributesFromInterleavedMdxRecords()
    {
        const int modelDataSize = 956;
        const int mdxStride = 48;
        var bytes = new byte[12 + modelDataSize + mdxStride * 2];
        WriteUInt32(bytes, 4, modelDataSize);
        WriteUInt32(bytes, 8, mdxStride * 2);
        WriteUInt32(bytes, 12 + 72, 232);

        var node = 12 + 232;
        WriteFixed(bytes, node + 32, 32, "skinned");
        WriteUInt32(bytes, node + 108, 0x60);

        var mesh = node + 112;
        WriteUInt32(bytes, mesh + 440, mdxStride);
        WriteInt32(bytes, mesh + 444, 0);
        WriteUInt16(bytes, mesh + 448, 2);
        WriteInt32(bytes, mesh + 452, -1);
        WriteInt32(bytes, mesh + 468, 12);

        var skin = mesh + 512;
        WriteInt32(bytes, skin + 12, 24);
        WriteInt32(bytes, skin + 16, 40);

        var mdx = 12 + modelDataSize;
        for (var index = 0; index < 2; index++)
        {
            var vertex = mdx + index * mdxStride;
            WriteVector3(bytes, vertex, new Vector3(index + 1, index + 2, index + 3));
            WriteVector3(bytes, vertex + 12, Vector3.UnitZ);
        }
        WriteVector4(bytes, mdx + 24, new Vector4(1f, 0f, 0f, 0f));
        WriteInt16(bytes, mdx + 40, 3);
        WriteInt16(bytes, mdx + 42, 4);
        WriteInt16(bytes, mdx + 44, 5);
        WriteInt16(bytes, mdx + 46, 6);
        WriteVector4(bytes, mdx + mdxStride + 24, new Vector4(.25f, .75f, 0f, 0f));
        WriteInt16(bytes, mdx + mdxStride + 40, 7);
        WriteInt16(bytes, mdx + mdxStride + 42, 8);
        WriteInt16(bytes, mdx + mdxStride + 44, 9);
        WriteInt16(bytes, mdx + mdxStride + 46, 10);

        var model = new MdlReader().Parse(bytes);

        var parsed = model.GetMeshNodes().Should().ContainSingle().Subject
            .Should().BeOfType<MdlSkinmeshNode>().Subject;
        parsed.Vertices.Should().Equal(new Vector3(1, 2, 3), new Vector3(2, 3, 4));
        parsed.Normals.Should().OnlyContain(normal => normal == Vector3.UnitZ);
        parsed.BoneWeights.Should().Equal(
            new Vector4(1f, 0f, 0f, 0f),
            new Vector4(.25f, .75f, 0f, 0f));
        parsed.BoneIndices.Should().Equal(
            new MdlBoneIndices(3, 4, 5, 6),
            new MdlBoneIndices(7, 8, 9, 10));
    }

    [Test]
    public void RejectsBinaryMdxStrideSmallerThanAnAttribute()
    {
        const int modelDataSize = 856;
        var bytes = new byte[12 + modelDataSize + 12];
        WriteUInt32(bytes, 4, modelDataSize);
        WriteUInt32(bytes, 8, 12);
        WriteUInt32(bytes, 12 + 72, 232);

        var node = 12 + 232;
        WriteUInt32(bytes, node + 108, 0x20);
        var mesh = node + 112;
        WriteUInt32(bytes, mesh + 440, 8);
        WriteInt32(bytes, mesh + 444, 0);
        WriteUInt16(bytes, mesh + 448, 1);
        WriteInt32(bytes, mesh + 452, -1);
        WriteInt32(bytes, mesh + 468, -1);

        Action action = () => new MdlReader().Parse(bytes);

        action.Should().Throw<NwnFormatException>()
            .WithMessage("*element size 12 exceeds MDX vertex stride 8*");
    }

    [Test]
    public void ReadsAnimationGeometryAndControllerTracks()
    {
        const int modelDataSize = 632;
        var bytes = new byte[12 + modelDataSize];
        WriteUInt32(bytes, 4, modelDataSize);
        WriteUInt32(bytes, 12 + 120, 232);
        WriteUInt32(bytes, 12 + 124, 1);
        WriteUInt32(bytes, 12 + 128, 1);
        WriteUInt32(bytes, 12 + 232, 236);

        var animation = 12 + 236;
        WriteFixed(bytes, animation + 8, 64, "open");
        WriteUInt32(bytes, animation + 72, 432);
        WriteSingle(bytes, animation + 112, 1f);
        WriteSingle(bytes, animation + 116, 0.25f);

        var node = 12 + 432;
        WriteFixed(bytes, node + 32, 32, "door");
        WriteUInt32(bytes, node + 84, 544);
        WriteUInt32(bytes, node + 88, 2);
        WriteUInt32(bytes, node + 92, 2);
        WriteUInt32(bytes, node + 96, 568);
        WriteUInt32(bytes, node + 100, 13);
        WriteUInt32(bytes, node + 104, 13);

        var controller = 12 + 544;
        WriteUInt32(bytes, controller, 8);
        WriteUInt16(bytes, controller + 4, 2);
        WriteUInt16(bytes, controller + 6, 0);
        WriteUInt16(bytes, controller + 8, 2);
        bytes[controller + 10] = 3;
        WriteUInt32(bytes, controller + 12, 20);
        WriteUInt16(bytes, controller + 16, 1);
        WriteUInt16(bytes, controller + 18, 8);
        WriteUInt16(bytes, controller + 20, 9);
        bytes[controller + 22] = 4;

        var data = 12 + 568;
        WriteSingle(bytes, data, 0f);
        WriteSingle(bytes, data + 4, 1f);
        WriteVector3(bytes, data + 8, Vector3.Zero);
        WriteVector3(bytes, data + 20, new Vector3(4f, 5f, 6f));
        WriteSingle(bytes, data + 32, 0.5f);
        WriteSingle(bytes, data + 36, 0f);
        WriteSingle(bytes, data + 40, 0f);
        WriteSingle(bytes, data + 44, MathF.Sqrt(0.5f));
        WriteSingle(bytes, data + 48, MathF.Sqrt(0.5f));

        var model = new MdlReader().Parse(bytes);

        var parsed = model.Animations.Should().ContainSingle().Subject;
        parsed.Name.Should().Be("open");
        parsed.Length.Should().Be(1f);
        parsed.TransitionTime.Should().Be(0.25f);
        parsed.GeometryRoot.Should().NotBeNull();
        parsed.GeometryRoot!.Name.Should().Be("door");
        parsed.GeometryRoot.PositionTimes.Should().Equal(0f, 1f);
        parsed.GeometryRoot.PositionValues.Should().Equal(Vector3.Zero, new Vector3(4f, 5f, 6f));
        parsed.GeometryRoot.OrientationTimes.Should().Equal(0.5f);
        parsed.GeometryRoot.OrientationValues.Should().Equal(
            new Quaternion(0f, 0f, MathF.Sqrt(0.5f), MathF.Sqrt(0.5f)));
    }

    [Test]
    public void RejectsTextAndOutOfBoundsPointers()
    {
        var text = Encoding.ASCII.GetBytes("not an mdl");
        Action parseText = () => new MdlReader().Parse(text);
        parseText.Should().Throw<NwnFormatException>();

        var binary = new byte[12 + 232];
        WriteUInt32(binary, 4, 232);
        WriteUInt32(binary, 12 + 72, 0xFFFF_FFF0);
        Action parsePointer = () => new MdlReader().Parse(binary);
        parsePointer.Should().Throw<NwnFormatException>();
    }

    /// <summary>
    /// Real corpus files end truncated — sw_t_cepdesert/ztd01_o64_01.mdl stops mid-node with no
    /// endnode or block terminator — and every prior reader tolerated them, so a missing
    /// terminator after readable nodes must finalize what was read instead of throwing.
    /// </summary>
    [Test]
    public void ToleratesAsciiGeometryMissingItsTerminatorAfterACompleteNode()
    {
        var text = """
                   newmodel sample
                   beginmodelgeom sample
                     node dummy sample
                       parent NULL
                     endnode
                   """;

        var model = new MdlReader().Parse(Encoding.ASCII.GetBytes(text));

        model.GeometryRoot.Should().NotBeNull();
        model.GeometryRoot!.Name.Should().Be("sample");
    }

    [Test]
    public void ToleratesAsciiNodeCutOffAtEndOfFile()
    {
        var text = """
                   newmodel sample
                   beginmodelgeom sample
                     node dummy sample
                       parent NULL
                   """;

        var model = new MdlReader().Parse(Encoding.ASCII.GetBytes(text));

        model.GeometryRoot.Should().NotBeNull();
        model.GeometryRoot!.Name.Should().Be("sample");
    }

    [Test]
    public void BinaryModelHeaderMustFitInsideDeclaredModelData()
    {
        var bytes = new byte[12 + 232];
        WriteUInt32(bytes, 4, 200);
        WriteUInt32(bytes, 8, 32);

        Action action = () => new MdlReader().Parse(bytes);

        action.Should().Throw<NwnFormatException>()
            .WithMessage("*model header*model data*");
    }

    [Test]
    public void IncompatibleBinaryNodeFlagsAreRejectedAsFormatErrors()
    {
        var bytes = BuildBinaryNode(0x04 | 0x20);

        Action action = () => new MdlReader().Parse(bytes);

        action.Should().Throw<NwnFormatException>()
            .WithMessage("*incompatible node types*");
    }

    [Test]
    public void SharedFaceTablesCannotMultiplyManagedAllocationBeyondTheParseBudget()
    {
        var bytes = BuildBinaryMdlWithSharedFaceTable(meshCount: 5, faceCount: ushort.MaxValue);

        Action action = () => new MdlReader().Parse(bytes);

        action.Should().Throw<NwnFormatException>()
            .WithMessage("*allocation budget*");
    }

    [Test]
    public void ReadsAsciiMeshAndReindexesTextureVertices()
    {
        var text = """
                   #MAXMODEL ASCII
                   newmodel sample
                   setsupermodel sample NULL
                   setanimationscale 1
                   beginmodelgeom sample
                     node dummy sample
                       parent NULL
                     endnode
                     node trimesh panel
                       parent sample
                       position 1 2 3
                       orientation 0 0 1 1.57079632679
                       render 1
                       tilefade 3
                       bitmap panel_texture
                       materialname panel_material
                       verts 3
                         0 0 0
                         1 0 0
                         0 1 0
                       normals 3
                         0 0 1
                         0 0 1
                         0 0 1
                       tverts 4
                         0 0 0
                         1 0 0
                         0 1 0
                         1 1 0
                       faces 2
                         0 1 2 55 0 1 2 7
                         0 2 1 66 3 2 1 8
                     endnode
                   endmodelgeom sample
                   donemodel sample
                   """;

        var model = new MdlReader().Parse(Encoding.ASCII.GetBytes(text));

        model.Name.Should().Be("sample");
        model.SuperModel.Should().BeEmpty();
        var mesh = model.GetMeshNodes().Should().ContainSingle().Subject;
        mesh.Name.Should().Be("panel");
        mesh.Render.Should().BeTrue();
        mesh.TileFade.Should().Be(3);
        mesh.Bitmap.Should().Be("panel_texture");
        mesh.MaterialName.Should().Be("panel_material");
        mesh.Position.Should().Be(new Vector3(1, 2, 3));
        mesh.Orientation.Z.Should().BeApproximately(MathF.Sqrt(0.5f), 0.0001f);
        mesh.Orientation.W.Should().BeApproximately(MathF.Sqrt(0.5f), 0.0001f);
        mesh.Vertices.Should().HaveCount(4);
        mesh.TextureCoordinates.Should().HaveCount(4);
        mesh.Faces.Should().HaveCount(2);
        // Face column layout is "v1 v2 v3 smoothgroup tv1 tv2 tv3 material" - SurfaceId must come
        // from the material column (index 7: 7/8), not the smoothing-group column (index 3: 55/66).
        mesh.Faces[0].SurfaceId.Should().Be(7);
        mesh.Faces[1].SurfaceId.Should().Be(8);
        mesh.Faces[1].VertexIndex0.Should().NotBe(mesh.Faces[0].VertexIndex0);
    }

    [Test]
    public void AsciiMeshGeneratesNormalsAcrossOverlappingSmoothingGroups()
    {
        var text = """
                   newmodel sample
                   beginmodelgeom sample
                     node dummy sample
                       parent NULL
                     endnode
                     node trimesh panel
                       parent sample
                       render 1
                       verts 4
                         0 0 0
                         1 0 0
                         0 1 0
                         0 0 1
                       tverts 4
                         0 0 0
                         1 0 0
                         0 1 0
                         1 1 0
                       faces 3
                         0 1 2 1 0 1 2 0
                         0 1 3 3 0 1 3 0
                         0 2 3 2 0 2 3 0
                     endnode
                   endmodelgeom sample
                   donemodel sample
                   """;

        var mesh = new MdlReader()
            .Parse(Encoding.ASCII.GetBytes(text))
            .GetMeshNodes()
            .Should().ContainSingle().Subject;

        mesh.Vertices.Should().HaveCount(5,
            "Aurora splits vertices at hard smoothing-group boundaries");
        mesh.Normals.Should().HaveCount(mesh.Vertices.Length);
        mesh.Faces[0].VertexIndex0.Should().Be(mesh.Faces[1].VertexIndex0,
            "faces with overlapping smoothing-group masks share the generated corner normal");
        mesh.Faces[2].VertexIndex0.Should().Be(mesh.Faces[0].VertexIndex0,
            "overlap through mask 3 connects masks 1 and 2 into one smoothing component");

        var smoothed = mesh.Normals[mesh.Faces[0].VertexIndex0];
        var componentValue = 1f / MathF.Sqrt(3f);
        smoothed.X.Should().BeApproximately(componentValue, 0.0001f);
        smoothed.Y.Should().BeApproximately(-componentValue, 0.0001f);
        smoothed.Z.Should().BeApproximately(componentValue, 0.0001f);

        mesh.Faces[0].VertexIndex2.Should().NotBe(mesh.Faces[2].VertexIndex1,
            "masks 1 and 2 remain a hard boundary where no bridging face shares that vertex");
        mesh.Normals[mesh.Faces[0].VertexIndex2].Should().Be(Vector3.UnitZ);
        mesh.Normals[mesh.Faces[2].VertexIndex1].Should().Be(Vector3.UnitX);

        var normalizedText = text.ReplaceLineEndings("\n");
        var reorderedText = normalizedText.Replace(
                "0 1 2 1 0 1 2 0\n      0 1 3 3 0 1 3 0\n      0 2 3 2 0 2 3 0",
                "0 2 3 2 0 2 3 0\n      0 1 3 3 0 1 3 0\n      0 1 2 1 0 1 2 0",
                StringComparison.Ordinal);
        reorderedText.Should().NotBe(normalizedText, "the fixture must actually reorder the face rows");
        var reordered = new MdlReader()
            .Parse(Encoding.ASCII.GetBytes(reorderedText))
            .GetMeshNodes()
            .Should().ContainSingle().Subject;
        reordered.Vertices.Should().HaveCount(mesh.Vertices.Length);
        var reorderedShared = reordered.Normals[reordered.Faces[0].VertexIndex0];
        reordered.Faces.Should().OnlyContain(face =>
            reordered.Normals[face.VertexIndex0] == reorderedShared);
        reorderedShared.Should().Be(smoothed,
            "reordering masks 1, 3, 2 must not change their shared normal");
    }

    [Test]
    public void AsciiMeshFallsBackToTexture0WhenBitmapIsAbsent()
    {
        var text = """
                   newmodel sample
                   beginmodelgeom sample
                     node dummy sample
                       parent NULL
                     endnode
                     node trimesh panel
                       parent sample
                       texture0 foo
                       verts 3
                         0 0 0
                         1 0 0
                         0 1 0
                       faces 1
                         0 1 2 0 0 0 0 1
                     endnode
                   endmodelgeom sample
                   donemodel sample
                   """;

        var model = new MdlReader().Parse(Encoding.ASCII.GetBytes(text));

        var mesh = model.GetMeshNodes().Should().ContainSingle().Subject;
        mesh.Bitmap.Should().Be("foo");
    }

    [Test]
    public void AsciiMeshBitmapKeywordTakesPrecedenceOverTexture0RegardlessOfOrder()
    {
        var text = """
                   newmodel sample
                   beginmodelgeom sample
                     node dummy sample
                       parent NULL
                     endnode
                     node trimesh panel
                       parent sample
                       texture0 fallback_texture
                       bitmap real_bitmap
                       verts 3
                         0 0 0
                         1 0 0
                         0 1 0
                       faces 1
                         0 1 2 0 0 0 0 1
                     endnode
                   endmodelgeom sample
                   donemodel sample
                   """;

        var model = new MdlReader().Parse(Encoding.ASCII.GetBytes(text));

        var mesh = model.GetMeshNodes().Should().ContainSingle().Subject;
        mesh.Bitmap.Should().Be("real_bitmap");
    }

    [Test]
    public void ReadsAsciiSkinEmitterAndAnimationControllers()
    {
        var text = """
                   newmodel sample
                   beginmodelgeom sample
                     node dummy sample
                       parent NULL
                     endnode
                     node skin cloth
                       parent sample
                       render 1
                       bitmap cloth
                       verts 3
                         0 0 0
                         1 0 0
                         0 1 0
                       tverts 3
                         0 0 0
                         1 0 0
                         0 1 0
                       faces 1
                         0 1 2 0 0 1 2 1
                       weights 3
                         sample 1
                         sample .5 arm .5
                         arm 1
                     endnode
                     node emitter sparks
                       parent sample
                       texture fx_spark
                       xgrid 4
                       ygrid 2
                       update Explosion
                       render Normal
                       blend Lighten
                       loop 1
                       twosidedtex 1
                     endnode
                   endmodelgeom sample
                   newanim open sample
                     length 1
                     transtime .25
                     node dummy sample
                       parent NULL
                       positionkey 2
                         0 0 0 0
                         1 4 5 6
                       orientationkey
                         0 0 0 0 0
                         1 0 0 1 3.14159265
                       endlist
                       scalekey 1
                         0 1
                     endnode
                   doneanim open sample
                   donemodel sample
                   """;

        var model = new MdlReader().Parse(Encoding.ASCII.GetBytes(text));

        var nodes = Enumerate(model.GeometryRoot!).ToArray();
        var skin = nodes.OfType<MdlSkinmeshNode>().Should().ContainSingle().Subject;
        skin.VertexInfluences.Should().HaveCount(3);
        skin.VertexInfluences[1].Should().Equal(
            new MdlSkinInfluence("sample", .5f),
            new MdlSkinInfluence("arm", .5f));
        var emitter = nodes.OfType<MdlEmitterNode>().Should().ContainSingle().Subject;
        emitter.Texture.Should().Be("fx_spark");
        emitter.XGrid.Should().Be(4);
        emitter.YGrid.Should().Be(2);
        emitter.Loop.Should().BeTrue();
        emitter.TextureIsTwoSided.Should().BeTrue();

        var animation = model.Animations.Should().ContainSingle().Subject;
        animation.Name.Should().Be("open");
        animation.GeometryRoot!.PositionTimes.Should().Equal(0f, 1f);
        animation.GeometryRoot.PositionValues[1].Should().Be(new Vector3(4, 5, 6));
        animation.GeometryRoot.OrientationValues.Should().HaveCount(2);
        animation.GeometryRoot.ScaleValues.Should().Equal(1f);
    }

    [Test]
    public void ReadsLegacyConcatenatedDirectivesAndTwoDimensionalVertices()
    {
        var text = """
                   newmodelsample
                   setsupermodelsample NULL
                   beginmodelgeomsample
                   node dummysample
                     parent NULL
                   endnode
                   node trimesh panel
                     parentsample
                     bitmap ##0x7f6bfe28!body
                     verts 3
                       0 0
                       1 0
                       0 1
                     faces 1
                       0 1 2 0 0 0 0 1
                   endnode
                   donemodelsample
                   """;

        var model = new MdlReader().Parse(Encoding.ASCII.GetBytes(text));

        model.Name.Should().Be("sample");
        var mesh = model.GetMeshNodes().Should().ContainSingle().Subject;
        mesh.Bitmap.Should().Be("##0x7f6bfe28!body");
        mesh.Vertices.Should().Equal(Vector3.Zero, Vector3.UnitX, Vector3.UnitY);
    }

    private static IEnumerable<MdlNode> Enumerate(MdlNode root)
    {
        var pending = new Stack<MdlNode>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            yield return current;
            foreach (var child in current.Children)
                pending.Push(child);
        }
    }

    private static byte[] BuildBinaryNode(uint content)
    {
        const int modelHeaderSize = 232;
        const int nodeHeaderSize = 112;
        const int modelDataSize = modelHeaderSize + nodeHeaderSize;
        var bytes = new byte[12 + modelDataSize];
        WriteUInt32(bytes, 4, modelDataSize);
        WriteUInt32(bytes, 12 + 72, modelHeaderSize);
        WriteUInt32(bytes, 12 + modelHeaderSize + 108, content);
        return bytes;
    }

    private static byte[] BuildBinaryMdlWithSharedFaceTable(int meshCount, int faceCount)
    {
        const int modelHeaderSize = 232;
        const int nodeHeaderSize = 112;
        const int meshHeaderSize = 512;
        var rootPointer = modelHeaderSize;
        var childPointers = rootPointer + nodeHeaderSize;
        var meshNodes = checked(childPointers + meshCount * 4);
        var facePointer = checked(meshNodes + meshCount * (nodeHeaderSize + meshHeaderSize));
        var modelDataSize = checked(facePointer + faceCount * 32);
        var bytes = new byte[checked(12 + modelDataSize)];

        WriteUInt32(bytes, 4, checked((uint)modelDataSize));
        WriteUInt32(bytes, 12 + 72, checked((uint)rootPointer));
        var root = 12 + rootPointer;
        WriteUInt32(bytes, root + 72, checked((uint)childPointers));
        WriteUInt32(bytes, root + 76, checked((uint)meshCount));

        for (var index = 0; index < meshCount; index++)
        {
            var pointer = checked(meshNodes + index * (nodeHeaderSize + meshHeaderSize));
            WriteUInt32(bytes, 12 + childPointers + index * 4, checked((uint)pointer));
            var node = 12 + pointer;
            WriteUInt32(bytes, node + 108, 0x20);
            var mesh = node + nodeHeaderSize;
            WriteUInt32(bytes, mesh + 8, checked((uint)facePointer));
            WriteUInt32(bytes, mesh + 12, checked((uint)faceCount));
        }

        return bytes;
    }

    /// <summary>
    /// A collision node has to be distinguishable from artwork. ASCII never writes a
    /// <c>render</c> line for one, so it arrives at the default of true and carries no bitmap -
    /// which is exactly the shape of a mesh a renderer draws untextured.
    /// </summary>
    [TestCase("aabb")]
    [TestCase("pwk")]
    [TestCase("dwk")]
    public void AsciiCollisionNodesAreFlaggedAsWalkmesh(string nodeType)
    {
        var text = $"""
                    #MAXMODEL ASCII
                    newmodel sample
                    beginmodelgeom sample
                      node dummy sample
                        parent NULL
                      endnode
                      node {nodeType} walkmesh
                        parent sample
                        verts 3
                          0 0 0
                          1 0 0
                          0 1 0
                        faces 1
                          0 1 2 1 0 1 2 1
                      endnode
                      node trimesh ground
                        parent sample
                        render 1
                        bitmap ground_texture
                        verts 3
                          0 0 0
                          1 0 0
                          0 1 0
                        faces 1
                          0 1 2 1 0 1 2 1
                      endnode
                    endmodelgeom sample
                    donemodel sample
                    """;

        var model = new MdlReader().Parse(Encoding.ASCII.GetBytes(text));
        var meshes = model.GetMeshNodes().ToDictionary(node => node.Name, StringComparer.OrdinalIgnoreCase);

        meshes["walkmesh"].IsWalkmesh.Should().BeTrue($"'node {nodeType}' is collision, not artwork");
        meshes["walkmesh"].Render.Should().BeTrue("ASCII writes no render line for a collision node");
        meshes["ground"].IsWalkmesh.Should().BeFalse();
    }

    /// <summary>The binary path carries the same information in the node's AABB payload flag.</summary>
    [Test]
    public void BinaryAabbNodesAreFlaggedAsWalkmesh()
    {
        const int modelDataSize = 888;
        var bytes = new byte[12 + modelDataSize + 96];
        WriteUInt32(bytes, 4, modelDataSize);
        WriteUInt32(bytes, 8, 96);
        WriteFixed(bytes, 20, 64, "sample");
        WriteUInt32(bytes, 84, 232);

        var node = 12 + 232;
        WriteFixed(bytes, node + 32, 32, "walkmesh");
        WriteUInt32(bytes, node + 108, 0x20 | 0x200); // mesh payload + AABB payload

        var mesh = node + 112;
        WriteUInt32(bytes, mesh + 8, 856);
        WriteUInt32(bytes, mesh + 12, 1);
        WriteUInt32(bytes, mesh + 16, 1);
        WriteUInt32(bytes, mesh + 108, 1);
        WriteUInt32(bytes, mesh + 440, 32);
        WriteInt32(bytes, mesh + 444, 0);
        WriteUInt16(bytes, mesh + 448, 3);
        WriteUInt16(bytes, mesh + 450, 1);
        WriteInt32(bytes, mesh + 452, 12);
        for (var offset = 456; offset <= 496; offset += 4)
            WriteInt32(bytes, mesh + offset, -1);

        var face = 12 + 856;
        WriteSingle(bytes, face + 8, 1f);
        WriteUInt16(bytes, face + 26, 0);
        WriteUInt16(bytes, face + 28, 1);
        WriteUInt16(bytes, face + 30, 2);

        var model = new MdlReader().Parse(bytes);
        var walkmesh = model.GetMeshNodes().Single();

        walkmesh.Name.Should().Be("walkmesh");
        walkmesh.IsWalkmesh.Should().BeTrue("the node declares the AABB payload");
    }

    private static void WriteFixed(byte[] bytes, int offset, int length, string value)
    {
        var encoded = Encoding.ASCII.GetBytes(value);
        encoded.AsSpan(0, Math.Min(encoded.Length, length)).CopyTo(bytes.AsSpan(offset, length));
    }

    private static void WriteUInt16(byte[] bytes, int offset, ushort value) =>
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset, 2), value);

    private static void WriteInt16(byte[] bytes, int offset, short value) =>
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(offset, 2), value);

    private static void WriteUInt32(byte[] bytes, int offset, uint value) =>
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset, 4), value);

    private static void WriteInt32(byte[] bytes, int offset, int value) =>
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset, 4), value);

    private static void WriteSingle(byte[] bytes, int offset, float value) =>
        WriteInt32(bytes, offset, BitConverter.SingleToInt32Bits(value));

    private static void WriteVector2(byte[] bytes, int offset, Vector2 value)
    {
        WriteSingle(bytes, offset, value.X);
        WriteSingle(bytes, offset + 4, value.Y);
    }

    private static void WriteVector3(byte[] bytes, int offset, Vector3 value)
    {
        WriteSingle(bytes, offset, value.X);
        WriteSingle(bytes, offset + 4, value.Y);
        WriteSingle(bytes, offset + 8, value.Z);
    }

    private static void WriteVector4(byte[] bytes, int offset, Vector4 value)
    {
        WriteSingle(bytes, offset, value.X);
        WriteSingle(bytes, offset + 4, value.Y);
        WriteSingle(bytes, offset + 8, value.Z);
        WriteSingle(bytes, offset + 12, value.W);
    }
}
