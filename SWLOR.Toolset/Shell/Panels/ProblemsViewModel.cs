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
        public ProblemRow(string resRef, ScriptAnalysisDiagnostic diagnostic)
        {
            ResRef = resRef;
            Diagnostic = diagnostic;
        }

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

        /// <summary>Replaces the findings for one script, leaving other scripts' rows in place.</summary>
        public void SetDiagnostics(string resRef, IReadOnlyList<ScriptAnalysisDiagnostic> diagnostics)
        {
            for (var i = Rows.Count - 1; i >= 0; i--)
            {
                if (string.Equals(Rows[i].ResRef, resRef, StringComparison.OrdinalIgnoreCase))
                    Rows.RemoveAt(i);
            }

            foreach (var diagnostic in diagnostics)
                Rows.Add(new ProblemRow(resRef, diagnostic));

            RaiseCounts();
        }

        /// <summary>Drops every row for a script, e.g. when its tab closes.</summary>
        public void Clear(string resRef) => SetDiagnostics(resRef, Array.Empty<ScriptAnalysisDiagnostic>());

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
