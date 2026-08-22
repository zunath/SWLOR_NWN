using System.Text;
using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>Lazily finds a representative WAV inside a sound-set resource.</summary>
    public sealed class CreatureSoundSetPreviewResolver
    {
        private static readonly int[] PreferredEntries = { 34, 0, 1, 14, 18 };
        private readonly TwoDaService? _twoDa;
        private readonly ResourceIndex? _resources;
        private readonly Dictionary<int, string?> _cache = new();

        public CreatureSoundSetPreviewResolver(TwoDaService? twoDa, ResourceIndex? resources)
        {
            _twoDa = twoDa;
            _resources = resources;
        }

        public string? Resolve(int soundSetId)
        {
            if (_cache.TryGetValue(soundSetId, out var cached))
                return cached;

            var result = ResolveCore(soundSetId);
            _cache[soundSetId] = result;
            return result;
        }

        private string? ResolveCore(int soundSetId)
        {
            if (_resources == null || _twoDa?.TryGetTable("soundset", out var table) != true)
                return null;
            var soundSetResRef = table!.GetString(soundSetId, "RESREF");
            if (string.IsNullOrWhiteSpace(soundSetResRef))
                return null;

            try
            {
                var identity = new ResourceIdentity(
                    soundSetResRef,
                    ResourceIdentity.TypeFromExtension("ssf"));
                if (!_resources.TryLookup(identity, out var handle))
                    return null;
                return ReadPreviewResRef(handle.GetBytes());
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or ArgumentException)
            {
                return null;
            }
        }

        private static string? ReadPreviewResRef(byte[] data)
        {
            if (data.Length < 40)
                return null;
            using var stream = new MemoryStream(data, writable: false);
            using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: false);
            if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "SSF ")
                return null;
            reader.ReadBytes(4);
            var count = reader.ReadUInt32();
            var tableOffset = reader.ReadUInt32();
            if (count == 0 || count > 1024 || tableOffset > data.Length - count * 4L)
                return null;

            stream.Position = tableOffset;
            var offsets = new uint[count];
            for (var index = 0; index < offsets.Length; index++)
                offsets[index] = reader.ReadUInt32();

            foreach (var index in PreferredEntries.Concat(Enumerable.Range(0, offsets.Length)).Distinct())
            {
                if (index < 0 || index >= offsets.Length || offsets[index] > data.Length - 20L)
                    continue;
                stream.Position = offsets[index];
                var resRef = Encoding.ASCII.GetString(reader.ReadBytes(NwnResRef.MaxLength)).TrimEnd('\0');
                if (!string.IsNullOrWhiteSpace(resRef) && resRef != "****")
                    return resRef;
            }
            return null;
        }
    }
}
