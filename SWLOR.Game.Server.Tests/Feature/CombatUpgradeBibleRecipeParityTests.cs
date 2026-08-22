using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Permanent regression guard enforcing parity between the crafting recipes documented in
/// the "SWLOR Design Bible - Combat Upgrade" workbook and the recipes the game actually
/// registers at runtime (every <see cref="IRecipeListDefinition"/> in
/// SWLOR.Game.Server.Feature.RecipeDefinition).
/// </summary>
public class CombatUpgradeBibleRecipeParityTests
{
    private static readonly string[] RecipeWorksheetNames =
    {
        "Smithery Recipes",
        "Engineering Recipes",
        "Cooking Recipes",
        "Fabrication Recipes",
    };

    /// <summary>
    /// Recipes whose Bible rows do not yet match the live recipe registry because a design
    /// decision about them is still pending. These rows are skipped by the field-by-field
    /// parity assertions (they still must exist in the registry). Remove entries here as the
    /// pending design decisions are resolved and the workbook is reconciled with the code.
    /// </summary>
    private static readonly HashSet<RecipeType> PendingDesignDecisionExclusions = new()
    {
        RecipeType.CarpetMedallion,
        RecipeType.CityHallStyle1,
        RecipeType.CoffeeMaker,
        RecipeType.CouchLeatherPanelsGrey,
        RecipeType.MetalWallSinglePanels,
        RecipeType.MunchFungusBread,
        RecipeType.Missile25,
        RecipeType.Missile35,
        RecipeType.CombatBoots,
        RecipeType.ForzaBoots,
        RecipeType.SupremeBoots,
        RecipeType.SurvivalBoots,
        RecipeType.ValorBoots,
        RecipeType.TrainingSaber1,
        RecipeType.TrainingSaber2,
        RecipeType.TrainingSaber3,
        RecipeType.TrainingSaber4,
        RecipeType.TrainingSaber5,
    };

    /// <summary>
    /// Live recipes that are intentionally not documented in the Bible recipe tabs. These are
    /// the crafting-skill submission-token recipes used by the item submission flow.
    /// </summary>
    private static readonly HashSet<RecipeType> UndocumentedLiveRecipeExceptions = new()
    {
        RecipeType.ArmorSubmissionTokenSmithery,
        RecipeType.ArmorSubmissionTokenEngineering,
        RecipeType.WeaponSubmissionTokenSmithery,
        RecipeType.WeaponSubmissionTokenEngineering,
        RecipeType.FoodSubmissionTokenAgriculture,
    };

    /// <summary>
    /// Legacy grouped droid-boost recipes documented in the Bible that have no live code
    /// counterpart. These are the pre-rework grouped weapon-skill boosts that were replaced by
    /// per-weapon droid boosts.
    /// </summary>
    private static readonly HashSet<string> LegacyGroupedDroidBoostRows = new(StringComparer.Ordinal)
    {
        "MartialArtsBoost1",
        "MartialArtsBoost2",
        "OneHandedBoost1",
        "OneHandedBoost2",
        "TwoHandedBoost1",
        "TwoHandedBoost2",
        "RangedBoost1",
        "RangedBoost2",
    };

    /// <summary>
    /// Legacy starship-missile ammo tiers documented in the Bible that have no live code
    /// counterpart (superseded by the current missile tiers).
    /// </summary>
    private static readonly HashSet<string> LegacyMissileTierRows = new(StringComparer.Ordinal)
    {
        "Missile5",
        "Missile15",
        "Missile45",
    };

    private static readonly Regex Droid005VariantPattern =
        new(@"^D[A-Z]{3}00\d[AC]$", RegexOptions.Compiled);

    [Test]
    public void RecipeBibleRows_MatchLiveRecipeRegistry()
    {
        var registry = BuildLiveRecipeRegistry();
        var workbookRows = ReadWorkbookRecipeRows();

        var mismatches = new List<string>();

        foreach (var row in workbookRows.Values.OrderBy(r => r.Enum, StringComparer.Ordinal))
        {
            if (!TryResolveRecipeType(row.Enum, out var recipeType))
                continue;

            if (!registry.TryGetValue(recipeType, out var detail))
                continue;

            if (PendingDesignDecisionExclusions.Contains(recipeType))
                continue;

            void Compare(string field, object expected, object actual)
            {
                if (!Equals(expected, actual))
                    mismatches.Add($"[{row.Worksheet} row {row.RowNumber}] {row.Enum}.{field}: Bible='{expected}' vs code='{actual}'");
            }

            Compare("Skill", row.Skill, detail.Skill.ToString());
            Compare("Category", row.Category, detail.Category.ToString());
            Compare("Resref", row.Resref, detail.Resref);
            Compare("Level", row.Level, detail.Level);
            Compare("Quantity", row.Quantity, detail.Quantity);
            Compare("EnhancementType", row.EnhancementType, detail.EnhancementType);
            Compare("EnhancementSlots", row.EnhancementSlots, detail.EnhancementSlots);

            if (!ComponentsEqual(row.Components, detail.Components))
            {
                mismatches.Add(
                    $"[{row.Worksheet} row {row.RowNumber}] {row.Enum}.Components: " +
                    $"Bible={FormatComponents(row.Components)} vs code={FormatComponents(detail.Components)}");
            }
        }

        mismatches.Should().BeEmpty(
            "the Design Bible recipe tabs must match the live recipe registry. Mismatches:\n" +
            string.Join("\n", mismatches));
    }

