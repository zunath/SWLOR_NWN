using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.ConversationService
{
    public sealed class ConversationSession : IConversationSession
    {
        private readonly ConversationGraph _graph;
        private readonly IConversationRuntime _runtime;
        private List<ConversationTextBlock> _currentText = new();
        private List<ConversationChoice> _visibleChoices = new();
        private bool _hasStarted;

        public ConversationContext Context { get; }
        public ConversationNode CurrentNode { get; private set; }
        public IReadOnlyList<ConversationTextBlock> CurrentText => _currentText;
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
            if (!ExecuteActions(_graph.OnStartActions))
            {
                End(ConversationEndReason.RuntimeError);
                return false;
            }

            var entry = SelectFirstPassingLink(_graph.EntryPoints);
            if (entry == null)
            {
                End(ConversationEndReason.NoValidEntryPoint);
                return false;
            }

            EnterNode(entry.TargetNodeId);
            return !HasEnded;
        }

        public ConversationSelectionResult SelectChoice(int visibleChoiceIndex)
        {
            if (!_hasStarted || HasEnded)
                return ConversationSelectionResult.ConversationEnded;
            if (visibleChoiceIndex < 0 || visibleChoiceIndex >= _visibleChoices.Count)
                return ConversationSelectionResult.InvalidChoice;

            var choice = _visibleChoices[visibleChoiceIndex];
            if (!ExecuteActions(choice.Actions))
            {
                End(ConversationEndReason.RuntimeError);
                return ConversationSelectionResult.ConversationEnded;
            }

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
            return HasEnded
                ? ConversationSelectionResult.ConversationEnded
                : ConversationSelectionResult.MovedToNextNode;
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
            _currentText = new List<ConversationTextBlock>();
            var automaticPath = new HashSet<string>(StringComparer.Ordinal);
            while (!HasEnded)
            {
                if (!automaticPath.Add(nodeId))
                {
                    End(ConversationEndReason.RuntimeError);
                    return;
                }

                CurrentNode = _graph.Nodes[nodeId];
                if (!ExecuteActions(CurrentNode.OnEnterActions))
                {
                    End(ConversationEndReason.RuntimeError);
                    return;
                }

                if (CurrentNode.Text != null)
                    _currentText.AddRange(CurrentNode.Text);

                var passingChoices = (CurrentNode.Choices ?? new List<ConversationChoiceLink>())
                    .Where(link => ConditionsPass(link.Conditions))
                    .Select(link => _graph.Choices[link.ChoiceId])
                    .ToList();
                var automaticChoices = passingChoices.Where(choice => choice.IsAutomatic).ToList();
                if (automaticChoices.Count == 0)
                {
                    _visibleChoices = passingChoices;
                    return;
                }

                _visibleChoices = new List<ConversationChoice>();
                if (automaticChoices.Count != 1)
                {
                    End(ConversationEndReason.RuntimeError);
                    return;
                }

                var automaticChoice = automaticChoices[0];
                if (!ExecuteActions(automaticChoice.Actions))
                {
                    End(ConversationEndReason.RuntimeError);
                    return;
                }

                if (automaticChoice.EndsConversation)
                {
                    End(ConversationEndReason.Completed);
                    return;
                }

                var next = SelectFirstPassingLink(automaticChoice.Next);
                if (next == null)
                {
                    End(ConversationEndReason.NoValidTransition);
                    return;
                }

                nodeId = next.TargetNodeId;
            }
        }

        private ConversationLink SelectFirstPassingLink(IEnumerable<ConversationLink> links)
        {
            return links?.FirstOrDefault(link => ConditionsPass(link.Conditions));
        }

        private bool ConditionsPass(IEnumerable<ConversationCondition> conditions)
        {
            return conditions == null || conditions.All(condition => _runtime.EvaluateCondition(Context, condition));
        }

        private bool ExecuteActions(IEnumerable<ConversationAction> actions)
        {
            if (actions == null)
                return true;

            foreach (var action in actions)
            {
                if (!_runtime.ExecuteAction(Context, action))
                    return false;
            }

            return true;
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
