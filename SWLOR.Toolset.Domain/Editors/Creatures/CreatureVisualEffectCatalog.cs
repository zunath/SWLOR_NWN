using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.GameData.GameCode;

namespace SWLOR.Toolset.Domain.Editors.Creatures
{
    /// <summary>Permanent creature visuals proven by the reference metadata to be duration effects.</summary>
    public static class CreatureVisualEffectCatalog
    {
        public static IReadOnlyList<BehaviorChoice> Build(
            IReadOnlyDictionary<int, string> visualEffects,
            IReadOnlyDictionary<int, VisualEffectReferenceInfo> references)
        {
            ArgumentNullException.ThrowIfNull(visualEffects);
            ArgumentNullException.ThrowIfNull(references);
            if (references.Count == 0)
                return Array.Empty<BehaviorChoice>();

            return visualEffects
                .Where(entry => references.TryGetValue(entry.Key, out var reference) &&
                                string.Equals(reference.Group, "DUR", StringComparison.OrdinalIgnoreCase))
                .OrderBy(entry => entry.Value, StringComparer.OrdinalIgnoreCase)
                .Select(entry =>
                {
                    var reference = references[entry.Key];
                    return new BehaviorChoice(
                        entry.Key,
                        $"{Humanize(entry.Value)} ({entry.Key})",
                        imageUrl: reference.ImageUrl);
                })
                .ToList();
        }

        private static string Humanize(string value) =>
            System.Text.RegularExpressions.Regex.Replace(value, "([a-z0-9])([A-Z])", "$1 $2")
                .Replace('_', ' ');
    }
}
