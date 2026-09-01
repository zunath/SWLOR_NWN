using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.ConversationService
{
    /// <summary>Builds a code-driven conversation menu without creating an NWN DLG resource.</summary>
    public sealed class ConversationMenuBuilder
    {
        private readonly ConversationMenuSpec _menu = new();

        public ConversationMenuBuilder WithTitle(string title)
        {
            _menu.Title = title ?? string.Empty;
            return this;
        }

        public ConversationMenuBuilder WithPortrait(string portraitResref)
        {
            _menu.PortraitResref = portraitResref ?? string.Empty;
            return this;
        }

        public ConversationMenuBuilder WithDataModel(object dataModel)
        {
            _menu.DataModel = dataModel;
            return this;
        }

        public ConversationMenuBuilder AddInitializationAction(Action action)
        {
            _menu.InitializationActions.Add(action ?? throw new ArgumentNullException(nameof(action)));
            return this;
        }

        public ConversationMenuBuilder AddBackAction(Action<string, string> action)
        {
            _menu.BackActions.Add(action ?? throw new ArgumentNullException(nameof(action)));
            return this;
        }

        public ConversationMenuBuilder AddEndAction(Action action)
        {
            _menu.EndActions.Add(action ?? throw new ArgumentNullException(nameof(action)));
            return this;
        }

        public ConversationMenuBuilder AddPage(string pageId, Action<ConversationMenuPage> initialize)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pageId);
            if (_menu.Pages.Count == 0)
                _menu.DefaultPageId = pageId;
            _menu.Pages.Add(pageId, new ConversationMenuPage(initialize));
            return this;
        }

        public ConversationMenuSpec Build()
        {
            if (_menu.Pages.Count == 0)
                throw new InvalidOperationException("A conversation menu needs at least one page.");
            return _menu;
        }
    }
}
