using System.Text.Json;
using System.Security.Cryptography;
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
        private const string TransactionPrefix = ".swlor-toolset-item-rename-";

        public static Transaction Begin(
            string moduleRoot,
            string oldPath,
            string newPath,
            byte[] newContent)
        {
            ArgumentNullException.ThrowIfNull(newContent);
            moduleRoot = Path.GetFullPath(moduleRoot);
            oldPath = Path.GetFullPath(oldPath);
            newPath = Path.GetFullPath(newPath);
            RequirePathUnder(moduleRoot, oldPath, "original item");
            RequirePathUnder(moduleRoot, newPath, "renamed item");

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
                var categoryExisted = File.Exists(categoryPath);
                if (categoryExisted)
                    File.Copy(categoryPath, categoryBackupPath);

                var manifest = new Manifest
                {
                    TransactionRoot = transactionRoot,
                    OldPath = oldPath,
                    NewPath = newPath,
                    ItemBackupPath = itemBackupPath,
                    CategoryPath = categoryPath,
                    CategoryBackupPath = categoryBackupPath,
                    CategoryExisted = categoryExisted,
                    NewContentSha256 = Convert.ToHexString(SHA256.HashData(newContent))
                };
                WriteMarker(markerPath, manifest);
                return new Transaction(moduleRoot, markerPath, manifest);
            }
            catch
            {
                DeleteDirectoryBestEffort(transactionRoot);
                throw;
            }
        }

        public static IReadOnlyList<string> RecoverInterruptedRenames(string moduleRoot)
        {
            moduleRoot = Path.GetFullPath(moduleRoot);
            if (!Directory.Exists(moduleRoot))
                return Array.Empty<string>();

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
                File.Move(temporaryPath, markerPath);
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

                Directory.CreateDirectory(Path.GetDirectoryName(manifest.OldPath)!);
                File.Copy(manifest.ItemBackupPath, manifest.OldPath, overwrite: true);
                if (!string.Equals(
                        manifest.OldPath,
                        manifest.NewPath,
                        StringComparison.OrdinalIgnoreCase) &&
                    File.Exists(manifest.NewPath) &&
                    string.Equals(
                        Convert.ToHexString(
                            SHA256.HashData(File.ReadAllBytes(manifest.NewPath))),
                        manifest.NewContentSha256,
                        StringComparison.Ordinal))
                {
                    File.Delete(manifest.NewPath);
                }

                if (manifest.CategoryExisted)
                {
                    if (!File.Exists(manifest.CategoryBackupPath))
                        throw new FileNotFoundException(
                            "The original category sidecar backup is missing.",
                            manifest.CategoryBackupPath);
                    Directory.CreateDirectory(Path.GetDirectoryName(manifest.CategoryPath)!);
                    File.Copy(
                        manifest.CategoryBackupPath,
                        manifest.CategoryPath,
                        overwrite: true);
                }
                else
                {
                    File.Delete(manifest.CategoryPath);
                }

                File.Delete(markerPath);
                DeleteDirectoryBestEffort(manifest.TransactionRoot);
            }
            catch (Exception exception) when (exception is not ItemRenameRecoveryException)
            {
                throw new ItemRenameRecoveryException(
                    $"Could not recover interrupted item rename '{manifest.OldPath}'. " +
                    $"Recovery evidence remains at '{markerPath}'.",
                    exception);
            }
        }

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
            private bool _completed;

            internal Transaction(string moduleRoot, string markerPath, Manifest manifest)
            {
                _moduleRoot = moduleRoot;
                _markerPath = markerPath;
                _manifest = manifest;
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

            public void Dispose()
            {
                if (!_completed && File.Exists(_markerPath))
                    RollBack(_markerPath, _manifest);
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
            public string NewContentSha256 { get; set; } = string.Empty;
        }
    }

    internal sealed class ItemRenameRecoveryException(string message, Exception innerException)
        : IOException(message, innerException);
}
