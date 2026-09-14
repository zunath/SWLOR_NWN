namespace SWLOR.Game.Server.Service.SnippetService
{
    /// <summary>
    /// What a snippet argument means, so a conversation editor can offer the right picker instead of
    /// a text box. The runtime does not branch on this - every argument still arrives as a string -
    /// but declaring it is what lets a quest id be chosen from the real quest list rather than typed
    /// and mistyped.
    /// </summary>
    public enum SnippetArgumentType
    {
        /// <summary>Free text with no known set of valid values.</summary>
        Text,

        /// <summary>A quest id declared by <c>QuestBuilder.Create</c>.</summary>
        QuestId,

        /// <summary>A 1-based quest state number, bounded by that quest's declared state count.</summary>
        QuestState,

        /// <summary>A <c>KeyItemType</c>, given by name or by its integer value.</summary>
        KeyItemId,

        /// <summary>A <c>FactionType</c> integer value.</summary>
        FactionId,

        /// <summary>A <c>SkillType</c>, given by name or by its integer value.</summary>
        SkillId,

        /// <summary>A skill rank.</summary>
        SkillRank,

        /// <summary>A plain number of points, standing, or similar.</summary>
        Amount,

        /// <summary>The tag of a placed store.</summary>
        StoreTag,

        /// <summary>The tag of a placed waypoint.</summary>
        WaypointTag
    }

    /// <summary>
    /// One declared argument of a snippet: what it is called, what kind of value it holds, and
    /// whether it may be left off.
    /// </summary>
    public class SnippetArgument
    {
        public SnippetArgument(string name, SnippetArgumentType type, bool isOptional)
        {
            Name = name;
            Type = type;
            IsOptional = isOptional;
        }

        /// <summary>The name a phrase template refers to this argument by, e.g. <c>questId</c>.</summary>
        public string Name { get; }

        public SnippetArgumentType Type { get; }

        /// <summary>True when the snippet still works with this argument absent.</summary>
        public bool IsOptional { get; }
    }
}
