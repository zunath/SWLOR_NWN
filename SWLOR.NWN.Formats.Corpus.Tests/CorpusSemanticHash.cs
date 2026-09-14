// SPDX-License-Identifier: MIT

using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using SWLOR.NWN.Formats.Gff;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.NWN.Formats.TwoDA;

namespace SWLOR.NWN.Formats.Corpus.Tests;

internal sealed class CorpusHash : IDisposable
{
    private readonly IncrementalHash _hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    private bool _finished;

    public void AddByte(byte value) => _hash.AppendData([value]);

    public void AddBoolean(bool value) => AddByte(value ? (byte)1 : (byte)0);

    public void AddInt16(short value)
    {
        Span<byte> bytes = stackalloc byte[2];
        BinaryPrimitives.WriteInt16LittleEndian(bytes, value);
        _hash.AppendData(bytes);
    }

    public void AddUInt16(ushort value)
    {
        Span<byte> bytes = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(bytes, value);
        _hash.AppendData(bytes);
    }

    public void AddInt32(int value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        _hash.AppendData(bytes);
    }

    public void AddUInt32(uint value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        _hash.AppendData(bytes);
    }

    public void AddInt64(long value)
    {
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(bytes, value);
        _hash.AppendData(bytes);
    }

    public void AddUInt64(ulong value)
    {
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(bytes, value);
        _hash.AppendData(bytes);
    }

    public void AddSingle(float value) => AddInt32(BitConverter.SingleToInt32Bits(value));

    public void AddDouble(double value) => AddInt64(BitConverter.DoubleToInt64Bits(value));

    public void AddString(string? value)
    {
        if (value == null)
        {
            AddInt32(-1);
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(value);
        AddInt32(bytes.Length);
        _hash.AppendData(bytes);
    }

    public void AddBytes(ReadOnlySpan<byte> bytes)
    {
        AddInt32(bytes.Length);
        _hash.AppendData(bytes);
    }

    public void AddVector2(Vector2 value)
    {
        AddSingle(value.X);
        AddSingle(value.Y);
    }

    public void AddVector3(Vector3 value)
    {
        AddSingle(value.X);
        AddSingle(value.Y);
        AddSingle(value.Z);
    }

    public void AddVector4(Vector4 value)
    {
        AddSingle(value.X);
        AddSingle(value.Y);
        AddSingle(value.Z);
        AddSingle(value.W);
    }

    public void AddQuaternion(Quaternion value)
    {
        AddSingle(value.X);
        AddSingle(value.Y);
        AddSingle(value.Z);
        AddSingle(value.W);
    }

    public string Finish()
    {
        if (_finished)
            throw new InvalidOperationException("A corpus hash can only be finalized once.");
        _finished = true;
        return Convert.ToHexString(_hash.GetHashAndReset()).ToLowerInvariant();
    }

    public void Dispose() => _hash.Dispose();
}

internal static class CorpusSemanticHash
{
    public static void AddInput(CorpusHash hash, string identity, byte[] bytes)
    {
        hash.AddString(identity);
        hash.AddInt64(bytes.LongLength);
        hash.AddBytes(SHA256.HashData(bytes));
    }

    public static void AddTwoDa(CorpusHash hash, TwoDAFile table)
    {
        hash.AddString("2da-v1");
        hash.AddString(table.DefaultValue);
        hash.AddInt32(table.Columns.Count);
        foreach (var column in table.Columns)
            hash.AddString(column);
        hash.AddInt32(table.RowCount);
        for (var row = 0; row < table.RowCount; row++)
        {
            hash.AddString(table.RowLabels[row]);
            foreach (var column in table.Columns)
                hash.AddString(table.GetValue(row, column));
        }
    }

    public static void AddGff(CorpusHash hash, GffFile file)
    {
        hash.AddString("gff-v1");
        hash.AddString(file.FileType);
        hash.AddString(file.FileVersion);
        AddGffStruct(hash, file.RootStruct);
    }

