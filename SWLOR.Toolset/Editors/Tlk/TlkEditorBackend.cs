using System.Security.Cryptography;
using System.Text;
using SWLOR.NWN.Formats.Common;
using SWLOR.NWN.Formats.Tlk;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Services;

namespace SWLOR.Toolset.Editors.Tlk;

/// <summary>The repository files which make the SWLOR-only TLK editor available.</summary>
public sealed record TlkEditorSource(
    string JsonPath,
    string BinaryPath,
    string TwoDaDirectory,
    string? RepositoryRoot = null)
{
    public bool IsAvailable => File.Exists(JsonPath) && Directory.Exists(TwoDaDirectory);

    public string UnavailableReason => !File.Exists(JsonPath)
        ? $"SWLOR custom TLK source was not found at '{JsonPath}'."
        : !Directory.Exists(TwoDaDirectory)
            ? $"SWLOR 2DA source directory was not found at '{TwoDaDirectory}'."
            : string.Empty;
}

public sealed record TlkEditorEntry(int Id, string Text);

public sealed record TlkEditorUsage(
    string FileName,
    int RowIndex,
    string? RowLabel,
    string ColumnName,
    uint StrRef);

/// <summary>
/// The editor-facing boundary around TLK parsing, reference discovery, and the two-file save.
/// Kept narrow so navigation/edit history can be tested with an in-memory implementation.
/// </summary>
public interface ITlkEditorBackend
{
    string JsonPath { get; }
    string BinaryPath { get; }
    int Language { get; }
    int Count { get; }
    int MaxEntryId { get; }
    IReadOnlyList<TlkEditorEntry> Entries { get; }
    IReadOnlyList<string> ReferenceWarnings { get; }

    bool ContainsEntry(int id);
    string? GetText(int id);
    void SetText(int id, string text);
    bool Clear(int id);
    bool IsReferenced(int id);
    int UsageCountFor(int id);
    IReadOnlyList<TlkEditorUsage> UsagesOf(int id);
    int FindFirstAvailableBlank();
    int FindNextAvailableBlank(int currentId);
    void RefreshReferences();
    bool HasExternalChange();
    void Reload();
    void Save(bool overwriteExternalChanges = false);
    void Publish();
}

/// <summary>
/// Production adapter for the sparse JSON document. Save stages JSON and generated binary output,
/// verifies the staged binary by reading it back, then commits the pair with rollback protection.
/// </summary>
public sealed class TlkEditorBackend : ITlkEditorBackend
{
    private readonly TlkEditorSource _source;
    private readonly TlkService? _tlkService;
    private TlkDocument _document;
    private TlkReferenceIndex _references;
    private FileFingerprint _jsonFingerprint;
    private FileFingerprint _binaryFingerprint;
    private TlkFile? _acceptedBinary;
    private TlkEditorEntry[]? _entrySnapshot;

    public TlkEditorBackend(
        TlkEditorSource source,
        TlkService? tlkService = null,
        CancellationToken cancellationToken = default)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        if (!source.IsAvailable)
            throw new FileNotFoundException(source.UnavailableReason, source.JsonPath);

