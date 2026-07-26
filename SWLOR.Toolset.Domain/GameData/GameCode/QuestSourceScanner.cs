using System.Text.RegularExpressions;

namespace SWLOR.Toolset.Domain.GameData.GameCode
{
    /// <summary>
    /// Everything a conversation editor needs to know about a quest without running the game: what
    /// it is called, how many steps it has, what the journal says at each, and what gates it.
    /// </summary>
    public sealed class QuestDefinitionInfo
    {
        public required string Id { get; init; }

        /// <summary>The player-facing name from <c>QuestBuilder.Create(id, name)</c>.</summary>
        public required string Name { get; init; }

        /// <summary>
        /// How many states the quest declares. State 1 is the accepted state and the last is the
        /// turn-in, which is what lets a coverage strip know how many cells to draw.
        /// </summary>
        public required int StateCount { get; init; }

        /// <summary>Journal text by 1-based state number, where the definition sets one.</summary>
        public required IReadOnlyDictionary<int, string> JournalTextByState { get; init; }

        public required bool IsRepeatable { get; init; }

        public required IReadOnlyList<string> PrerequisiteQuestIds { get; init; }

        public required IReadOnlyList<string> PrerequisiteKeyItems { get; init; }

        /// <summary>Skill gates as declared, e.g. ("BeastMastery", 20).</summary>
        public required IReadOnlyList<(string Skill, int Rank)> PrerequisiteSkills { get; init; }

        /// <summary>The definition file this was read from, for a jump-to-source link.</summary>
        public required string SourceFile { get; init; }

        public override string ToString() => $"{Name} ({Id})";
    }

    /// <summary>
    /// Reads quest definitions out of the SWLOR.Game.Server source tree. Quests are declared as
    /// fluent <c>QuestBuilder</c> chains inside method bodies, so — like the ids
    /// <see cref="SourceIdScanner"/> already recovers — none of this is visible to reflection.
    /// </summary>
    /// <remarks>
    /// Deliberately a scanner rather than a parser. It splits each file at <c>Create(</c> calls and
    /// reads the chain that follows, which is exactly as much structure as the repo's own convention
    /// guarantees: one <c>_builder.Create(...)</c> per quest, followed by its own fluent calls, with
    /// no interleaving. Anything it cannot resolve is left absent rather than guessed at, so a quest
    /// whose id comes through a helper parameter simply has no entry — the same limitation, and for
    /// the same reason, as the id scanner it sits beside.
    /// </remarks>
    internal static class QuestSourceScanner
    {
        private static readonly Regex ConstStringRegex = new(
            @"const\s+string\s+(?<name>[A-Za-z_]\w*)\s*=\s*""(?<value>(?:[^""\\]|\\.)*)""\s*;",
            RegexOptions.Compiled);

        private static readonly Regex CreateCallRegex = new(
            @"(?:builder|_builder)\.Create\(\s*(?:""(?<idLiteral>(?:[^""\\]|\\.)*)""|(?<idIdentifier>[A-Za-z_]\w*))\s*,\s*(?:""(?<nameLiteral>(?:[^""\\]|\\.)*)""|(?<nameIdentifier>[A-Za-z_]\w*))",
            RegexOptions.Compiled);

        private static readonly Regex AddStateRegex = new(@"\.AddState\(\s*\)", RegexOptions.Compiled);

        private static readonly Regex JournalTextRegex = new(
            @"\.SetStateJournalText\(\s*""(?<text>(?:[^""\\]|\\.)*)""",
            RegexOptions.Compiled);

        private static readonly Regex RepeatableRegex = new(@"\.IsRepeatable\(\s*\)", RegexOptions.Compiled);

        private static readonly Regex PrerequisiteQuestRegex = new(
            @"\.PrerequisiteQuest\(\s*(?:""(?<literal>(?:[^""\\]|\\.)*)""|(?<identifier>[A-Za-z_]\w*))",
            RegexOptions.Compiled);

        private static readonly Regex PrerequisiteKeyItemRegex = new(
            @"\.PrerequisiteKeyItem\(\s*KeyItemType\.(?<name>\w+)",
            RegexOptions.Compiled);

        private static readonly Regex PrerequisiteSkillRegex = new(
            @"\.PrerequisiteSkill\(\s*SkillType\.(?<name>\w+)\s*,\s*(?<rank>\d+)",
            RegexOptions.Compiled);

