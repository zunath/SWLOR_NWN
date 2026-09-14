// SPDX-License-Identifier: MIT

using SWLOR.NWN.Formats.Common;
using SWLOR.NWN.Formats.Internal;

namespace SWLOR.NWN.Formats.Key;

/// <summary>
/// Reads BioWare KEY V1 metadata.
/// </summary>
public static class KeyReader
{
    private const int HeaderSize = 64;
    private const int BifEntrySize = 12;
    private const int ResourceEntrySize = 22;
    private const int MaximumBifs = 65_536;
    private const int MaximumResources = 16_000_000;

    public static KeyFile Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Read(File.ReadAllBytes(path));
    }

    public static KeyFile Read(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        var reader = new GuardedBinaryReader(bytes);
        reader.ValidateRange(0, HeaderSize, "KEY header");
        if (reader.ReadAscii(0, 4, "KEY signature") != "KEY ")
            throw new NwnFormatException("Invalid KEY signature.");
        if (reader.ReadAscii(4, 4, "KEY version") != "V1  ")
            throw new NwnFormatException("Unsupported KEY version; expected V1.");

        var bifCount = reader.ReadUInt32(8);
        var resourceCount = reader.ReadUInt32(12);
        var bifTableOffset = reader.ReadUInt32(16);
        var resourceTableOffset = reader.ReadUInt32(20);
        var bifTableBytes = GuardedBinaryReader.CheckedCount(bifCount, BifEntrySize, MaximumBifs, "KEY BIF entries");
        var resourceTableBytes = GuardedBinaryReader.CheckedCount(
            resourceCount,
            ResourceEntrySize,
            MaximumResources,
            "KEY resource entries");
        reader.ValidateRange(bifTableOffset, bifTableBytes, "KEY BIF table");
        reader.ValidateRange(resourceTableOffset, resourceTableBytes, "KEY resource table");

        var allocationBudget = new AllocationBudget("KEY");
        allocationBudget.ReserveElements(bifCount, 64, "KEY BIF metadata");
        // Each public entry retains a list slot, a reference-type object, and its own decoded
        // UTF-16 ResRef string. Charge conservatively rather than using only the fixed record size.
        allocationBudget.ReserveElements(resourceCount, 128, "KEY resource metadata");

        // Validate and charge every declared filename before decoding any of them. Repeated table
        // entries may legally point at the same bytes, but each decoded string would otherwise
        // multiply managed allocation from a tiny aliased input region.
        for (var index = 0; index < bifCount; index++)
        {
            var offset = bifTableOffset + (long)index * BifEntrySize;
            var filenameOffset = reader.ReadUInt32(offset + 4);
            var filenameSize = reader.ReadUInt16(offset + 8);
            reader.ValidateRange(filenameOffset, filenameSize, $"KEY BIF filename {index}");
            allocationBudget.ReserveElements(filenameSize, sizeof(char), $"KEY BIF filename {index}");
        }

        var bifs = new List<KeyBifEntry>(checked((int)bifCount));
        var filenameCache = new Dictionary<(uint Offset, ushort Size), string>();
        for (var index = 0; index < bifCount; index++)
        {
            var offset = bifTableOffset + (long)index * BifEntrySize;
            var fileSize = reader.ReadUInt32(offset);
            var filenameOffset = reader.ReadUInt32(offset + 4);
            var filenameSize = reader.ReadUInt16(offset + 8);
            var drives = reader.ReadUInt16(offset + 10);
            var filenameKey = (filenameOffset, filenameSize);
            if (!filenameCache.TryGetValue(filenameKey, out var filename))
            {
                filename = NwnTextEncoding.DecodeGeneral(
                    reader.Slice(filenameOffset, filenameSize, $"KEY BIF filename {index}")).TrimEnd('\0');
                filenameCache.Add(filenameKey, filename);
            }
            if (string.IsNullOrWhiteSpace(filename))
                throw new NwnFormatException($"KEY BIF filename {index} is empty.");
            bifs.Add(new KeyBifEntry(fileSize, filename, drives));
        }

        var resources = new List<KeyResourceEntry>(checked((int)resourceCount));
        for (var index = 0; index < resourceCount; index++)
        {
            var offset = resourceTableOffset + (long)index * ResourceEntrySize;
            var resRef = reader.ReadAscii(offset, NwnResRef.MaxLength, $"KEY ResRef {index}", trimNull: true);
            var resourceType = reader.ReadUInt16(offset + NwnResRef.MaxLength);
            var resourceId = reader.ReadUInt32(offset + 18);
            var entry = new KeyResourceEntry(resRef, resourceType, resourceId);
            if (entry.BifIndex >= bifs.Count)
                throw new NwnFormatException($"KEY resource {index} references missing BIF index {entry.BifIndex}.");
            resources.Add(entry);
        }

        return new KeyFile(bifs, resources);
    }
}