    [Test]
    public void RecipeBibleRows_WithoutCodeCounterparts_AreKnownUnimplemented()
    {
        var registry = BuildLiveRecipeRegistry();
        var workbookRows = ReadWorkbookRecipeRows();

        var unexpectedOrphans = new List<string>();

        foreach (var enumName in workbookRows.Keys.OrderBy(name => name, StringComparer.Ordinal))
        {
            if (TryResolveRecipeType(enumName, out var recipeType) && registry.ContainsKey(recipeType))
                continue;

            if (IsKnownUnimplementedBibleRow(enumName))
                continue;

            unexpectedOrphans.Add(enumName);
        }

        unexpectedOrphans.Should().BeEmpty(
            "every Bible recipe row without a live code counterpart must match a documented " +
            "unimplemented pattern. Unexpected orphan rows:\n" + string.Join("\n", unexpectedOrphans));
    }

    [Test]
    public void LiveRecipes_AreDocumentedInBible()
    {
        var registry = BuildLiveRecipeRegistry();
        var workbookRows = ReadWorkbookRecipeRows();

        var undocumented = new List<string>();

        foreach (var recipeType in registry.Keys.OrderBy(type => type.ToString(), StringComparer.Ordinal))
        {
            if (UndocumentedLiveRecipeExceptions.Contains(recipeType))
                continue;

            if (!workbookRows.ContainsKey(recipeType.ToString()))
                undocumented.Add(recipeType.ToString());
        }

        undocumented.Should().BeEmpty(
            "every live recipe must be documented in one of the Design Bible recipe tabs " +
            "(except the crafting submission-token recipes). Undocumented live recipes:\n" +
            string.Join("\n", undocumented));
    }

    private static bool IsKnownUnimplementedBibleRow(string enumName)
    {
        // Droid instruction recipes are documented ahead of implementation.
        if (enumName.StartsWith("Instruction", StringComparison.Ordinal))
            return true;

        // Sovereign-tier smithery / equipment sets are documented ahead of implementation.
        if (enumName.StartsWith("Sovereign", StringComparison.Ordinal) ||
            enumName.StartsWith("Astral", StringComparison.Ordinal) ||
            enumName.StartsWith("Citadel", StringComparison.Ordinal) ||
            enumName.StartsWith("Paragon", StringComparison.Ordinal))
        {
            return true;
        }

        // Top-tier (005) droid-equipment A/C variants are documented ahead of implementation.
        if (Droid005VariantPattern.IsMatch(enumName))
            return true;

        // Legacy grouped droid boosts, replaced by per-weapon droid boosts.
        if (LegacyGroupedDroidBoostRows.Contains(enumName))
            return true;

        // Legacy starship missile ammo tiers, superseded by the current missile tiers.
        if (LegacyMissileTierRows.Contains(enumName))
            return true;

        return false;
    }

    private static bool TryResolveRecipeType(string enumName, out RecipeType recipeType)
    {
        recipeType = RecipeType.Invalid;

        if (!Enum.TryParse(enumName, false, out RecipeType parsed))
            return false;

        if (!Enum.IsDefined(typeof(RecipeType), parsed) || parsed == RecipeType.Invalid)
            return false;

        recipeType = parsed;
        return true;
    }

    private static Dictionary<RecipeType, RecipeDetail> BuildLiveRecipeRegistry()
    {
        var registry = new Dictionary<RecipeType, RecipeDetail>();

        var definitionTypes = typeof(IRecipeListDefinition).Assembly
            .GetTypes()
            .Where(type => typeof(IRecipeListDefinition).IsAssignableFrom(type) &&
                           !type.IsInterface &&
                           !type.IsAbstract);

        foreach (var type in definitionTypes)
        {
            var instance = (IRecipeListDefinition)Activator.CreateInstance(type)!;
            foreach (var (recipeType, detail) in instance.BuildRecipes())
            {
                // Mirror Craft.CacheRecipes: the first registration for a recipe type wins.
                if (!registry.ContainsKey(recipeType))
                    registry[recipeType] = detail;
            }
        }

        return registry;
    }

