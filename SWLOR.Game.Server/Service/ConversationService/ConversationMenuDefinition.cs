namespace SWLOR.Game.Server.Service.ConversationService
{
    /// <summary>
    /// Base for code-driven NUI interactions. Each player receives a fresh definition instance, so
    /// context is explicit and concurrent users never share mutable dialog state.
    /// </summary>
    public abstract class ConversationMenuDefinition
    {
        private ConversationMenuSession _session;

        protected uint Player { get; private set; }
        protected uint Owner { get; private set; }

        internal void Bind(uint player, uint owner)
        {
            Player = player;
            Owner = owner;
        }

        internal void Attach(ConversationMenuSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        protected T Data<T>() where T : class
        {
            return _session?.Menu.DataModel as T
                   ?? throw new InvalidOperationException($"Conversation data is not a {typeof(T).Name}.");
        }

        protected void GoToPage(string pageId, bool rememberCurrentPage = true)
        {
            ActiveSession().GoToPage(pageId, rememberCurrentPage);
        }

        protected void Close()
        {
            ActiveSession().End(ConversationEndReason.Completed);
        }

        private ConversationMenuSession ActiveSession() =>
            _session ?? throw new InvalidOperationException("The conversation menu has not started yet.");

        public abstract ConversationMenuSpec Build();
    }
}
