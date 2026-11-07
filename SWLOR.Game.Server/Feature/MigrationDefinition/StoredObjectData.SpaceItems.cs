using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// Saved-data conversion of retired ship deeds and modules (plan section 15): each becomes the item it maps to, made
/// from that item's blueprint, keeping its identity, position, stack, locals and crafted Module and Starship Bonus
/// properties. The blueprint brings the new name, model, value and Requires Skill rank and drops the retired perk
/// stamps. Configuration modules are removed and their value counted as credits owed to the owner.
/// </summary>
internal sealed partial class StoredObjectData
{
    private const string SpaceDroidVariable = "CONSTRUCTED_DROID";

    /// <summary>Fields an item keeps when it becomes another item.</summary>
    private static readonly HashSet<string> KeptItemFields = new()
    {
        "ObjectId", "UUID", "Repos_PosX", "Repos_Posy", "StackSize", "Pickpocketable", "Dropable", "Identified", "Stolen",
        "VarTable", "XPosition", "YPosition", "ZPosition", "XOrientation", "YOrientation", "ZOrientation",
        "ActionList", "EffectList",
    };

    /// <summary>Converts the retired space items of a saved item or creature (UTI or BIC/UTC, base64).</summary>
    internal static bool MigrateLegacySpaceItems(string original, out string migrated, LegacySpaceItemTally tally)
    {
        migrated = original;
        if (string.IsNullOrWhiteSpace(original))
            return false;
        var document = new StoredObjectData(Convert.FromBase64String(original));
        if (!ConvertLegacySpaceItems(document._root, !document.IsCreature, tally))
            return false;
        migrated = document.Serialize();
        return true;
    }

    /// <summary>
    /// Converts a saved item node (when <paramref name="isItem"/>) and everything in its inventory lists and saved
    /// droid. A root configuration module is left in place and reported on the tally.
    /// </summary>
    internal static bool ConvertLegacySpaceItems(Node root, bool isItem, LegacySpaceItemTally tally)
    {
        var changed = false;
        if (isItem)
        {
            var resref = ReadResRef(root);
            if (LegacySpaceItemCatalog.TryGetConfigurationModule(resref, out var credits))
            {
                tally.RootIsConfigurationModule = true;
                tally.Credits += credits;
                tally.ConfigurationModulesRefunded++;
                tally.Count(resref);
                return false;
            }
            if (ConvertLegacySpaceItem(root, tally, out var replacement))
            {
                tally.RootReplacementResref = replacement;
                tally.RootReplacement = LegacySpaceItemBlueprints.Get(replacement);
                changed = true;
            }
        }
        return ConvertLegacySpaceContents(root, tally) | changed;
    }

    /// <summary>Converts one retired hull or module node in place. False for any other item.</summary>
    internal static bool ConvertLegacySpaceItem(Node item, LegacySpaceItemTally tally, out string replacement)
    {
        var resref = ReadResRef(item);
        if (!LegacySpaceItemCatalog.TryGetReplacement(resref, out replacement))
            return false;
        RewriteItem(item, ParseItem(LegacySpaceItemBlueprints.Get(replacement).Data));
        tally.Count(resref);
        if (LegacySpaceItemCatalog.TryGetHullByResref(resref, out var hull) &&
            TryGetHullDesignKey(hull, out var key) && !tally.HullDesigns.Contains(key))
            tally.HullDesigns.Add(key);
        return true;
    }

    /// <summary>The Hull Design key of a retired hull's model on its new ship, when that model is one of its designs.</summary>
    internal static bool TryGetHullDesignKey(LegacyShipHull hull, out string key)
    {
        var design = HullDesignRules.DesignsFor(hull.ShipTag).FirstOrDefault(x => (int)x.Appearance == hull.Appearance);
        key = design == null ? null : HullDesignRules.Key(design);
        return design != null;
    }

    private static bool ConvertLegacySpaceContents(Node node, LegacySpaceItemTally tally)
    {
        var changed = ConvertLegacySpaceDroid(node, tally);
        foreach (var list in node.Fields.Where(IsInventoryList))
        {
            var numbered = list.Children.Select((child, index) => child.Type == (uint)index).All(x => x);
            var removed = false;
            foreach (var child in list.Children.ToArray())
            {
                var resref = ReadResRef(child);
                if (LegacySpaceItemCatalog.TryGetConfigurationModule(resref, out var credits))
                {
                    list.Children.Remove(child);
                    tally.Credits += credits;
                    tally.ConfigurationModulesRefunded++;
                    tally.Count(resref);
                    removed = true;
                    continue;
                }
                changed |= ConvertLegacySpaceItem(child, tally, out _);
                changed |= ConvertLegacySpaceContents(child, tally);
            }
            if (removed && numbered)
                NumberListElements(list);
            changed |= removed;
        }
        return changed;
    }

