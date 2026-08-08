using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Documents;

namespace SWLOR.Toolset.Domain.Editors.Creatures
{
    /// <summary>UTC field, role-local, feat, equipment and loot mutations in one domain surface.</summary>
    public sealed class CreatureValueStore : BehaviorValueStore
    {
        public CreatureValueStore(JsonGffStruct creature) : base(creature)
        {
        }

        public JsonGffStruct Creature => Owner;

        public string? EquippedResRef(int slotId) => Creature.GetListOrEmpty("Equip_ItemList")
            .FirstOrDefault(entry => entry.StructId == (uint)slotId)
            ?.GetStringOrNull("EquippedRes");

        public void SetEquippedResRef(int slotId, string? resRef)
        {
            var field = Creature.GetOrNull("Equip_ItemList");
            if (field == null)
            {
                field = JsonGffField.CreateList();
                Creature.Add("Equip_ItemList", field);
            }

            var existing = field.Elements!
                .Select((entry, index) => (entry, index))
                .FirstOrDefault(item => item.entry.StructId == (uint)slotId);

            if (string.IsNullOrWhiteSpace(resRef))
            {
                if (existing.entry != null)
                    field.RemoveElementAt(existing.index);
                return;
            }

            if (existing.entry != null)
            {
                existing.entry.SetString("EquippedRes", GffFieldType.ResRef, resRef);
                return;
            }

            var entry = JsonGffField.CreateStruct((uint)slotId).Struct!;
            entry.SetString("EquippedRes", GffFieldType.ResRef, resRef);
            field.InsertElement(field.Elements!.Count, entry);
        }

        public IReadOnlyList<int> Feats => Creature.GetListOrEmpty("FeatList")
            .Select(entry => entry.GetIntOrNull("Feat") ?? -1)
            .Where(value => value >= 0)
            .ToList();

        public void AddFeat(int featId)
        {
            if (Feats.Contains(featId))
                return;

            var field = Creature.GetOrNull("FeatList");
            if (field == null)
            {
                field = JsonGffField.CreateList();
                Creature.Add("FeatList", field);
            }

            var entry = JsonGffField.CreateStruct(1).Struct!;
            entry.SetInt("Feat", GffFieldType.Word, featId);
            field.InsertElement(field.Elements!.Count, entry);
        }

        public void RemoveFeat(int featId)
        {
            var field = Creature.GetOrNull("FeatList");
            if (field?.Elements == null)
                return;

            var index = field.Elements.FindIndex(entry => entry.GetIntOrNull("Feat") == featId);
            if (index >= 0)
                field.RemoveElementAt(index);
        }

        public IReadOnlyList<CreatureLootEntry> ReadLoot(out bool hasGap)
        {
            var result = new List<CreatureLootEntry>();
            hasGap = false;
            var expectedIndex = 1;
            var rows = Locals
                .Select(entry => entry.Name)
                .Select(name => (Name: name, Index: LootIndex(name)))
                .Where(row => row.Index > 0)
                .OrderBy(row => row.Index)
                .ToList();

            foreach (var row in rows)
            {
                if (row.Index != expectedIndex)
                    hasGap = true;
                expectedIndex = row.Index + 1;

                var raw = Locals.GetString(row.Name);
                if (string.IsNullOrWhiteSpace(raw))
                {
                    hasGap = true;
                    continue;
                }

                var parts = raw.Split(',', StringSplitOptions.TrimEntries);
                if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
                    continue;

                var chance = parts.Length > 1 && int.TryParse(parts[1], out var parsedChance)
                    ? Math.Clamp(parsedChance, 1, 100)
                    : 100;
                var pulls = parts.Length > 2 && int.TryParse(parts[2], out var parsedPulls)
                    ? Math.Max(1, parsedPulls)
                    : 1;
                result.Add(new CreatureLootEntry(parts[0], chance, pulls));
            }

            return result;
        }

        private static int LootIndex(string name)
        {
            const string prefix = "LOOT_TABLE_";
            return name.StartsWith(prefix, StringComparison.Ordinal) &&
                   int.TryParse(name.AsSpan(prefix.Length), out var index)
                ? index
                : -1;
        }

        public void WriteLoot(IEnumerable<CreatureLootEntry> entries)
        {
            var normalized = entries
                .Where(entry => !string.IsNullOrWhiteSpace(entry.TableId))
                .Select(entry => entry with
                {
                    TableId = entry.TableId.Trim(),
                    Chance = Math.Clamp(entry.Chance, 1, 100),
                    Pulls = Math.Max(1, entry.Pulls)
                })
                .ToList();

            foreach (var name in Locals.Select(entry => entry.Name)
                         .Where(name => System.Text.RegularExpressions.Regex.IsMatch(
                             name,
                             "^LOOT_TABLE_[0-9]+$"))
                         .ToList())
            {
                Locals.Remove(name);
            }

            for (var index = 0; index < normalized.Count; index++)
            {
                var entry = normalized[index];
                Locals.SetString(
                    $"LOOT_TABLE_{index + 1}",
                    $"{entry.TableId},{entry.Chance},{entry.Pulls}");
            }
        }
    }
}
