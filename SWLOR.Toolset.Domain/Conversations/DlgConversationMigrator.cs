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
    private sealed record OperationMapping(string Key, params string[] Arguments);
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

    // These old one-off scripts are no longer present in the module. Only scripts whose complete
    // behavior can be expressed by registered NUI operations are translated here; anything else
    // remains an explicit legacy exception.
    private static readonly Dictionary<string, OperationMapping[]> CustomConditions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["var_courreur_a_1"] = [LocalCondition("player", "SWLOR_SKYRACE_REGISTERED", ">=", "1")],
            ["var_courreur_a_0"] = [LocalCondition("player", "SWLOR_SKYRACE_REGISTERED", "=", "0")],
            ["pc_a_50_or"] = [new OperationMapping("condition-player-credits", "50")],
            ["var_encour_a_1"] = [LocalCondition("module", "SWLOR_SKYRACE_RUNNING", ">=", "1")],
            ["var_skyracer_a_6"] = [LocalCondition("module", "SWLOR_SKYRACE_PARTICIPANTS", ">=", "6")],
            ["var_skyracer_m_6"] = [LocalCondition("module", "SWLOR_SKYRACE_PARTICIPANTS", "<", "6")],
            ["doc_pc_mort_a_1"] = [LocalCondition("player", "SWLOR_DOCTOR_RESCUE", ">=", "1")],
            ["q1_quest_eor_a_1"] = [LocalCondition("player", "SWLOR_NIKKA_QUEST_COMPLETE", ">=", "1")],
            ["q1_femme_eor_a_2"] = [LocalCondition("player", "SWLOR_NIKKA_RESCUE_STATE", ">=", "2")],
            ["has_quest_1"] = [LocalCondition("player", "SWLOR_ABANDONED_STATION_QUEST", "=", "0")],
            ["has_quest_2"] = [LocalCondition("player", "SWLOR_EXAMPLE_QUEST_2", "=", "0")],
            ["on_qst1_state_1"] = [LocalCondition("player", "SWLOR_ABANDONED_STATION_QUEST", "=", "1")],
            ["on_qst1_state_2"] = [LocalCondition("player", "SWLOR_ABANDONED_STATION_QUEST", "=", "2")],
            ["can_accept_1"] = [LocalCondition("player", "SWLOR_ABANDONED_STATION_QUEST", "=", "0")],
            ["quest_done_1"] = [LocalCondition("player", "SWLOR_ABANDONED_STATION_QUEST", ">=", "3")],
            ["sc_journal_int"] = [new OperationMapping("condition-player-ability", "Perception", "14")],
            ["sc_rng_journal"] = [new OperationMapping("condition-random-chance", "20")],
            ["spawnb_cc_activ"] = [LocalCondition("owner", "SWLOR_SPAWN_ACTIVE", "=", "0")],
            ["spawnb_cc_dactiv"] = [LocalCondition("owner", "SWLOR_SPAWN_ACTIVE", "!=", "0")],
            ["spawnb_cc_trck"] = [LocalCondition("owner", "SWLOR_SPAWN_TRACKING", "=", "0")],
            ["spawnb_cc_notrck"] = [LocalCondition("owner", "SWLOR_SPAWN_TRACKING", "!=", "0")],
            ["spawnb_cc_dump"] = [LocalCondition("owner", "SWLOR_SPAWN_DUMP", "=", "0")],
            ["spawnb_cc_nodump"] = [LocalCondition("owner", "SWLOR_SPAWN_DUMP", "!=", "0")],
            ["spawnb_cc_sdlog"] = [LocalCondition("area", "SWLOR_SPAWN_DELAY_LOG", "=", "0")],
            ["spawnb_cc_nsdlog"] = [LocalCondition("area", "SWLOR_SPAWN_DELAY_LOG", "!=", "0")],
            ["spawnb_cc_sclog"] = [LocalCondition("area", "SWLOR_SPAWN_COUNT_LOG", "=", "0")],
            ["spawnb_cc_nsclog"] = [LocalCondition("area", "SWLOR_SPAWN_COUNT_LOG", "!=", "0")]
        };

    private static readonly Dictionary<string, OperationMapping[]> CustomActions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["race_price_token"] = [],
            ["race_record_toke"] = [],
            ["space_remove_hen"] = [],
            ["desinscrit_race"] =
            [
                SetLocal("player", "SWLOR_SKYRACE_REGISTERED", "0"),
                AdjustLocal("module", "SWLOR_SKYRACE_PARTICIPANTS", "-1")
            ],
            ["inscript_skyrace"] =
            [
                new OperationMapping("action-take-player-credits", "50"),
                SetLocal("player", "SWLOR_SKYRACE_REGISTERED", "1"),
                AdjustLocal("module", "SWLOR_SKYRACE_PARTICIPANTS", "1")
            ],
            ["ouvmag_bar_gen"] = [new OperationMapping("action-open-store")],
            ["doc_pc_mort_d_0"] = [SetLocal("player", "SWLOR_DOCTOR_RESCUE", "0")],
            ["doc_soigne_pc"] = [new OperationMapping("action-heal-player", "50")],
            ["accept_quest_1"] = [SetLocal("player", "SWLOR_ABANDONED_STATION_QUEST", "1")],
            ["accept_quest_2"] = [SetLocal("player", "SWLOR_EXAMPLE_QUEST_2", "1")],
            ["finish_quest_1"] = [SetLocal("player", "SWLOR_ABANDONED_STATION_QUEST", "3")],
            ["enter_world"] = [new OperationMapping("action-teleport", "ENTRY_STARTING_WP")],
            ["tel_aban_station"] = [new OperationMapping("action-teleport", "ABAN_STATION_LANDING")],
            ["spawnb_sc_activ"] = [SetLocal("owner", "SWLOR_SPAWN_ACTIVE", "1")],
            ["spawnb_sc_dactiv"] = [SetLocal("owner", "SWLOR_SPAWN_ACTIVE", "0")],
            ["spawnb_sc_trck"] = [SetLocal("owner", "SWLOR_SPAWN_TRACKING", "1")],
            ["spawnb_sc_notrck"] = [SetLocal("owner", "SWLOR_SPAWN_TRACKING", "0")],
            ["spawnb_sc_dump"] = [SetLocal("owner", "SWLOR_SPAWN_DUMP", "1")],
            ["spawnb_sc_nodump"] = [SetLocal("owner", "SWLOR_SPAWN_DUMP", "0")],
            ["spawnb_sc_sdlog"] = [SetLocal("area", "SWLOR_SPAWN_DELAY_LOG", "1")],
            ["spawnb_sc_nsdlog"] = [SetLocal("area", "SWLOR_SPAWN_DELAY_LOG", "0")],
            ["spawnb_sc_sclog"] = [SetLocal("area", "SWLOR_SPAWN_COUNT_LOG", "1")],
            ["spawnb_sc_snclog"] = [SetLocal("area", "SWLOR_SPAWN_COUNT_LOG", "0")]
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
        (new Regex("<R>", RegexOptions.IgnoreCase | RegexOptions.Compiled), "{{player.race}}"),
        (new Regex("<CUSTOM4000>", RegexOptions.IgnoreCase | RegexOptions.Compiled), "{{skyrace.record-holder}}"),
        (new Regex("<CUSTOM4001>", RegexOptions.IgnoreCase | RegexOptions.Compiled), "{{skyrace.record-time}}"),
        (new Regex("<CUSTOM4002>", RegexOptions.IgnoreCase | RegexOptions.Compiled), "{{skyrace.entry-fee}}"),
        (new Regex("<CUSTOM4003>", RegexOptions.IgnoreCase | RegexOptions.Compiled), "{{skyrace.prize}}"),
        (new Regex("<CUSTOM1000>", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Spawn controls for "),
        (new Regex("<CUSTOM999>", RegexOptions.IgnoreCase | RegexOptions.Compiled), "{{owner.name}}"),
        (new Regex("<CUSTOM1001>", RegexOptions.IgnoreCase | RegexOptions.Compiled), ".")
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
            if (CustomConditions.TryGetValue(link.Active, out var mapped))
            {
                foreach (var operation in mapped)
                {
                    destination.Add(new ConversationCondition
                    {
                        Key = operation.Key,
                        Arguments = operation.Arguments.ToList()
                    });
                }
                return;
            }

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
            if (CustomActions.TryGetValue(node.Script, out var mapped))
            {
                foreach (var operation in mapped)
                {
                    destination.Add(new ConversationAction
                    {
                        Key = operation.Key,
                        Arguments = operation.Arguments.ToList()
                    });
                }
                return;
            }

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
            destination.Add(new ConversationAction
            {
                Key = action.SnippetKey,
                Arguments = action.Arguments.ToList()
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

    private static OperationMapping LocalCondition(string scope, string variable, string comparison, string value) =>
        new("condition-local-number", scope, variable, comparison, value);

    private static OperationMapping SetLocal(string scope, string variable, string value) =>
        new("action-set-local-number", scope, variable, value);

    private static OperationMapping AdjustLocal(string scope, string variable, string value) =>
        new("action-adjust-local-number", scope, variable, value);
}