        /// <summary>
        /// Scans every *.cs file under <paramref name="directoryPath"/> for quest definitions.
        /// Returns an empty map (never throws) if the directory is missing or unreadable.
        /// </summary>
        public static Dictionary<string, QuestDefinitionInfo> Scan(string directoryPath, out bool complete)
        {
            var quests = new Dictionary<string, QuestDefinitionInfo>(StringComparer.Ordinal);
            complete = true;

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(directoryPath, "*.cs", SearchOption.AllDirectories).ToList();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                complete = false;
                return quests;
            }

            foreach (var file in files)
            {
                if (!ScanFile(file, quests))
                    complete = false;
            }

            return quests;
        }

        private static bool ScanFile(string filePath, Dictionary<string, QuestDefinitionInfo> quests)
        {
            string text;
            try
            {
                text = File.ReadAllText(filePath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return false;
            }

            var constants = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Match match in ConstStringRegex.Matches(text))
                constants[match.Groups["name"].Value] = match.Groups["value"].Value;

            var creates = CreateCallRegex.Matches(text);
            for (var i = 0; i < creates.Count; i++)
            {
                var create = creates[i];
                var id = Resolve(create.Groups["idLiteral"], create.Groups["idIdentifier"], constants);
                if (id == null)
                    continue;

                var name = Resolve(create.Groups["nameLiteral"], create.Groups["nameIdentifier"], constants) ?? id;

                // One quest's chain runs from its Create call to the next one, or to end of file.
                var start = create.Index;
                var end = i + 1 < creates.Count ? creates[i + 1].Index : text.Length;
                var chain = text[start..end];

                quests[id] = ReadChain(id, name, chain, constants, Path.GetFileName(filePath));
            }

            return true;
        }

        private static QuestDefinitionInfo ReadChain(
            string id,
            string name,
            string chain,
            IReadOnlyDictionary<string, string> constants,
            string sourceFile)
        {
            // Journal text belongs to whichever AddState() precedes it, so both are walked in
            // document order rather than counted separately.
            var journalByState = new Dictionary<int, string>();
            var stateCount = 0;
            foreach (Match match in Regex.Matches(chain, @"\.AddState\(\s*\)|\.SetStateJournalText\(\s*""(?<text>(?:[^""\\]|\\.)*)"""))
            {
                if (match.Value.StartsWith(".AddState", StringComparison.Ordinal))
                {
                    stateCount++;
                    continue;
                }

                if (stateCount > 0)
                    journalByState[stateCount] = Unescape(match.Groups["text"].Value);
            }

            // Prerequisites are resolved through the file's constants as well as from literals. The
            // capstone chains declare every quest id as a private const and refer to it by name, so
            // reading only literals loses the prerequisite on precisely the quests that have the
            // longest chains - and a chain whose links are invisible looks like a pile of offers
            // that all fire at once.
            var prerequisiteQuests = new List<string>();
            foreach (Match match in PrerequisiteQuestRegex.Matches(chain))
            {
                var resolved = Resolve(match.Groups["literal"], match.Groups["identifier"], constants);
                if (resolved != null)
                    prerequisiteQuests.Add(resolved);
            }

            var prerequisiteKeyItems = PrerequisiteKeyItemRegex.Matches(chain)
                .Select(match => match.Groups["name"].Value)
                .ToList();

            var prerequisiteSkills = PrerequisiteSkillRegex.Matches(chain)
                .Select(match => (match.Groups["name"].Value, int.Parse(match.Groups["rank"].Value)))
                .ToList();

            return new QuestDefinitionInfo
            {
                Id = id,
                Name = name,
                StateCount = stateCount,
                JournalTextByState = journalByState,
                IsRepeatable = RepeatableRegex.IsMatch(chain),
                PrerequisiteQuestIds = prerequisiteQuests,
                PrerequisiteKeyItems = prerequisiteKeyItems,
                PrerequisiteSkills = prerequisiteSkills,
                SourceFile = sourceFile
            };
        }

        private static string? Resolve(Group literal, Group identifier, IReadOnlyDictionary<string, string> constants)
        {
            if (literal.Success)
                return Unescape(literal.Value);

            return identifier.Success && constants.TryGetValue(identifier.Value, out var resolved)
                ? resolved
                : null;
        }

        private static string Unescape(string value) =>
            value.Replace("\\\"", "\"").Replace("\\\\", "\\");
    }
}
