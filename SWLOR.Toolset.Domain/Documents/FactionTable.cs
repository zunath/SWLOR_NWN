using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Documents
{
    /// <summary>One row from a module's faction table.</summary>
    public sealed record FactionDefinition(
        int Id,
        string Name,
        bool GlobalEffect,
        int? ParentId,
        bool IsStandard);

    /// <summary>
    /// Reads and edits the directional starting reputations in <c>repute.fac</c>.
    /// </summary>
    /// <remarks>
    /// NWN stores a relationship as <c>FactionID2</c> (the source faction) reacting to
    /// <c>FactionID1</c> (the target faction). The names are easy to read in the opposite
    /// direction, so every public method here deliberately uses source, then target.
    /// </remarks>
    public sealed class FactionTable
    {
        public const int StandardFactionCount = 5;
        public const int HostileMaximum = 10;
        public const int FriendlyMinimum = 90;
        public const int DefaultHostileReputation = 0;
        public const int DefaultNeutralReputation = 50;
        public const int DefaultFriendlyReputation = 100;
        public const uint NoParent = uint.MaxValue;

        private readonly FacDocument _document;

        public FactionTable(FacDocument document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
        }

        public int Count => _document.FactionList.Count;

        public IReadOnlyList<FactionDefinition> Factions => _document.FactionList
            .Select((entry, id) => new FactionDefinition(
                id,
                entry.Get("FactionName").GetString(),
                entry.Get("FactionGlobal").GetInteger() != 0,
                Parent(entry),
                id < StandardFactionCount))
            .ToList();

        public JsonGffStruct EntryAt(int id)
        {
            RequireFaction(id);
            return _document.FactionList[id];
        }

        public void SetName(int id, string name)
        {
            RequireFaction(id);
            name = name?.Trim() ?? string.Empty;
            if (name.Length == 0)
                throw new ArgumentException("A faction name is required.", nameof(name));
            if (_document.FactionList
                .Select((entry, index) => (entry, index))
                .Any(item => item.index != id &&
                             string.Equals(
                                 item.entry.Get("FactionName").GetString(),
                                 name,
                                 StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException($"A faction named '{name}' already exists.", nameof(name));
            }

            EntryAt(id).Get("FactionName").SetString(name);
        }

        public void SetGlobalEffect(int id, bool enabled)
        {
            EntryAt(id).Get("FactionGlobal").SetInteger(enabled ? 1 : 0);
        }

        public int GetReputation(int sourceId, int targetId)
        {
            RequireFaction(sourceId);
            RequireFaction(targetId);

            var entry = FindReputation(sourceId, targetId);
            if (entry != null)
                return Math.Clamp((int)entry.Get("FactionRep").GetInteger(), 0, 100);

            // Sparse FAC files occur in real modules. Same-faction friendliness and neutral
            // cross-faction standing are the least surprising engine-compatible defaults.
            return sourceId == targetId
                ? DefaultFriendlyReputation
                : DefaultNeutralReputation;
        }

        public void SetReputation(int sourceId, int targetId, int value)
        {
            RequireFaction(sourceId);
            RequireFaction(targetId);
            if (value is < 0 or > 100)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Faction reputation must be from 0 to 100.");

            var existing = FindReputation(sourceId, targetId);
            if (existing != null)
            {
                existing.Get("FactionRep").SetInteger(value);
                return;
            }

            var repField = _document.Fields.Get("RepList");
            var entry = NewReputation(repField.Elements!.Count, sourceId, targetId, value);
            repField.InsertElement(repField.Elements.Count, entry);
        }

        /// <summary>
        /// Adds a custom faction by copying both directions of a standard template faction.
        /// </summary>
        public int AddFaction(string name, int parentId)
        {
            name = name?.Trim() ?? string.Empty;
            if (name.Length == 0)
                throw new ArgumentException("A faction name is required.", nameof(name));
            if (parentId is < 1 or >= StandardFactionCount)
                throw new ArgumentOutOfRangeException(
                    nameof(parentId), parentId,
                    "A custom faction must use Hostile, Commoner, Merchant, or Defender as its parent.");
            if (Factions.Any(faction => string.Equals(faction.Name, name, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException($"A faction named '{name}' already exists.", nameof(name));

            var oldCount = Count;
            var outward = Enumerable.Range(0, oldCount)
                .Select(target => GetReputation(parentId, target))
                .ToArray();
            var inward = Enumerable.Range(0, oldCount)
                .Select(source => GetReputation(source, parentId))
                .ToArray();

            var faction = new JsonGffStruct();
            faction.SetStructId((uint)oldCount);
            faction.SetInt("FactionGlobal", GffFieldType.Word, 1);
            faction.SetString("FactionName", GffFieldType.CExoString, name);
            faction.SetUInt("FactionParentID", GffFieldType.Dword, (uint)parentId);
            var factionField = _document.Fields.Get("FactionList");
            factionField.InsertElement(oldCount, faction);

            var repField = _document.Fields.Get("RepList");
            for (var target = 0; target < oldCount; target++)
            {
                repField.InsertElement(
                    repField.Elements!.Count,
                    NewReputation(repField.Elements.Count, oldCount, target, outward[target]));
            }

            for (var source = 0; source < oldCount; source++)
            {
                repField.InsertElement(
                    repField.Elements!.Count,
                    NewReputation(repField.Elements.Count, source, oldCount, inward[source]));
            }

            repField.InsertElement(
                repField.Elements!.Count,
                NewReputation(
                    repField.Elements.Count,
                    oldCount,
                    oldCount,
                    DefaultFriendlyReputation));

            return oldCount;
        }

        /// <summary>
        /// Removes a custom faction and compacts every FAC-local id. References in other module
        /// resources are remapped separately by the application-layer grouped save.
        /// </summary>
        public int RemoveFaction(int id)
        {
            RequireFaction(id);
            if (id < StandardFactionCount)
                throw new InvalidOperationException("NWN's five standard factions cannot be removed.");

            var removedParent = Parent(EntryAt(id))
                                ?? throw new InvalidOperationException(
                                    "A custom faction has no valid standard parent and cannot be removed safely.");

            var factionField = _document.Fields.Get("FactionList");
            factionField.RemoveElementAt(id);
            for (var factionId = id; factionId < factionField.Elements!.Count; factionId++)
            {
                var entry = factionField.Elements[factionId];
                entry.SetStructId((uint)factionId);

                var parent = entry.Get("FactionParentID").GetUnsignedInteger();
                if (parent == (uint)id)
                    entry.Get("FactionParentID").SetUnsignedInteger((uint)removedParent);
                else if (parent != NoParent && parent > (uint)id)
                    entry.Get("FactionParentID").SetUnsignedInteger(parent - 1);
            }

            var repField = _document.Fields.Get("RepList");
            for (var index = repField.Elements!.Count - 1; index >= 0; index--)
            {
                var entry = repField.Elements[index];
                var target = (int)entry.Get("FactionID1").GetInteger();
                var source = (int)entry.Get("FactionID2").GetInteger();
                if (target == id || source == id)
                {
                    repField.RemoveElementAt(index);
                    continue;
                }

                if (target > id)
                    entry.Get("FactionID1").SetInteger(target - 1);
                if (source > id)
                    entry.Get("FactionID2").SetInteger(source - 1);
            }

            for (var index = 0; index < repField.Elements.Count; index++)
                repField.Elements[index].SetStructId((uint)index);

            return removedParent;
        }

        public static string DescribeReputation(int value) => value switch
        {
            <= HostileMaximum => "Hostile",
            >= FriendlyMinimum => "Friendly",
            _ => "Neutral"
        };

        private JsonGffStruct? FindReputation(int sourceId, int targetId)
        {
            return _document.RepList.FirstOrDefault(entry =>
                entry.Get("FactionID2").GetInteger() == sourceId &&
                entry.Get("FactionID1").GetInteger() == targetId);
        }

        private static JsonGffStruct NewReputation(
            int structId,
            int sourceId,
            int targetId,
            int value)
        {
            var entry = new JsonGffStruct();
            entry.SetStructId((uint)structId);
            entry.SetUInt("FactionID1", GffFieldType.Dword, (uint)targetId);
            entry.SetUInt("FactionID2", GffFieldType.Dword, (uint)sourceId);
            entry.SetUInt("FactionRep", GffFieldType.Dword, (uint)value);
            return entry;
        }

        private static int? Parent(JsonGffStruct entry)
        {
            var value = entry.Get("FactionParentID").GetUnsignedInteger();
            return value == NoParent ? null : checked((int)value);
        }

        private void RequireFaction(int id)
        {
            if (id < 0 || id >= Count)
                throw new ArgumentOutOfRangeException(nameof(id), id, "Faction id is outside the module faction list.");
        }
    }
}
