using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using SWLOR.Toolset.Domain.Script.Syntax;

namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>One row in the Problems list.</summary>
    public sealed class ProblemRow
    {
        public ProblemRow(string ownerResRef, ScriptAnalysisDiagnostic diagnostic)
        {
            OwnerResRef = ownerResRef;
            ResRef = diagnostic.ResRef ?? ownerResRef;
            Diagnostic = diagnostic;
        }

        /// <summary>The entry point whose diagnostics batch owns this row.</summary>
        public string OwnerResRef { get; }

        public string ResRef { get; }

        public ScriptAnalysisDiagnostic Diagnostic { get; }

        public string Location => $"{ResRef}.nss({Diagnostic.Line})";

        public string Message => Diagnostic.Message;

        public bool IsError => Diagnostic.Severity == ScriptDiagnosticSeverity.Error;

        public string SeverityGlyph => IsError ? "✕" : "⚠";

        /// <summary>
        /// "editor" or "compiler". The visible half of the two-tier rule: our own analysis is
        /// advisory, the compiler is authoritative, and without the tag a disagreement between them
        /// reads as a compiler bug.
        /// </summary>
        public string SourceTag => Diagnostic.Source == ScriptDiagnosticSource.Compiler ? "compiler" : "editor";
    }

    /// <summary>
    /// The Problems dock panel: script diagnostics from both tiers, with click-to-navigate.
    /// </summary>
    /// <remarks>
    /// Separate from Validation, which reports module-integrity issues. A problem is "this code is
    /// wrong"; a validation issue is "this data is inconsistent". Staleness deliberately lands in
    /// Validation rather than here, because the code is fine and it is the build artifact that is out
    /// of date.
    /// </remarks>
    public partial class ProblemsViewModel : Tool
    {
        public ProblemsViewModel()
        {
            Id = "Problems";
            Title = "Problems";
        }

        public ObservableCollection<ProblemRow> Rows { get; } = new();

        [ObservableProperty]
        private ProblemRow? _selectedRow;

        /// <summary>Raised when a row is activated, so the shell can focus that line.</summary>
        public event Action<ProblemRow>? NavigateRequested;

        public int ErrorCount => Rows.Count(r => r.IsError);

        public int WarningCount => Rows.Count(r => !r.IsError);

        public string Summary => Rows.Count == 0
            ? "No problems"
            : $"{ErrorCount} error(s), {WarningCount} warning(s)";

        /// <summary>
        /// Replaces one script's findings <b>from one tier</b>, leaving the other tier's rows and
        /// every other script's rows in place.
        /// </summary>
        /// <remarks>
        /// The two tiers arrive on completely different schedules — the editor re-analyses on every
        /// idle keystroke, the compiler only on save or Ctrl+B — so they must not overwrite each other.
        /// Replacing all of a script's rows from the idle pass would wipe the compiler's findings a
        /// quarter-second after they appeared.
        /// </remarks>
        public void SetDiagnostics(
            string resRef,
            ScriptDiagnosticSource source,
            IReadOnlyList<ScriptAnalysisDiagnostic> diagnostics)
        {
            for (var i = Rows.Count - 1; i >= 0; i--)
            {
                if (string.Equals(Rows[i].OwnerResRef, resRef, StringComparison.OrdinalIgnoreCase) &&
                    Rows[i].Diagnostic.Source == source)
                    Rows.RemoveAt(i);
            }

            foreach (var diagnostic in diagnostics)
                Rows.Add(new ProblemRow(resRef, diagnostic));

            // Errors first, then by line: the Problems list is scanned, not read in arrival order.
            var ordered = Rows
                .OrderByDescending(r => r.IsError)
                .ThenBy(r => r.ResRef, StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.Diagnostic.Line)
                .ToList();

            Rows.Clear();
            foreach (var row in ordered)
                Rows.Add(row);

            RaiseCounts();
        }

        /// <summary>Drops every row for a script, e.g. when its tab closes.</summary>
        public void Clear(string resRef)
        {
            SetDiagnostics(resRef, ScriptDiagnosticSource.Editor, Array.Empty<ScriptAnalysisDiagnostic>());
            SetDiagnostics(resRef, ScriptDiagnosticSource.Compiler, Array.Empty<ScriptAnalysisDiagnostic>());
        }

        public void ClearAll()
        {
            Rows.Clear();
            RaiseCounts();
        }

        [RelayCommand]
        private void Navigate(ProblemRow? row)
        {
            if (row != null)
                NavigateRequested?.Invoke(row);
        }

        private void RaiseCounts()
        {
            OnPropertyChanged(nameof(ErrorCount));
            OnPropertyChanged(nameof(WarningCount));
            OnPropertyChanged(nameof(Summary));
            Title = Rows.Count == 0 ? "Problems" : $"Problems {Rows.Count}";
        }
    }
}
