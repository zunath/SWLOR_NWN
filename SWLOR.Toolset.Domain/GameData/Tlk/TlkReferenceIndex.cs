using System.Globalization;
using SWLOR.NWN.Formats.Tlk;
using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Domain.GameData.Tlk
{
    /// <summary>One 2DA cell that contains a custom TLK StrRef.</summary>
    public sealed record TlkReferenceUsage(
        string FileName,
        int RowIndex,
        string RowLabel,
        string ColumnName,
        uint StrRef,
        int EntryId);

    /// <summary>
    /// Immutable index of custom TLK StrRefs found in structured cells across SWLOR's 2DAs.
    /// Quoting, real column headers, empty markers, and source row labels are handled by the shared
    /// 2DA parser. Malformed files receive a conservative raw-text scan so reference safety does
    /// not depend on every legacy file having a valid header.
    /// </summary>
    public sealed class TlkReferenceIndex
    {
        /// <summary>Column marker used when a malformed 2DA is covered by raw-text fallback.</summary>
        public const string FallbackColumnName = "<raw-text>";

        private readonly IReadOnlyDictionary<int, IReadOnlyList<TlkReferenceUsage>> _usagesByEntryId;

        public static TlkReferenceIndex Empty { get; } = new(
            new Dictionary<int, IReadOnlyList<TlkReferenceUsage>>(),
            Array.Empty<string>());

        private TlkReferenceIndex(
            IReadOnlyDictionary<int, IReadOnlyList<TlkReferenceUsage>> usagesByEntryId,
            IReadOnlyList<string> unscannableFiles)
        {
            _usagesByEntryId = usagesByEntryId;
            UnscannableFiles = unscannableFiles;
            ReferencedEntryIds = usagesByEntryId.Keys.OrderBy(id => id).ToArray();
            MaxReferencedEntryId = ReferencedEntryIds.Count == 0 ? -1 : ReferencedEntryIds[^1];
        }

        public IReadOnlyList<int> ReferencedEntryIds { get; }

        public int MaxReferencedEntryId { get; }

        /// <summary>Files that could neither be parsed nor read by the raw-text fallback.</summary>
        public IReadOnlyList<string> UnscannableFiles { get; }

        /// <summary>Builds the index from the repository's <c>SWLOR_Haks/sw_2da</c> directory.</summary>
        public static TlkReferenceIndex Build(
            string sw2DaDirectoryPath,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sw2DaDirectoryPath);
            var service = new TwoDaService(sw2DaDirectoryPath);
            var usages = new Dictionary<int, List<TlkReferenceUsage>>();
            var unscannable = new List<string>();

            foreach (var tableName in service.GetTableNames().OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fileName = tableName + ".2da";
                TwoDaTable? table;
                try
                {
                    if (!service.TryGetTable(tableName, out table) || table == null)
                    {
                        if (!TryScanRawText(
                                Path.Combine(sw2DaDirectoryPath, fileName),
                                fileName,
                                usages,
                                cancellationToken))
                        {
                            unscannable.Add(fileName);
                        }
                        continue;
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    if (!TryScanRawText(
                            Path.Combine(sw2DaDirectoryPath, fileName),
                            fileName,
                            usages,
                            cancellationToken))
                    {
                        unscannable.Add(fileName);
                    }
                    continue;
                }

                ScanTable(fileName, table, usages, cancellationToken);
            }

            var frozen = usages.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<TlkReferenceUsage>)pair.Value.ToArray());
            return new TlkReferenceIndex(frozen, unscannable.ToArray());
        }

        public bool IsReferenced(int entryId) =>
            entryId >= 0 && _usagesByEntryId.ContainsKey(entryId);

        public int UsageCountFor(int entryId) =>
            entryId >= 0 && _usagesByEntryId.TryGetValue(entryId, out var usages) ? usages.Count : 0;

        public IReadOnlyList<TlkReferenceUsage> UsagesOf(int entryId) =>
            entryId >= 0 && _usagesByEntryId.TryGetValue(entryId, out var usages)
                ? usages
                : Array.Empty<TlkReferenceUsage>();

        private static void ScanTable(
            string fileName,
            TwoDaTable table,
            Dictionary<int, List<TlkReferenceUsage>> usages,
            CancellationToken cancellationToken)
        {
            for (var rowIndex = 0; rowIndex < table.RowCount; rowIndex++)
            {
                if ((rowIndex & 255) == 0)
                    cancellationToken.ThrowIfCancellationRequested();

                var rowLabel = table.GetRowLabel(rowIndex) ?? rowIndex.ToString(CultureInfo.InvariantCulture);
                foreach (var columnName in table.ColumnNames)
                {
                    var raw = table.GetString(rowIndex, columnName);
                    if (raw == null ||
                        !uint.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var strRef) ||
                        strRef < TlkService.CustomTlkBase)
                    {
                        continue;
                    }

                    var rawEntryId = (long)strRef - TlkService.CustomTlkBase;
                    if (rawEntryId > TlkFormatLimits.MaximumEntryId)
                        continue;

                    AddUsage(usages, new TlkReferenceUsage(
                        fileName,
                        rowIndex,
                        rowLabel,
                        columnName,
                        strRef,
                        (int)rawEntryId));
                }
            }
        }

        /// <summary>
        /// Conservatively covers malformed 2DAs. Every run of decimal digits is considered so a
        /// quoted or punctuated token cannot hide a custom StrRef. This may reserve a numeric value
        /// that was not semantically a StrRef, which is safer than offering a referenced row as a
        /// writable blank.
        /// </summary>
        private static bool TryScanRawText(
            string path,
            string fileName,
            Dictionary<int, List<TlkReferenceUsage>> usages,
            CancellationToken cancellationToken)
        {
            try
            {
                var lineIndex = 0;
                foreach (var line in File.ReadLines(path))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var rowLabel = BestEffortRowLabel(line, lineIndex);
                    var span = line.AsSpan();
                    var cursor = 0;
                    while (cursor < span.Length)
                    {
                        while (cursor < span.Length && !char.IsAsciiDigit(span[cursor]))
                            cursor++;
                        var start = cursor;
                        while (cursor < span.Length && char.IsAsciiDigit(span[cursor]))
                            cursor++;
                        if (start == cursor ||
                            !uint.TryParse(
                                span[start..cursor],
                                NumberStyles.None,
                                CultureInfo.InvariantCulture,
                                out var strRef) ||
                            strRef < TlkService.CustomTlkBase)
                        {
                            continue;
                        }

                        var rawEntryId = (long)strRef - TlkService.CustomTlkBase;
                        if (rawEntryId > TlkFormatLimits.MaximumEntryId)
                            continue;

                        AddUsage(usages, new TlkReferenceUsage(
                            fileName,
                            lineIndex,
                            rowLabel,
                            FallbackColumnName,
                            strRef,
                            (int)rawEntryId));
                    }

                    lineIndex++;
                }

                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }

        private static string BestEffortRowLabel(string line, int lineIndex)
        {
            var trimmed = line.AsSpan().TrimStart();
            var length = 0;
            while (length < trimmed.Length && !char.IsWhiteSpace(trimmed[length]))
                length++;

            return length == 0
                ? lineIndex.ToString(CultureInfo.InvariantCulture)
                : trimmed[..length].Trim('"').ToString();
        }

        private static void AddUsage(
            Dictionary<int, List<TlkReferenceUsage>> usages,
            TlkReferenceUsage usage)
        {
            if (!usages.TryGetValue(usage.EntryId, out var entryUsages))
                usages[usage.EntryId] = entryUsages = new List<TlkReferenceUsage>();
            entryUsages.Add(usage);
        }
    }
}
