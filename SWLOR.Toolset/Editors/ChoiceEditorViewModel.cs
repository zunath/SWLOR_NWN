using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Game.Server.Service.SnippetService;
using SWLOR.Toolset.Domain.Conversations;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.GameCode;

namespace SWLOR.Toolset.Editors
{
    /// <summary>
    /// One argument of a guard or consequence, as a control: a dropdown of real values where the
    /// game code knows them, a plain box where it does not.
    /// </summary>
    public sealed partial class ArgumentEditorViewModel : ObservableObject
    {
        private readonly Action _onChanged;
        private bool _loading;

        [ObservableProperty]
        private ArgumentOption? _selected;

        [ObservableProperty]
        private string _freeText = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasOptions))]
        [NotifyPropertyChangedFor(nameof(IsFreeText))]
        [NotifyPropertyChangedFor(nameof(Value))]
        private IReadOnlyList<ArgumentOption> _options = Array.Empty<ArgumentOption>();

        public ArgumentEditorViewModel(
            SnippetArgument argument,
            string value,
            IReadOnlyList<ArgumentOption> options,
            Action onChanged)
        {
            Argument = argument;
            _onChanged = onChanged;

            _loading = true;
            _options = options;
            _freeText = value;
            _selected = options.FirstOrDefault(option => option.Value == value);
            _loading = false;
        }

        public SnippetArgument Argument { get; }

        public string Name => Argument.Name;

        /// <summary>True when there is a real list to pick from; otherwise the box is free text.</summary>
        public bool HasOptions => Options.Count > 0;

        public bool IsFreeText => Options.Count == 0;

        /// <summary>What would be written into the conversation.</summary>
        public string Value => HasOptions ? Selected?.Value ?? FreeText : FreeText;

        /// <summary>
        /// Rebuilds this argument's picker after an earlier sibling changes, preserving its stored
        /// value even when that value is no longer in the newly selected option family.
        /// </summary>
        public void RefreshOptions(IReadOnlyList<ArgumentOption> options)
        {
            ArgumentNullException.ThrowIfNull(options);

            var value = Value;
            _loading = true;
            try
            {
                Options = options;
                FreeText = value;
                Selected = options.FirstOrDefault(option => option.Value == value);
            }
            finally
            {
                _loading = false;
            }

            OnPropertyChanged(nameof(Value));
        }

        partial void OnSelectedChanged(ArgumentOption? value)
        {
            if (_loading)
                return;

            if (value != null)
            {
                _loading = true;
                FreeText = value.Value;
                _loading = false;
            }

            _onChanged();
        }

        partial void OnFreeTextChanged(string value)
        {
            if (!_loading)
                _onChanged();
        }
    }

    /// <summary>
    /// One guard on a route, or one consequence of a line, with its arguments spelled out. This is
    /// what turns "condition-on-quest-state field_tinctures 2" into a sentence and two dropdowns.
    /// </summary>
    public sealed partial class SnippetEditorViewModel : ObservableObject
    {
        private readonly Action<SnippetEditorViewModel> _onCommit;
        private readonly Action<SnippetEditorViewModel> _onRemove;
        private readonly SnippetArgumentOptions _options;
        private bool _loading;

        [ObservableProperty]
        private bool _isNegated;

        private readonly Func<SnippetArgument, string, string?>? _display;

        public SnippetEditorViewModel(
            DlgParam param,
            SnippetDescriptor snippet,
            SnippetArgumentOptions options,
            bool canNegate,
            Action<SnippetEditorViewModel> onCommit,
            Action<SnippetEditorViewModel> onRemove,
            Func<SnippetArgument, string, string?>? display = null,
            bool canRemove = true)
        {
            Param = param;
            Snippet = snippet;
            CanNegate = canNegate;
            CanRemove = canRemove;
            _onCommit = onCommit;
            _onRemove = onRemove;
            _options = options;
            _display = display;

            _loading = true;
            _isNegated = param.IsNegated;

            var values = param.Arguments;
            var count = Math.Max(values.Length, snippet.MinimumArgumentCount);
            for (var i = 0; i < count; i++)
            {
                var argument = snippet.ArgumentAt(i);
                if (argument == null)
                    continue;

                var value = i < values.Length ? values[i] : string.Empty;
                var argumentIndex = i;
                Arguments.Add(new ArgumentEditorViewModel(
                    argument,
                    value,
                    options.For(argument, values),
                    () => CommitArgument(argumentIndex)));
            }

            _loading = false;
        }

        public DlgParam Param { get; }

        public SnippetDescriptor Snippet { get; }

        /// <summary>Guards can be negated; consequences cannot.</summary>
        public bool CanNegate { get; }

        /// <summary>False for outcomes supplied by the selected guided behavior.</summary>
        public bool CanRemove { get; }

        public ObservableCollection<ArgumentEditorViewModel> Arguments { get; } = new();

        public bool CanAddArguments =>
            Snippet.RepeatGroupSize > 0 || Arguments.Count < Snippet.Arguments.Count;

        public bool CanRemoveArguments => Arguments.Count > Snippet.MinimumArgumentCount;

        /// <summary>
        /// The whole thing as one sentence, which is what the writer reads. Ids are resolved to
        /// names — a panel that says "field_tinctures" where the rest of the editor says "Field
        /// Tinctures" is the one place the raw data would leak back out.
        /// </summary>
        public string Sentence =>
            Snippet.ToSentence(Arguments.Select(argument => argument.Value).ToList(), IsNegated, _display);

        public string Description => Snippet.Description;

        [RelayCommand(CanExecute = nameof(CanRemove))]
        private void Remove() => _onRemove(this);

        [RelayCommand(CanExecute = nameof(CanAddArguments))]
        private void AddArguments()
        {
            var count = Snippet.RepeatGroupSize > 0 && Arguments.Count >= Snippet.Arguments.Count
                ? Snippet.RepeatGroupSize
                : 1;

            _loading = true;
            for (var i = 0; i < count; i++)
            {
                var argumentIndex = Arguments.Count;
                var argument = Snippet.ArgumentAt(argumentIndex);
                if (argument == null)
                    break;

                Arguments.Add(new ArgumentEditorViewModel(
                    argument,
                    string.Empty,
                    _options.For(argument, Arguments.Select(item => item.Value).ToArray()),
                    () => CommitArgument(argumentIndex)));
            }
            _loading = false;

            NotifyArgumentShapeChanged();
            Commit();
        }

        [RelayCommand(CanExecute = nameof(CanRemoveArguments))]
        private void RemoveArguments()
        {
            var removable = Arguments.Count - Snippet.MinimumArgumentCount;
            var count = Snippet.RepeatGroupSize > 0 && Arguments.Count > Snippet.Arguments.Count
                ? Math.Min(Snippet.RepeatGroupSize, removable)
                : 1;

            for (var i = 0; i < count && Arguments.Count > Snippet.MinimumArgumentCount; i++)
                Arguments.RemoveAt(Arguments.Count - 1);

            NotifyArgumentShapeChanged();
            Commit();
        }

        private void NotifyArgumentShapeChanged()
        {
            OnPropertyChanged(nameof(CanAddArguments));
            OnPropertyChanged(nameof(CanRemoveArguments));
            AddArgumentsCommand.NotifyCanExecuteChanged();
            RemoveArgumentsCommand.NotifyCanExecuteChanged();
        }

        /// <summary>The key and value this should be stored as.</summary>
        public (string Key, string Value) ToParam() =>
            (IsNegated && CanNegate ? "!" + Snippet.Key : Snippet.Key,
                string.Join(' ', Arguments.Select(argument => argument.Value).Where(value => value.Length > 0)));

        private void CommitArgument(int changedIndex)
        {
            if (_loading)
                return;

            var values = Arguments.Select(argument => argument.Value).ToArray();
            for (var i = changedIndex + 1; i < Arguments.Count; i++)
            {
                var argument = Snippet.ArgumentAt(i);
                if (argument == null)
                    continue;

                Arguments[i].RefreshOptions(_options.For(argument, values));
                values[i] = Arguments[i].Value;
            }

            Commit();
        }

        private void Commit()
        {
            if (_loading)
                return;

            OnPropertyChanged(nameof(Sentence));
            _onCommit(this);
        }

        partial void OnIsNegatedChanged(bool value) => Commit();

    }
}
