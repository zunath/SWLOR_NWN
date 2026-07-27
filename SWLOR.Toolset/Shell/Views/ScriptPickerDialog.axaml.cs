using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SWLOR.Toolset.Shell.Views
{
    /// <summary>One selectable script in the picker.</summary>
    public sealed class ScriptPickerRow
    {
        public ScriptPickerRow(
            string resRef, string label, bool isInclude, int usageCount, bool hasSource = true)
        {
            ResRef = resRef;
            Label = label;
            IsInclude = isInclude;
            UsageCount = usageCount;
            HasSource = hasSource;
        }

        public string ResRef { get; }

        public string Label { get; }

        public bool IsInclude { get; }

        public int UsageCount { get; }

        /// <summary>
        /// False for a compiled .ncs with no .nss beside it. The module has many; they run perfectly
        /// well and simply cannot be opened here.
        /// </summary>
        public bool HasSource { get; }

        /// <summary>
        /// Includes are labelled rather than hidden: an <c>_inc</c> file in an event slot is almost
        /// always a mistake, but marking beats silently filtering — the builder may know better. The
        /// same reasoning covers source-less scripts, which used to be missing from this list
        /// entirely: a slot naming one was reported as pointing at a script that does not exist, and
        /// no other slot could be pointed at it at all.
        /// </summary>
        public string Note => !HasSource
            ? "compiled only"
            : IsInclude
                ? "include"
                : UsageCount > 0 ? $"used by {UsageCount}" : string.Empty;

        public IBrush NoteBrush => IsInclude || !HasSource
            ? new SolidColorBrush(Color.Parse("#6C7683"))
            : new SolidColorBrush(Color.Parse("#5FBE8C"));
    }

    /// <summary>
    /// The script-slot picker. Redeems the promise left in <c>EditorKind.ScriptSlot</c>
    /// ("script picker in a later package").
    /// </summary>
    public partial class ScriptPickerDialog : Window
    {
        private readonly List<ScriptPickerRow> _all = new();

        public ScriptPickerDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public ObservableCollection<ScriptPickerRow> Rows { get; } = new();

        public string SlotDescription { get; private set; } = "Select script";

        public string MissingWarning { get; private set; } = string.Empty;

        public bool HasMissingWarning => MissingWarning.Length > 0;

        private string _filter = string.Empty;

        public string Filter
        {
            get => _filter;
            set
            {
                if (_filter == value)
                    return;

                _filter = value;
                Rebuild();
            }
        }

        public ScriptPickerRow? SelectedRow { get; set; }

        /// <summary>Set by the caller so "New script..." can create one without this dialog knowing how.</summary>
        public Func<Task<string?>>? NewScriptFactory { get; set; }

        /// <summary>
        /// Configures the dialog. <paramref name="current"/> drives the missing-script warning, which
        /// is the finding this whole picker exists to surface.
        /// </summary>
        public void Configure(
            string slotLabel,
            string ownerDescription,
            string current,
            IReadOnlyList<ScriptPickerRow> scripts)
        {
            SlotDescription = $"{ownerDescription} · {slotLabel}";
            _all.Clear();
            _all.AddRange(scripts.OrderBy(s => s.ResRef, StringComparer.OrdinalIgnoreCase));

            MissingWarning = !string.IsNullOrWhiteSpace(current) &&
                             _all.All(s => !string.Equals(s.ResRef, current, StringComparison.OrdinalIgnoreCase))
                ? $"Current value '{current}' does not exist in this module."
                : string.Empty;

            Rebuild();
            SelectedRow = _all.FirstOrDefault(s => string.Equals(s.ResRef, current, StringComparison.OrdinalIgnoreCase));
        }

        private void Rebuild()
        {
            Rows.Clear();

            foreach (var row in _all)
            {
                if (_filter.Length > 0 && !row.ResRef.Contains(_filter, StringComparison.OrdinalIgnoreCase) &&
                    !row.Label.Contains(_filter, StringComparison.OrdinalIgnoreCase))
                    continue;

                Rows.Add(row);
            }
        }

        [RelayCommand]
        private void Accept() => Close(SelectedRow?.ResRef);

        [RelayCommand]
        private void Cancel() => Close(null);

        /// <summary>Clearing a slot is a real edit, so it returns empty rather than null.</summary>
        [RelayCommand]
        private void Clear() => Close(string.Empty);

        [RelayCommand]
        private async Task NewScript()
        {
            if (NewScriptFactory == null)
                return;

            var created = await NewScriptFactory().ConfigureAwait(true);
            if (created != null)
                Close(created);
        }

        private void OnRowDoubleTapped(object? sender, RoutedEventArgs e)
        {
            if (SelectedRow != null)
                Close(SelectedRow.ResRef);
        }
    }
}
