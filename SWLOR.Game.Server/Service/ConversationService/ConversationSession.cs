using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.ConversationService
{
    public sealed class ConversationSession : IConversationSession
    {
        private readonly ConversationGraph _graph;
        private readonly IConversationRuntime _runtime;
        private List<ConversationChoice> _visibleChoices = new();
        private bool _hasStarted;

        public ConversationContext Context { get; }
        public ConversationNode CurrentNode { get; private set; }
        public IReadOnlyList<ConversationChoice> VisibleChoices => _visibleChoices;
        public bool HasEnded { get; private set; }
        public ConversationEndReason? EndReason { get; private set; }
        public string Title => _graph.Title;

        public ConversationSession(
            ConversationGraph graph,
            ConversationContext context,
            IConversationRuntime runtime)
        {
            var errors = ConversationGraphValidator.Validate(graph);
            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join(Environment.NewLine, errors));

            _graph = graph;
            Context = context ?? throw new ArgumentNullException(nameof(context));
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        }

        /// <summary>
        /// Starts the session at the first entry point whose conditions pass.
        /// </summary>
        public bool Start()
        {
            if (_hasStarted)
                throw new InvalidOperationException("The conversation session has already been started.");

            _hasStarted = true;
            ExecuteActions(_graph.OnStartActions);

            var entry = SelectFirstPassingLink(_graph.EntryPoints);
            if (entry == null)
            {
                End(ConversationEndReason.NoValidEntryPoint);
                return false;
            }

            EnterNode(entry.TargetNodeId);
            return true;
        }

        public ConversationSelectionResult SelectChoice(int visibleChoiceIndex)
        {
            if (!_hasStarted || HasEnded)
                return ConversationSelectionResult.ConversationEnded;
            if (visibleChoiceIndex < 0 || visibleChoiceIndex >= _visibleChoices.Count)
                return ConversationSelectionResult.InvalidChoice;

            var choice = _visibleChoices[visibleChoiceIndex];
            ExecuteActions(choice.Actions);

            if (choice.EndsConversation)
            {
                End(ConversationEndReason.Completed);
                return ConversationSelectionResult.ConversationEnded;
            }

            var next = SelectFirstPassingLink(choice.Next);
            if (next == null)
            {
                End(ConversationEndReason.NoValidTransition);
                return ConversationSelectionResult.NoValidTransition;
            }

            EnterNode(next.TargetNodeId);
            return ConversationSelectionResult.MovedToNextNode;
        }

        public string ResolveText(string text)
        {
            return _runtime.ResolveText(Context, text);
        }

        public void End(ConversationEndReason reason)
        {
            if (HasEnded)
                return;

            HasEnded = true;
            EndReason = reason;
            _visibleChoices = new List<ConversationChoice>();

            ExecuteActions(reason == ConversationEndReason.Aborted
                ? _graph.OnAbortActions
                : _graph.OnEndActions);
        }

        private void EnterNode(string nodeId)
        {
            CurrentNode = _graph.Nodes[nodeId];
            ExecuteActions(CurrentNode.OnEnterActions);
            _visibleChoices = (CurrentNode.Choices ?? new List<ConversationChoiceLink>())
                .Where(link => ConditionsPass(link.Conditions))
                .Select(link => _graph.Choices[link.ChoiceId])
                .ToList();
        }

        private ConversationLink SelectFirstPassingLink(IEnumerable<ConversationLink> links)
        {
            return links?.FirstOrDefault(link => ConditionsPass(link.Conditions));
        }

        private bool ConditionsPass(IEnumerable<ConversationCondition> conditions)
        {
            return conditions == null || conditions.All(condition => _runtime.EvaluateCondition(Context, condition));
        }

        private void ExecuteActions(IEnumerable<ConversationAction> actions)
        {
            if (actions == null)
                return;

            foreach (var action in actions)
                _runtime.ExecuteAction(Context, action);
        }
    }

    public enum ConversationSelectionResult
    {
        InvalidChoice = 0,
        MovedToNextNode = 1,
        NoValidTransition = 2,
        ConversationEnded = 3
    }

    public enum ConversationEndReason
    {
        Completed = 0,
        Aborted = 1,
        NoValidEntryPoint = 2,
        NoValidTransition = 3,
        RuntimeError = 4
    }
}
