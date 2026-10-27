using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Domain.Administration
{
    /// <summary>
    /// Builds the icons, one-line summaries and details the DM chooser and creator show: the logic and state that used
    /// to be static in <c>Service.DMObjectDetails</c>, moved here unchanged. The facade forwards every call to the
    /// container's instance and repeats <see cref="NoSelectionIcon"/> and <see cref="AreaIcon"/>.
    /// </summary>
    public class DMObjectDetailsService : IDMObjectDetailsService
    {
        /// <summary>Shown in the details panel when nothing is selected. <c>Service.DMObjectDetails.NoSelectionIcon</c> repeats it.</summary>
        public const string NoSelectionIcon = "dm_examine";

        /// <summary>Shown for areas, which have no portrait. <c>Service.DMObjectDetails.AreaIcon</c> repeats it.</summary>
        public const string AreaIcon = "dm_goto";

        private const string TemporaryStorageTag = "TEMP_ITEM_STORAGE";
        private const string LootTablePrefix = "LOOT_TABLE_";
        private const string ListPortraitSize = "s";
        private const string DetailPortraitSize = "l";
        private const int CreatureArmorSlotStructId = 1 << 17;

        private readonly Dictionary<(DMBlueprintCategory, string), (string Icon, string Summary)> _blueprintRows = new();
        private List<string> _factionNames;

        private readonly IDMBlueprintCatalogService _catalog;

        public DMObjectDetailsService(IDMBlueprintCatalogService catalog)
        {
            _catalog = catalog;
        }

        public (string Icon, string Summary) GetBlueprintRow(DMBlueprint blueprint)
        {
            var key = (blueprint.Category, blueprint.Resref.ToLowerInvariant());
            if (_blueprintRows.TryGetValue(key, out var row))
                return row;

            row = ReadBlueprintRow(blueprint);
            row.Icon = WithFallbackIcon(row.Icon, _catalog.GetObjectType(blueprint.Category));
            _blueprintRows[key] = row;
            return row;
        }

        public DMDetails DescribeBlueprint(DMBlueprint blueprint)
        {
            var objectType = _catalog.GetObjectType(blueprint.Category);
            var template = _catalog.ReadTemplate(blueprint);
            if (JsonGetType(template) != JsonType.Object)
                return new DMDetails(WithFallbackIcon(string.Empty, objectType), blueprint.Name, $"The {blueprint.Resref} template could not be read.");

            var lines = blueprint.Category switch
            {
                DMBlueprintCategory.Creatures => DescribeCreatureTemplate(blueprint, template),
                DMBlueprintCategory.Items => DescribeItemBlueprint(blueprint),
                DMBlueprintCategory.Placeables => DescribePlaceableTemplate(template),
                DMBlueprintCategory.Stores => DescribeStoreTemplate(template),
                DMBlueprintCategory.Triggers => DescribeTriggerTemplate(template),
                DMBlueprintCategory.Encounters => DescribeEncounterTemplate(template),
                _ => DescribeWaypointTemplate(template)
            };

            lines.Add(string.Empty);
            lines.Add($"Palette: {blueprint.Source} / {blueprint.CategoryPath}");
            lines.Add($"Resref: {blueprint.Resref}   Tag: {DMTemplateFields.ReadString(template, "Tag")}");

            var icon = WithFallbackIcon(GetBlueprintIcon(blueprint, template, DetailPortraitSize), objectType);
            return new DMDetails(icon, blueprint.Name, string.Join("\n", lines));
        }

        public (string Icon, string Summary) GetObjectRow(uint obj)
        {
            var row = ReadObjectRow(obj);
            row.Icon = WithFallbackIcon(row.Icon, GetObjectType(obj));
            return row;
        }

        private (string Icon, string Summary) ReadObjectRow(uint obj)
        {
            switch (GetObjectType(obj))
            {
                case ObjectType.Creature:
                    var stats = Stat.GetNPCStats(obj);
                    var level = stats.Level > 0 ? stats.Level : GetHitDice(obj);
                    return (GetObjectPortrait(obj, ListPortraitSize), $"Lv {level} · HP {GetCurrentHitPoints(obj)}/{GetMaxHitPoints(obj)}");
                case ObjectType.Item:
                    return (Item.GetIconResref(obj), GetBaseItemName((int)GetBaseItemType(obj)));
                case ObjectType.Placeable:
                    return (GetObjectPortrait(obj, ListPortraitSize), JoinFlags(
                        (GetHasInventory(obj), "Container"),
                        (GetUseableFlag(obj), "Useable"),
                        (GetLocked(obj), "Locked"),
                        (GetPlotFlag(obj), "Plot")));
                case ObjectType.Door:
                    return (string.Empty, JoinFlags((GetIsOpen(obj), "Open"), (GetLocked(obj), "Locked"), (GetPlotFlag(obj), "Plot")));
                default:
                    return (string.Empty, string.Empty);
            }
        }

        public DMDetails DescribeObject(uint obj)
        {
            if (!GetIsObjectValid(obj))
                return new DMDetails(NoSelectionIcon, "Gone", "That object no longer exists.");

            var lines = GetObjectType(obj) switch
            {
                ObjectType.Creature => DescribeCreature(obj),
                ObjectType.Item => DescribeItem(obj),
                ObjectType.Placeable => DescribePlaceable(obj),
                ObjectType.Door => DescribeDoor(obj),
                ObjectType.Trigger => DescribeTransition(obj),
                ObjectType.Store => DescribeStore(obj),
                ObjectType.Encounter => new List<string>
                {
                    GetEncounterActive(obj) != 0 ? "Active" : "Inactive",
                    $"Spawned {GetEncounterSpawnsCurrent(obj)} of {GetEncounterSpawnsMax(obj)}"
                },
                _ => new List<string>()
            };

            lines.Add(string.Empty);
            lines.Add($"Resref: {GetResRef(obj)}   Tag: {GetTag(obj)}");
            if (GetObjectType(obj) != ObjectType.Item || GetIsObjectValid(GetArea(obj)))
                lines.Add($"Area: {GetName(GetArea(obj))}");

            var icon = GetObjectType(obj) == ObjectType.Item
                ? Item.GetIconResref(obj)
                : GetObjectPortrait(obj, DetailPortraitSize);
            return new DMDetails(WithFallbackIcon(icon, GetObjectType(obj)), GetName(obj), string.Join("\n", lines));
        }

        private (string Icon, string Summary) ReadBlueprintRow(DMBlueprint blueprint)
        {
            var leafCategory = blueprint.CategoryPath.Split(DMPaletteReader.PathSeparator).LastOrDefault() ?? string.Empty;
            if (blueprint.Category == DMBlueprintCategory.Items)
            {
                var itemTemplate = _catalog.ReadTemplate(blueprint);
                var baseItem = DMTemplateFields.ReadInt(itemTemplate, "BaseItem", -1);
                return (Cache.GetItemIconByResref(blueprint.Resref), baseItem >= 0 ? GetBaseItemName(baseItem) : leafCategory);
            }

            if (blueprint.Category is DMBlueprintCategory.Stores or DMBlueprintCategory.Waypoints
                or DMBlueprintCategory.Triggers or DMBlueprintCategory.Encounters)
                return (string.Empty, leafCategory);

            var template = _catalog.ReadTemplate(blueprint);
            if (JsonGetType(template) != JsonType.Object)
                return (string.Empty, leafCategory);

            var icon = GetBlueprintIcon(blueprint, template, ListPortraitSize);
            if (blueprint.Category == DMBlueprintCategory.Placeables)
            {
                return (icon, JoinFlags(
                    (DMTemplateFields.ReadInt(template, "HasInventory") != 0, "Container"),
                    (DMTemplateFields.ReadInt(template, "Useable") != 0, "Useable"),
                    (DMTemplateFields.ReadInt(template, "Locked") != 0, "Locked"),
                    (DMTemplateFields.ReadInt(template, "Plot") != 0, "Plot")));
            }

            var level = ReadSkinLevel(template);
            if (level <= 0)
                level = ReadClassLevels(template);
            var parts = new List<string> { $"Lv {level}" };
            if (!string.IsNullOrWhiteSpace(blueprint.Faction))
                parts.Add(blueprint.Faction);
            return (icon, string.Join(" · ", parts));
        }

        private List<string> DescribeCreatureTemplate(DMBlueprint blueprint, Json template)
        {
            var lines = new List<string>();
            var skinResref = FindSkinResref(template);
            var stats = new NPCStats();
            var skinHP = 0;
            if (!string.IsNullOrEmpty(skinResref))
            {
                WithTemporaryItem(skinResref, skin =>
                {
                    stats = Stat.GetNPCSkinStats(skin);
                    skinHP = Stat.GetNPCSkinMaxHP(skin);
                });
            }

            var level = stats.Level > 0 ? stats.Level : ReadClassLevels(template);
            var hp = skinHP > 0 ? skinHP : DMTemplateFields.ReadInt(template, "MaxHitPoints");
            var faction = !string.IsNullOrWhiteSpace(blueprint.Faction)
                ? blueprint.Faction
                : GetFactionName(DMTemplateFields.ReadInt(template, "FactionID", -1));

            lines.Add($"Level {level}   Faction: {faction}");
            AddCombatStats(lines, hp, stats);
            lines.Add(FormatAbilities(
                DMTemplateFields.ReadInt(template, "Str"),
                DMTemplateFields.ReadInt(template, "Dex"),
                DMTemplateFields.ReadInt(template, "Con"),
                DMTemplateFields.ReadInt(template, "Int"),
                DMTemplateFields.ReadInt(template, "Wis"),
                DMTemplateFields.ReadInt(template, "Cha")));
            lines.Add($"Race: {GetRaceName(DMTemplateFields.ReadInt(template, "Race", -1))}   Gender: {GetGenderName(DMTemplateFields.ReadInt(template, "Gender", -1))}");
            AddFlags(lines,
                (DMTemplateFields.ReadInt(template, "Plot") != 0, "Plot"),
                (DMTemplateFields.ReadInt(template, "IsImmortal") != 0, "Immortal"));

            var conversation = DMTemplateFields.ReadString(template, "Conversation");
            if (!string.IsNullOrWhiteSpace(conversation))
                lines.Add($"Conversation: {conversation}");

            var lootTables = ReadTemplateVariables(template, LootTablePrefix);
            if (lootTables.Count > 0)
                lines.Add($"Loot: {string.Join(", ", lootTables.Select(FormatLootTable))}");

            AddDescription(lines, DMTemplateFields.ReadLocString(template, "Description"));
            return lines;
        }

        private List<string> DescribeCreature(uint creature)
        {
            var lines = new List<string>();
            var stats = Stat.GetNPCStats(creature);
            var level = stats.Level > 0 ? stats.Level : GetHitDice(creature);
            lines.Add($"Level {level}   Faction: {GetFactionName(CreaturePlugin.GetFaction(creature))}");
            lines.Add($"HP {GetCurrentHitPoints(creature)} / {GetMaxHitPoints(creature)}");
            AddCombatStats(lines, 0, stats);
            lines.Add(FormatAbilities(
                GetAbilityScore(creature, AbilityType.Might),
                GetAbilityScore(creature, AbilityType.Perception),
                GetAbilityScore(creature, AbilityType.Vitality),
                GetAbilityScore(creature, AbilityType.Agility),
                GetAbilityScore(creature, AbilityType.Willpower),
                GetAbilityScore(creature, AbilityType.Social)));
            lines.Add($"Race: {GetRaceName((int)GetRacialType(creature))}   Gender: {GetGenderName((int)GetGender(creature))}");
            lines.Add($"AI: {AI.GetAIProfile(creature)}");
            AddFlags(lines, (GetPlotFlag(creature), "Plot"), (GetImmortal(creature), "Immortal"), (GetIsDMPossessed(creature), "DM possessed"));

            var lootTables = ReadLocalVariables(creature, LootTablePrefix);
            if (lootTables.Count > 0)
                lines.Add($"Loot: {string.Join(", ", lootTables.Select(FormatLootTable))}");

            AddDescription(lines, GetDescription(creature));
            return lines;
        }

        private List<string> DescribeItemBlueprint(DMBlueprint blueprint)
        {
            var lines = new List<string>();
            if (!WithTemporaryItem(blueprint.Resref, item => lines.AddRange(DescribeItem(item))))
                lines.Add("The item could not be created for inspection.");
            return lines;
        }

        private List<string> DescribeItem(uint item)
        {
            var lines = new List<string> { GetBaseItemName((int)GetBaseItemType(item)) };
            var hasDMG = false;
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.DMG)
                    hasDMG = true;
            }

            if (hasDMG)
                lines.Add($"DMG {Item.GetDMG(item)}");

            lines.Add($"Weight {(GetWeight(item) / 10.0).ToString("0.0", CultureInfo.InvariantCulture)} lbs");
            lines.Add($"Value {GetGoldPieceValue(item)} credits");
            if (Item.IsEconomyRestricted(item))
                lines.Add("NPC-only: kept out of player markets and searches");

            var properties = Item.BuildItemPropertyString(item);
            if (!string.IsNullOrWhiteSpace(properties))
            {
                lines.Add(string.Empty);
                lines.Add(properties.TrimEnd());
            }

            AddDescription(lines, GetDescription(item, false, true));
            return lines;
        }

        private List<string> DescribePlaceableTemplate(Json template)
        {
            var lines = new List<string> { $"Appearance: {GetPlaceableAppearanceName(DMTemplateFields.ReadInt(template, "Appearance", -1))}" };
            AddFlags(lines,
                (DMTemplateFields.ReadInt(template, "Useable") != 0, "Useable"),
                (DMTemplateFields.ReadInt(template, "HasInventory") != 0, "Container"),
                (DMTemplateFields.ReadInt(template, "Locked") != 0, "Locked"),
                (DMTemplateFields.ReadInt(template, "Plot") != 0, "Plot"),
                (DMTemplateFields.ReadInt(template, "Static") != 0, "Static"));
            lines.Add($"HP {DMTemplateFields.ReadInt(template, "HP")}");

            var conversation = DMTemplateFields.ReadString(template, "Conversation");
            if (!string.IsNullOrWhiteSpace(conversation))
                lines.Add($"Conversation: {conversation}");

            var lootTables = ReadTemplateVariables(template, LootTablePrefix);
            if (lootTables.Count > 0)
                lines.Add($"Loot: {string.Join(", ", lootTables.Select(FormatLootTable))}");

            AddDescription(lines, DMTemplateFields.ReadLocString(template, "Description"));
            return lines;
        }

        private List<string> DescribePlaceable(uint placeable)
        {
            var lines = new List<string>();
            AddFlags(lines,
                (GetUseableFlag(placeable), "Useable"),
                (GetHasInventory(placeable), "Container"),
                (GetLocked(placeable), "Locked"),
                (GetPlotFlag(placeable), "Plot"));
            lines.Add($"HP {GetCurrentHitPoints(placeable)} / {GetMaxHitPoints(placeable)}");

            if (GetLocalInt(placeable, "DM_SPAWNED_PLACEABLE") != 0)
                lines.Add($"Spawned by DM {GetLocalString(placeable, "DM_SPAWNED_BY_NAME")}");

            var lootTables = ReadLocalVariables(placeable, LootTablePrefix);
            if (lootTables.Count > 0)
                lines.Add($"Loot: {string.Join(", ", lootTables.Select(FormatLootTable))}");

            AddDescription(lines, GetDescription(placeable));
            return lines;
        }

        private List<string> DescribeDoor(uint door)
        {
            var lines = new List<string>();
            AddFlags(lines, (GetIsOpen(door), "Open"), (GetLocked(door), "Locked"), (GetPlotFlag(door), "Plot"));
            if (GetLockKeyRequired(door))
                lines.Add($"Key required: {GetLockKeyTag(door)}");
            lines.AddRange(DescribeTransition(door));
            return lines;
        }

        private List<string> DescribeTransition(uint transition)
        {
            var lines = new List<string>();
            var target = GetTransitionTarget(transition);
            if (GetIsObjectValid(target))
                lines.Add($"Leads to: {GetName(GetArea(target))} ({GetTag(target)})");
            return lines;
        }

        private List<string> DescribeStoreTemplate(Json template)
        {
            var itemCount = 0;
            var categories = DMTemplateFields.ReadList(template, "StoreList");
            for (var index = 0; index < JsonGetLength(categories); index++)
            {
                itemCount += JsonGetLength(DMTemplateFields.ReadList(JsonArrayGet(categories, index), "ItemList"));
            }

            var gold = DMTemplateFields.ReadInt(template, "StoreGold", -1);
            return new List<string>
            {
                $"Sells {itemCount} item{(itemCount == 1 ? string.Empty : "s")}",
                $"Mark-up {DMTemplateFields.ReadInt(template, "MarkUp")}%   Mark-down {DMTemplateFields.ReadInt(template, "MarkDown")}%",
                $"Gold: {(gold < 0 ? "unlimited" : gold.ToString(CultureInfo.InvariantCulture))}"
            };
        }

        private List<string> DescribeStore(uint store)
        {
            var itemCount = 0;
            for (var item = GetFirstItemInInventory(store); GetIsObjectValid(item); item = GetNextItemInInventory(store))
            {
                itemCount++;
            }

            var gold = GetStoreGold(store);
            return new List<string>
            {
                $"Sells {itemCount} item{(itemCount == 1 ? string.Empty : "s")}",
                $"Gold: {(gold < 0 ? "unlimited" : gold.ToString(CultureInfo.InvariantCulture))}   Max buy price: {GetStoreMaxBuyPrice(store)}"
            };
        }

        private List<string> DescribeTriggerTemplate(Json template)
        {
            var lines = new List<string>
            {
                DMTemplateFields.ReadInt(template, "Type") switch
                {
                    1 => "Area transition",
                    2 => "Trap",
                    _ => "Generic trigger"
                }
            };

            if (DMTemplateFields.ReadInt(template, "TrapFlag") != 0)
            {
                lines.Add($"Detect DC {DMTemplateFields.ReadInt(template, "TrapDetectDC")}   Disarm DC {DMTemplateFields.ReadInt(template, "DisarmDC")}");
                lines.Add(DMTemplateFields.ReadInt(template, "TrapOneShot") != 0 ? "Fires once" : "Fires every time");
            }

            var linkedTo = DMTemplateFields.ReadString(template, "LinkedTo");
            if (!string.IsNullOrWhiteSpace(linkedTo))
                lines.Add($"Leads to: {linkedTo}");

            var onEnter = DMTemplateFields.ReadString(template, "ScriptOnEnter");
            if (!string.IsNullOrWhiteSpace(onEnter))
                lines.Add($"On enter: {onEnter}");

            lines.Add($"Created as a {_catalog.GetSquareSize(DMBlueprintCategory.Triggers):0} m square.");
            return lines;
        }

        private List<string> DescribeEncounterTemplate(Json template)
        {
            var lines = new List<string>
            {
                $"Up to {DMTemplateFields.ReadInt(template, "MaxCreatures")} creatures, {DMTemplateFields.ReadInt(template, "RecCreatures")} recommended",
                DMTemplateFields.ReadInt(template, "SpawnOption") == 1 ? "Spawns once" : "Spawns continuously"
            };
            AddFlags(lines,
                (DMTemplateFields.ReadInt(template, "Active") == 0, "Inactive"),
                (DMTemplateFields.ReadInt(template, "PlayerOnly") != 0, "Players only"));

            var creatures = DMTemplateFields.ReadList(template, "CreatureList");
            var resrefs = new List<string>();
            for (var index = 0; index < JsonGetLength(creatures); index++)
            {
                var resref = DMTemplateFields.ReadString(JsonArrayGet(creatures, index), "ResRef");
                if (!string.IsNullOrWhiteSpace(resref))
                    resrefs.Add(resref);
            }

            if (resrefs.Count > 0)
                lines.Add($"Creatures: {string.Join(", ", resrefs)}");

            lines.Add($"Created as a {_catalog.GetSquareSize(DMBlueprintCategory.Encounters):0} m square.");
            return lines;
        }

        private List<string> DescribeWaypointTemplate(Json template)
        {
            var lines = new List<string>();
            if (DMTemplateFields.ReadInt(template, "HasMapNote") != 0)
                lines.Add($"Map note: {DMTemplateFields.ReadLocString(template, "MapNote")}");
            AddDescription(lines, DMTemplateFields.ReadLocString(template, "Description"));
            return lines;
        }

        public int CountPlayersInArea(uint area)
        {
            return Area.GetPlayersInArea(area).Count(player => !GetIsDM(player) && !GetIsDMPossessed(player));
        }

        public DMDetails DescribeArea(uint area)
        {
            var players = CountPlayersInArea(area);
            var lines = new List<string>
            {
                $"{players} player{(players == 1 ? string.Empty : "s")} here",
                $"Size: {GetAreaSize(Dimension.Width, area)} x {GetAreaSize(Dimension.Height, area)} tiles",
                string.Empty,
                $"Resref: {GetResRef(area)}   Tag: {GetTag(area)}"
            };
            return new DMDetails(AreaIcon, GetName(area), string.Join("\n", lines));
        }

        private void AddCombatStats(List<string> lines, int hp, NPCStats stats)
        {
            var vitals = new List<string>();
            if (hp > 0)
                vitals.Add($"HP {hp}");
            if (stats.FP > 0)
                vitals.Add($"FP {stats.FP}");
            if (stats.Stamina > 0)
                vitals.Add($"STM {stats.Stamina}");
            if (vitals.Count > 0)
                lines.Add(string.Join("   ", vitals));

            if (stats.Attack > 0 || stats.ForceAttack > 0 || stats.Accuracy > 0)
                lines.Add($"Attack {stats.Attack}   Force Attack {stats.ForceAttack}   Accuracy {stats.Accuracy}");

            var defenses = stats.Defenses
                .Where(defense => defense.Value != 0)
                .Select(defense => $"{defense.Key} {defense.Value}")
                .ToList();
            if (defenses.Count > 0 || stats.Evasion > 0)
                lines.Add($"Defense: {(defenses.Count > 0 ? string.Join(", ", defenses) : "none")}   Evasion {stats.Evasion}");

            var resistances = stats.Resistances
                .Where(resistance => resistance.Value != 0)
                .Select(resistance => $"{resistance.Key} {resistance.Value}")
                .ToList();
            if (resistances.Count > 0)
                lines.Add($"Resistances: {string.Join(", ", resistances)}");
        }

        private string FormatAbilities(int might, int perception, int vitality, int agility, int willpower, int social)
        {
            return $"MGT {might}   PER {perception}   VIT {vitality}\nAGI {agility}   WIL {willpower}   SOC {social}";
        }

        private void AddFlags(List<string> lines, params (bool IsSet, string Label)[] flags)
        {
            var text = JoinFlags(flags);
            if (!string.IsNullOrEmpty(text))
                lines.Add(text);
        }

        private string JoinFlags(params (bool IsSet, string Label)[] flags)
        {
            return string.Join(" · ", flags.Where(flag => flag.IsSet).Select(flag => flag.Label));
        }

        private void AddDescription(List<string> lines, string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return;

            lines.Add(string.Empty);
            lines.Add(UtilPlugin.StripColors(description).Trim());
        }

        private string FormatLootTable(string value)
        {
            var parts = value.Split(',');
            var table = parts[0].Trim();
            return parts.Length > 1 && int.TryParse(parts[1].Trim(), out var chance)
                ? $"{table} ({chance}%)"
                : table;
        }

        // Creates an item in the no-access storage container, runs the action on it and destroys it.
        // Creature items (skins, claws) take no inventory space, so they go on the ground beside the container.
        private bool WithTemporaryItem(string resref, Action<uint> action)
        {
            var storage = GetObjectByTag(TemporaryStorageTag);
            if (!GetIsObjectValid(storage))
                return false;

            var item = CreateItemOnObject(resref, storage);
            if (!GetIsObjectValid(item))
                item = CreateObject(ObjectType.Item, resref, GetLocation(storage));
            if (!GetIsObjectValid(item))
                return false;

            try
            {
                action(item);
            }
            finally
            {
                DestroyObject(item);
            }

            return true;
        }

        private string FindSkinResref(Json template)
        {
            var equipped = DMTemplateFields.ReadList(template, "Equip_ItemList");
            for (var index = 0; index < JsonGetLength(equipped); index++)
            {
                var entry = JsonArrayGet(equipped, index);
                if (JsonGetInt(JsonObjectGet(entry, "__struct_id")) == CreatureArmorSlotStructId)
                    return DMTemplateFields.ReadString(entry, "EquippedRes");
            }

            return string.Empty;
        }

        // The NPC level property is read straight from the skin template so listing a page creates nothing.
        private int ReadSkinLevel(Json creatureTemplate)
        {
            var skinResref = FindSkinResref(creatureTemplate);
            if (string.IsNullOrEmpty(skinResref))
                return 0;

            var properties = DMTemplateFields.ReadList(TemplateToJson(skinResref, ResType.UTI), "PropertiesList");
            for (var index = 0; index < JsonGetLength(properties); index++)
            {
                var property = JsonArrayGet(properties, index);
                if (DMTemplateFields.ReadInt(property, "PropertyName", -1) == (int)ItemPropertyType.NPCLevel)
                    return DMTemplateFields.ReadInt(property, "CostValue");
            }

            return 0;
        }

        private int ReadClassLevels(Json creatureTemplate)
        {
            var classes = DMTemplateFields.ReadList(creatureTemplate, "ClassList");
            var levels = 0;
            for (var index = 0; index < JsonGetLength(classes); index++)
            {
                levels += DMTemplateFields.ReadInt(JsonArrayGet(classes, index), "ClassLevel");
            }

            return levels;
        }

        private List<string> ReadTemplateVariables(Json template, string prefix)
        {
            var values = new List<string>();
            var variables = DMTemplateFields.ReadList(template, "VarTable");
            for (var index = 0; index < JsonGetLength(variables); index++)
            {
                var variable = JsonArrayGet(variables, index);
                var name = DMTemplateFields.ReadString(variable, "Name");
                var value = DMTemplateFields.ReadString(variable, "Value");
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(value))
                    values.Add(value);
            }

            return values;
        }

        private List<string> ReadLocalVariables(uint obj, string prefix)
        {
            var values = new List<string>();
            var count = ObjectPlugin.GetLocalVariableCount(obj);
            for (var index = 0; index < count; index++)
            {
                var variable = ObjectPlugin.GetLocalVariable(obj, index);
                if (variable.Type != LocalVariableType.String ||
                    !variable.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                var value = GetLocalString(obj, variable.Key);
                if (!string.IsNullOrWhiteSpace(value))
                    values.Add(value);
            }

            return values;
        }

        private string GetBlueprintIcon(DMBlueprint blueprint, Json template, string portraitSize)
        {
            if (blueprint.Category == DMBlueprintCategory.Items)
                return Cache.GetItemIconByResref(blueprint.Resref);

            if (blueprint.Category is not (DMBlueprintCategory.Creatures or DMBlueprintCategory.Placeables))
                return string.Empty;

            var portraitId = DMTemplateFields.ReadInt(template, "PortraitId");
            if (portraitId > 0 && portraitId < ushort.MaxValue)
                return GetPortrait(Get2DAString("portraits", "BaseResRef", portraitId), portraitSize);

            return GetPortrait(DMTemplateFields.ReadString(template, "Portrait"), portraitSize);
        }

        private string GetObjectPortrait(uint obj, string size)
        {
            var portrait = GetPortraitResRef(obj);
            return string.IsNullOrWhiteSpace(portrait) ? string.Empty : portrait + size;
        }

        // Base-game DM and radial-menu icons stand in for objects without a portrait or icon.
        private string WithFallbackIcon(string icon, ObjectType type)
        {
            if (!string.IsNullOrWhiteSpace(icon))
                return icon;

            return type switch
            {
                ObjectType.Creature => "dm_ai",
                ObjectType.Item => "dm_item",
                ObjectType.Placeable => "dm_placeable",
                ObjectType.Door => "ir_open",
                ObjectType.Trigger => "dm_trigger",
                ObjectType.Waypoint => "ir_moveto",
                ObjectType.Store => "ir_buy",
                ObjectType.Encounter => "dm_encounter",
                _ => NoSelectionIcon
            };
        }

        // Portrait resrefs are "po_" + the portraits.2da base name + a size letter.
        private string GetPortrait(string baseResref, string size)
        {
            if (string.IsNullOrWhiteSpace(baseResref) || baseResref == "****")
                return string.Empty;

            var portrait = baseResref.StartsWith("po_", StringComparison.OrdinalIgnoreCase)
                ? baseResref
                : "po_" + baseResref;
            return portrait + size;
        }

        private string GetBaseItemName(int baseItem)
        {
            return Get2DAName("baseitems", "Name", baseItem, "Item");
        }

        private string GetRaceName(int race)
        {
            return Get2DAName("racialtypes", "Name", race, "Unknown");
        }

        private string GetPlaceableAppearanceName(int appearance)
        {
            var name = Get2DAName("placeables", "StrRef", appearance, string.Empty);
            return string.IsNullOrWhiteSpace(name)
                ? Get2DAString("placeables", "Label", appearance)
                : name;
        }

        private string Get2DAName(string twoDA, string column, int row, string fallback)
        {
            if (row < 0)
                return fallback;

            return int.TryParse(Get2DAString(twoDA, column, row), out var strRef) && strRef > 0
                ? GetStringByStrRef(strRef)
                : fallback;
        }

        private string GetGenderName(int gender)
        {
            return gender switch
            {
                (int)Gender.Male => "Male",
                (int)Gender.Female => "Female",
                _ => "None"
            };
        }

        private string GetFactionName(int factionId)
        {
            if (_factionNames == null)
            {
                _factionNames = new List<string>();
                var factions = DMTemplateFields.ReadList(TemplateToJson("repute", ResType.FAC), "FactionList");
                for (var index = 0; index < JsonGetLength(factions); index++)
                {
                    _factionNames.Add(DMTemplateFields.ReadString(JsonArrayGet(factions, index), "FactionName"));
                }
            }

            return factionId >= 0 && factionId < _factionNames.Count
                ? _factionNames[factionId]
                : $"Faction {factionId}";
        }
    }
}
