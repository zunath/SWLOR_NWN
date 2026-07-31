using System.Text.RegularExpressions;
using SWLOR.Game.Server.Service.NPCService;

namespace SWLOR.Toolset.Domain.GameData.GameCode
{
    /// <summary>Best-effort reverse index from NPC kill groups to authored quest ids.</summary>
    internal static class NpcGroupQuestSourceScanner
    {
        private static readonly Regex CreateRegex = new(
            "(?:builder|_builder)\\.Create\\(\\s*\"(?<id>[^\"]+)\"",
            RegexOptions.Compiled);
        private static readonly Regex KillRegex = new(
            @"\.AddKillObjective\(\s*NPCGroupType\.(?<group>[A-Za-z0-9_]+)",
            RegexOptions.Compiled);

        public static IReadOnlyDictionary<int, IReadOnlyList<string>> Scan(string? directory)
        {
            var result = new Dictionary<int, HashSet<string>>();
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                return new Dictionary<int, IReadOnlyList<string>>();

            try
            {
                foreach (var path in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
                {
                    var text = File.ReadAllText(path);
                    var creates = CreateRegex.Matches(text);
                    foreach (Match kill in KillRegex.Matches(text))
                    {
                        var create = creates.Cast<Match>().LastOrDefault(candidate => candidate.Index < kill.Index);
                        if (create == null ||
                            !Enum.TryParse<NPCGroupType>(kill.Groups["group"].Value, out var group))
                            continue;

                        var id = create.Groups["id"].Value;
                        var value = Convert.ToInt32(group);
                        if (!result.TryGetValue(value, out var quests))
                        {
                            quests = new HashSet<string>(StringComparer.Ordinal);
                            result[value] = quests;
                        }
                        quests.Add(id);
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
            }

            return result.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<string>)pair.Value.Order(StringComparer.Ordinal).ToList());
        }
    }
}