    public static void AddModel(CorpusHash hash, MdlModel model)
    {
        hash.AddString("mdl-v2");
        hash.AddString(model.Name);
        hash.AddString(model.SuperModel);
        hash.AddByte(model.ModelType);
        hash.AddVector3(model.BoundsMinimum);
        hash.AddVector3(model.BoundsMaximum);
        hash.AddSingle(model.Radius);
        hash.AddSingle(model.Scale);
        AddOptionalNode(hash, model.GeometryRoot);
        hash.AddInt32(model.Animations.Count);
        foreach (var animation in model.Animations)
        {
            hash.AddString(animation.Name);
            hash.AddSingle(animation.Length);
            hash.AddSingle(animation.TransitionTime);
            AddOptionalNode(hash, animation.GeometryRoot);
        }
    }

    private static void AddGffStruct(CorpusHash hash, GffStruct value)
    {
        hash.AddUInt32(value.Type);
        hash.AddInt32(value.Fields.Count);
        foreach (var field in value.Fields)
        {
            hash.AddUInt32(field.Type);
            hash.AddString(field.Label);
            AddGffValue(hash, field.Value);
        }
    }

    private static void AddGffValue(CorpusHash hash, object? value)
    {
        switch (value)
        {
            case null:
                hash.AddByte(0);
                break;
            case byte item:
                hash.AddByte(1);
                hash.AddByte(item);
                break;
            case sbyte item:
                hash.AddByte(2);
                hash.AddByte(unchecked((byte)item));
                break;
            case ushort item:
                hash.AddByte(3);
                hash.AddUInt16(item);
                break;
            case short item:
                hash.AddByte(4);
                hash.AddInt16(item);
                break;
            case uint item:
                hash.AddByte(5);
                hash.AddUInt32(item);
                break;
            case int item:
                hash.AddByte(6);
                hash.AddInt32(item);
                break;
            case ulong item:
                hash.AddByte(7);
                hash.AddUInt64(item);
                break;
            case long item:
                hash.AddByte(8);
                hash.AddInt64(item);
                break;
            case float item:
                hash.AddByte(9);
                hash.AddSingle(item);
                break;
            case double item:
                hash.AddByte(10);
                hash.AddDouble(item);
                break;
            case string item:
                hash.AddByte(11);
                hash.AddString(item);
                break;
            case byte[] item:
                hash.AddByte(12);
                hash.AddBytes(item);
                break;
            case CExoLocString item:
                hash.AddByte(13);
                hash.AddUInt32(item.StrRef);
                hash.AddInt32(item.LocalizedStrings.Count);
                foreach (var pair in item.LocalizedStrings.OrderBy(pair => pair.Key))
                {
                    hash.AddUInt32(pair.Key);
                    hash.AddString(pair.Value);
                }
                break;
            case GffStruct item:
                hash.AddByte(14);
                AddGffStruct(hash, item);
                break;
            case GffList item:
                hash.AddByte(15);
                hash.AddInt32(item.Elements.Count);
                foreach (var element in item.Elements)
                    AddGffStruct(hash, element);
                break;
            default:
                throw new InvalidDataException(
                    $"Unsupported GFF semantic value type '{value.GetType().FullName}'.");
        }
    }

    private static void AddOptionalNode(CorpusHash hash, MdlNode? node)
    {
        hash.AddBoolean(node != null);
        if (node != null)
            AddNode(hash, node);
    }

    private static void AddNode(CorpusHash hash, MdlNode node)
    {
        hash.AddString(node.GetType().Name);
        hash.AddString(node.Name);
        hash.AddVector3(node.Position);
        hash.AddQuaternion(node.Orientation);
        hash.AddSingle(node.Scale);
        AddFloatArray(hash, node.PositionTimes);
        AddVector3Array(hash, node.PositionValues);
        AddFloatArray(hash, node.OrientationTimes);
        AddQuaternionArray(hash, node.OrientationValues);
        AddFloatArray(hash, node.ScaleTimes);
        AddFloatArray(hash, node.ScaleValues);

        if (node is MdlTrimeshNode mesh)
            AddMesh(hash, mesh);
        if (node is MdlSkinmeshNode skin)
            AddSkin(hash, skin);
        if (node is MdlEmitterNode emitter)
            AddEmitter(hash, emitter);

        hash.AddInt32(node.Children.Count);
        foreach (var child in node.Children)
            AddNode(hash, child);
    }

