using System.Text.RegularExpressions;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Toolset.Domain.Documents;

namespace SWLOR.Toolset.Domain.Conversations;

public enum ConversationMigrationIssueSeverity
{
    Warning = 0,
    RequiresLegacyException = 1
}

public sealed record ConversationMigrationIssue(
    ConversationMigrationIssueSeverity Severity,
    string Location,
    string Message);

public sealed record ConversationMigrationResult(
    ConversationGraph Graph,
    IReadOnlyList<ConversationMigrationIssue> Issues)
{
    public bool CanRunInNui => Issues.All(issue =>
        issue.Severity != ConversationMigrationIssueSeverity.RequiresLegacyException);
}

/// <summary>
/// Converts an Aurora DLG into the stable-ID NUI graph without flattening shared entries/replies.
/// Unsupported custom NWScript and custom-token behavior is reported as an explicit exception;
/// it is never silently emitted as though it were supported.
/// </summary>
public static class DlgConversationMigrator
{
    private const string OwnerScriptAction = "system.execute-owner-script";
    private static readonly Regex MarkupPattern = new(
        "(<StartAction>|<StartHighlight>|</Start>)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex RemainingTokenPattern = new(
        "<[^>]+>",
        RegexOptions.Compiled);

    private static readonly Dictionary<string, string> SupportedRootScripts =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["nw_walk_wp"] = OwnerScriptAction
        };

    private static readonly (Regex Pattern, string Replacement)[] TokenConversions =
    {
        (new Regex("<FirstName>", RegexOptions.IgnoreCase | RegexOptions.Compiled), "{{player.name}}"),
        (new Regex("<LastName>", RegexOptions.IgnoreCase | RegexOptions.Compiled), "{{player.name}}"),
        (new Regex("<FullName>", RegexOptions.IgnoreCase | RegexOptions.Compiled), "{{player.name}}"),
        (new Regex("<boy/girl>", RegexOptions.IgnoreCase | RegexOptions.Compiled), "{{player.gender.boy-girl}}"),
        (new Regex("<lad/lass>", RegexOptions.IgnoreCase | RegexOptions.Compiled), "{{player.gender.lad-lass}}"),
        (new Regex("<sir/madam>", RegexOptions.IgnoreCase | RegexOptions.Compiled), "{{player.gender.sir-madam}}"),
        (new Regex("<man/woman>", RegexOptions.IgnoreCase | RegexOptions.Compiled), "{{player.gender.man-woman}}"),
        (new Regex("<R>", RegexOptions.IgnoreCase | RegexOptions.Compiled), "{{player.race}}")
    };

    public static ConversationMigrationResult Convert(string conversationId, DlgDocument document)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentNullException.ThrowIfNull(document);

        var issues = new List<ConversationMigrationIssue>();
        var graph = new ConversationGraph
        {
            Id = conversationId,
            Title = conversationId,
            DefaultNpcDelay = document.DelayEntry,
            DefaultPlayerDelay = document.DelayReply,
            PreventZoomIn = document.PreventZoomIn
        };

        ConvertRootScript(document.EndConversation, "completed conversation", graph.OnEndActions, issues);
        ConvertRootScript(document.EndConverAbort, "aborted conversation", graph.OnAbortActions, issues);

        foreach (var entry in document.Entries)
        {
            var nodeId = EntryId(entry.Index);
            var node = new ConversationNode
            {
                Id = nodeId,
                SpeakerTag = entry.Speaker,
                SoundResref = entry.Sound,
                Animation = entry.Animation,
                AnimationLoops = entry.AnimLoop,
                Delay = entry.Delay,
                Comment = entry.Comment,
                JournalQuest = entry.Quest,
                Text = ConvertText(entry.Text, ConversationTextStyle.Normal, $"entry {entry.Index}", issues)
            };

            ConvertActions(entry, node.OnEnterActions, $"entry {entry.Index}", issues);
            foreach (var replyLink in entry.Links)
            {
                var choiceLink = new ConversationChoiceLink
                {
                    ChoiceId = ReplyId(replyLink.TargetIndex)
                };
                ConvertConditions(replyLink, choiceLink.Conditions,
                    $"entry {entry.Index} -> reply {replyLink.TargetIndex}", issues);
                node.Choices.Add(choiceLink);
            }

            graph.Nodes.Add(nodeId, node);
        }

        foreach (var reply in document.Replies)
        {
            var choiceId = ReplyId(reply.Index);
            var choice = new ConversationChoice
            {
                Id = choiceId,
                SoundResref = reply.Sound,
                Animation = reply.Animation,
                AnimationLoops = reply.AnimLoop,
                Delay = reply.Delay,
                Comment = reply.Comment,
                JournalQuest = reply.Quest,
                Text = CollapseChoiceText(ConvertText(
                    FriendlyReplyText(reply),
                    ConversationTextStyle.PlayerReply,
                    $"reply {reply.Index}",
                    issues)),
                EndsConversation = reply.Links.Count == 0,
                IsAutomatic = string.IsNullOrWhiteSpace(reply.Text) && reply.Links.Count > 0
            };

            ConvertActions(reply, choice.Actions, $"reply {reply.Index}", issues);
            foreach (var entryLink in reply.Links)
            {
                var next = new ConversationLink
                {
                    TargetNodeId = EntryId(entryLink.TargetIndex)
                };
                ConvertConditions(entryLink, next.Conditions,
                    $"reply {reply.Index} -> entry {entryLink.TargetIndex}", issues);
                choice.Next.Add(next);
            }

            graph.Choices.Add(choiceId, choice);
        }

        foreach (var opening in document.Openings)
        {
            var link = new ConversationLink
            {
                TargetNodeId = EntryId(opening.TargetIndex)
            };
            ConvertConditions(opening, link.Conditions,
                $"opening -> entry {opening.TargetIndex}", issues);
            graph.EntryPoints.Add(link);
        }

        foreach (var validationError in ConversationGraphValidator.Validate(graph))
        {
            issues.Add(new ConversationMigrationIssue(
                ConversationMigrationIssueSeverity.RequiresLegacyException,
                "graph",
                validationError));
        }

        return new ConversationMigrationResult(graph, issues);
    }

    private static void ConvertConditions(
        DlgLink link,
        ICollection<ConversationCondition> destination,
        string location,
        ICollection<ConversationMigrationIssue> issues)
    {
        if (!string.IsNullOrWhiteSpace(link.Active) && !DlgDocument.IsConditionDispatcher(link.Active))
        {
            issues.Add(new ConversationMigrationIssue(
                ConversationMigrationIssueSeverity.RequiresLegacyException,
                location,
                $"Uses custom condition script '{link.Active}'."));
            return;
        }

        if (string.IsNullOrWhiteSpace(link.Active) && link.Conditions.Count > 0)
        {
            issues.Add(new ConversationMigrationIssue(
                ConversationMigrationIssueSeverity.Warning,
                location,
                "Has condition parameters but no condition dispatcher."));
            return;
        }

        foreach (var condition in link.Conditions)
        {
            destination.Add(new ConversationCondition
            {
                Key = condition.SnippetKey,
                IsNegated = condition.IsNegated,
                Arguments = condition.Arguments.ToList()
            });
        }
    }

    private static void ConvertActions(
        DlgNode node,
        ICollection<ConversationAction> destination,
        string location,
        ICollection<ConversationMigrationIssue> issues)
    {
        if (!string.IsNullOrWhiteSpace(node.Script) && !DlgDocument.IsActionDispatcher(node.Script))
        {
            issues.Add(new ConversationMigrationIssue(
                ConversationMigrationIssueSeverity.RequiresLegacyException,
                location,
                $"Uses custom action script '{node.Script}'."));
            return;
        }

        var actions = node.Actions;
        if (string.IsNullOrWhiteSpace(node.Script) && actions.Count > 0)
        {
            issues.Add(new ConversationMigrationIssue(
                ConversationMigrationIssueSeverity.Warning,
                location,
                "Has action parameters but no action dispatcher."));
            return;
        }

        foreach (var action in actions.Where(action => !action.IsOncePerPlayerMarker))
        {
            var marker = actions.FirstOrDefault(candidate =>
                candidate.IsOncePerPlayerMarker &&
                candidate.MarkedActionKey.Equals(action.SnippetKey, StringComparison.OrdinalIgnoreCase));

            destination.Add(new ConversationAction
            {
                Key = action.SnippetKey,
                Arguments = action.Arguments.ToList(),
                OncePerPlayerId = marker?.Value ?? string.Empty
            });
        }
    }

    private static void ConvertRootScript(
        string script,
        string location,
        ICollection<ConversationAction> destination,
        ICollection<ConversationMigrationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(script))
            return;

        if (!SupportedRootScripts.TryGetValue(script, out var actionKey))
        {
            issues.Add(new ConversationMigrationIssue(
                ConversationMigrationIssueSeverity.RequiresLegacyException,
                location,
                $"Uses unsupported root script '{script}'."));
            return;
        }

        destination.Add(new ConversationAction
        {
            Key = actionKey,
            Arguments = { script }
        });
    }

    private static List<ConversationTextBlock> ConvertText(
        string source,
        ConversationTextStyle defaultStyle,
        string location,
        ICollection<ConversationMigrationIssue> issues)
    {
        var converted = source ?? string.Empty;
        foreach (var (pattern, replacement) in TokenConversions)
            converted = pattern.Replace(converted, replacement);

        var blocks = new List<ConversationTextBlock>();
        var style = defaultStyle;
        var position = 0;
        foreach (Match match in MarkupPattern.Matches(converted))
        {
            AddTextBlock(blocks, converted[position..match.Index], style);
            var marker = match.Value;
            style = marker.Equals("<StartAction>", StringComparison.OrdinalIgnoreCase)
                ? ConversationTextStyle.Action
                : marker.Equals("<StartHighlight>", StringComparison.OrdinalIgnoreCase)
                    ? ConversationTextStyle.Highlight
                    : defaultStyle;
            position = match.Index + match.Length;
        }

        AddTextBlock(blocks, converted[position..], style);
        if (blocks.Count == 0)
            blocks.Add(new ConversationTextBlock { Text = string.Empty, Style = defaultStyle });

        foreach (var block in blocks)
        {
            foreach (Match unsupported in RemainingTokenPattern.Matches(block.Text))
            {
                issues.Add(new ConversationMigrationIssue(
                    ConversationMigrationIssueSeverity.RequiresLegacyException,
                    location,
                    $"Uses unsupported text token '{unsupported.Value}'."));
            }
        }

        return blocks;
    }

    private static ConversationTextBlock CollapseChoiceText(IReadOnlyList<ConversationTextBlock> blocks)
    {
        if (blocks.Count == 1)
            return blocks[0];

        return new ConversationTextBlock
        {
            Text = string.Concat(blocks.Select(block => block.Text)),
            Style = blocks.FirstOrDefault(block => block.Style != ConversationTextStyle.PlayerReply)?.Style
                    ?? ConversationTextStyle.PlayerReply
        };
    }

    private static string FriendlyReplyText(DlgNode reply)
    {
        if (!string.IsNullOrWhiteSpace(reply.Text))
            return reply.Text;

        return reply.Links.Count == 0 ? "Goodbye." : "Continue";
    }

    private static void AddTextBlock(
        ICollection<ConversationTextBlock> blocks,
        string text,
        ConversationTextStyle style)
    {
        if (string.IsNullOrEmpty(text))
            return;

        blocks.Add(new ConversationTextBlock
        {
            Text = text,
            Style = style
        });
    }

    private static string EntryId(int index) => $"entry-{index:D5}";
    private static string ReplyId(int index) => $"reply-{index:D5}";
}
