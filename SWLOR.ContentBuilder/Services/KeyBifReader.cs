using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SWLOR.ContentBuilder.Services
{
    /// <summary>
    /// Minimal reader for NWN:EE's KEY/BIF resource archive format, sufficient to pull a single
    /// texture resource (a tileset minimap TGA) out of the base game data without a full resource
    /// manager. Parses "data\nwn_base.key" once, then opens the referenced .bif lazily per lookup.
    /// </summary>
    internal sealed class KeyBifReader
    {
        public const int ResTypeTga = 3;
        public const int ResTypeDds = 2033;

        private readonly string _installRoot;
        private readonly List<string> _bifRelativePaths;
        private readonly Dictionary<(string Resref, int ResType), (int BifIndex, int ResourceIndexInBif)> _keyIndex;

        // Per-bif variable resource table cache: bifIndex -> list of (id, offset, fileSize, resourceType).
        private readonly Dictionary<int, List<(uint Id, uint Offset, uint FileSize, uint ResourceType)>> _bifTableCache = new();

        private KeyBifReader(
            string installRoot,
            List<string> bifRelativePaths,
            Dictionary<(string, int), (int, int)> keyIndex)
        {
            _installRoot = installRoot;
            _bifRelativePaths = bifRelativePaths;
            _keyIndex = keyIndex;
        }

        /// <summary>
        /// Probes the well-known NWN:EE install locations (Steam x86/x64, GOG) plus a
        /// CONTENTBUILDER_NWN_PATH environment variable override, and parses nwn_base.key from the
        /// first one found. Returns null (with a reason) rather than throwing so the map-graphics
        /// preview mode can degrade gracefully when the base game isn't installed on this machine.
        /// </summary>
        public static KeyBifReader TryCreate(out string errorOrNull)
        {
            var candidates = new List<string>();

            var envOverride = Environment.GetEnvironmentVariable("CONTENTBUILDER_NWN_PATH");
            if (!string.IsNullOrEmpty(envOverride))
                candidates.Add(envOverride);

            candidates.Add(@"C:\Program Files (x86)\Steam\steamapps\common\Neverwinter Nights");
            candidates.Add(@"C:\Program Files\Steam\steamapps\common\Neverwinter Nights");
            candidates.Add(@"C:\GOG Games\Neverwinter Nights");

            string installRoot = null;
            foreach (var candidate in candidates)
            {
                if (string.IsNullOrEmpty(candidate)) continue;
                var keyPath = Path.Combine(candidate, "data", "nwn_base.key");
                if (File.Exists(keyPath))
                {
                    installRoot = candidate;
                    break;
                }
            }

            if (installRoot == null)
            {
                errorOrNull = "No NWN:EE install found (checked Steam x86/x64, GOG, CONTENTBUILDER_NWN_PATH).";
                return null;
            }

            try
            {
                var reader = Parse(installRoot);
                errorOrNull = null;
                return reader;
            }
            catch (Exception ex)
            {
                errorOrNull = $"Failed to parse {Path.Combine(installRoot, "data", "nwn_base.key")}: {ex.Message}";
                return null;
            }
        }

        private static KeyBifReader Parse(string installRoot)
        {
            var keyPath = Path.Combine(installRoot, "data", "nwn_base.key");
            var bytes = File.ReadAllBytes(keyPath);

            var fileType = Encoding.ASCII.GetString(bytes, 0, 4);
            if (fileType != "KEY ")
                throw new InvalidDataException($"Unexpected KEY file type '{fileType}'.");

            var bifCount = BitConverter.ToInt32(bytes, 8);
            var keyCount = BitConverter.ToInt32(bytes, 12);
            var offsetToFileTable = BitConverter.ToInt32(bytes, 16);
            var offsetToKeyTable = BitConverter.ToInt32(bytes, 20);

            var bifPaths = new List<string>(bifCount);
            for (var i = 0; i < bifCount; i++)
            {
                var entryOffset = offsetToFileTable + i * 12;
                var filenameOffset = BitConverter.ToInt32(bytes, entryOffset + 4);
                var filenameSize = BitConverter.ToInt16(bytes, entryOffset + 8);
                var filename = Encoding.ASCII.GetString(bytes, filenameOffset, filenameSize).TrimEnd('\0');
                bifPaths.Add(filename.Replace('/', '\\'));
            }

            var keyIndex = new Dictionary<(string, int), (int, int)>();
            for (var i = 0; i < keyCount; i++)
            {
                var entryOffset = offsetToKeyTable + i * 22;
                var resref = Encoding.ASCII.GetString(bytes, entryOffset, 16).TrimEnd('\0').Trim();
                var resType = BitConverter.ToUInt16(bytes, entryOffset + 16);
                var resId = BitConverter.ToUInt32(bytes, entryOffset + 18);

                var bifIndex = (int)(resId >> 20);
                var resourceIndexInBif = (int)(resId & 0xFFFFF);

                // Later entries win on duplicate (resref, resType) — matches how the real resource
                // manager treats override priority (later-registered keys shadow earlier ones).
                keyIndex[(resref.ToUpperInvariant(), resType)] = (bifIndex, resourceIndexInBif);
            }

            return new KeyBifReader(installRoot, bifPaths, keyIndex);
        }

        /// <summary>
        /// Looks up a resource by resref + type (see ResTypeTga/ResTypeDds) and returns its raw bytes
        /// from the owning .bif, or false if not present in this archive.
        /// </summary>
        public bool TryGetResourceBytes(string resref, int resType, out byte[] bytes)
        {
            bytes = null;
            if (string.IsNullOrEmpty(resref)) return false;

            if (!_keyIndex.TryGetValue((resref.ToUpperInvariant(), resType), out var location))
                return false;

            var table = GetBifTable(location.BifIndex);
            if (table == null || location.ResourceIndexInBif < 0 || location.ResourceIndexInBif >= table.Count)
                return false;

            var entry = table[location.ResourceIndexInBif];
            var bifPath = Path.Combine(_installRoot, _bifRelativePaths[location.BifIndex]);
            if (!File.Exists(bifPath)) return false;

            using var stream = new FileStream(bifPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            stream.Seek(entry.Offset, SeekOrigin.Begin);
            var buffer = new byte[entry.FileSize];
            var read = 0;
            while (read < buffer.Length)
            {
                var n = stream.Read(buffer, read, buffer.Length - read);
                if (n <= 0) break;
                read += n;
            }

            if (read != buffer.Length) return false;

            bytes = buffer;
            return true;
        }

        private List<(uint Id, uint Offset, uint FileSize, uint ResourceType)> GetBifTable(int bifIndex)
        {
            if (_bifTableCache.TryGetValue(bifIndex, out var cached))
                return cached;

            if (bifIndex < 0 || bifIndex >= _bifRelativePaths.Count)
            {
                _bifTableCache[bifIndex] = null;
                return null;
            }

            var bifPath = Path.Combine(_installRoot, _bifRelativePaths[bifIndex]);
            if (!File.Exists(bifPath))
            {
                _bifTableCache[bifIndex] = null;
                return null;
            }

            List<(uint, uint, uint, uint)> table;
            try
            {
                table = ParseBifTable(bifPath);
            }
            catch
            {
                table = null;
            }

            _bifTableCache[bifIndex] = table;
            return table;
        }

        private static List<(uint Id, uint Offset, uint FileSize, uint ResourceType)> ParseBifTable(string bifPath)
        {
            // Header is small and fixed; only the variable resource table (offset + count) is
            // needed, so read the small header first rather than the whole (often large) .bif.
            using var stream = new FileStream(bifPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            Span<byte> header = stackalloc byte[20];
            var headerRead = stream.Read(header);
            if (headerRead < 20) throw new InvalidDataException("BIF header truncated.");

            var fileType = Encoding.ASCII.GetString(header.Slice(0, 4));
            if (fileType != "BIFF")
                throw new InvalidDataException($"Unexpected BIF file type '{fileType}'.");

            var variableResourceCount = BitConverter.ToInt32(header.Slice(8, 4));
            var variableTableOffset = BitConverter.ToInt32(header.Slice(16, 4));

            var entrySize = 16;
            var tableBytes = new byte[variableResourceCount * entrySize];
            stream.Seek(variableTableOffset, SeekOrigin.Begin);
            var read = 0;
            while (read < tableBytes.Length)
            {
                var n = stream.Read(tableBytes, read, tableBytes.Length - read);
                if (n <= 0) break;
                read += n;
            }

            var table = new List<(uint, uint, uint, uint)>(variableResourceCount);
            for (var i = 0; i < variableResourceCount; i++)
            {
                var o = i * entrySize;
                var id = BitConverter.ToUInt32(tableBytes, o);
                var offset = BitConverter.ToUInt32(tableBytes, o + 4);
                var fileSize = BitConverter.ToUInt32(tableBytes, o + 8);
                var resourceType = BitConverter.ToUInt32(tableBytes, o + 12);
                table.Add((id, offset, fileSize, resourceType));
            }

            return table;
        }
    }
}
