using System.Security.Cryptography;
using System.Text.Json;
using Nwn.Authoring.Documents.Native;

namespace SWLOR.Toolset.Services;

/// <summary>Restores deletion journals written before module resources used shared transactions.</summary>
internal static class ModuleResourceDeletionLegacyRecovery
{
    private const string DeleteBackupSuffix = ".delete-backup";
    private const string DeleteTransactionSuffix = ".resource-delete-transaction.json";
    private const int DeleteTransactionVersion = 1;

    public static IReadOnlyList<string> Recover(string moduleRoot, string conversationRoot)
    {
        var recovered = new List<string>();
        foreach (var manifestPath in Directory.EnumerateFiles(
                     moduleRoot,
                     ".*" + DeleteTransactionSuffix,
                     SearchOption.TopDirectoryOnly))
        {
            try
            {
                var manifest = JsonSerializer.Deserialize<DeleteTransactionManifest>(File.ReadAllText(manifestPath))
                               ?? throw new InvalidDataException("manifest is empty");
                ValidateManifest(moduleRoot, conversationRoot, manifestPath, manifest);
                RecoverManifest(manifestPath, manifest);
                recovered.Add($"{manifest.Type.ToLowerInvariant()} '{manifest.ResRef}'");
            }
            catch (Exception exception) when (exception is not ModuleResourceDeleteRecoveryException)
            {
                throw new ModuleResourceDeleteRecoveryException(manifestPath, exception);
            }
        }

        return recovered;
    }

        private static string TransactionManifestPath(string moduleRoot, string transactionId) =>
            Path.Combine(moduleRoot, "." + transactionId + DeleteTransactionSuffix);

        private static void ValidateManifest(
            string moduleRoot,
            string conversationRoot,
            string manifestPath,
            DeleteTransactionManifest manifest)
        {
            if (manifest.Version != DeleteTransactionVersion)
                throw new InvalidDataException($"unsupported manifest version {manifest.Version}");
            if (!Guid.TryParseExact(manifest.TransactionId, "N", out _))
                throw new InvalidDataException("transaction id is invalid");

            var expectedManifestPath = TransactionManifestPath(moduleRoot, manifest.TransactionId);
            if (!PathsEqual(expectedManifestPath, manifestPath))
                throw new InvalidDataException("transaction id does not match the manifest filename");
            if (!PathsEqual(moduleRoot, manifest.ModuleRoot))
                throw new InvalidDataException("manifest belongs to a different module root");
            if (!Enum.TryParse<ResourceType>(manifest.Type, ignoreCase: false, out var type) ||
                type is not (ResourceType.Area or ResourceType.Dlg or ResourceType.Nss))
            {
                throw new InvalidDataException("resource type is invalid");
            }
            if (string.IsNullOrWhiteSpace(manifest.ResRef))
                throw new InvalidDataException("resource ResRef is missing");
            if (manifest.Entries.Count == 0)
                throw new InvalidDataException("manifest contains no resource files");

            var seenSources = new HashSet<string>(PathComparer);
            foreach (var entry in manifest.Entries)
            {
                var sourcePath = CanonicalManifestPath(entry.SourcePath, "source");
                var backupPath = CanonicalManifestPath(entry.BackupPath, "backup");
                if (!IsPathUnderRoot(moduleRoot, sourcePath) &&
                    !IsPathUnderRoot(conversationRoot, sourcePath))
                {
                    throw new InvalidDataException($"source path escapes the resource roots: {sourcePath}");
                }

                var expectedBackup = sourcePath + "." + manifest.TransactionId + DeleteBackupSuffix;
                if (!PathsEqual(expectedBackup, backupPath))
                    throw new InvalidDataException($"backup path does not match its source: {backupPath}");
                if (!seenSources.Add(sourcePath))
                    throw new InvalidDataException($"source path is duplicated: {sourcePath}");
                ValidateSha256(entry.SourceSha256, "resource");
            }

            if (manifest.IfoPath == null)
            {
                if (manifest.ExpectedIfoBase64 != null || manifest.UpdatedIfoSha256 != null)
                    throw new InvalidDataException("IFO recovery data has no IFO path");
                return;
            }

            var expectedIfoPath = Path.Combine(moduleRoot, "ifo", "module.ifo.json");
            if (!PathsEqual(expectedIfoPath, manifest.IfoPath))
                throw new InvalidDataException("IFO path is not this module's module.ifo.json");
            if (manifest.ExpectedIfoBase64 == null)
                throw new InvalidDataException("original IFO generation is missing");
            try
            {
                _ = Convert.FromBase64String(manifest.ExpectedIfoBase64);
            }
            catch (FormatException ex)
            {
                throw new InvalidDataException("original IFO generation is invalid", ex);
            }

            if (manifest.UpdatedIfoSha256 != null)
                ValidateSha256(manifest.UpdatedIfoSha256, "updated IFO");
        }