    /// <summary>The items a droid controller saves: its equipment and inventory.</summary>
    internal static bool ConvertLegacySpaceDroid(Node item, LegacySpaceItemTally tally)
    {
        var local = FindLocal(item, SpaceDroidVariable, 3);
        var saved = local == null ? null : ReadText(local);
        if (string.IsNullOrWhiteSpace(saved))
            return false;
        var droid = JObject.Parse(saved);
        var changed = false;
        foreach (var field in new[] { "EquippedItems", "Inventory" })
        {
            if (droid[field] is not JObject items)
                continue;
            foreach (var entry in items.Properties().ToArray())
            {
                var entryTally = new LegacySpaceItemTally();
                var converted = MigrateLegacySpaceItems((string)entry.Value, out var migrated, entryTally);
                tally.Add(entryTally);
                if (entryTally.RootIsConfigurationModule)
                {
                    entry.Remove();
                    changed = true;
                }
                else if (converted)
                {
                    entry.Value = migrated;
                    changed = true;
                }
            }
        }
        if (changed)
            WriteText(local, droid.ToString(Formatting.None));
        return changed;
    }

    /// <summary>
    /// Makes an item node the blueprint's item: every blueprint field, except the fields an item keeps, which stay as
    /// they were, plus its crafted Module and Starship Bonus properties.
    /// </summary>
    internal static void RewriteItem(Node item, Node blueprint)
    {
        var bonuses = item.Fields.SingleOrDefault(x => x.Name == "PropertiesList" && x.Type == 15)?.Children
            .Where(property => ReadInteger(property, "PropertyName") is (int)ItemPropertyType.ModuleBonus or (int)ItemPropertyType.StarshipBonus)
            .Select(CloneNode).ToList() ?? new List<Node>();
        var fields = new List<Field>();
        var blueprintNames = new HashSet<string>();
        foreach (var field in blueprint.Fields)
        {
            blueprintNames.Add(field.Name);
            if (KeptItemFields.Contains(field.Name))
            {
                var kept = item.Fields.FirstOrDefault(x => x.Name == field.Name);
                if (kept != null && field.Name == "VarTable" && kept.Type == 15 && field.Type == 15)
                {
                    // The item's locals stay, and the blueprint's own locals are added.
                    var names = kept.Children.Select(ReadVariableName).ToHashSet();
                    kept.Children.AddRange(field.Children.Where(x => !names.Contains(ReadVariableName(x))).Select(CloneNode));
                    fields.Add(kept);
                }
                else if (kept != null)
                    fields.Add(kept);
                // Two items must never share a UUID: a new UUID is assigned when the item loads.
                else if (field.Name != "UUID")
                    fields.Add(CloneField(field));
                continue;
            }
            fields.Add(CloneField(field));
        }
        fields.AddRange(item.Fields.Where(x => KeptItemFields.Contains(x.Name) && !blueprintNames.Contains(x.Name)));

        var properties = fields.SingleOrDefault(x => x.Name == "PropertiesList" && x.Type == 15);
        if (properties == null && bonuses.Count > 0)
        {
            properties = MakeField(15, "PropertiesList", children: new List<Node>());
            fields.Add(properties);
        }
        if (properties != null)
        {
            properties.Children.AddRange(bonuses);
            NumberListElements(properties);
        }
        item.Fields = fields;
    }

    /// <summary>Saved-data <see cref="Service.Item.GetRequiredSkills"/>: the highest Requires Skill rank per skill, or null.</summary>
    internal static Dictionary<SkillType, int> ReadRequiredSkills(string data)
    {
        Dictionary<SkillType, int> required = null;
        var properties = ParseItem(data).Fields.SingleOrDefault(x => x.Name == "PropertiesList" && x.Type == 15);
        foreach (var property in properties?.Children ?? new List<Node>())
        {
            if (ReadInteger(property, "PropertyName") != (int)ItemPropertyType.RequiresSkill)
                continue;
            var skill = (SkillType)ReadInteger(property, "Subtype");
            var rank = ReadInteger(property, "CostValue");
            required ??= new Dictionary<SkillType, int>();
            if (!required.TryGetValue(skill, out var existing) || rank > existing)
                required[skill] = rank;
        }
        return required;
    }

    /// <summary>A saved item's resref, tag and first localized name.</summary>
    internal static (string Resref, string Tag, string Name) ReadItemIdentity(string data)
    {
        var item = ParseItem(data);
        var tag = item.Fields.SingleOrDefault(x => x.Name == "Tag" && x.Type == 10);
        var tagText = tag == null ? string.Empty : System.Text.Encoding.ASCII.GetString(tag.Data, 4, tag.Data.Length - 4);
        return (ReadResRef(item) ?? string.Empty, tagText, ReadLocalizedName(item));
    }

    private static Node CloneNode(Node node) => new()
    {
        Type = node.Type,
        Fields = node.Fields.Select(CloneField).ToList(),
    };

    private static Field CloneField(Field field) => new()
    {
        Type = field.Type,
        Label = field.Label.ToArray(),
        Data = field.Data?.ToArray(),
        Children = field.Children?.Select(CloneNode).ToList(),
    };
}
