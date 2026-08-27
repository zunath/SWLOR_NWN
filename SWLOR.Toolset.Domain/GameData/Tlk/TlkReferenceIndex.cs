using System.Globalization;
using System.Security.Cryptography;
using Serilog;
using SWLOR.NWN.Formats;
using SWLOR.NWN.Formats.Tlk;
using SWLOR.NWN.Formats.TwoDA;
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
        private static readonly ILogger Logger = Log.ForContext<TlkReferenceIndex>();

        /// <summary>Column marker used when a malformed 2DA is covered by raw-text fallback.</summary>
        public const string FallbackColumnName = "<raw-text>";

        /// <summary>Column marker used for conservative references in non-2DA repository text.</summary>
        public const string RepositoryTextColumnName = "<repository-text>";

        private static readonly HashSet<string> RepositoryTextExtensions = new(
            new[]
            {
                ".axaml", ".bat", ".cmd", ".config", ".cs", ".csproj", ".csv", ".ini",
                ".json", ".md", ".nss", ".props", ".ps1", ".py", ".resx", ".sh", ".slnx",
                ".sql", ".targets", ".tml", ".toml", ".txt", ".xml", ".yaml", ".yml"
            },
            StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> IgnoredDirectoryNames = new(
            new[]
            {
                ".agents", ".claude", ".codex", ".git", ".idea", ".vs", "bin", "node_modules", "obj"
            },
            StringComparer.OrdinalIgnoreCase);

        private readonly IReadOnlyDictionary<int, IReadOnlyList<TlkReferenceUsage>> _usagesByEntryId;
        private readonly IReadOnlyDictionary<string, SourceSnapshot> _sourceSnapshots;

        public static TlkReferenceIndex Empty { get; } = new(
            new Dictionary<int, IReadOnlyList<TlkReferenceUsage>>(),
            Array.Empty<string>(),
            new Dictionary<string, SourceSnapshot>(StringComparer.OrdinalIgnoreCase),
            0);

        private TlkReferenceIndex(
            IReadOnlyDictionary<int, IReadOnlyList<TlkReferenceUsage>> usagesByEntryId,
            IReadOnlyList<string> unscannableFiles,
            IReadOnlyDictionary<string, SourceSnapshot> sourceSnapshots,
            int scannedSourceCount)
        {
            _usagesByEntryId = usagesByEntryId;
            _sourceSnapshots = sourceSnapshots;
            UnscannableFiles = unscannableFiles;
            ReferencedEntryIds = usagesByEntryId.Keys.OrderBy(id => id).ToArray();
            MaxReferencedEntryId = ReferencedEntryIds.Count == 0 ? -1 : ReferencedEntryIds[^1];
            LastRefreshScannedSourceCount = scannedSourceCount;
        }

        public IReadOnlyList<int> ReferencedEntryIds { get; }

        public int MaxReferencedEntryId { get; }

        /// <summary>Files that could neither be parsed nor read by the raw-text fallback.</summary>
        public IReadOnlyList<string> UnscannableFiles { get; }

        /// <summary>Number of changed or new files whose references were parsed by the latest refresh.</summary>
        internal int LastRefreshScannedSourceCount { get; }

        /// <summary>Builds the index from the repository's <c>SWLOR_Haks/sw_2da</c> directory.</summary>
        public static TlkReferenceIndex Build(
            string sw2DaDirectoryPath,
            string? repositoryRootPath = null,
            CancellationToken cancellationToken = default) =>
            Empty.Refresh(sw2DaDirectoryPath, repositoryRootPath, cancellationToken);

        /// <summary>
        /// Refreshes repository references while reusing every unchanged file's parsed result.
        /// Content hashes prevent timestamp-preserving file replacements from reusing stale
        /// references; unchanged sources are fingerprinted but do not need to be parsed again.
        /// </summary>
        public TlkReferenceIndex Refresh(
            string sw2DaDirectoryPath,
            string? repositoryRootPath = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sw2DaDirectoryPath);
            if (!Directory.Exists(sw2DaDirectoryPath))
                throw new DirectoryNotFoundException($"2DA directory not found: {sw2DaDirectoryPath}");

            var descriptors = new List<SourceDescriptor>();
            var enumerationWarnings = new List<string>();
            try
            {
                descriptors.AddRange(Directory.EnumerateFiles(sw2DaDirectoryPath, "*.2da")
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .Select(path => new SourceDescriptor(
                        Path.GetFullPath(path),
                        Path.GetFileName(path),
                        SourceKind.TwoDa)));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Logger.Warning(ex, "Could not enumerate TLK reference sources under {TwoDaRoot}", sw2DaDirectoryPath);
                enumerationWarnings.Add($"{Path.GetFullPath(sw2DaDirectoryPath)} (2DA enumeration failed)");
            }

            if (!string.IsNullOrWhiteSpace(repositoryRootPath))
                AddRepositorySourceDescriptors(repositoryRootPath, descriptors, enumerationWarnings, cancellationToken);

            var snapshots = new Dictionary<string, SourceSnapshot>(StringComparer.OrdinalIgnoreCase);
            var scannedSourceCount = 0;
            foreach (var descriptor in descriptors)
            {
                cancellationToken.ThrowIfCancellationRequested();
                SourceFingerprint fingerprint;
                try
                {
                    fingerprint = SourceFingerprint.Capture(descriptor.Path, cancellationToken);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    Logger.Warning(ex, "Could not inspect TLK reference source {ReferencePath}", descriptor.Path);
                    snapshots[descriptor.Path] = new SourceSnapshot(
                        descriptor,
                        default,
                        Array.Empty<TlkReferenceUsage>(),
                        descriptor.DisplayName);
                    scannedSourceCount++;
                    continue;
                }

                if (_sourceSnapshots.TryGetValue(descriptor.Path, out var previous) &&
                    previous.Warning == null &&
                    previous.Descriptor.Kind == descriptor.Kind &&
                    previous.Descriptor.DisplayName == descriptor.DisplayName &&
                    previous.Fingerprint == fingerprint)
                {
                    snapshots[descriptor.Path] = previous;
                    continue;
                }

                snapshots[descriptor.Path] = ScanSource(descriptor, fingerprint, cancellationToken);
                scannedSourceCount++;
            }

            var usages = new Dictionary<int, List<TlkReferenceUsage>>();
            var unscannable = new List<string>(enumerationWarnings);
            foreach (var snapshot in snapshots.Values)
            {
                foreach (var usage in snapshot.Usages)
                    AddUsage(usages, usage);
                if (snapshot.Warning != null)
                    unscannable.Add(snapshot.Warning);
            }

            var frozen = usages.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<TlkReferenceUsage>)pair.Value.ToArray());
            return new TlkReferenceIndex(frozen, unscannable.ToArray(), snapshots, scannedSourceCount);
        }

        private static SourceSnapshot ScanSource(
            SourceDescriptor descriptor,
            SourceFingerprint fingerprint,
            CancellationToken cancellationToken)
        {
            var usages = new Dictionary<int, List<TlkReferenceUsage>>();
            var scanned = descriptor.Kind == SourceKind.TwoDa
                ? TryScanTwoDa(descriptor.Path, descriptor.DisplayName, usages, cancellationToken)
                : TryScanRepositoryTextFile(
                    descriptor.Path,
                    descriptor.DisplayName,
                    usages,
                    cancellationToken);

            SourceFingerprint after;
            try
            {
                after = SourceFingerprint.Capture(descriptor.Path, cancellationToken);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Logger.Warning(ex, "Could not verify TLK reference source {ReferencePath}", descriptor.Path);
                return new SourceSnapshot(
                    descriptor,
                    fingerprint,
                    Flatten(usages),
                    descriptor.DisplayName);
            }

            var warning = scanned && after == fingerprint
                ? null
                : descriptor.DisplayName;
            return new SourceSnapshot(descriptor, after, Flatten(usages), warning);
        }

        private static bool TryScanTwoDa(
            string path,
            string fileName,
            Dictionary<int, List<TlkReferenceUsage>> usages,
            CancellationToken cancellationToken)
        {
            try
            {
                var table = new TwoDaTable(Path.GetFileNameWithoutExtension(fileName), TwoDAReader.Read(path));
                ScanTable(fileName, table, usages, cancellationToken);
                return true;
            }
            catch (NwnFormatException)
            {
                return TryScanRawText(path, fileName, usages, cancellationToken);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Logger.Warning(ex, "Could not parse TLK reference source {ReferencePath}", path);
                return TryScanRawText(path, fileName, usages, cancellationToken);
            }
        }

        private static TlkReferenceUsage[] Flatten(Dictionary<int, List<TlkReferenceUsage>> usages) =>
            usages.OrderBy(pair => pair.Key).SelectMany(pair => pair.Value).ToArray();

        private static void AddRepositorySourceDescriptors(
            string repositoryRootPath,
            List<SourceDescriptor> descriptors,
            List<string> unscannable,
            CancellationToken cancellationToken)
        {
            var root = Path.GetFullPath(repositoryRootPath);
            try
            {
                foreach (var path in EnumerateRepositoryFiles(root))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var relativePath = Path.GetRelativePath(root, path);
                    if (IsIgnoredRepositoryPath(relativePath) ||
                        Path.GetExtension(path).Equals(".2da", StringComparison.OrdinalIgnoreCase) ||
                        !RepositoryTextExtensions.Contains(Path.GetExtension(path)))
                    {
                        continue;
                    }

                    descriptors.Add(new SourceDescriptor(
                        Path.GetFullPath(path),
                        relativePath.Replace('\\', '/'),
                        SourceKind.RepositoryText));
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Logger.Warning(ex, "Could not enumerate TLK reference sources under {RepositoryRoot}", root);
                unscannable.Add($"{root} (repository enumeration failed)");
            }
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
                Logger.Warning(ex, "Could not scan TLK reference source {ReferencePath}", path);
                return false;
            }
        }

        private static IEnumerable<string> EnumerateRepositoryFiles(string root)
        {
            var pending = new Stack<string>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                var directory = pending.Pop();
                foreach (var path in Directory.EnumerateFiles(directory))
                    yield return path;
                foreach (var child in Directory.EnumerateDirectories(directory))
                {
                    var name = Path.GetFileName(child);
                    if (!IgnoredDirectoryNames.Contains(name) &&
                        (File.GetAttributes(child) & FileAttributes.ReparsePoint) == 0)
                    {
                        pending.Push(child);
                    }
                }
            }
        }

        private static bool TryScanRepositoryTextFile(
            string path,
            string relativePath,
            Dictionary<int, List<TlkReferenceUsage>> usages,
            CancellationToken cancellationToken)
        {
            try
            {
                var lineNumber = 0;
                foreach (var line in File.ReadLines(path))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    lineNumber++;
                    ScanDecimalTokens(
                        line,
                        relativePath.Replace('\\', '/'),
                        lineNumber,
                        RepositoryTextColumnName,
                        usages);
                }
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Logger.Warning(ex, "Could not scan TLK reference source {ReferencePath}", path);
                return false;
            }
        }

        private static void ScanDecimalTokens(
            string text,
            string fileName,
            int rowIndex,
            string columnName,
            Dictionary<int, List<TlkReferenceUsage>> usages)
        {
            var span = text.AsSpan();
            var cursor = 0;
            while (cursor < span.Length)
            {
                while (cursor < span.Length && !char.IsAsciiDigit(span[cursor]))
                    cursor++;
                var start = cursor;
                while (cursor < span.Length && char.IsAsciiDigit(span[cursor]))
                    cursor++;
                if (start == cursor ||
                    start > 0 && span[start - 1] == '.' ||
                    cursor < span.Length && span[cursor] == '.' ||
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
                    rowIndex,
                    rowIndex.ToString(CultureInfo.InvariantCulture),
                    columnName,
                    strRef,
                    (int)rawEntryId));
            }
        }

        private static bool IsIgnoredRepositoryPath(string relativePath) =>
            relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(IgnoredDirectoryNames.Contains);

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

        private enum SourceKind
        {
            TwoDa,
            RepositoryText
        }

        private sealed record SourceDescriptor(string Path, string DisplayName, SourceKind Kind);

        private sealed record SourceSnapshot(
            SourceDescriptor Descriptor,
            SourceFingerprint Fingerprint,
            TlkReferenceUsage[] Usages,
            string? Warning);

        private readonly record struct SourceFingerprint(
            long Length,
            DateTime LastWriteUtc,
            string ContentHash)
        {
            public static SourceFingerprint Capture(string path, CancellationToken cancellationToken)
            {
                var info = new FileInfo(path);
                info.Refresh();
                if (!info.Exists)
                    throw new FileNotFoundException("TLK reference source no longer exists.", path);

                using var stream = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);
                using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                Span<byte> buffer = stackalloc byte[16 * 1024];
                int read;
                while ((read = stream.Read(buffer)) > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    hash.AppendData(buffer[..read]);
                }

                return new SourceFingerprint(
                    info.Length,
                    info.LastWriteTimeUtc,
                    Convert.ToHexString(hash.GetHashAndReset()));
            }
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
