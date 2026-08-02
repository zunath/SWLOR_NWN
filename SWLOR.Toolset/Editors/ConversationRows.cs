using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Conversations;
using SWLOR.Toolset.Domain.Documents;

namespace SWLOR.Toolset.Editors
{
    public enum ConversationBehaviorKind
    {
        Merchant,
        QuestGiver,
        Conversation
    }

    /// <summary>One plain-language authoring shape offered by the dialogue editor.</summary>
    public sealed record ConversationBehaviorOption(
        ConversationBehaviorKind Kind,
        string Name,
        string Explanation)
    {
        public override string ToString() => Name;
    }

    /// <summary>A friendly label paired with the exact token NWN stores in dialogue text.</summary>
    public sealed record DynamicTextTokenOption(string Name, string Token)
    {
        public override string ToString() => Name;
    }

    /// <summary>One row of the situation rail: a circumstance the conversation answers.</summary>
    public sealed partial class SituationRowViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isSelected;

        public SituationRowViewModel(Situation situation)
        {
            Situation = situation;
        }

        public Situation Situation { get; }

        public int Order => Situation.Order;

        public string Title => Situation.Title;

        public string When => Situation.When;

        /// <summary>The dot: filled when written, hollow when blank, crossed when it cannot happen.</summary>
        public string Marker => Situation.State switch
        {
            SituationState.Written => "●",
            SituationState.Empty => "○",
            _ => "⊘"
        };

        /// <summary>The small line under the title - what is there, or why nothing is.</summary>
        public string Summary => Situation.State switch
        {
            SituationState.Unreachable => "CAN NEVER HAPPEN",
            SituationState.Empty => "NOTHING WRITTEN",
            _ => Describe()
        };

        public bool IsWritten => Situation.State == SituationState.Written;

        public bool IsUnreachable => Situation.State == SituationState.Unreachable;

        public bool IsEmpty => Situation.State == SituationState.Empty;

        public bool HasCompetingLines { get; internal set; }

