using System.Text.Json;

namespace SWLOR.Toolset.Domain.Workspace
{
    /// <summary>Resolves the module archive name without requiring the configured output to exist yet.</summary>
    public static class ModuleFileNameResolver
    {
        public const string DefaultFileName = "Star Wars LOR v2.mod";

        public static string Read(string moduleRoot)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(moduleRoot);

            try
            {
                var configPath = Path.Combine(moduleRoot, "config.json");
                if (File.Exists(configPath))
                {
                    using var document = JsonDocument.Parse(File.ReadAllText(configPath));
                    if (document.RootElement.TryGetProperty("ModuleFileName", out var name) &&
                        name.GetString() is { } value && IsValidFileName(value))
                    {
                        // The archive is an output. A fresh workspace legitimately has no .mod yet,
                        // so existence cannot decide whether this explicit configuration is current.
                        return value;
                    }
                }
            }
            catch (Exception)
            {
                // Malformed/unreadable config falls through to the on-disk compatibility probe.
            }

            var existing = Directory.EnumerateFiles(moduleRoot, "*.mod")
                .Where(path => !Path.GetFileName(path)
                    .EndsWith(".packing.mod", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
            return existing != null ? Path.GetFileName(existing) : DefaultFileName;
        }

        private static bool IsValidFileName(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   string.Equals(value, value.Trim(), StringComparison.Ordinal) &&
                   string.Equals(Path.GetFileName(value), value, StringComparison.Ordinal) &&
                   string.Equals(Path.GetExtension(value), ".mod", StringComparison.OrdinalIgnoreCase) &&
                   value.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        }
    }
}