    private static bool ComponentsEqual(Dictionary<string, int> expected, Dictionary<string, int> actual)
    {
        if (expected.Count != actual.Count)
            return false;

        foreach (var (resref, quantity) in expected)
        {
            if (!actual.TryGetValue(resref, out var actualQuantity) || actualQuantity != quantity)
                return false;
        }

        return true;
    }

    private static string FormatComponents(Dictionary<string, int> components)
    {
        return "{" + string.Join(", ", components
            .OrderBy(component => component.Key, StringComparer.Ordinal)
            .Select(component => $"{component.Key}={component.Value}")) + "}";
    }

    private static Dictionary<string, WorkbookRecipeRow> ReadWorkbookRecipeRows()
    {
        var root = FindRepositoryRoot();
        var workbookPath = Path.Combine(
            root.FullName,
            "design",
            "bible",
            "SWLOR Design Bible - Combat Upgrade.xlsx");

        using var archive = OpenWorkbookWithRetry(workbookPath);
        var sharedStrings = ReadSharedStrings(archive);

        var rows = new Dictionary<string, WorkbookRecipeRow>(StringComparer.Ordinal);

        foreach (var worksheetName in RecipeWorksheetNames)
        {
            var worksheet = ReadWorksheetByName(archive, worksheetName);

            // Index every cell (address -> text) once. The worksheets are large (1500+ rows),
            // so re-scanning the document per cell would be prohibitively slow.
            var cells = BuildCellIndex(worksheet, sharedStrings);
            var headerColumns = BuildHeaderColumnMap(cells);

            string Cell(string column, int rowNumber) =>
                cells.TryGetValue($"{column}{rowNumber}", out var value) ? value : string.Empty;

            string Column(string header)
            {
                if (!headerColumns.TryGetValue(header, out var column))
                    throw new InvalidOperationException($"'{worksheetName}' is missing expected header '{header}'.");
                return column;
            }

            var enumColumn = Column("Recipe Enum");
            var skillColumn = Column("Skill");
            var categoryColumn = Column("Category Enum");
            var levelColumn = Column("Skill Level");
            var quantityColumn = Column("Quantity");
            var resrefColumn = Column("Resref");
            var enhancementTypeColumn = Column("Enhancement Type");
            var enhancementSlotsColumn = Column("Enhancement Slots");

            var componentColumns = new List<(string Component, string Quantity)>();
            for (var index = 1; index <= 8; index++)
            {
                componentColumns.Add((Column($"Component {index}"), Column($"Component Quantity {index}")));
            }

            var dataRowNumbers = cells.Keys
                .Select(GetRowNumber)
                .Where(rowNumber => rowNumber > 1)
                .Distinct()
                .OrderBy(rowNumber => rowNumber);

            // The Skill column is only filled on the first row of each sub-group; grouped
            // continuation rows leave it blank and inherit the section's skill. Forward-fill it.
            var inheritedSkill = string.Empty;

            foreach (var rowNumber in dataRowNumbers)
            {
                var rowSkill = Cell(skillColumn, rowNumber).Trim();
                if (rowSkill.Length > 0)
                    inheritedSkill = rowSkill;

                var enumName = Cell(enumColumn, rowNumber).Trim();
                if (enumName.Length == 0)
                    continue;

                var components = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var (componentColumn, componentQuantityColumn) in componentColumns)
                {
                    var resref = Cell(componentColumn, rowNumber).Trim();
                    if (resref.Length == 0)
                        continue;

                    components[resref] = ParseNumber(Cell(componentQuantityColumn, rowNumber));
                }

                var recipeRow = new WorkbookRecipeRow(
                    worksheetName,
                    rowNumber,
                    enumName,
                    inheritedSkill,
                    Cell(categoryColumn, rowNumber).Trim(),
                    Cell(resrefColumn, rowNumber).Trim(),
                    ParseNumber(Cell(levelColumn, rowNumber)),
                    ParseNumber(Cell(quantityColumn, rowNumber)),
                    ParseEnhancementType(Cell(enhancementTypeColumn, rowNumber)),
                    ParseNumber(Cell(enhancementSlotsColumn, rowNumber)),
                    components);

                if (rows.TryGetValue(enumName, out var existing))
                {
                    throw new InvalidOperationException(
                        $"Duplicate recipe enum '{enumName}' found in Bible " +
                        $"({existing.Worksheet} row {existing.RowNumber} and {worksheetName} row {rowNumber}).");
                }

                rows[enumName] = recipeRow;
            }
        }