        private static void RecoverManifest(
            string manifestPath,
            DeleteTransactionManifest manifest)
        {
            foreach (var entry in manifest.Entries)
            {
                var sourceExists = File.Exists(entry.SourcePath);
                var backupExists = File.Exists(entry.BackupPath);
                if (sourceExists == backupExists)
                {
                    var state = sourceExists
                        ? "both the source and backup exist"
                        : "both the source and backup are missing";
                    throw new IOException($"cannot restore '{entry.SourcePath}': {state}");
                }

                var survivingPath = backupExists ? entry.BackupPath : entry.SourcePath;
                VerifySha256(survivingPath, entry.SourceSha256);
            }

            byte[]? expectedIfo = null;
            var restoreIfo = false;
            if (manifest.IfoPath != null && manifest.ExpectedIfoBase64 != null)
            {
                if (!File.Exists(manifest.IfoPath))
                    throw new FileNotFoundException("module.ifo.json is missing", manifest.IfoPath);

                expectedIfo = Convert.FromBase64String(manifest.ExpectedIfoBase64);
                var currentIfo = File.ReadAllBytes(manifest.IfoPath);
                if (currentIfo.AsSpan().SequenceEqual(expectedIfo))
                {
                    restoreIfo = false;
                }
                else if (manifest.UpdatedIfoSha256 != null &&
                         Convert.ToHexString(SHA256.HashData(currentIfo))
                             .Equals(manifest.UpdatedIfoSha256, StringComparison.OrdinalIgnoreCase))
                {
                    restoreIfo = true;
                }
                else
                {
                    throw new IOException(
                        "module.ifo.json changed after the interrupted delete; automatic recovery was refused");
                }
            }

            for (var index = manifest.Entries.Count - 1; index >= 0; index--)
            {
                var entry = manifest.Entries[index];
                if (File.Exists(entry.BackupPath))
                    File.Move(entry.BackupPath, entry.SourcePath, overwrite: false);
            }

            if (restoreIfo)
                WriteAtomicUnderLease(manifest.IfoPath!, expectedIfo!);

            File.Delete(manifestPath);
        }

        private static string CanonicalManifestPath(string path, string description)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidDataException($"{description} path is missing");
            var canonical = Path.GetFullPath(path);
            if (!PathsEqual(canonical, path))
                throw new InvalidDataException($"{description} path is not canonical: {path}");
            return canonical;
        }

        private static bool IsPathUnderRoot(string root, string candidate)
        {
            var normalizedRoot = Path.GetFullPath(root).TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var normalizedCandidate = Path.GetFullPath(candidate);
            return normalizedCandidate.StartsWith(
                normalizedRoot,
                OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal);
        }

        private static bool PathsEqual(string left, string right) =>
            string.Equals(Path.GetFullPath(left), Path.GetFullPath(right),
                OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal);

        private static void ValidateSha256(string value, string description)
        {
            if (value.Length != 64 || value.Any(character => !Uri.IsHexDigit(character)))
                throw new InvalidDataException($"{description} SHA-256 is invalid");
        }

        private static void VerifySha256(string path, string expected)
        {
            var actual = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
            if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
                throw new IOException($"'{path}' changed after the interrupted delete");
        }

        private static void WriteAtomicUnderLease(string path, byte[] bytes)
        {
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllBytes(temporary, bytes);
                File.Move(temporary, path, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
        }

        private static StringComparer PathComparer =>
            OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        private sealed class DeleteTransactionManifest
        {
            public int Version { get; set; }
            public string TransactionId { get; set; } = string.Empty;
            public string ModuleRoot { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string ResRef { get; set; } = string.Empty;
            public List<DeleteTransactionEntry> Entries { get; set; } = new();
            public string? IfoPath { get; set; }
            public string? ExpectedIfoBase64 { get; set; }
            public string? UpdatedIfoSha256 { get; set; }
        }

        private sealed class DeleteTransactionEntry
        {
            public string SourcePath { get; set; } = string.Empty;
            public string BackupPath { get; set; } = string.Empty;
            public string SourceSha256 { get; set; } = string.Empty;
        }

}
