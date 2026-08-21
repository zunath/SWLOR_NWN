using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.ConversationService
{
    /// <summary>
    /// Executes a code-driven menu directly for the NUI window. Its response list is not capped at
    /// twelve: NUI scrolls it, eliminating the old Next/Previous shell nodes and token offsets.
    /// </summary>
    public sealed class ConversationMenuSession : IConversationSession
    {
        private readonly IConversationRuntime _runtime;
        private readonly Stack<string> _navigation = new();
        private readonly List<ConversationMenuResponse> _visibleResponses = new();
        private readonly List<ConversationChoice> _visibleChoices = new();
        private string _currentPageId;
        private bool _hasStarted;
        private bool _ranEndActions;

        internal ConversationMenuSpec Menu { get; }
        public ConversationContext Context { get; }
        public ConversationNode CurrentNode { get; private set; }
        public IReadOnlyList<ConversationTextBlock> CurrentText
        {
            get
            {
                if (CurrentNode == null)
                    return Array.Empty<ConversationTextBlock>();

                return CurrentNode.Text;
            }
        }
        public IReadOnlyList<ConversationChoice> VisibleChoices => _visibleChoices;
        public bool HasEnded { get; private set; }
        public string Title => Menu.Title;

        public ConversationMenuSession(
            ConversationMenuSpec menu,
            ConversationContext context,
            IConversationRuntime runtime)
        {
            Menu = menu ?? throw new ArgumentNullException(nameof(menu));
            Context = context ?? throw new ArgumentNullException(nameof(context));
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _currentPageId = menu.DefaultPageId;
        }

        public bool Start()
        {
            if (_hasStarted)
                throw new InvalidOperationException("The conversation menu has already started.");
            _hasStarted = true;

            foreach (var action in Menu.InitializationActions)
                action();
            if (HasEnded)
                return false;

            RenderCurrentPage();
            return !HasEnded;
        }

        public ConversationSelectionResult SelectChoice(int visibleChoiceIndex)
        {
            if (!_hasStarted || HasEnded)
                return ConversationSelectionResult.ConversationEnded;
            if (visibleChoiceIndex < 0 || visibleChoiceIndex >= _visibleChoices.Count)
                return ConversationSelectionResult.InvalidChoice;

            if (visibleChoiceIndex < _visibleResponses.Count)
            {
                _visibleResponses[visibleChoiceIndex].Action();
            }
            else
            {
                var oldPage = _currentPageId;
                var previousPage = _navigation.Pop();
                foreach (var backAction in Menu.BackActions)
                    backAction(oldPage, previousPage);
                _currentPageId = previousPage;
            }

            if (HasEnded)
                return ConversationSelectionResult.ConversationEnded;

            RenderCurrentPage();
            return ConversationSelectionResult.MovedToNextNode;
        }

        public string ResolveText(string text) => _runtime.ResolveText(Context, text);

        public void GoToPage(string pageId, bool rememberCurrentPage = true)
        {
            if (!Menu.Pages.ContainsKey(pageId))
                throw new KeyNotFoundException($"Conversation page '{pageId}' does not exist.");

            if (rememberCurrentPage && !string.Equals(_currentPageId, pageId, StringComparison.Ordinal))
                _navigation.Push(_currentPageId);
            _currentPageId = pageId;
        }

        public void End(ConversationEndReason reason)
        {
            if (HasEnded)
                return;
            HasEnded = true;
            _visibleResponses.Clear();
            _visibleChoices.Clear();

            if (_ranEndActions)
                return;
            _ranEndActions = true;
            foreach (var action in Menu.EndActions)
                action();
        }

        private void RenderCurrentPage()
        {
            var page = Menu.Pages[_currentPageId];
            page.Header = string.Empty;
            page.Responses.Clear();
            page.Initialize(page);
            if (HasEnded)
                return;

            _visibleResponses.Clear();
            _visibleResponses.AddRange(page.Responses.Where(response => response.IsActive));

            _visibleChoices.Clear();
            foreach (var response in _visibleResponses)
            {
                _visibleChoices.Add(new ConversationChoice
                {
                    Id = $"menu-{_visibleChoices.Count}",
                    Text = ConversationMarkup.CollapseForChoice(response.Text)
                });
            }

            if (_navigation.Count > 0)
            {
                _visibleChoices.Add(new ConversationChoice
                {
                    Id = "menu-back",
                    Text = new ConversationTextBlock
                    {
                        Text = "Back",
                        Style = ConversationTextStyle.Muted
                    }
                });
            }

            CurrentNode = new ConversationNode
            {
                Id = _currentPageId,
                PortraitResref = Menu.PortraitResref,
                Text = new List<ConversationTextBlock>
                {
                    ConversationMarkup.CollapseForHeader(page.Header, ConversationTextStyle.Normal)
                }
            };
        }
    }
}