        _tlkService = tlkService;
        // CommitAll protects ordinary failures immediately; this closes the remaining process-kill
        // case before either half of a previously interrupted JSON/binary pair is read.
        SaveService.RecoverInterruptedSaves(Path.GetDirectoryName(source.JsonPath)!);
        var snapshot = CaptureSnapshot();
        _document = snapshot.Document;
        _jsonFingerprint = snapshot.JsonFingerprint;
        _binaryFingerprint = snapshot.BinaryFingerprint;
        _acceptedBinary = snapshot.VerifiedBinary;
        cancellationToken.ThrowIfCancellationRequested();
        _references = TlkReferenceIndex.Build(
            source.TwoDaDirectory,
            source.RepositoryRoot,
            cancellationToken);
    }

    public string JsonPath => _source.JsonPath;
    public string BinaryPath => _source.BinaryPath;
    public int Language => _document.Language;
    public int Count => _document.Count;
    public int MaxEntryId => _document.MaxEntryId;
    public IReadOnlyList<TlkEditorEntry> Entries => _entrySnapshot ??=
        _document.Entries.Select(entry => new TlkEditorEntry(entry.Id, entry.Text)).ToArray();
    public IReadOnlyList<string> ReferenceWarnings => _references.UnscannableFiles;

    public bool ContainsEntry(int id) => _document.ContainsEntry(id);
    public string? GetText(int id) => _document.GetText(id);
    public void SetText(int id, string text)
    {
        _document.SetText(id, text);
        _entrySnapshot = null;
    }
    public bool Clear(int id)
    {
        var removed = _document.Clear(id);
        if (removed)
            _entrySnapshot = null;
        return removed;
    }
    public bool IsReferenced(int id) => _references.IsReferenced(id);
    public int UsageCountFor(int id) => _references.UsageCountFor(id);
    public IReadOnlyList<TlkEditorUsage> UsagesOf(int id) =>
        _references.UsagesOf(id)
            .Select(usage => new TlkEditorUsage(
                usage.FileName,
                usage.RowIndex,
                usage.RowLabel,
                usage.ColumnName,
                usage.StrRef))
            .ToArray();
    public int FindFirstAvailableBlank() => _document.FindFirstAvailableBlank(_references);
    public int FindNextAvailableBlank(int currentId) =>
        _document.FindNextAvailableBlank(currentId, _references);
    public void RefreshReferences()
    {
        _references = _references.Refresh(_source.TwoDaDirectory, _source.RepositoryRoot);
    }

    public bool HasExternalChange() =>
        !_jsonFingerprint.Matches(JsonPath) || !_binaryFingerprint.Matches(BinaryPath);

    public void Reload()
    {
        // Capture content and its accepted baseline in one generation. Reference indexing may be
        // lengthy, so it follows outside the lease; a write during that scan remains detectable.
        // Unlike initial open, reload must reject a torn external JSON/binary generation before it
        // can replace the editor state or be published to open toolset fields.
        var snapshot = CaptureSnapshot(verifyBinaryPair: true);
        var references = _references.Refresh(_source.TwoDaDirectory, _source.RepositoryRoot);
        _document = snapshot.Document;
        _references = references;
        _jsonFingerprint = snapshot.JsonFingerprint;
        _binaryFingerprint = snapshot.BinaryFingerprint;
        _acceptedBinary = snapshot.VerifiedBinary;
        _entrySnapshot = null;
    }

    public void Save(bool overwriteExternalChanges = false)
    {
        if (overwriteExternalChanges)
        {
            using var acceptedGeneration = ModuleWriteLock.AcquireForResourcePath(JsonPath);
            _jsonFingerprint = FileFingerprint.Capture(JsonPath);
            _binaryFingerprint = FileFingerprint.Capture(BinaryPath);
        }
        else if (HasExternalChange())
        {
            throw new TlkExternalChangeException(JsonPath);
        }

        var jsonBytes = Encoding.UTF8.GetBytes(_document.ToJson());
        var entries = _document.Entries.ToDictionary(entry => entry.Id, entry => entry.Text);
        var binaryBytes = TlkWriter.Write((uint)_document.Language, entries);

        SaveService.StagedWrite jsonStage = default;
        SaveService.StagedWrite binaryStage = default;
        var jsonStaged = false;
        var binaryStaged = false;
        try
        {
            jsonStage = SaveService.Stage(JsonPath, jsonBytes);
            jsonStaged = true;
            binaryStage = SaveService.Stage(BinaryPath, binaryBytes);
            binaryStaged = true;

            var verifiedBinary = VerifyStagedPair(jsonStage.TemporaryPath, binaryStage.TemporaryPath);

            // Serialization/verification can take long enough for another process to write. The
            // final fingerprint check and pair commit share one cross-process lease, closing the
            // check/replace race. CommitAll's nested acquisition is deliberately re-entrant.
            using (ModuleWriteLock.AcquireForResourcePath(JsonPath))
            {
                if (HasExternalChange())
                    throw new TlkExternalChangeException(JsonPath);
                SaveService.CommitAll(new[] { jsonStage, binaryStage });
                _jsonFingerprint = FileFingerprint.Capture(JsonPath);
                _binaryFingerprint = FileFingerprint.Capture(BinaryPath);
            }
            _acceptedBinary = verifiedBinary;
            jsonStaged = false;
            binaryStaged = false;
        }
        finally
        {
            if (jsonStaged)
                SaveService.Discard(jsonStage);
            if (binaryStaged)
                SaveService.Discard(binaryStage);
        }

    }

    public void Publish()
    {
        if (_tlkService == null)
            return;
        _tlkService.PublishCustomTlk(_acceptedBinary ??
            throw new InvalidOperationException("No verified TLK binary generation is available to publish."));
    }

    private Snapshot CaptureSnapshot(bool verifyBinaryPair = false)
    {
        using var sourceLease = ModuleWriteLock.AcquireForResourcePath(JsonPath);
        var json = CapturedFile.Capture(JsonPath);
        if (!json.Exists)
            throw new FileNotFoundException("SWLOR custom TLK source was not found.", JsonPath);
        var binary = CapturedFile.Capture(BinaryPath);
        using var jsonStream = new MemoryStream(json.Content, writable: false);
        var document = TlkDocument.Parse(jsonStream);
        var verifiedBinary = verifyBinaryPair
            ? binary.Exists
                ? VerifyDocumentBinary(document, TlkReader.Read(binary.Content))
                : throw new FileNotFoundException("Generated TLK binary was not found.", BinaryPath)
            : LoadAcceptedBinary(document, binary);
        return new Snapshot(
            document,
            json.Fingerprint,
            binary.Fingerprint,
            verifiedBinary);
    }

    private TlkFile VerifyStagedPair(string stagedJsonPath, string stagedBinaryPath)
    {
        var stagedDocument = TlkDocument.Load(stagedJsonPath);
        if (stagedDocument.Language != _document.Language ||
            !stagedDocument.Entries.SequenceEqual(_document.Entries))
        {
            throw new InvalidDataException("Generated TLK JSON failed round-trip verification.");
        }

        return VerifyDocumentBinaryPair(stagedDocument, stagedBinaryPath);
    }

    private static TlkFile VerifyDocumentBinaryPair(TlkDocument document, string binaryPath)
    {
        return VerifyDocumentBinary(document, TlkReader.Read(binaryPath));
    }

    private static TlkFile CreateVerifiedBinary(TlkDocument document)
    {
        var entries = document.Entries.ToDictionary(entry => entry.Id, entry => entry.Text);
        return VerifyDocumentBinary(document, TlkReader.Read(TlkWriter.Write((uint)document.Language, entries)));
    }

    private static TlkFile LoadAcceptedBinary(TlkDocument document, CapturedFile binary)
    {
        try
        {
            if (binary.Exists)
                return VerifyDocumentBinary(document, TlkReader.Read(binary.Content));
        }
        catch (Exception ex) when (ex is InvalidDataException or FormatException)
        {
            // JSON is the editable source of truth. A missing, stale, or malformed generated file
            // is repaired on save, while the in-memory generation below keeps open labels current.
        }

        return CreateVerifiedBinary(document);
    }

    private static TlkFile VerifyDocumentBinary(TlkDocument document, TlkFile binary)
    {
        if (binary.LanguageId != (uint)document.Language)
            throw new InvalidDataException("Generated TLK language does not match the JSON source.");

        var requiredCount = document.MaxEntryId < 0 ? 0 : document.MaxEntryId + 1;
        if (binary.Entries.Count != requiredCount)
            throw new InvalidDataException(
                $"Generated TLK has {binary.Entries.Count} rows; expected {requiredCount}.");

        for (var id = 0; id < requiredCount; id++)
        {
            if (!string.Equals(binary.GetString((uint)id), document.GetText(id), StringComparison.Ordinal))
                throw new InvalidDataException($"Generated TLK row {id} failed round-trip verification.");
        }

        return binary;
    }

    private sealed record Snapshot(
        TlkDocument Document,
        FileFingerprint JsonFingerprint,
        FileFingerprint BinaryFingerprint,
        TlkFile VerifiedBinary);

    private sealed record CapturedFile(bool Exists, byte[] Content, FileFingerprint Fingerprint)
    {
        public static CapturedFile Capture(string path)
        {
            if (!File.Exists(path))
                return new CapturedFile(false, Array.Empty<byte>(), FileFingerprint.Missing);

            var content = File.ReadAllBytes(path);
            return new CapturedFile(true, content, FileFingerprint.FromContent(path, content));
        }
    }

    private sealed record FileFingerprint(bool Exists, DateTime LastWriteUtc, byte[] Hash)
    {
        public static FileFingerprint Missing { get; } =
            new(false, default, Array.Empty<byte>());

        public static FileFingerprint FromContent(string path, byte[] content) =>
            new(true, File.GetLastWriteTimeUtc(path), SHA256.HashData(content));

        public static FileFingerprint Capture(string path)
        {
            return CapturedFile.Capture(path).Fingerprint;
        }

        public bool Matches(string path)
        {
            if (!File.Exists(path))
                return !Exists;
            if (!Exists || File.GetLastWriteTimeUtc(path) != LastWriteUtc)
                return false;
            return SHA256.HashData(File.ReadAllBytes(path)).AsSpan().SequenceEqual(Hash);
        }
    }
}

public sealed class TlkExternalChangeException(string path)
    : IOException($"TLK source changed outside SWLOR Toolset: {path}")
{
    public string FilePath { get; } = path;
}
