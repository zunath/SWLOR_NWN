using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editors.Items
{
    /// <summary>
    /// Item-property storage layered over the shared blueprint/placement value store: reads and
    /// writes entries of a uti's PropertiesList by property id and subtype.
    /// </summary>
    /// <remarks>
    /// A property with no subtype table stores Subtype as 0, not as a 255 sentinel - verified
    /// against Module\uti\adren_harness.uti.json, where HP (property 90) and STMRegen (property
    /// 120) both carry <c>"Subtype": 0</c>. Passing subtypeId &lt; 0 for "no subtype" matches (or
    /// writes) that same 0, so -1 and 0 are interchangeable for those properties; a real subtype id
    /// such as Defense's 1 (Physical) or 2 (Force) still requires an exact match.
    /// </remarks>
    public sealed class ItemValueStore : BehaviorValueStore
    {
        private const string PropertiesListName = "PropertiesList";

        public ItemValueStore(JsonGffStruct item) : base(item)
        {
        }

        public JsonGffStruct Item => Owner;

        public IReadOnlyList<(int PropertyId, int SubtypeId, int CostValue)> Properties =>
            Owner.GetListOrEmpty(PropertiesListName)
                .Where(entry => entry.Contains("PropertyName") && entry.Contains("CostValue"))
                .Select(entry => (
                    PropertyId: (int)entry.Get("PropertyName").GetInteger(),
                    SubtypeId: entry.TryGet("Subtype", out var subtype) ? (int)subtype.GetInteger() : 0,
                    CostValue: (int)entry.Get("CostValue").GetInteger()))
                .ToList();

        public bool HasProperty(int propertyId) =>
            Properties.Any(property => property.PropertyId == propertyId);

        /// <summary>The CostValue of the matching PropertiesList entry, or null when none matches.</summary>
        public int? GetPropertyValue(int propertyId, int subtypeId)
        {
            var entry = FindEntry(propertyId, subtypeId);
            return entry == null ? null : (int)entry.Get("CostValue").GetInteger();
        }

        /// <summary>Sums duplicate matching entries, as the creature stat runtime does for NPC HP.</summary>
        public int GetCombinedPropertyValue(int propertyId, int subtypeId)
        {
            return Properties
                .Where(property => property.PropertyId == propertyId &&
                                   (subtypeId < 0 || property.SubtypeId == subtypeId))
                .Sum(property => property.CostValue);
        }

        /// <summary>Normalizes all matching entries to one value while preserving unrelated properties.</summary>
        public void SetCombinedPropertyValue(
            int propertyId,
            int subtypeId,
            int costTableId,
            int? value)
        {
            foreach (var entry in Owner.GetListOrEmpty(PropertiesListName)
                         .Where(entry => Matches(entry, propertyId, subtypeId))
                         .ToList())
            {
                RemoveEntry(entry);
            }

            if (value.HasValue)
                AddEntry(propertyId, subtypeId, costTableId, value.Value);
        }

        /// <summary>
        /// Adds, updates, or removes the PropertiesList entry for <paramref name="propertyId"/> /
        /// <paramref name="subtypeId"/>. Only a null <paramref name="value"/> removes the entry -
        /// zero is a real stored CostValue, because a subtype-carrying property's meaning lives in
        /// the subtype (imp_molytex_3 stores WeaponDamageType with CostValue 0, and the runtime
        /// reads its subtype). New entries are written with Param1=255, Param1Value=0, and
        /// ChanceAppear=100, matching every entry in the corpus (none of those three fields vary
        /// today).
        /// </summary>
        public void SetPropertyValue(int propertyId, int subtypeId, int costTableId, int? value)
        {
            var entry = FindEntry(propertyId, subtypeId);

            if (value is null)
            {
                if (entry != null)
                    RemoveEntry(entry);
                return;
            }

            if (entry != null)
            {
                entry.SetInt("CostTable", GffFieldType.Byte, costTableId);
                entry.SetInt("CostValue", GffFieldType.Word, value.Value);
                return;
            }

            AddEntry(propertyId, subtypeId, costTableId, value.Value);
        }

        /// <summary>
        /// Writes the one PropertiesList entry an exclusive property (WeaponDamageType, 134) may
        /// carry: every existing entry of <paramref name="propertyId"/> is removed first, then a
        /// fresh one is added with CostValue 0. <see cref="SetPropertyValue"/> cannot do this
        /// directly - it only ever touches the entry matching its subtype, and exclusivity means
        /// switching subtypes must also remove the previously selected one.
        /// </summary>
        public void SetExclusiveProperty(int propertyId, int subtypeId, int costTableId)
        {
            ClearProperty(propertyId);
            AddEntry(propertyId, subtypeId, costTableId, 0);
        }

        /// <summary>Removes every PropertiesList entry of <paramref name="propertyId"/>, regardless of subtype.</summary>
        public void ClearProperty(int propertyId)
        {
            foreach (var entry in Owner.GetListOrEmpty(PropertiesListName)
                         .Where(entry => entry.TryGet("PropertyName", out var name) &&
                                         (int)name.GetInteger() == propertyId)
                         .ToList())
            {
                RemoveEntry(entry);
            }
        }

        private JsonGffStruct? FindEntry(int propertyId, int subtypeId)
        {
            foreach (var entry in Owner.GetListOrEmpty(PropertiesListName))
            {
                if (!entry.TryGet("PropertyName", out var nameField) ||
                    (int)nameField.GetInteger() != propertyId)
                {
                    continue;
                }

                if (subtypeId >= 0)
                {
                    var stored = entry.TryGet("Subtype", out var subtypeField)
                        ? (int)subtypeField.GetInteger()
                        : 0;
                    if (stored != subtypeId)
                        continue;
                }

                return entry;
            }

            return null;
        }

        private static bool Matches(JsonGffStruct entry, int propertyId, int subtypeId)
        {
            if (!entry.TryGet("PropertyName", out var nameField) ||
                (int)nameField.GetInteger() != propertyId)
                return false;

            if (subtypeId < 0)
                return true;

            var stored = entry.TryGet("Subtype", out var subtypeField)
                ? (int)subtypeField.GetInteger()
                : 0;
            return stored == subtypeId;
        }

        private void AddEntry(int propertyId, int subtypeId, int costTableId, int value)
        {
            var entry = JsonGffField.CreateStruct(0).Struct!;
            entry.SetInt("PropertyName", GffFieldType.Word, propertyId);
            entry.SetInt("Subtype", GffFieldType.Word, subtypeId < 0 ? 0 : subtypeId);
            entry.SetInt("CostTable", GffFieldType.Byte, costTableId);
            entry.SetInt("CostValue", GffFieldType.Word, value);
            entry.SetInt("Param1", GffFieldType.Byte, 255);
            entry.SetInt("Param1Value", GffFieldType.Byte, 0);
            entry.SetInt("ChanceAppear", GffFieldType.Byte, 100);

            var list = GetOrAddPropertiesListField();
            list.InsertElement(list.Elements!.Count, entry);
        }

        private void RemoveEntry(JsonGffStruct entry)
        {
            var list = Owner.GetOrNull(PropertiesListName);
            var index = list?.Elements?.IndexOf(entry) ?? -1;
            if (index >= 0)
                list!.RemoveElementAt(index);
        }

        private JsonGffField GetOrAddPropertiesListField()
        {
            if (Owner.GetOrNull(PropertiesListName) is { } existing)
                return existing;

            var list = JsonGffField.CreateList();
            Owner.Add(PropertiesListName, list);
            return list;
        }
    }
}
