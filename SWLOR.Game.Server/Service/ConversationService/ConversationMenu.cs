using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.ConversationService
{
    /// <summary>A code-driven NUI interaction made of refreshable pages and responses.</summary>
    public sealed class ConversationMenuSpec
    {
        public string Title { get; set; } = "Conversation";
        public string PortraitResref { get; set; } = string.Empty;
        public string DefaultPageId { get; set; } = string.Empty;
        public Dictionary<string, ConversationMenuPage> Pages { get; } = new();
        public List<Action> InitializationActions { get; } = new();
        public List<Action<string, string>> BackActions { get; } = new();
        public List<Action> EndActions { get; } = new();
        public object DataModel { get; set; }
    }

    public sealed class ConversationMenuPage
    {
        public string Header { get; set; } = string.Empty;
        public Action<ConversationMenuPage> Initialize { get; }
        public List<ConversationMenuResponse> Responses { get; } = new();

        public ConversationMenuPage(Action<ConversationMenuPage> initialize)
        {
            Initialize = initialize ?? throw new ArgumentNullException(nameof(initialize));
        }

        public ConversationMenuPage AddResponse(string text, Action action)
        {
            Responses.Add(new ConversationMenuResponse(text, action));
            return this;
        }
    }

    public sealed class ConversationMenuResponse
    {
        public string Text { get; set; }
        public bool IsActive { get; set; }
        public Action Action { get; set; }
        public object Data { get; set; }

        public ConversationMenuResponse(
            string text,
            Action action,
            bool isActive = true,
            object data = null)
        {
            Text = text ?? string.Empty;
            Action = action ?? throw new ArgumentNullException(nameof(action));
            IsActive = isActive;
            Data = data;
        }
    }
}
