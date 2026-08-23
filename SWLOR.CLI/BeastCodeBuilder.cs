using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.CLI
{
    internal class BeastCodeBuilder
    {
        private class BeastCodeDetail
        {
            public string Code { get; set; }
            public Dictionary<int, string> Levels { get; set; }
            public bool IsIncubation { get; set; }

            public BeastCodeDetail()
            {
                Levels = new Dictionary<int, string>();
            }
        }

        private sealed class MutationSourceRow
        {
            public string ResultEnum { get; init; }
            public string Weight { get; init; }
            public string LyaseColor { get; init; }
            public string LyaseCount { get; init; }
            public string IsomeraseColor { get; init; }
            public string IsomeraseCount { get; init; }
            public string HydrolaseColor { get; init; }
            public string HydrolaseCount { get; init; }
            public string Days { get; init; }
        }

        private sealed class WorkbookRow
        {
            private readonly IReadOnlyDictionary<string, string> _values;

            public WorkbookRow(IReadOnlyDictionary<string, string> values)
            {
                _values = values;
            }

            public string this[string header] =>
                _values.TryGetValue(header, out var value)
                    ? value.Trim()
                    : string.Empty;
        }

        private const string PublicBibleWorkbook = "SWLOR Design Bible - Combat Upgrade.xlsx";
        private const string PrivateSourceWorkbook = "SWLOR Design Bible - Private Source Data.xlsx";
        private const string Template = "Templates/beast_builder_template.txt";
        private const string LevelTemplate = "Templates/beast_level_template.txt";
        private const string OutputFolder = "OutputBeasts";

        public void Process()
        {
            ClearOutputDirectory();

            var template = File.ReadAllText(ResolveCliPath(Template));
            var levelTemplate = File.ReadAllText(ResolveCliPath(LevelTemplate));
            var publicWorkbookPath = ResolveBiblePath(PublicBibleWorkbook);
            var privateWorkbookPath = ResolveBiblePath(PrivateSourceWorkbook);
            var beastLevelRows = WorkbookReader
                .ReadRows(publicWorkbookPath, "Beast Levels", 4, 5)
                .Where(row => !string.IsNullOrWhiteSpace(row["NPC Name"]))
                .ToList();
            var mutationRowsBySource = WorkbookReader
                .ReadRows(privateWorkbookPath, "Mutation Requirements", 1, 2)
                .Where(row =>
                    !string.IsNullOrWhiteSpace(row["Source Enum"]) &&
                    !string.IsNullOrWhiteSpace(row["Result Enum"]))
                .GroupBy(row => row["Source Enum"])
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(row => new MutationSourceRow
                        {
                            ResultEnum = row["Result Enum"],
                            Weight = row["Weight"],
                            LyaseColor = row["Lyase Color"],
                            LyaseCount = row["Lyase Count"],
                            IsomeraseColor = row["Isomerase Color"],
                            IsomeraseCount = row["Isomerase Count"],
                            HydrolaseColor = row["Hydrolase Color"],
                            HydrolaseCount = row["Hydrolase Count"],
                            Days = row["Days"]
                        })
                        .ToList());
            var beasts = new Dictionary<BeastType, BeastCodeDetail>();

            foreach (var row in beastLevelRows)
            {
                var enumNameText = row["Enum"];
                if (string.IsNullOrWhiteSpace(enumNameText))
                    continue;

                var enumName = Enum.Parse<BeastType>(enumNameText);

                if (!beasts.ContainsKey(enumName))
                    beasts.Add(enumName, new BeastCodeDetail());

                var detail = beasts[enumName];
                detail.IsIncubation = row["Incubation?"] == "Y";

                if (string.IsNullOrWhiteSpace(detail.Code))
                {
                    var className = $"{enumName}BeastDefinition";
                    var name = EscapeCSharpString(row["NPC Name"]);
                    var beastType = row["Enum"];
                    var accuracyStat = row["Acc Stat"];
                    var damageStat = row["Att Stat"];
                    var role = row["Role"];
                    var appearance = row["Appearance Enum"];
                    var portraitId = FormatInteger(row["Portrait Id"]);
                    var soundSetId = FormatInteger(row["Sound Set Id"]);
                    var scaling = FormatFloat(row["Scaling %"]) + "f";
                    var mutations = BuildMutations(mutationRowsBySource.GetValueOrDefault(beastType));

                    detail.Code = template
                        .Replace("%%BEASTNAME%%", name)
                        .Replace("%%APPEARANCETYPE%%", appearance)
                        .Replace("%%APPEARANCESCALE%%", scaling)
                        .Replace("%%SOUNDSETID%%", soundSetId)
                        .Replace("%%PORTRAITID%%", portraitId)
                        .Replace("%%CLASSNAME%%", className)
                        .Replace("%%NAMESPACE%%", detail.IsIncubation ? "IncubationBeastDefinition" : "TamableBeastDefinition")
                        .Replace("%%BEASTTYPE%%", beastType)
                        .Replace("%%ACCURACYSTAT%%", GetAbilityEnumName(accuracyStat))
                        .Replace("%%DAMAGESTAT%%", GetAbilityEnumName(damageStat))
                        .Replace("%%BEASTROLE%%", role)
                        .Replace("%%MUTATION_TEMPLATES%%", mutations);
                }

                var level = int.Parse(FormatInteger(row["Level"]), CultureInfo.InvariantCulture);
                if (!detail.Levels.ContainsKey(level))
                    detail.Levels.Add(level, levelTemplate);

                detail.Levels[level] = detail.Levels[level]
                    .Replace("%%LEVEL%%", level.ToString())
                    .Replace("%%HP%%", FormatInteger(row["HP"]))
                    .Replace("%%STM%%", FormatInteger(row["STM"]))
                    .Replace("%%FP%%", FormatInteger(row["FP"]))
                    .Replace("%%DMG%%", FormatInteger(row["Base Damage"]))
                    .Replace("%%ATTACKDELAY%%", FormatAttackDelay(row["Attack Delay"]))

                    .Replace("%%MGT%%", FormatInteger(row["MGT"]))
                    .Replace("%%PER%%", FormatInteger(row["PER"]))
                    .Replace("%%VIT%%", FormatInteger(row["VIT"]))
                    .Replace("%%WIL%%", FormatInteger(row["WIL"]))
                    .Replace("%%AGI%%", FormatInteger(row["AGI"]))
                    .Replace("%%SOC%%", FormatInteger(row["SOC"]))

                    .Replace("%%MAXATTACKBONUS%%", FormatInteger(row["Attack Bonus"]))
                    .Replace("%%MAXACCURACYBONUS%%", FormatInteger(row["Accuracy Bonus"]))
                    .Replace("%%MAXEVASIONBONUS%%", FormatInteger(row["Evasion Bonus"]))

                    .Replace("%%MAXPHYSICALDEFENSE%%", FormatInteger(row["Physical DEF Bonus"]))
                    .Replace("%%MAXFORCEDEFENSE%%", FormatInteger(row["Force DEF Bonus"]))
                    .Replace("%%MAXFIREDEFENSE%%", FormatInteger(row["Fire DEF"]))
                    .Replace("%%MAXPOISONDEFENSE%%", FormatInteger(row["Poison DEF"]))
                    .Replace("%%MAXELECTRICALDEFENSE%%", FormatInteger(row["Electrical DEF"]))
                    .Replace("%%MAXICEDEFENSE%%", FormatInteger(row["Ice DEF"]))

                    .Replace("%%MAXWILL%%", FormatInteger(row["Will Bonus"]))
                    .Replace("%%MAXFORTITUDE%%", FormatInteger(row["Fortitude Bonus"]))
                    .Replace("%%MAXREFLEX%%", FormatInteger(row["Reflex Bonus"]));
            }

            foreach (var (type, detail) in beasts)
            {
                var levels = detail.Levels.OrderBy(o => o.Key);
                var levelText = string.Empty;
                var levelFunctionCalls = string.Empty;

                foreach (var (levelId, level) in levels)
                {
                    levelText += level;
                    levelFunctionCalls += $"\t\t\tLevel{levelId}();" + Environment.NewLine;
                }

                var output = detail.Code
                    .Replace("%%LEVELLIST%%", levelText)
                    .Replace("%%LEVELCALLS%%", levelFunctionCalls);

                var folderName = detail.IsIncubation
                    ? "IncubationBeastDefinition"
                    : "TamableBeastDefinition";
                var outputFolder = Path.Combine(ResolveOutputFolder(), folderName);

                if (!Directory.Exists(outputFolder))
                    Directory.CreateDirectory(outputFolder);

                File.WriteAllText(Path.Combine(outputFolder, $"{type}BeastDefinition.cs"), output);
            }
        }

        private string GetAbilityEnumName(string shorthand)
        {
            switch (shorthand)
            {
                case "MGT":
                    return AbilityType.Might.ToString();
                case "PER":
                    return AbilityType.Perception.ToString();
                case "VIT":
                    return AbilityType.Vitality.ToString();
                case "WIL":
                    return AbilityType.Willpower.ToString();
                case "AGI":
                    return AbilityType.Agility.ToString();
                case "SOC":
                    return AbilityType.Social.ToString();
            }

            return AbilityType.Invalid.ToString();
        }

        private string GetMutationDays(string days)
        {
            var output = new List<string>();
            foreach (var c in days.Where(c => !char.IsWhiteSpace(c) && c != ','))
            {
                var day = c switch
                {
                    'M' => "DayOfWeek.Monday",
                    'T' => "DayOfWeek.Tuesday",
                    'W' => "DayOfWeek.Wednesday",
                    'R' => "DayOfWeek.Thursday",
                    'F' => "DayOfWeek.Friday",
                    'S' => "DayOfWeek.Saturday",
                    'U' => "DayOfWeek.Sunday",
                    _ => throw new InvalidOperationException($"Unsupported mutation day code '{c}' in '{days}'.")
                };

                output.Add(day);
            }

            return string.Join(", ", output);
        }

        private string BuildMutation(MutationSourceRow row)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.ResultEnum))
                return string.Empty;

            const string Tabs = "\t\t\t\t";
            var output = string.Empty;

            output += $".CanMutateInto(BeastType.{row.ResultEnum})" + Environment.NewLine +
                      $"{Tabs}.MutationWeight({FormatInteger(row.Weight)})" + Environment.NewLine;

            if (!string.IsNullOrWhiteSpace(row.LyaseColor))
            {
                output += $"{Tabs}.MutationRequiresLyaseColor(EnzymeColorType.{row.LyaseColor}, {FormatInteger(row.LyaseCount)})" + Environment.NewLine;
            }
            if (!string.IsNullOrWhiteSpace(row.IsomeraseColor))
            {
                output += $"{Tabs}.MutationRequiresIsomeraseColor(EnzymeColorType.{row.IsomeraseColor}, {FormatInteger(row.IsomeraseCount)})" + Environment.NewLine;
            }
            if (!string.IsNullOrWhiteSpace(row.HydrolaseColor))
            {
                output += $"{Tabs}.MutationRequiresHydrolaseColor(EnzymeColorType.{row.HydrolaseColor}, {FormatInteger(row.HydrolaseCount)})" + Environment.NewLine;
            }

            if (!string.IsNullOrWhiteSpace(row.Days))
            {
                var formattedDays = GetMutationDays(row.Days);
                output += $"{Tabs}.MutationRequiresDayOfWeek({formattedDays})" + Environment.NewLine;
            }

            return output;
        }

        private string BuildMutations(IEnumerable<MutationSourceRow> rows)
        {
            if (rows == null)
                return string.Empty;

            var mutations = string.Join(
                Environment.NewLine + Environment.NewLine + "\t\t\t\t",
                rows
                    .Select(BuildMutation)
                    .Where(mutation => !string.IsNullOrWhiteSpace(mutation)));

            return string.IsNullOrWhiteSpace(mutations)
                ? string.Empty
                : "                " + mutations;
        }

        private static string EscapeCSharpString(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
        }

        private static string FormatInteger(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "0";

            if (!decimal.TryParse(
                    value.Replace(",", string.Empty),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var number))
            {
                throw new InvalidOperationException($"Expected an integer-compatible value but found '{value}'.");
            }

            return decimal.ToInt32(decimal.Round(number, 0, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture);
        }

        private static string FormatAttackDelay(string value)
        {
            var costTableValue = int.Parse(FormatInteger(value), CultureInfo.InvariantCulture);
            if (!Enum.IsDefined(typeof(ItemPropertyAttackDelay), costTableValue) ||
                costTableValue == (int)ItemPropertyAttackDelay.Invalid)
            {
                throw new InvalidOperationException(
                    $"Attack Delay '{value}' is not a valid iprp_delay.2da cost-table value.");
            }

            return $"{nameof(ItemPropertyAttackDelay)}.Delay{costTableValue * 10}";
        }

        private static string FormatFloat(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "1";

            if (!decimal.TryParse(
                    value,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var number))
            {
                throw new InvalidOperationException($"Expected a float-compatible value but found '{value}'.");
            }

            return number.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private string ResolveCliPath(string relativePath)
        {
            var path = Path.Combine(ResolveCliRoot(), relativePath);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Unable to locate CLI file '{relativePath}'.", path);

            return path;
        }

        private string ResolveOutputFolder()
        {
            return Path.Combine(ResolveCliRoot(), OutputFolder);
        }

        private string ResolveCliRoot()
        {
            var current = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (current != null)
            {
                if (Directory.Exists(Path.Combine(current.FullName, "Templates")) &&
                    File.Exists(Path.Combine(current.FullName, "SWLOR.CLI.csproj")))
                {
                    return current.FullName;
                }

                var candidate = Path.Combine(current.FullName, "SWLOR.CLI");
                if (Directory.Exists(Path.Combine(candidate, "Templates")) &&
                    File.Exists(Path.Combine(candidate, "SWLOR.CLI.csproj")))
                {
                    return candidate;
                }

                current = current.Parent;
            }

            throw new DirectoryNotFoundException("Unable to locate the SWLOR.CLI directory.");
        }

        private string ResolveBiblePath(string fileName)
        {
            var current = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (current != null)
            {
                var candidate = Path.Combine(current.FullName, "design", "bible", fileName);
                if (File.Exists(candidate))
                    return candidate;

                current = current.Parent;
            }

            throw new FileNotFoundException($"Unable to locate Bible workbook '{fileName}'.");
        }

        private void ClearOutputDirectory()
        {
            var outputFolder = ResolveOutputFolder();
            if (Directory.Exists(outputFolder))
            {
                Directory.Delete(outputFolder, true);
            }

            Directory.CreateDirectory(outputFolder);
        }

        private static class WorkbookReader
        {
            private static readonly XNamespace SpreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            private static readonly XNamespace RelationshipNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            private static readonly XNamespace PackageRelationshipNs = "http://schemas.openxmlformats.org/package/2006/relationships";

            public static IReadOnlyList<WorkbookRow> ReadRows(
                string path,
                string sheetName,
                int headerRowNumber,
                int firstDataRowNumber)
            {
                using var archive = ZipFile.OpenRead(path);
                var sharedStrings = ReadSharedStrings(archive);
                var sheetEntryName = GetSheetEntryName(archive, sheetName);
                var worksheet = ReadXml(archive, sheetEntryName);
                var headerRow = worksheet
                    .Descendants(SpreadsheetNs + "row")
                    .FirstOrDefault(row => (int?)row.Attribute("r") == headerRowNumber);

                if (headerRow == null)
                    throw new InvalidOperationException($"Header row {headerRowNumber} was not found in '{sheetName}'.");

                var headersByColumn = headerRow
                    .Elements(SpreadsheetNs + "c")
                    .Select(cell => (
                        Column: GetColumnName((string)cell.Attribute("r")),
                        Header: GetCellValue(cell, sharedStrings).Trim()))
                    .Where(cell => !string.IsNullOrWhiteSpace(cell.Header))
                    .ToDictionary(cell => cell.Column, cell => cell.Header);
                var rows = new List<WorkbookRow>();

                foreach (var row in worksheet.Descendants(SpreadsheetNs + "row"))
                {
                    var rowNumber = (int?)row.Attribute("r") ?? 0;
                    if (rowNumber < firstDataRowNumber)
                        continue;

                    var values = new Dictionary<string, string>();
                    foreach (var cell in row.Elements(SpreadsheetNs + "c"))
                    {
                        var column = GetColumnName((string)cell.Attribute("r"));
                        if (!headersByColumn.TryGetValue(column, out var header))
                            continue;

                        values[header] = GetCellValue(cell, sharedStrings);
                    }

                    rows.Add(new WorkbookRow(values));
                }

                return rows;
            }

            private static XDocument ReadXml(ZipArchive archive, string entryName)
            {
                var entry = archive.GetEntry(entryName);
                if (entry == null)
                    throw new InvalidOperationException($"Workbook entry '{entryName}' was not found.");

                using var stream = entry.Open();
                return XDocument.Load(stream);
            }

            private static string GetSheetEntryName(ZipArchive archive, string sheetName)
            {
                var workbook = ReadXml(archive, "xl/workbook.xml");
                var relationships = ReadXml(archive, "xl/_rels/workbook.xml.rels");
                var sheet = workbook
                    .Descendants(SpreadsheetNs + "sheet")
                    .FirstOrDefault(x => (string)x.Attribute("name") == sheetName);

                if (sheet == null)
                    throw new InvalidOperationException($"Worksheet '{sheetName}' was not found.");

                var relationshipId = (string)sheet.Attribute(RelationshipNs + "id");
                var relationship = relationships
                    .Descendants(PackageRelationshipNs + "Relationship")
                    .FirstOrDefault(x => (string)x.Attribute("Id") == relationshipId);

                if (relationship == null)
                    throw new InvalidOperationException($"Relationship '{relationshipId}' for worksheet '{sheetName}' was not found.");

                var target = ((string)relationship.Attribute("Target"))?.TrimStart('/') ?? string.Empty;
                if (!target.StartsWith("xl/", StringComparison.Ordinal))
                    target = "xl/" + target;

                return target;
            }

            private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
            {
                if (archive.GetEntry("xl/sharedStrings.xml") == null)
                    return Array.Empty<string>();

                var sharedStrings = ReadXml(archive, "xl/sharedStrings.xml");
                return sharedStrings
                    .Descendants(SpreadsheetNs + "si")
                    .Select(item => string.Concat(item.Descendants(SpreadsheetNs + "t").Select(text => text.Value)))
                    .ToList();
            }

            private static string GetCellValue(XElement cell, IReadOnlyList<string> sharedStrings)
            {
                var cellType = (string)cell.Attribute("t");
                if (cellType == "inlineStr")
                    return string.Concat(cell.Descendants(SpreadsheetNs + "t").Select(text => text.Value));

                var value = cell.Element(SpreadsheetNs + "v")?.Value ?? string.Empty;
                if (cellType == "s" && int.TryParse(value, out var sharedStringIndex))
                    return sharedStrings[sharedStringIndex];

                return value;
            }

            private static string GetColumnName(string cellReference)
            {
                return Regex.Match(cellReference ?? string.Empty, "^[A-Z]+").Value;
            }
        }
    }
}
