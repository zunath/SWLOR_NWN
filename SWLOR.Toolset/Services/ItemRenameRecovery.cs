using System.Text.Json;
using System.Security.Cryptography;
using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Categories;

namespace SWLOR.Toolset.Services
{
    /// <summary>
    /// Makes an item rename recoverable across process or machine interruption. The marker is
    /// written before either the item or its category sidecar changes and is removed only after
    /// the old item has been deleted.
    /// </summary>
    internal static class ItemRenameRecovery
    {
        private const string MarkerPattern = ".swlor-toolset-item-rename-*.pending.json";
        internal const string TransactionPrefix = ".swlor-toolset-item-rename-";

        public static Transaction Begin(
            string moduleRoot,
            string oldPath,
            string newPath,
            byte[] newContent,
            byte[] expectedOriginalContentHash)
        {
            ArgumentNullException.ThrowIfNull(newContent);
            ArgumentNullException.ThrowIfNull(expectedOriginalContentHash);
            if (expectedOriginalContentHash.Length != SHA256.HashSizeInBytes)
            {
                throw new ArgumentException(
                    "The original item fingerprint must be SHA-256.",
                    nameof(expectedOriginalContentHash));
            }
            moduleRoot = Path.GetFullPath(moduleRoot);
            oldPath = Path.GetFullPath(oldPath);
            newPath = Path.GetFullPath(newPath);
            RequirePathUnder(moduleRoot, oldPath, "original item");
            RequirePathUnder(moduleRoot, newPath, "renamed item");
            var moduleWriteLock = ModuleWriteLock.Acquire(moduleRoot);

            var transactionName = TransactionPrefix + Guid.NewGuid().ToString("N");
            var transactionRoot = Path.Combine(moduleRoot, transactionName);
            var markerPath = transactionRoot + ".pending.json";
            var itemBackupPath = Path.Combine(transactionRoot, "item.original");
            var categoryPath = CategoryCatalog.DefaultPathFor(moduleRoot);
            var categoryBackupPath = Path.Combine(transactionRoot, "categories.original");

            Directory.CreateDirectory(transactionRoot);
            try
            {
                File.Copy(oldPath, itemBackupPath);
                var backedUpOriginalHash = SHA256.HashData(File.ReadAllBytes(itemBackupPath));
                if (!backedUpOriginalHash.AsSpan().SequenceEqual(expectedOriginalContentHash))
                {
                    throw new IOException(
                        $"The original item '{oldPath}' changed while the rename transaction was starting.");
                }

                var categoryExisted = File.Exists(categoryPath);
                if (categoryExisted)
                    File.Copy(categoryPath, categoryBackupPath);
                var categoryOriginalHash = categoryExisted
                    ? Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(categoryBackupPath)))
                    : string.Empty;

