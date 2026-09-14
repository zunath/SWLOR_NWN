// SPDX-License-Identifier: MIT

using SWLOR.NWN.Formats.Common;
using SWLOR.NWN.Formats.Internal;

namespace SWLOR.NWN.Formats.Gff;

/// <summary>
/// Reads BioWare Generic File Format V3.2 resources.
/// </summary>
public static class GffReader
{
    private const int HeaderSize = 56;
    private const int StructSize = 12;
    private const int FieldSize = 12;
    private const int LabelSize = 16;
    private const int MaximumStructs = 4_000_000;
    private const int MaximumFields = 16_000_000;
    private const int MaximumLabels = 1_000_000;
    private const int MaximumDepth = 128;
    private const int MaximumVariableData = 512 * 1024 * 1024;

    public static GffFile Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Read(File.ReadAllBytes(path));
    }

    public static GffFile Read(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        var state = new ReaderState(bytes);
        return state.Read();
    }

    private sealed class ReaderState
    {
        private readonly GuardedBinaryReader _reader;
        private readonly AllocationBudget _allocationBudget = new("GFF");
        private readonly HashSet<uint> _activeStructs = new();
        private readonly Dictionary<uint, GffStruct> _structCache = new();
        private readonly Dictionary<uint, long> _structRetainedBytes = new();
        private readonly Dictionary<uint, CachedField> _fieldCache = new();
        private string[] _labels = [];

        private uint _structOffset;
        private uint _structCount;
        private uint _fieldOffset;
        private uint _fieldCount;
        private uint _fieldDataOffset;
        private uint _fieldDataCount;
        private uint _fieldIndicesOffset;
        private uint _fieldIndicesCount;
        private uint _listIndicesOffset;
        private uint _listIndicesCount;

        public ReaderState(byte[] bytes)
        {
            _reader = new GuardedBinaryReader(bytes);
        }

        public GffFile Read()
        {
            _reader.ValidateRange(0, HeaderSize, "GFF header");
            var fileType = _reader.ReadAscii(0, 4, "GFF file type");
            var version = _reader.ReadAscii(4, 4, "GFF version");
            if (version != "V3.2")
                throw new NwnFormatException($"Unsupported GFF version '{version}'; expected V3.2.");

            _structOffset = _reader.ReadUInt32(8);
            _structCount = _reader.ReadUInt32(12);
            _fieldOffset = _reader.ReadUInt32(16);
            _fieldCount = _reader.ReadUInt32(20);
            var labelOffset = _reader.ReadUInt32(24);
            var labelCount = _reader.ReadUInt32(28);
            _fieldDataOffset = _reader.ReadUInt32(32);
            _fieldDataCount = _reader.ReadUInt32(36);
            _fieldIndicesOffset = _reader.ReadUInt32(40);
            _fieldIndicesCount = _reader.ReadUInt32(44);
            _listIndicesOffset = _reader.ReadUInt32(48);
            _listIndicesCount = _reader.ReadUInt32(52);

            if (_structCount == 0)
                throw new NwnFormatException("GFF contains no root structure.");
            ValidateSection(_structOffset, _structCount, StructSize, MaximumStructs, "GFF structs");
            ValidateSection(_fieldOffset, _fieldCount, FieldSize, MaximumFields, "GFF fields");
            ValidateSection(labelOffset, labelCount, LabelSize, MaximumLabels, "GFF labels");
            ValidateByteSection(_fieldDataOffset, _fieldDataCount, MaximumVariableData, "GFF field data");
            ValidateIndexedSection(_fieldIndicesOffset, _fieldIndicesCount, "GFF field indices");
            ValidateIndexedSection(_listIndicesOffset, _listIndicesCount, "GFF list indices");

            _allocationBudget.ReserveElements(_structCount, 256, "GFF structures");
            _allocationBudget.ReserveElements(labelCount, 64, "GFF labels");
            _labels = ReadLabels(labelOffset, labelCount);
            var root = ReadStruct(0, 0);
            if (root.Type != uint.MaxValue)
                throw new NwnFormatException("GFF root structure type must be 0xFFFFFFFF.");

            return new GffFile
            {
                FileType = fileType,
                FileVersion = version,
                RootStruct = root
            };
        }

        private string[] ReadLabels(uint offset, uint count)
        {
            var interned = new Dictionary<string, string>(StringComparer.Ordinal);
            var labels = new string[checked((int)count)];
            for (var index = 0; index < count; index++)
            {
                var label = _reader.ReadAscii(offset + (long)index * LabelSize, LabelSize, "GFF label", trimNull: true);
                if (!interned.TryGetValue(label, out var shared))
                {
                    shared = label;
                    interned.Add(label, shared);
                }
                labels[index] = shared;
            }
            return labels;
        }

        private GffStruct ReadStruct(uint index, int depth)
        {
            if (depth > MaximumDepth)
                throw new NwnFormatException($"GFF nesting exceeds {MaximumDepth} levels.");
            if (index >= _structCount)
                throw new NwnFormatException($"GFF references missing struct index {index}.");
            if (_structCache.TryGetValue(index, out var cached))
            {
                // A repeat reference reuses the parsed struct, but consumers (the JSON bridge)
                // expand every reference into a fresh subtree - charge the full retained cost of
                // the subtree again so aliasing cannot multiply a payload past the budget.
                _allocationBudget.Reserve(_structRetainedBytes[index], $"GFF aliased struct {index}");
                return cached;
            }
            if (!_activeStructs.Add(index))
                throw new NwnFormatException($"GFF contains a cyclic struct reference at index {index}.");

            var reservedBefore = _allocationBudget.ReservedBytes;
            try
            {
                var offset = _structOffset + (long)index * StructSize;
                var result = new GffStruct { Type = _reader.ReadUInt32(offset) };
                var dataOrOffset = _reader.ReadUInt32(offset + 4);
                var fieldCount = _reader.ReadUInt32(offset + 8);
                if (fieldCount > MaximumFields)
                    throw new NwnFormatException($"GFF struct {index} field count is excessive.");
                _allocationBudget.ReserveElements(fieldCount, IntPtr.Size, $"GFF struct {index} field references");

                if (fieldCount == 1)
                {
                    result.Fields.Add(ReadField(dataOrOffset, depth));
                }
                else if (fieldCount > 1)
                {
                    long byteCount;
                    try
                    {
                        byteCount = checked((long)fieldCount * 4);
                    }
                    catch (OverflowException ex)
                    {
                        throw new NwnFormatException($"GFF struct {index} field-index length overflows.", ex);
                    }
                    ValidateRelativeRange(dataOrOffset, byteCount, _fieldIndicesCount, $"GFF struct {index} field indices");
                    for (var field = 0u; field < fieldCount; field++)
                    {
                        var fieldIndex = _reader.ReadUInt32(_fieldIndicesOffset + dataOrOffset + (long)field * 4);
                        result.Fields.Add(ReadField(fieldIndex, depth));
                    }
                }

                _structCache.Add(index, result);
                _structRetainedBytes[index] =
                    Math.Max(64, _allocationBudget.ReservedBytes - reservedBefore);
                return result;
            }
            finally
            {
                _activeStructs.Remove(index);
            }
        }

        private GffField ReadField(uint index, int depth)
        {
            if (index >= _fieldCount)
                throw new NwnFormatException($"GFF references missing field index {index}.");
            if (_fieldCache.TryGetValue(index, out var cached))
            {
                // Charge the logical expansion even though the immutable parsed value is reused.
                // This prevents a tiny field-index section from expanding one aliased payload an
                // unbounded number of times.
                _allocationBudget.Reserve(cached.RepeatCharge, $"GFF aliased field {index}");
                return cached.Field;
            }

            var offset = _fieldOffset + (long)index * FieldSize;
            var type = _reader.ReadUInt32(offset);
            var labelIndex = _reader.ReadUInt32(offset + 4);
            var data = _reader.ReadUInt32(offset + 8);
            if (type > GffField.List)
                throw new NwnFormatException($"GFF field {index} has unknown type {type}.");
            if (labelIndex >= _labels.Length)
                throw new NwnFormatException($"GFF field {index} references missing label {labelIndex}.");

            var repeatCharge = CheckedAddAllocation(
                64,
                EstimateFieldPayloadAllocation(type, data, index),
                $"GFF field {index}");
            _allocationBudget.Reserve(repeatCharge, $"GFF field {index}");

            // Struct and List payloads are only fully known after the nested parse; measure what
            // it actually reserved so an aliased reference to this field re-charges the whole
            // retained subtree, not just the reference slots the static estimate covers.
            var reservedBeforeValue = _allocationBudget.ReservedBytes;

            object? value = type switch
            {
                GffField.BYTE => (byte)data,
                GffField.CHAR => unchecked((sbyte)(byte)data),
                GffField.WORD => (ushort)data,
                GffField.SHORT => unchecked((short)(ushort)data),
                GffField.DWORD => data,
                GffField.INT => unchecked((int)data),
                GffField.DWORD64 => ReadFieldUInt64(data, $"GFF DWORD64 field {index}"),
                GffField.INT64 => ReadFieldInt64(data, $"GFF INT64 field {index}"),
                GffField.FLOAT => BitConverter.Int32BitsToSingle(unchecked((int)data)),
                GffField.DOUBLE => ReadFieldDouble(data, $"GFF DOUBLE field {index}"),
                GffField.CExoString => ReadLengthPrefixedString(data, 4, int.MaxValue, $"GFF string field {index}"),
                GffField.CResRef => ReadLengthPrefixedString(
                    data, 1, NwnResRef.MaxLength, $"GFF ResRef field {index}"),
                GffField.CExoLocString => ReadLocString(data, index),
                GffField.VOID => ReadVoid(data, index),
                GffField.Struct => ReadStruct(data, depth + 1),
                GffField.List => ReadList(data, depth + 1, index),
                _ => throw new NwnFormatException($"GFF field {index} has unknown type {type}.")
            };

            var field = new GffField(type, _labels[labelIndex], value);
            if (type is GffField.Struct or GffField.List)
            {
                repeatCharge = CheckedAddAllocation(
                    repeatCharge,
                    _allocationBudget.ReservedBytes - reservedBeforeValue,
                    $"GFF field {index}");
            }
            _fieldCache.Add(index, new CachedField(field, repeatCharge));
            return field;
        }

        private long EstimateFieldPayloadAllocation(uint type, uint data, uint fieldIndex)
        {
            switch (type)
            {
                case GffField.CExoString:
                    return EstimateLengthPrefixedString(data, 4, int.MaxValue, $"GFF string field {fieldIndex}");
                case GffField.CResRef:
                    return EstimateLengthPrefixedString(
                        data, 1, NwnResRef.MaxLength, $"GFF ResRef field {fieldIndex}");
                case GffField.CExoLocString:
                {
                    ValidateRelativeRange(data, 12, _fieldDataCount, $"GFF locstring field {fieldIndex}");
                    var absolute = _fieldDataOffset + data;
                    var totalSize = _reader.ReadUInt32(absolute);
                    ValidateRelativeRange(
                        CheckedAddOffset(data, 4, $"GFF locstring field {fieldIndex}"),
                        totalSize,
                        _fieldDataCount,
                        $"GFF locstring field {fieldIndex}");
                    if (totalSize < 8)
                        throw new NwnFormatException($"GFF locstring field {fieldIndex} is shorter than its fixed data.");
                    var count = _reader.ReadUInt32(absolute + 8);
                    if (count > MaximumFields)
                        throw new NwnFormatException($"GFF locstring field {fieldIndex} has excessive substring count.");
                    return CheckedAddAllocation(
                        CheckedMultiplyAllocation(totalSize, sizeof(char), $"GFF locstring field {fieldIndex}"),
                        CheckedMultiplyAllocation(count, 64, $"GFF locstring field {fieldIndex} substrings"),
                        $"GFF locstring field {fieldIndex}");
                }
                case GffField.VOID:
                {
                    ValidateRelativeRange(data, 4, _fieldDataCount, $"GFF void field {fieldIndex}");
                    var length = _reader.ReadUInt32(_fieldDataOffset + data);
                    ValidateRelativeRange(
                        CheckedAddOffset(data, 4, $"GFF void field {fieldIndex}"),
                        length,
                        _fieldDataCount,
                        $"GFF void field {fieldIndex}");
                    return CheckedAddAllocation(length, 32, $"GFF void field {fieldIndex}");
                }
                case GffField.List:
                {
                    ValidateRelativeRange(data, 4, _listIndicesCount, $"GFF list field {fieldIndex}");
                    var count = _reader.ReadUInt32(_listIndicesOffset + data);
                    if (count > MaximumStructs)
                        throw new NwnFormatException($"GFF list field {fieldIndex} has excessive element count.");
                    return CheckedAddAllocation(
                        CheckedMultiplyAllocation(count, IntPtr.Size, $"GFF list field {fieldIndex}"),
                        64,
                        $"GFF list field {fieldIndex}");
                }
                default:
                    return 0;
            }
        }

        private long EstimateLengthPrefixedString(uint relativeOffset, int prefixBytes, int maximum, string context)
        {
            ValidateRelativeRange(relativeOffset, prefixBytes, _fieldDataCount, context);
            var absolute = _fieldDataOffset + relativeOffset;
            var length = prefixBytes == 1 ? _reader.ReadByte(absolute) : _reader.ReadUInt32(absolute);
            if (length > maximum)
                throw new NwnFormatException($"{context} length {length} exceeds {maximum}.");
            ValidateRelativeRange(CheckedAddOffset(relativeOffset, (uint)prefixBytes, context), length, _fieldDataCount, context);
            return CheckedAddAllocation(
                CheckedMultiplyAllocation(length, sizeof(char), context),
                32,
                context);
        }

        private ulong ReadFieldUInt64(uint relativeOffset, string context)
        {
            ValidateRelativeRange(relativeOffset, 8, _fieldDataCount, context);
            return _reader.ReadUInt64(_fieldDataOffset + relativeOffset);
        }

        private long ReadFieldInt64(uint relativeOffset, string context)
        {
            ValidateRelativeRange(relativeOffset, 8, _fieldDataCount, context);
            return _reader.ReadInt64(_fieldDataOffset + relativeOffset);
        }

        private double ReadFieldDouble(uint relativeOffset, string context)
        {
            ValidateRelativeRange(relativeOffset, 8, _fieldDataCount, context);
            return _reader.ReadDouble(_fieldDataOffset + relativeOffset);
        }

        private string ReadLengthPrefixedString(uint relativeOffset, int prefixBytes, int maximum, string context)
        {
            ValidateRelativeRange(relativeOffset, prefixBytes, _fieldDataCount, context);
            var absolute = _fieldDataOffset + relativeOffset;
            var length = prefixBytes == 1 ? _reader.ReadByte(absolute) : _reader.ReadUInt32(absolute);
            if (length > maximum)
                throw new NwnFormatException($"{context} length {length} exceeds {maximum}.");
            ValidateRelativeRange(CheckedAddOffset(relativeOffset, (uint)prefixBytes, context), length, _fieldDataCount, context);
            return NwnTextEncoding.DecodeGeneral(_reader.Slice(absolute + prefixBytes, length, context));
        }

        private CExoLocString ReadLocString(uint relativeOffset, uint fieldIndex)
        {
            ValidateRelativeRange(relativeOffset, 12, _fieldDataCount, $"GFF locstring field {fieldIndex}");
            var absolute = _fieldDataOffset + relativeOffset;
            var totalSize = _reader.ReadUInt32(absolute);
            ValidateRelativeRange(
                CheckedAddOffset(relativeOffset, 4, $"GFF locstring field {fieldIndex}"),
                totalSize,
                _fieldDataCount,
                $"GFF locstring field {fieldIndex}");
            if (totalSize < 8)
                throw new NwnFormatException($"GFF locstring field {fieldIndex} is shorter than its fixed data.");

            var result = new CExoLocString { StrRef = _reader.ReadUInt32(absolute + 4) };
            var count = _reader.ReadUInt32(absolute + 8);
            if (count > MaximumFields)
                throw new NwnFormatException($"GFF locstring field {fieldIndex} has excessive substring count.");

            long cursor = absolute + 12;
            var end = absolute + 4L + totalSize;
            for (var index = 0u; index < count; index++)
            {
                if (cursor > end - 8)
                    throw new NwnFormatException($"GFF locstring field {fieldIndex} substring table is truncated.");
                var stringId = _reader.ReadUInt32(cursor);
                var length = _reader.ReadUInt32(cursor + 4);
                cursor += 8;
                if (length > end - cursor)
                    throw new NwnFormatException($"GFF locstring field {fieldIndex} substring {index} is truncated.");
                // The string id encodes language*2 + gender; legacy substrings are stored in that
                // language's codepage (Polish ids 10/11 are Windows-1250), same as TLK text.
                var text = NwnTextEncoding.ForLanguage(stringId / 2)
                    .GetString(_reader.Slice(cursor, length, "GFF locstring substring"));
                if (!result.LocalizedStrings.TryAdd(stringId, text))
                    throw new NwnFormatException($"GFF locstring field {fieldIndex} repeats string id {stringId}.");
                cursor += length;
            }
            if (cursor != end)
                throw new NwnFormatException($"GFF locstring field {fieldIndex} size does not match its substring data.");
            return result;
        }

        private byte[] ReadVoid(uint relativeOffset, uint fieldIndex)
        {
            ValidateRelativeRange(relativeOffset, 4, _fieldDataCount, $"GFF void field {fieldIndex}");
            var absolute = _fieldDataOffset + relativeOffset;
            var length = _reader.ReadUInt32(absolute);
            ValidateRelativeRange(
                CheckedAddOffset(relativeOffset, 4, $"GFF void field {fieldIndex}"),
                length,
                _fieldDataCount,
                $"GFF void field {fieldIndex}");
            return _reader.ReadBytes(absolute + 4, length, $"GFF void field {fieldIndex}");
        }

        private GffList ReadList(uint relativeOffset, int depth, uint fieldIndex)
        {
            ValidateRelativeRange(relativeOffset, 4, _listIndicesCount, $"GFF list field {fieldIndex}");
            var absolute = _listIndicesOffset + relativeOffset;
            var count = _reader.ReadUInt32(absolute);
            if (count > MaximumStructs)
                throw new NwnFormatException($"GFF list field {fieldIndex} has excessive element count.");
            long byteCount;
            try
            {
                byteCount = checked(4L + (long)count * 4);
            }
            catch (OverflowException ex)
            {
                throw new NwnFormatException($"GFF list field {fieldIndex} length overflows.", ex);
            }
            ValidateRelativeRange(relativeOffset, byteCount, _listIndicesCount, $"GFF list field {fieldIndex}");

            var result = new GffList();
            for (var element = 0u; element < count; element++)
            {
                var structIndex = _reader.ReadUInt32(absolute + 4L + element * 4L);
                result.Elements.Add(ReadStruct(structIndex, depth));
            }
            return result;
        }

        private void ValidateSection(uint offset, uint count, int elementSize, int maximum, string context)
        {
            var bytes = GuardedBinaryReader.CheckedCount(count, elementSize, maximum, context);
            _reader.ValidateRange(offset, bytes, context);
        }

        private void ValidateByteSection(uint offset, uint count, int maximum, string context)
        {
            if (count > maximum)
                throw new NwnFormatException($"{context} byte count {count} exceeds {maximum}.");
            _reader.ValidateRange(offset, count, context);
        }

        private void ValidateIndexedSection(uint offset, uint count, string context)
        {
            if ((count & 3) != 0)
                throw new NwnFormatException($"{context} byte count must be divisible by four.");
            ValidateByteSection(offset, count, MaximumVariableData, context);
        }

        private static void ValidateRelativeRange(uint offset, long count, uint sectionLength, string context)
        {
            if (count < 0 || offset > sectionLength || count > sectionLength - offset)
                throw new NwnFormatException($"{context} is outside its declared GFF section.");
        }

        private static uint CheckedAddOffset(uint left, uint right, string context)
        {
            try
            {
                return checked(left + right);
            }
            catch (OverflowException ex)
            {
                throw new NwnFormatException($"{context} offset overflows.", ex);
            }
        }

        private static long CheckedMultiplyAllocation(long count, int bytesPerElement, string context)
        {
            try
            {
                return checked(count * bytesPerElement);
            }
            catch (OverflowException ex)
            {
                throw new NwnFormatException($"{context} allocation size overflows.", ex);
            }
        }

        private static long CheckedAddAllocation(long left, long right, string context)
        {
            try
            {
                return checked(left + right);
            }
            catch (OverflowException ex)
            {
                throw new NwnFormatException($"{context} allocation size overflows.", ex);
            }
        }

        private readonly record struct CachedField(GffField Field, long RepeatCharge);
    }
}
