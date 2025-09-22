using SWLOR.Component.Quest.Service;

namespace SWLOR.Component.Quest.Contracts
{
    /// <summary>
    /// Factory interface for creating QuestDetail instances.
    /// This allows for proper DI management of QuestDetail creation.
    /// </summary>
    public interface IQuestDetailFactory
    {
        /// <summary>
        /// Creates a new QuestDetail instance with the specified quest ID and name.
        /// </summary>
        /// <param name="questId">The unique identifier for the quest</param>
        /// <param name="name">The display name of the quest</param>
        /// <returns>A new QuestDetail instance with all required services injected</returns>
        QuestDetail Create(string questId, string name);
    }
}