                var manifest = new Manifest
                {
                    TransactionRoot = transactionRoot,
                    OldPath = oldPath,
                    NewPath = newPath,
                    ItemBackupPath = itemBackupPath,
                    CategoryPath = categoryPath,
                    CategoryBackupPath = categoryBackupPath,
                    CategoryExisted = categoryExisted,
                    CaseOnlyRename =
                        !string.Equals(oldPath, newPath, StringComparison.Ordinal) &&
                        string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase),
                    CategoryOriginalContentSha256 = categoryOriginalHash,
                    OriginalContentSha256 = Convert.ToHexString(expectedOriginalContentHash),
                    NewContentSha256 = Convert.ToHexString(SHA256.HashData(newContent))
                };
                WriteMarker(markerPath, manifest);
                return new Transaction(moduleRoot, markerPath, manifest, moduleWriteLock);
            }
            catch
            {
                DeleteDirectoryBestEffort(transactionRoot);
                moduleWriteLock.Dispose();
                throw;
            }
        }

        public static IReadOnlyList<string> RecoverInterruptedRenames(string moduleRoot)
        {
            moduleRoot = Path.GetFullPath(moduleRoot);
            if (!Directory.Exists(moduleRoot))
                return Array.Empty<string>();

            using var moduleWriteLock = ModuleWriteLock.Acquire(moduleRoot);
            var recovered = new List<string>();
            foreach (var markerPath in Directory.EnumerateFiles(
                         moduleRoot,
                         MarkerPattern,
                         SearchOption.TopDirectoryOnly))
            {
                var manifest = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(markerPath))
                               ?? throw new InvalidDataException(
                                   $"Item rename recovery marker '{markerPath}' is empty.");
                ValidateManifest(moduleRoot, markerPath, manifest);
                RollBack(markerPath, manifest);
                recovered.Add(manifest.OldPath);
            }

            return recovered;
        }

        private static void WriteMarker(string markerPath, Manifest manifest)
        {
            var temporaryPath = markerPath + ".tmp";
            try
            {
                File.WriteAllText(
                    temporaryPath,
                    JsonSerializer.Serialize(
                        manifest,
                        new JsonSerializerOptions { WriteIndented = true }));
                File.Move(temporaryPath, markerPath, overwrite: true);
            }
            finally
            {
                File.Delete(temporaryPath);
            }
        }

        private static void ValidateManifest(
            string moduleRoot,
            string markerPath,
            Manifest manifest)
        {
            var expectedCategoryPath = Path.GetFullPath(CategoryCatalog.DefaultPathFor(moduleRoot));
            var transactionRoot = Path.GetFullPath(manifest.TransactionRoot);
            var expectedMarkerPath = transactionRoot + ".pending.json";
            RequirePathUnder(moduleRoot, transactionRoot, "transaction directory");
            if (!Path.GetFileName(transactionRoot).StartsWith(
                    TransactionPrefix,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Item rename recovery marker '{markerPath}' names an invalid transaction directory.");
            }

            if (!string.Equals(
                    Path.GetFullPath(markerPath),
                    expectedMarkerPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    $"Item rename recovery marker '{markerPath}' does not match its transaction.");
            }

            RequirePathUnder(moduleRoot, manifest.OldPath, "original item");
            RequirePathUnder(moduleRoot, manifest.NewPath, "renamed item");
            if (manifest.CaseOnlyRename &&
                (string.Equals(manifest.OldPath, manifest.NewPath, StringComparison.Ordinal) ||
                 !string.Equals(
                     manifest.OldPath,
                     manifest.NewPath,
                     StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidDataException(
                    $"Item rename recovery marker '{markerPath}' has invalid case-only paths.");
            }
            RequirePathUnder(transactionRoot, manifest.ItemBackupPath, "item backup");
            RequirePathUnder(transactionRoot, manifest.CategoryBackupPath, "category backup");
            if (!string.Equals(
                    Path.GetFullPath(manifest.CategoryPath),
                    expectedCategoryPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    $"Item rename recovery marker '{markerPath}' names an unexpected category sidecar.");
            }

            if (manifest.NewContentSha256.Length != 64 ||
                manifest.NewContentSha256.Any(character =>
                    character is not (>= '0' and <= '9' or >= 'A' and <= 'F')))
            {
                throw new InvalidDataException(
                    $"Item rename recovery marker '{markerPath}' has an invalid destination fingerprint.");
            }

            // Markers written before the original fingerprint was introduced remain recoverable.
            if (manifest.OriginalContentSha256.Length != 0 &&
                (manifest.OriginalContentSha256.Length != 64 ||
                 manifest.OriginalContentSha256.Any(character =>
                     character is not (>= '0' and <= '9' or >= 'A' and <= 'F'))))
            {
                throw new InvalidDataException(
                    $"Item rename recovery marker '{markerPath}' has an invalid original fingerprint.");
            }

            foreach (var (value, description) in new[]
                     {
                         (manifest.CategoryOriginalContentSha256, "original category"),
                         (manifest.CategoryInstalledContentSha256, "installed category")
                     })
            {
                if (value.Length != 0 &&
                    (value.Length != 64 ||
                     value.Any(character =>
                         character is not (>= '0' and <= '9' or >= 'A' and <= 'F'))))
                {
                    throw new InvalidDataException(
                        $"Item rename recovery marker '{markerPath}' has an invalid {description} fingerprint.");
                }
            }
        }

        private static void RequirePathUnder(string root, string path, string description)
        {
            root = Path.GetFullPath(root);
            path = Path.GetFullPath(path);
            var relative = Path.GetRelativePath(root, path);
            if (relative == ".." ||
                relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
                Path.IsPathRooted(relative))
            {
                throw new InvalidDataException(
                    $"The item rename {description} is outside the expected directory.");
            }
        }

        private static void RollBack(string markerPath, Manifest manifest)
        {
            try
            {
                if (!File.Exists(manifest.ItemBackupPath))
                    throw new FileNotFoundException(
                        "The original item backup is missing.",
                        manifest.ItemBackupPath);

                ValidateCategoryGeneration(manifest);

                RollBackItem(manifest);

                if (manifest.CategoryExisted)
                {
                    var currentHash = ContentHash(manifest.CategoryPath);
                    var originalHash = CategoryOriginalHash(manifest);
                    if (!string.Equals(currentHash, originalHash, StringComparison.Ordinal))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(manifest.CategoryPath)!);
                        File.Copy(
                            manifest.CategoryBackupPath,
                            manifest.CategoryPath,
                            overwrite: true);
                    }
                }
                else
                {
                    if (File.Exists(manifest.CategoryPath))
                        File.Delete(manifest.CategoryPath);
                }

                File.Delete(markerPath);
                DeleteDirectoryBestEffort(manifest.TransactionRoot);
            }
            catch (Exception exception) when (exception is not ItemRenameRecoveryException)
            {
                throw new ItemRenameRecoveryException(
                    $"Could not recover interrupted item rename '{manifest.OldPath}'. " +
                    $"Recovery evidence remains at '{markerPath}': {exception.Message}",
                    exception);
            }
        }

        private static void RollBackItem(Manifest manifest)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(manifest.OldPath)!);
            if (manifest.CaseOnlyRename)
            {
                var currentPath = File.Exists(manifest.NewPath)
                    ? manifest.NewPath
                    : File.Exists(manifest.OldPath)
                        ? manifest.OldPath
                        : null;
                if (currentPath != null)
                {
                    var currentHash = ContentHash(currentPath);
                    if (string.Equals(
                            currentHash,
                            manifest.NewContentSha256,
                            StringComparison.Ordinal))
                    {
                        File.Delete(currentPath);
                    }
                    else if (manifest.OriginalContentSha256.Length != 0 &&
                             !string.Equals(
                                 currentHash,
                                 manifest.OriginalContentSha256,
                                 StringComparison.Ordinal))
                    {
                        // A newer external generation owns the case-insensitive path. Preserve it
                        // just as the ordinary rename recovery path preserves a changed original.
                        return;
                    }
                }

                File.Copy(manifest.ItemBackupPath, manifest.OldPath, overwrite: true);
                return;
            }

            var preserveExternallyChangedOriginal =
                manifest.OriginalContentSha256.Length != 0 &&
                File.Exists(manifest.OldPath) &&
                !string.Equals(
                    ContentHash(manifest.OldPath),
                    manifest.OriginalContentSha256,
                    StringComparison.Ordinal);
            if (!preserveExternallyChangedOriginal)
                File.Copy(manifest.ItemBackupPath, manifest.OldPath, overwrite: true);
            if (!string.Equals(
                    manifest.OldPath,
                    manifest.NewPath,
                    StringComparison.OrdinalIgnoreCase) &&
                File.Exists(manifest.NewPath) &&
                string.Equals(
                    ContentHash(manifest.NewPath),
                    manifest.NewContentSha256,
                    StringComparison.Ordinal))
            {
                File.Delete(manifest.NewPath);
            }
        }

        private static void ValidateCategoryGeneration(Manifest manifest)
        {
            if (manifest.CategoryExisted && !File.Exists(manifest.CategoryBackupPath))
            {
                throw new FileNotFoundException(
                    "The original category sidecar backup is missing.",
                    manifest.CategoryBackupPath);
            }

            if (!File.Exists(manifest.CategoryPath))
            {
                if (manifest.CategoryExisted)
                {
                    throw new IOException(
                        $"The category sidecar '{manifest.CategoryPath}' disappeared after the " +
                        "interrupted rename. Recovery was refused.");
                }

                return;
            }

            var currentHash = ContentHash(manifest.CategoryPath);
            if (manifest.CategoryExisted &&
                string.Equals(
                    currentHash,
                    CategoryOriginalHash(manifest),
                    StringComparison.Ordinal))
            {
                return;
            }

            if (manifest.CategoryInstalledContentSha256.Length != 0 &&
                string.Equals(
                    currentHash,
                    manifest.CategoryInstalledContentSha256,
                    StringComparison.Ordinal))
            {
                return;
            }

            throw new IOException(
                $"The category sidecar '{manifest.CategoryPath}' changed after the interrupted " +
                "item rename. Recovery was refused so the newer file is preserved.");
        }

        private static string CategoryOriginalHash(Manifest manifest) =>
            manifest.CategoryOriginalContentSha256.Length != 0
                ? manifest.CategoryOriginalContentSha256
                : ContentHash(manifest.CategoryBackupPath);

        private static string ContentHash(string path) =>
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

        private static void DeleteDirectoryBestEffort(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, recursive: true);
            }
            catch
            {
                // A completed transaction does not become unsafe because obsolete backups could
                // not be cleaned up. A live marker is the authority for whether recovery is needed.
            }
        }

        internal sealed class Transaction : IDisposable
        {
            private readonly string _moduleRoot;
            private readonly string _markerPath;
            private readonly Manifest _manifest;
            private readonly ModuleWriteLock _moduleWriteLock;
            private bool _completed;

            internal Transaction(
                string moduleRoot,
                string markerPath,
                Manifest manifest,
                ModuleWriteLock moduleWriteLock)
            {
                _moduleRoot = moduleRoot;
                _markerPath = markerPath;
                _manifest = manifest;
                _moduleWriteLock = moduleWriteLock;
            }

            public void Complete()
            {
                if (_completed)
                    return;

                ValidateManifest(_moduleRoot, _markerPath, _manifest);
                File.Delete(_markerPath);
                _completed = true;
                DeleteDirectoryBestEffort(_manifest.TransactionRoot);
            }

            public bool OriginalStillMatches()
            {
                if (_manifest.OriginalContentSha256.Length == 0 ||
                    !File.Exists(_manifest.OldPath))
                {
                    return false;
                }

                return string.Equals(
                    Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(_manifest.OldPath))),
                    _manifest.OriginalContentSha256,
                    StringComparison.Ordinal);
            }

            public void RecordCategoryGeneration(string? installedContentSha256)
            {
                if (_completed)
                    throw new InvalidOperationException("The item rename transaction is already complete.");

                if (installedContentSha256 != null &&
                    (installedContentSha256.Length != 64 ||
                     installedContentSha256.Any(character =>
                         character is not (>= '0' and <= '9' or >= 'A' and <= 'F'))))
                {
                    throw new InvalidDataException(
                        "The installed category fingerprint must be uppercase SHA-256.");
                }

                var categoryExists = File.Exists(_manifest.CategoryPath);
                var currentHash = categoryExists
                    ? ContentHash(_manifest.CategoryPath)
                    : string.Empty;
                if (installedContentSha256 != null)
                {
                    if (!string.Equals(
                            currentHash,
                            installedContentSha256,
                            StringComparison.Ordinal))
                    {
                        throw new IOException(
                            $"The category sidecar '{_manifest.CategoryPath}' changed after it was " +
                            "refiled. The item rename was not committed.");
                    }
                }
                else if ((_manifest.CategoryExisted &&
                          (!categoryExists ||
                           !string.Equals(
                               currentHash,
                               CategoryOriginalHash(_manifest),
                               StringComparison.Ordinal))) ||
                         (!_manifest.CategoryExisted && categoryExists))
                {
                    throw new IOException(
                        $"The category sidecar '{_manifest.CategoryPath}' changed during the item rename.");
                }

                _manifest.CategoryInstalledContentSha256 =
                    installedContentSha256 ?? string.Empty;
                WriteMarker(_markerPath, _manifest);
            }

            public void Dispose()
            {
                try
                {
                    if (!_completed && File.Exists(_markerPath))
                        RollBack(_markerPath, _manifest);
                }
                finally
                {
                    _moduleWriteLock.Dispose();
                }
            }
        }

        internal sealed class Manifest
        {
            public string TransactionRoot { get; set; } = string.Empty;
            public string OldPath { get; set; } = string.Empty;
            public string NewPath { get; set; } = string.Empty;
            public string ItemBackupPath { get; set; } = string.Empty;
            public string CategoryPath { get; set; } = string.Empty;
            public string CategoryBackupPath { get; set; } = string.Empty;
            public bool CategoryExisted { get; set; }
            public bool CaseOnlyRename { get; set; }
            public string CategoryOriginalContentSha256 { get; set; } = string.Empty;
            public string CategoryInstalledContentSha256 { get; set; } = string.Empty;
            public string OriginalContentSha256 { get; set; } = string.Empty;
            public string NewContentSha256 { get; set; } = string.Empty;
        }
    }

    internal sealed class ItemRenameRecoveryException(string message, Exception innerException)
        : IOException(message, innerException);
}