        return rows;
    }

    private static Dictionary<string, string> BuildCellIndex(XDocument worksheet, IReadOnlyList<string> sharedStrings)
    {
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var index = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var cell in worksheet.Descendants(ns + "c"))
        {
            var address = cell.Attribute("r")?.Value;
            if (address == null)
                continue;

            var type = cell.Attribute("t")?.Value;
            string text;
            if (type == "inlineStr")
            {
                text = string.Concat(cell.Descendants(ns + "t").Select(node => node.Value));
            }
            else
            {
                var value = cell.Element(ns + "v")?.Value;
                text = type == "s" && int.TryParse(value, out var sharedIndex)
                    ? sharedStrings[sharedIndex]
                    : value ?? string.Empty;
            }

            index[address] = text;
        }

        return index;
    }

    private static int GetRowNumber(string address)
    {
        var digits = new string(address.SkipWhile(char.IsLetter).ToArray());
        return int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rowNumber)
            ? rowNumber
            : 0;
    }

    private static int ParseNumber(string text)
    {
        text = (text ?? string.Empty).Replace(",", string.Empty).Trim();
        if (text.Length == 0)
            return 0;

        return (int)decimal.Parse(text, NumberStyles.Number, CultureInfo.InvariantCulture);
    }

    private static RecipeEnhancementType ParseEnhancementType(string text)
    {
        text = (text ?? string.Empty).Trim();
        if (text.Length == 0 || text.Equals("N/A", StringComparison.OrdinalIgnoreCase))
            return RecipeEnhancementType.None;

        return Enum.Parse<RecipeEnhancementType>(text, true);
    }

    private static Dictionary<string, string> BuildHeaderColumnMap(Dictionary<string, string> cells)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (address, value) in cells)
        {
            if (GetRowNumber(address) != 1)
                continue;

            var text = NormalizeWhitespace(value);
            if (text.Length == 0)
                continue;

            var column = new string(address.TakeWhile(char.IsLetter).ToArray());
            map[text] = column;
        }

        return map;
    }

    private static string NormalizeWhitespace(string text)
    {
        return Regex.Replace(text ?? string.Empty, @"\s+", " ").Trim();
    }

    private static ZipArchive OpenWorkbookWithRetry(string path)
    {
        // The workbook is occasionally locked briefly by another process while it is being saved.
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var buffer = new MemoryStream();
                stream.CopyTo(buffer);
                buffer.Position = 0;

                var archive = new ZipArchive(buffer, ZipArchiveMode.Read);

                // Force a read of a core part to surface partial/mid-save archives before returning.
                _ = ReadWorkbookXml(archive, "xl/workbook.xml");
                return archive;
            }
            catch (Exception ex) when ((ex is IOException || ex is InvalidDataException) && attempt < 20)
            {
                Thread.Sleep(250);
            }
        }
    }

    private static XDocument ReadWorkbookXml(ZipArchive archive, string entryName)
    {
        var entry = archive.GetEntry(entryName);
        entry.Should().NotBeNull($"{entryName} should exist in the combat Bible workbook");

        using var stream = entry!.Open();
        return XDocument.Load(stream);
    }

    private static XDocument ReadWorksheetByName(ZipArchive archive, string sheetName)
    {
        var workbook = ReadWorkbookXml(archive, "xl/workbook.xml");
        var relationships = ReadWorkbookXml(archive, "xl/_rels/workbook.xml.rels");
        XNamespace workbookNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace relationshipNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        XNamespace packageRelationshipNs = "http://schemas.openxmlformats.org/package/2006/relationships";

        var sheet = workbook
            .Descendants(workbookNs + "sheet")
            .Single(candidate => candidate.Attribute("name")?.Value == sheetName);
        var relationshipId = sheet.Attribute(relationshipNs + "id")?.Value;
        relationshipId.Should().NotBeNullOrWhiteSpace($"{sheetName} should have a workbook relationship id");

        var target = relationships
            .Descendants(packageRelationshipNs + "Relationship")
            .Single(candidate => candidate.Attribute("Id")?.Value == relationshipId)
            .Attribute("Target")?
            .Value
            .Replace('\\', '/');
        target.Should().NotBeNullOrWhiteSpace($"{sheetName} should resolve to a worksheet XML target");

        var entryName = target!.StartsWith("/", StringComparison.Ordinal)
            ? target.TrimStart('/')
            : $"xl/{target}";
        return ReadWorkbookXml(archive, entryName);
    }

    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry == null)
            return Array.Empty<string>();

        var sharedStrings = ReadWorkbookXml(archive, "xl/sharedStrings.xml");
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        return sharedStrings
            .Descendants(ns + "si")
            .Select(item => string.Concat(item.Descendants(ns + "t").Select(text => text.Value)))
            .ToArray();
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }

    private sealed record WorkbookRecipeRow(
        string Worksheet,
        int RowNumber,
        string Enum,
        string Skill,
        string Category,
        string Resref,
        int Level,
        int Quantity,
        RecipeEnhancementType EnhancementType,
        int EnhancementSlots,
        Dictionary<string, int> Components);
}
