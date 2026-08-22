using System.Text;
using SWLOR.NWN.Formats;
using SWLOR.NWN.Formats.Common;

namespace SWLOR.Toolset.Domain.GameData.Resources
{
    /// <summary>
    /// Read-only index over an Aurora HAK V1.0 archive. HAK uses the ERF container layout: a key
    /// table names each resource and a parallel resource table supplies its byte range.
    /// </summary>
    public sealed class HakArchiveCatalog : IHakResourceCatalog
    {
        private const int HeaderSize = 160;
        private const int KeySize = 24;
        private const int ResourceSize = 8;
        private const int MaximumEntries = 2_000_000;

        private readonly Dictionary<ResourceIdentity, Entry> _entries;

        private HakArchiveCatalog(string archivePath, Dictionary<ResourceIdentity, Entry> entries)
        {
            SourcePath = archivePath;
            _entries = entries;
            ContentVersionUtc = File.GetLastWriteTimeUtc(archivePath);
        }

        public string SourcePath { get; }

        public DateTime ContentVersionUtc { get; }

        public IEnumerable<ResourceIdentity> Resources => _entries.Keys;

        public int ResourceCount => _entries.Count;

        public static HakArchiveCatalog Open(string archivePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);
            archivePath = Path.GetFullPath(archivePath);

            using var stream = new FileStream(
                archivePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);

            if (stream.Length < HeaderSize)
                throw new NwnFormatException("HAK header is truncated.");

            var fileType = Encoding.ASCII.GetString(reader.ReadBytes(4));
            var version = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (fileType != "HAK ")
                throw new NwnFormatException("Invalid HAK signature.");
            if (version != "V1.0")
                throw new NwnFormatException("Unsupported HAK version; expected V1.0.");

            _ = reader.ReadUInt32(); // localized string count
            _ = reader.ReadUInt32(); // localized string byte size
            var entryCount = reader.ReadUInt32();
            _ = reader.ReadUInt32(); // localized string offset
            var keyOffset = reader.ReadUInt32();
            var resourceOffset = reader.ReadUInt32();

            if (entryCount > MaximumEntries)
                throw new NwnFormatException($"HAK entry count {entryCount} exceeds {MaximumEntries}.");

            ValidateRange(stream.Length, keyOffset, checked((long)entryCount * KeySize), "HAK key table");
            ValidateRange(
                stream.Length,
                resourceOffset,
                checked((long)entryCount * ResourceSize),
                "HAK resource table");

            var resources = new (uint Offset, uint Size)[checked((int)entryCount)];
            stream.Position = resourceOffset;
            for (var index = 0; index < resources.Length; index++)
            {
                var offset = reader.ReadUInt32();
                var size = reader.ReadUInt32();
                ValidateRange(stream.Length, offset, size, $"HAK resource {index}");
                resources[index] = (offset, size);
            }

            var entries = new Dictionary<ResourceIdentity, Entry>();
            stream.Position = keyOffset;
            for (var index = 0; index < entryCount; index++)
            {
                var rawResRef = reader.ReadBytes(NwnResRef.MaxLength);
                var terminator = Array.IndexOf(rawResRef, (byte)0);
                var length = terminator < 0 ? rawResRef.Length : terminator;
                var resRef = Encoding.ASCII.GetString(rawResRef, 0, length);
                var resourceId = reader.ReadUInt32();
                var resourceType = reader.ReadUInt16();
                _ = reader.ReadUInt16();

                if (resRef.Length == 0 || resourceId >= (uint)resources.Length)
                    continue;

                var range = resources[checked((int)resourceId)];
                entries[new ResourceIdentity(resRef, resourceType)] =
                    new Entry(range.Offset, range.Size);
            }

            return new HakArchiveCatalog(archivePath, entries);
        }

        public bool TryGetBytes(ResourceIdentity identity, out byte[] bytes)
        {
            if (!_entries.TryGetValue(identity, out var entry))
            {
                bytes = Array.Empty<byte>();
                return false;
            }

            using var stream = new FileStream(
                SourcePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            stream.Position = entry.Offset;
            bytes = new byte[checked((int)entry.Size)];
            stream.ReadExactly(bytes);
            return true;
        }

        public string Describe(ResourceIdentity identity) => SourcePath;

        private static void ValidateRange(long fileLength, long offset, long size, string name)
        {
            if (offset < 0 || size < 0 || offset > fileLength || size > fileLength - offset)
                throw new NwnFormatException($"{name} is outside the HAK file.");
        }

        private readonly record struct Entry(uint Offset, uint Size);
    }
}
