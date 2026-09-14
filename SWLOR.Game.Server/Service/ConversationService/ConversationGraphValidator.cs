using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.ConversationService
{
    public static class ConversationGraphValidator
    {
        public static IReadOnlyList<string> Validate(ConversationGraph graph)
        {
            var errors = new List<string>();

            if (graph == null)
            {
                errors.Add("Conversation graph is required.");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(graph.Id))
                errors.Add("Conversation graph ID is required.");
            if (graph.EntryPoints == null || graph.EntryPoints.Count == 0)
                errors.Add("At least one ordered entry point is required.");
            if (graph.Nodes == null || graph.Nodes.Count == 0)
                errors.Add("At least one NPC line is required.");
            if (graph.Choices == null)
                errors.Add("The player choice collection is required.");

            if (graph.Nodes == null || graph.Choices == null)
                return errors;

            ValidateLinks(graph, graph.EntryPoints, "entry points", errors);

            foreach (var (nodeId, node) in graph.Nodes)
            {
                if (node == null)
                {
                    errors.Add($"Node '{nodeId}' is null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(node.Id))
                    errors.Add($"Node stored under '{nodeId}' has no ID.");
                else if (!string.Equals(nodeId, node.Id, StringComparison.Ordinal))
                    errors.Add($"Node key '{nodeId}' does not match node ID '{node.Id}'.");

                foreach (var choiceLink in node.Choices ?? new List<ConversationChoiceLink>())
                {
                    if (choiceLink == null)
                    {
                        errors.Add($"Node '{nodeId}' has a null choice link.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(choiceLink.ChoiceId))
                        errors.Add($"Node '{nodeId}' has a choice link with no choice ID.");
                    else if (!graph.Choices.ContainsKey(choiceLink.ChoiceId))
                        errors.Add($"Node '{nodeId}' links to missing choice '{choiceLink.ChoiceId}'.");
                }
            }

            foreach (var (choiceId, choice) in graph.Choices)
            {
                if (choice == null)
                {
                    errors.Add($"Choice '{choiceId}' is null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(choice.Id))
                    errors.Add($"Choice stored under '{choiceId}' has no ID.");
                else if (!string.Equals(choiceId, choice.Id, StringComparison.Ordinal))
                    errors.Add($"Choice key '{choiceId}' does not match choice ID '{choice.Id}'.");

                if (string.IsNullOrWhiteSpace(choice.Text?.Text) && !choice.IsAutomatic)
                    errors.Add($"Choice '{choiceId}' has no text.");

                if (choice.EndsConversation && choice.Next != null && choice.Next.Count > 0)
                    errors.Add($"Choice '{choiceId}' both ends the conversation and links to another line.");
                else if (!choice.EndsConversation && (choice.Next == null || choice.Next.Count == 0))
                    errors.Add($"Choice '{choiceId}' neither ends the conversation nor links to another line.");

                ValidateLinks(graph, choice.Next, $"choice '{choiceId}'", errors);
            }

            return errors;
        }

        private static void ValidateLinks(
            ConversationGraph graph,
            IEnumerable<ConversationLink> links,
            string owner,
            ICollection<string> errors)
        {
            if (links == null)
                return;

            foreach (var link in links)
            {
                if (link == null)
                {
                    errors.Add($"The {owner} contain a null link.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(link.TargetNodeId))
                    errors.Add($"A link in {owner} has no target node ID.");
                else if (!graph.Nodes.ContainsKey(link.TargetNodeId))
                    errors.Add($"A link in {owner} targets missing node '{link.TargetNodeId}'.");
            }
        }
    }
}
