namespace SWLOR.Shared.Domain.Dialog.Contracts
{
    public interface ISnippetService
    {
        /// <summary>
        /// When the module loads, all available conversation snippets are loaded into the cache.
        /// </summary>
        void CacheData();

        /// <summary>
        /// When a conversation node with this script assigned in the "Appears When" event is run,
        /// check for any conversation conditions and process them.
        /// </summary>
        /// <returns></returns>
        bool ConversationAppearsWhen();

        /// <summary>
        /// When a conversation node with this script assigned in the "Actions Taken" event is run,
        /// check for any conversation actions and process them.
        /// </summary>
        void ConversationAction();
    }
}