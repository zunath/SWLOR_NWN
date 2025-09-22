namespace SWLOR.Component.Quest.Contracts
{
    /// <summary>
    /// Factory interface for creating QuestBuilder instances.
    /// This allows for proper DI management of QuestBuilder lifecycle.
    /// </summary>
    public interface IQuestBuilderFactory
    {
        /// <summary>
        /// Creates a new QuestBuilder instance.
        /// Each call returns a fresh instance suitable for building a single quest.
        /// </summary>
        /// <returns>A new QuestBuilder instance</returns>
        IQuestBuilder Create();
    }
}
