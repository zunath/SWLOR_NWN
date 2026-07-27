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
    /// no interleaving.
    /// <para>
    /// The guild definitions break that shape in one specific, regular way: a private helper takes
    /// the quest id as a parameter and every quest is one call to it. Reading only what
    /// <c>Create(</c> literally names lost the Smithery, Fabrication, Agriculture and Engineering
    /// tasks entirely — 651 quests that <c>GameCodeIndex.Quests</c> did not have while
    /// <c>IsSourceScanAvailable</c> still said yes, so the quest dropdown could not select them and
    /// conversation analysis reported real quests as nonexistent. Those helpers are expanded at
    /// their call sites; anything else it cannot resolve is still left absent rather than guessed at.
    /// </para>
    /// </remarks>
    internal static class QuestSourceScanner
    {
        private static readonly Regex ConstStringRegex = new(
            @"const\s+string\s+(?<name>[A-Za-z_]\w*)\s*=\s*""(?<value>(?:[^""\\]|\\.)*)""\s*;",
            RegexOptions.Compiled);

        /// <summary>
        /// A <c>Create(id, name)</c> call. The name is optional in the pattern, not in the API: the
        /// guild helpers build it by interpolation (<c>$"Craft {amount}x {itemName}"</c>), and a
        /// pattern that insisted on a plain literal or an identifier there failed the whole match -
        /// losing the id as well, which is how four guilds' worth of tasks went unread.
        /// </summary>
        private static readonly Regex CreateCallRegex = new(
            @"(?:builder|_builder)\.Create\(\s*(?:""(?<idLiteral>(?:[^""\\]|\\.)*)""|(?<idIdentifier>[A-Za-z_]\w*))\s*(?:,\s*(?:""(?<nameLiteral>(?:[^""\\]|\\.)*)""|(?<nameIdentifier>[A-Za-z_]\w*)))?",
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

        private static readonly Regex MethodRegex = new(
            @"\b(?:private|public|protected|internal)\s+(?:static\s+)?[A-Za-z_]\w*(?:<[^>{}]+>)?\s+" +
            @"(?<name>[A-Za-z_]\w*)\s*\((?<parameters>[^)]*)\)\s*\{",
            RegexOptions.Compiled);

        private static readonly Regex HelperCreateCallRegex = new(
            @"(?:builder|_builder)\.Create\(\s*(?<idIdentifier>[A-Za-z_]\w*)\s*,\s*" +
            @"(?<nameExpression>[^\r\n,)]*)",
            RegexOptions.Compiled);

        /// <summary>
        /// A method declaration whose body follows immediately: <c>Name(params) {</c>. Nested
        /// parentheses in the parameter list are excluded rather than handled, which is enough for
        /// the helpers this needs to find and keeps a scanner from becoming a parser.
        /// </summary>
        private static readonly Regex HelperDeclarationRegex = new(
            @"(?<name>[A-Za-z_]\w*)\s*\(\s*(?<params>[^()]*)\)\s*\{",
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

                // One quest's chain runs from its Create call to the next one, or to end of file.
                var start = create.Index;
                var end = i + 1 < creates.Count ? creates[i + 1].Index : text.Length;
                var chain = text[start..end];

                var id = Resolve(create.Groups["idLiteral"], create.Groups["idIdentifier"], constants);
                if (id == null)
                {
                    // Not a literal and not a file constant. If it is a parameter of the enclosing
                    // helper, every call site supplies one - so the chain describes many quests
                    // rather than none.
                    if (create.Groups["idIdentifier"].Success)
                    {
                        ExpandHelperCalls(
                            text,
                            create.Groups["idIdentifier"].Value,
                            chain,
                            constants,
                            Path.GetFileName(filePath),
                            quests);
                    }

                    continue;
                }

                var name = Resolve(create.Groups["nameLiteral"], create.Groups["nameIdentifier"], constants) ?? id;

                quests[id] = ReadChain(id, name, chain, constants, Path.GetFileName(filePath));
            }

            ScanHelperBuiltQuests(
                text, constants, quests, Path.GetFileName(filePath));
            return true;
        }

        private static void ScanHelperBuiltQuests(
            string text,
            IReadOnlyDictionary<string, string> constants,
            Dictionary<string, QuestDefinitionInfo> quests,
            string sourceFile)
        {
            foreach (Match method in MethodRegex.Matches(text))
            {
                var bodyEnd = FindMatchingBrace(text, method.Index + method.Length - 1);
                if (bodyEnd < 0)
                    continue;

                var body = text[method.Index..(bodyEnd + 1)];
                var create = HelperCreateCallRegex.Match(body);
                if (!create.Success)
                    continue;

                var parameters = ParameterNames(method.Groups["parameters"].Value);
                var idIndex = parameters.FindIndex(name =>
                    string.Equals(name, create.Groups["idIdentifier"].Value, StringComparison.Ordinal));
                if (idIndex < 0)
                    continue;

                var nameExpression = create.Groups["nameExpression"].Value.Trim();
                var nameIndex = parameters.FindIndex(name =>
                    string.Equals(name, nameExpression, StringComparison.Ordinal));
                var literalName = ResolveArgument(nameExpression, constants);

                var callPattern = new Regex(
                    $@"\b{Regex.Escape(method.Groups["name"].Value)}\s*\((?<arguments>[^;]*)\)\s*;",
                    RegexOptions.CultureInvariant);
                foreach (Match call in callPattern.Matches(text))
                {
                    // The helper body ends in a Create chain, not a recursive call, but excluding
                    // its range also protects a future helper that delegates to itself conditionally.
                    if (call.Index >= method.Index && call.Index <= bodyEnd)
                        continue;

                    var arguments = SplitArguments(call.Groups["arguments"].Value);
                    if (idIndex >= arguments.Count)
                        continue;

                    var id = ResolveArgument(arguments[idIndex], constants);
                    if (id == null)
                        continue;

                    var name = literalName;
                    if (name == null && nameIndex >= 0 && nameIndex < arguments.Count)
                        name = ResolveArgument(arguments[nameIndex], constants);

                    quests[id] = ReadChain(id, name ?? id, body, constants, sourceFile);
                }
            }
        }

        private static List<string> ParameterNames(string parameters)
        {
            var names = new List<string>();
            foreach (var parameter in SplitArguments(parameters))
            {
                var withoutDefault = parameter.Split('=', 2)[0].Trim();
                var match = Regex.Match(withoutDefault, @"(?<name>[A-Za-z_]\w*)\s*$");
                if (match.Success)
                    names.Add(match.Groups["name"].Value);
            }

            return names;
        }

        private static List<string> SplitArguments(string value)
        {
            var arguments = new List<string>();
            var start = 0;
            var depth = 0;
            var inString = false;
            var escaped = false;
            for (var i = 0; i < value.Length; i++)
            {
                var character = value[i];
                if (inString)
                {
                    if (escaped)
                        escaped = false;
                    else if (character == '\\')
                        escaped = true;
                    else if (character == '"')
                        inString = false;
                    continue;
                }

                if (character == '"')
                {
                    inString = true;
                    continue;
                }

                if (character is '(' or '[' or '{')
                    depth++;
                else if (character is ')' or ']' or '}')
                    depth--;
                else if (character == ',' && depth == 0)
                {
                    arguments.Add(value[start..i].Trim());
                    start = i + 1;
                }
            }

            arguments.Add(value[start..].Trim());
            return arguments;
        }

        private static string? ResolveArgument(
            string argument,
            IReadOnlyDictionary<string, string> constants)
        {
            var literal = Regex.Match(argument, "^\"(?<value>(?:[^\"\\\\]|\\\\.)*)\"$");
            if (literal.Success)
                return Unescape(literal.Groups["value"].Value);

            return constants.TryGetValue(argument.Trim(), out var constant) ? constant : null;
        }

        private static int FindMatchingBrace(string text, int openingBrace)
        {
            var depth = 0;
            var inString = false;
            var inCharacter = false;
            var escaped = false;
            var lineComment = false;
            var blockComment = false;

            for (var i = openingBrace; i < text.Length; i++)
            {
                var character = text[i];
                var next = i + 1 < text.Length ? text[i + 1] : '\0';

                if (lineComment)
                {
                    if (character == '\n')
                        lineComment = false;
                    continue;
                }

                if (blockComment)
                {
                    if (character == '*' && next == '/')
                    {
                        blockComment = false;
                        i++;
                    }
                    continue;
                }

                if (inString || inCharacter)
                {
                    if (escaped)
                        escaped = false;
                    else if (character == '\\')
                        escaped = true;
                    else if (inString && character == '"')
                        inString = false;
                    else if (inCharacter && character == '\'')
                        inCharacter = false;
                    continue;
                }

                if (character == '/' && next == '/')
                {
                    lineComment = true;
                    i++;
                    continue;
                }
                if (character == '/' && next == '*')
                {
                    blockComment = true;
                    i++;
                    continue;
                }
                if (character == '"')
                {
                    inString = true;
                    continue;
                }
                if (character == '\'')
                {
                    inCharacter = true;
                    continue;
                }

                if (character == '{')
                    depth++;
                else if (character == '}' && --depth == 0)
                    return i;
            }

            return -1;
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

        /// <summary>
        /// Records one quest per call site of the helper whose <paramref name="parameterName"/>
        /// supplies the id.
        /// </summary>
        /// <remarks>
        /// Everything but the id is shared: the helper's chain declares the same states, the same
        /// repeatability and the same gates for every quest it builds. The name is interpolated from
        /// the helper's other arguments and so cannot be read here, which leaves the id standing in
        /// for it - a task listed as "eng_tsk_001" is still a task the dropdown can select, where
        /// before it was not there at all.
        /// </remarks>
        private static void ExpandHelperCalls(
            string text,
            string parameterName,
            string chain,
            IReadOnlyDictionary<string, string> constants,
            string sourceFile,
            Dictionary<string, QuestDefinitionInfo> quests)
        {
            foreach (Match declaration in HelperDeclarationRegex.Matches(text))
            {
                var parameters = SplitArguments(declaration.Groups["params"].Value);
                var index = parameters.FindIndex(parameter => DeclaresParameter(parameter, parameterName));
                if (index < 0)
                    continue;

                var helperName = declaration.Groups["name"].Value;
                foreach (var id in CallSiteLiterals(text, helperName, index))
                {
                    // A real Create() for this id wins: it was read from the quest's own chain.
                    if (!quests.ContainsKey(id))
                        quests[id] = ReadChain(id, id, chain, constants, sourceFile);
                }
            }
        }

        /// <summary>Whether one declared parameter is <c>string &lt;name&gt;</c>.</summary>
        private static bool DeclaresParameter(string parameter, string name)
        {
            var trimmed = parameter.Trim();
            return trimmed.StartsWith("string ", StringComparison.Ordinal) &&
                   trimmed[7..].Trim() == name;
        }

        /// <summary>Every string literal passed at <paramref name="index"/> to a named method.</summary>
        private static IEnumerable<string> CallSiteLiterals(string text, string methodName, int index)
        {
            foreach (Match call in Regex.Matches(text, @"\b" + Regex.Escape(methodName) + @"\s*\("))
            {
                var open = call.Index + call.Length - 1;
                var close = FindMatchingParenthesis(text, open);
                if (close < 0)
                    continue;

                var arguments = SplitArguments(text[(open + 1)..close]);
                if (index >= arguments.Count)
                    continue;

                var argument = arguments[index].Trim();
                if (argument.Length >= 2 && argument[0] == '"' && argument[^1] == '"')
                    yield return Unescape(argument[1..^1]);
            }
        }

        private static int FindMatchingParenthesis(string text, int open)
        {
            var depth = 0;
            var inString = false;

            for (var i = open; i < text.Length; i++)
            {
                var c = text[i];

                if (inString)
                {
                    if (c == '\\')
                        i++;
                    else if (c == '"')
                        inString = false;
                    continue;
                }

                switch (c)
                {
                    case '"':
                        inString = true;
                        break;
                    case '(':
                        depth++;
                        break;
                    case ')':
                        if (--depth == 0)
                            return i;
                        break;
                }
            }

            return -1;
        }

        /// <summary>Splits an argument or parameter list at its top-level commas.</summary>
        private static List<string> SplitArguments(string text)
        {
            var parts = new List<string>();
            var depth = 0;
            var inString = false;
            var start = 0;

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                if (inString)
                {
                    if (c == '\\')
                        i++;
                    else if (c == '"')
                        inString = false;
                    continue;
                }

                switch (c)
                {
                    case '"':
                        inString = true;
                        break;
                    case '(' or '[' or '<' or '{':
                        depth++;
                        break;
                    case ')' or ']' or '>' or '}':
                        depth--;
                        break;
                    case ',' when depth == 0:
                        parts.Add(text[start..i]);
                        start = i + 1;
                        break;
                }
            }

            if (start <= text.Length)
                parts.Add(text[start..]);

            return parts;
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