        private string Describe()
        {
            var lines = Situation.LineCount == 1 ? "1 LINE" : $"{Situation.LineCount} LINES";
            if (Situation.ChoiceCount == 0)
                return Situation.IsCatchAll ? $"ANYONE ELSE · {lines}" : lines;

            var choices = Situation.ChoiceCount == 1 ? "1 CHOICE" : $"{Situation.ChoiceCount} CHOICES";
            return Situation.IsCatchAll ? $"ANYONE ELSE · {lines} · {choices}" : $"{lines} · {choices}";
        }
    }

    /// <summary>One cell of the coverage strip: a step of a quest, filled or hollow.</summary>
    public sealed class CoverageCellViewModel
    {
        public CoverageCellViewModel(CoverageCell cell, string questId)
        {
            Cell = cell;
            QuestId = questId;
        }

        public CoverageCell Cell { get; }

        public string QuestId { get; }

        public string Label => Cell.Label;

        public bool IsCovered => Cell.IsCovered;

        public string ToolTip => Cell.IsCovered
            ? $"There is a line for this."
            : Cell.Label switch
            {
                "OFFER" => "Nothing here starts this quest.",
                "DONE" => "Nothing is said once this quest is finished.",
                _ => $"Nothing is said to a player on step {Cell.Label}."
            };
    }

    /// <summary>One row of the coverage strip: a quest this conversation touches.</summary>
    public sealed class CoverageRowViewModel
    {
        public CoverageRowViewModel(QuestCoverage coverage)
        {
            Name = coverage.Name;
            IsRepeatable = coverage.IsRepeatable;
            Cells = coverage.Cells.Select(cell => new CoverageCellViewModel(cell, coverage.QuestId)).ToList();
        }

        public string Name { get; }

        public bool IsRepeatable { get; }

        public IReadOnlyList<CoverageCellViewModel> Cells { get; }
    }

    /// <summary>One numbered choice under the NPC's line, as the player would see it.</summary>
    public sealed class ChoiceRowViewModel
    {
        public ChoiceRowViewModel(
            DlgLink link, string number, string consequence, string? hiddenBecause,
            int order, int choiceCount, bool isDangling = false)
        {
            Link = link;
            Number = number;
            Consequence = consequence;
            HiddenBecause = hiddenBecause;
            Order = order;
            ChoiceCount = choiceCount;
            IsDangling = isDangling;
        }

        public DlgLink Link { get; }

        /// <summary>
        /// True when the link's target index is outside the document - an imported or externally
        /// edited route pointing at a line that no longer exists. <see cref="Target"/> throws for
        /// such a row, so every dereference checks this first.
        /// </summary>
        public bool IsDangling { get; }

        /// <summary>One-based position in the parent line's reply list—the order NWN displays.</summary>
        public int Order { get; }

        public int ChoiceCount { get; }

        public bool HasSiblings => ChoiceCount > 1;

        public bool CanMoveUp => Order > 1;

        public bool CanMoveDown => Order < ChoiceCount;

        public DlgNode Target => Link.Target;

        /// <summary>"1." for a visible choice, an em dash for one this player cannot see.</summary>
        public string Number { get; }

        public string Text => IsDangling
            ? "(this choice points at a line that no longer exists)"
            : FriendlyText(Target);

        public string PreviewText => IsDangling
            ? Text
            : DynamicTextPreview.Resolve(FriendlyText(Target));

        /// <summary>The stored words, without the friendly label used for structural blank replies.</summary>
        public string AuthoringText => IsDangling || string.IsNullOrWhiteSpace(Target.Text)
                                       || Target.Text == QuestConversationScaffold.Placeholder
            ? string.Empty
            : Target.Text;

        /// <summary>What picking this does, in plain English, or where it leads when it does nothing.</summary>
        public string Consequence { get; }

        /// <summary>The guard keeping this choice off the player's screen, when it is hidden.</summary>
        public string? HiddenBecause { get; }

        public bool IsHidden => HiddenBecause != null;

        public bool IsVisible => HiddenBecause == null;

        public bool CanAddFollowUp => !IsDangling && Target.Links.Count == 0;

        private static string FriendlyText(DlgNode target)
        {
            if (!string.IsNullOrWhiteSpace(target.Text)
                && target.Text != QuestConversationScaffold.Placeholder)
                return target.Text;

            return target.Links.Count == 0 ? "End conversation" : "Continue automatically";
        }
    }

    /// <summary>One finding, shown against the thing it is about.</summary>
    public sealed class ProblemRowViewModel
    {
        public ProblemRowViewModel(ConversationProblem problem)
        {
            Problem = problem;
        }

        public ConversationProblem Problem { get; }

        public string Message => Problem.Message;

        public string Severity => Problem.Severity switch
        {
            ProblemSeverity.Broken => "BROKEN",
            ProblemSeverity.Untidy => "UNTIDY",
            _ => "HINT"
        };

        public bool IsBroken => Problem.Severity == ProblemSeverity.Broken;

        public bool IsUntidy => Problem.Severity == ProblemSeverity.Untidy;

        public bool IsHint => Problem.Severity == ProblemSeverity.Hint;

        public bool CanFix => Problem.RuleId == "conditional-choice-no-fallback"
                              || (Problem.RuleId == "unreachable-opening"
                                  && Problem.Message.Contains("answers everybody", StringComparison.Ordinal));

        public string FixLabel => Problem.RuleId switch
        {
            "unreachable-opening" => "Move fallback last",
            "conditional-choice-no-fallback" => "Add Goodbye",
            _ => string.Empty
        };

        /// <summary>Where in the conversation this sits, for the row's right-hand caption.</summary>
        public string Where => Problem.Anchor switch
        {
            ProblemAnchor.Situation => Problem.Situation?.Title ?? "situation",
            ProblemAnchor.Line => "a line",
            ProblemAnchor.Choice => "a choice",
            _ => "this conversation"
        };
    }

    /// <summary>
    /// One quest the pretend player has a position on. Changing it redraws the conversation, which
    /// is how a writer moves between situations without knowing what a condition is.
    /// </summary>
    public sealed partial class QuestPillViewModel : ObservableObject
    {
        private readonly Action _onChanged;

        [ObservableProperty]
        private string _selectedOption;

        public QuestPillViewModel(string questId, string name, int stateCount, string selected, Action onChanged)
        {
            QuestId = questId;
            Name = name;
            _onChanged = onChanged;
            _selectedOption = selected;

            var options = new List<string> { NotStarted };
            for (var state = 1; state <= Math.Max(stateCount, 1); state++)
                options.Add($"on step {state}");
            options.Add(Finished);
            Options = options;
        }

        public const string NotStarted = "never started";
        public const string Finished = "finished";

        public string QuestId { get; }

        public string Name { get; }

        public IReadOnlyList<string> Options { get; }

        public QuestProgress ToProgress()
        {
            if (SelectedOption == Finished)
                return QuestProgress.Completed;

            if (SelectedOption.StartsWith("on step", StringComparison.Ordinal)
                && int.TryParse(SelectedOption.AsSpan("on step ".Length), out var state))
                return QuestProgress.OnStep(state);

            return QuestProgress.None;
        }

        partial void OnSelectedOptionChanged(string value) => _onChanged();
    }

    internal static class DynamicTextPreview
    {
        private static readonly IReadOnlyDictionary<string, string> Samples =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["<FirstName>"] = "Kori",
                ["<FullName>"] = "Kori Venn",
                ["<Class>"] = "Scout",
                ["<Day/Night>"] = "day",
                ["<Boy/Girl>"] = "girl"
            };

        public static string Resolve(string text)
        {
            foreach (var (token, sample) in Samples)
                text = text.Replace(token, sample, StringComparison.OrdinalIgnoreCase);

            return text;
        }
    }
}
