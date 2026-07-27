namespace SWLOR.Toolset.Domain.Conversations
{
    /// <summary>
    /// Where a player stands on one quest: not started, on a step, or finished.
    /// </summary>
    public sealed class QuestProgress
    {
        /// <summary>The step the player is on, 1-based, or null when they have not taken it.</summary>
        public int? CurrentState { get; init; }

        /// <summary>True once the quest has been completed at least once.</summary>
        public bool IsCompleted { get; init; }

        /// <summary>Not started at all.</summary>
        public static QuestProgress None => new();

        public static QuestProgress OnStep(int state) => new() { CurrentState = state };

        public static QuestProgress Completed => new() { IsCompleted = true };

        /// <summary>
        /// True when the player is actively working on the quest — which is what
        /// <c>condition-has-quest</c> tests, and is deliberately false once it is completed.
        /// </summary>
        public bool IsInProgress => CurrentState != null && !IsCompleted;
    }

    /// <summary>
    /// A hypothetical player, for asking "what would this person see?". Everything the editor draws
    /// — which opening wins, which choices are hidden, which coverage cells are filled — is that
    /// question asked with different values here.
    /// </summary>
    public sealed class PretendPlayer
    {
        private readonly Dictionary<string, QuestProgress> _quests = new(StringComparer.Ordinal);
        private readonly HashSet<string> _keyItems = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _skillRanks = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, int> _factionStanding = new();
        private readonly Dictionary<int, int> _factionPoints = new();

        /// <summary>Whether this player has finished the tutorial on some character.</summary>
        public bool HasCompletedTutorial { get; set; }

        public IReadOnlyDictionary<string, QuestProgress> Quests => _quests;

        public IReadOnlyCollection<string> KeyItems => _keyItems;

        public PretendPlayer WithQuest(string questId, QuestProgress progress)
        {
            _quests[questId] = progress;
            return this;
        }

        public PretendPlayer WithKeyItem(string keyItem)
        {
            _keyItems.Add(keyItem);
            return this;
        }

        /// <summary>Takes the key item away, for breaking a guard that requires having it.</summary>
        public PretendPlayer WithoutKeyItem(string keyItem)
        {
            _keyItems.Remove(keyItem);
            return this;
        }

        public PretendPlayer WithSkill(string skill, int rank)
        {
            _skillRanks[skill] = rank;
            return this;
        }

        public PretendPlayer WithFactionStanding(int factionId, int standing)
        {
            _factionStanding[factionId] = standing;
            return this;
        }

        public PretendPlayer WithFactionPoints(int factionId, int points)
        {
            _factionPoints[factionId] = points;
            return this;
        }

        public PretendPlayer WithTutorialCompleted()
        {
            HasCompletedTutorial = true;
            return this;
        }

        public QuestProgress GetQuest(string questId) =>
            _quests.TryGetValue(questId, out var progress) ? progress : QuestProgress.None;

        public bool HasKeyItem(string keyItem) => _keyItems.Contains(keyItem);

        public int GetSkillRank(string skill) =>
            _skillRanks.TryGetValue(skill, out var rank) ? rank : 0;

        public int GetFactionStanding(int factionId) =>
            _factionStanding.TryGetValue(factionId, out var standing) ? standing : 0;

        public int GetFactionPoints(int factionId) =>
            _factionPoints.TryGetValue(factionId, out var points) ? points : 0;

        /// <summary>A copy, so walking a conversation can apply actions without disturbing the caller's state.</summary>
        public PretendPlayer Clone()
        {
            var clone = new PretendPlayer { HasCompletedTutorial = HasCompletedTutorial };
            foreach (var (questId, progress) in _quests)
                clone._quests[questId] = progress;
            foreach (var keyItem in _keyItems)
                clone._keyItems.Add(keyItem);
            foreach (var (skill, rank) in _skillRanks)
                clone._skillRanks[skill] = rank;
            foreach (var (faction, standing) in _factionStanding)
                clone._factionStanding[faction] = standing;
            foreach (var (faction, points) in _factionPoints)
                clone._factionPoints[faction] = points;

            return clone;
        }
    }
}