    private static void AddMesh(CorpusHash hash, MdlTrimeshNode mesh)
    {
        hash.AddBoolean(mesh.Render);
        hash.AddInt32(mesh.TileFade);
        hash.AddString(mesh.Bitmap);
        hash.AddString(mesh.Lightmap);
        AddVector3Array(hash, mesh.Vertices);
        AddVector3Array(hash, mesh.Normals);
        hash.AddInt32(mesh.TextureCoordinates.Length);
        foreach (var value in mesh.TextureCoordinates)
            hash.AddVector2(value);
        hash.AddInt32(mesh.Faces.Length);
        foreach (var face in mesh.Faces)
        {
            hash.AddVector3(face.Normal);
            hash.AddSingle(face.Distance);
            hash.AddInt32(face.SurfaceId);
            hash.AddUInt16(face.VertexIndex0);
            hash.AddUInt16(face.VertexIndex1);
            hash.AddUInt16(face.VertexIndex2);
        }
    }

    private static void AddSkin(CorpusHash hash, MdlSkinmeshNode skin)
    {
        hash.AddInt32(skin.VertexInfluences.Length);
        foreach (var influences in skin.VertexInfluences)
        {
            hash.AddInt32(influences.Length);
            foreach (var influence in influences)
            {
                hash.AddString(influence.BoneName);
                hash.AddSingle(influence.Weight);
            }
        }
        hash.AddInt32(skin.BoneWeights.Length);
        foreach (var value in skin.BoneWeights)
            hash.AddVector4(value);
        hash.AddInt32(skin.BoneIndices.Length);
        foreach (var value in skin.BoneIndices)
        {
            hash.AddInt16(value.Index0);
            hash.AddInt16(value.Index1);
            hash.AddInt16(value.Index2);
            hash.AddInt16(value.Index3);
        }
        hash.AddInt32(skin.BoneMapping.Length);
        foreach (var value in skin.BoneMapping)
            hash.AddInt16(value);
        AddQuaternionArray(hash, skin.BoneQuaternions);
        AddVector3Array(hash, skin.BoneTranslations);
    }

    private static void AddEmitter(CorpusHash hash, MdlEmitterNode emitter)
    {
        hash.AddSingle(emitter.DeadSpace);
        hash.AddSingle(emitter.BlastRadius);
        hash.AddSingle(emitter.BlastLength);
        hash.AddInt32(emitter.XGrid);
        hash.AddInt32(emitter.YGrid);
        hash.AddString(emitter.Update);
        hash.AddString(emitter.RenderMode);
        hash.AddString(emitter.Blend);
        hash.AddString(emitter.Texture);
        hash.AddString(emitter.Chunk);
        hash.AddBoolean(emitter.TextureIsTwoSided);
        hash.AddBoolean(emitter.Loop);
        hash.AddUInt16(emitter.RenderOrder);
    }

    private static void AddFloatArray(CorpusHash hash, IReadOnlyList<float> values)
    {
        hash.AddInt32(values.Count);
        foreach (var value in values)
            hash.AddSingle(value);
    }

    private static void AddVector3Array(CorpusHash hash, IReadOnlyList<Vector3> values)
    {
        hash.AddInt32(values.Count);
        foreach (var value in values)
            hash.AddVector3(value);
    }

    private static void AddQuaternionArray(CorpusHash hash, IReadOnlyList<Quaternion> values)
    {
        hash.AddInt32(values.Count);
        foreach (var value in values)
            hash.AddQuaternion(value);
    }
}
