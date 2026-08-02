using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Script.Syntax;
using SWLOR.Toolset.Shell.Panels;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The Problems list holds two tiers that arrive on different schedules. The editor re-analyses
    /// on every idle keystroke; the compiler only on save or Ctrl+B. Neither may erase the other.
    /// </summary>
    public class ProblemsViewModelTests
    {
        private static ScriptAnalysisDiagnostic Diagnostic(
            string message, ScriptDiagnosticSource source, int line = 1,
            ScriptDiagnosticSeverity severity = ScriptDiagnosticSeverity.Error) =>
            new(message, 0, 5, severity, source, line);

        [Test]
        public void EditorAndCompilerFindingsCoexist()
        {
            var vm = new ProblemsViewModel();

            vm.SetDiagnostics("a", ScriptDiagnosticSource.Editor,
                new[] { Diagnostic("editor finding", ScriptDiagnosticSource.Editor) });
            vm.SetDiagnostics("a", ScriptDiagnosticSource.Compiler,
                new[] { Diagnostic("compiler finding", ScriptDiagnosticSource.Compiler) });

            vm.Rows.Should().HaveCount(2);
        }

        /// <summary>
        /// The regression this split exists to prevent: an idle re-analysis a quarter-second after a
        /// compile must not wipe the compiler's findings.
        /// </summary>
        [Test]
        public void ReanalysingTheEditorTierLeavesCompilerFindingsAlone()
        {
            var vm = new ProblemsViewModel();
            vm.SetDiagnostics("a", ScriptDiagnosticSource.Compiler,
                new[] { Diagnostic("compiler finding", ScriptDiagnosticSource.Compiler) });

            vm.SetDiagnostics("a", ScriptDiagnosticSource.Editor,
                new[] { Diagnostic("new editor finding", ScriptDiagnosticSource.Editor) });

            vm.Rows.Should().Contain(r => r.Message == "compiler finding");
        }

        [Test]
        public void ReplacingATierDropsOnlyThatTiersRows()
        {
            var vm = new ProblemsViewModel();
            vm.SetDiagnostics("a", ScriptDiagnosticSource.Editor,
                new[] { Diagnostic("old", ScriptDiagnosticSource.Editor) });

            vm.SetDiagnostics("a", ScriptDiagnosticSource.Editor,
                new[] { Diagnostic("new", ScriptDiagnosticSource.Editor) });

            vm.Rows.Should().ContainSingle().Which.Message.Should().Be("new");
        }

        [Test]
        public void OtherScriptsAreUntouched()
        {
            var vm = new ProblemsViewModel();
            vm.SetDiagnostics("a", ScriptDiagnosticSource.Editor,
                new[] { Diagnostic("in a", ScriptDiagnosticSource.Editor) });
            vm.SetDiagnostics("b", ScriptDiagnosticSource.Editor,
                new[] { Diagnostic("in b", ScriptDiagnosticSource.Editor) });

            vm.SetDiagnostics("a", ScriptDiagnosticSource.Editor, Array.Empty<ScriptAnalysisDiagnostic>());

            vm.Rows.Should().ContainSingle().Which.ResRef.Should().Be("b");
        }

        [Test]
        public void ClearDropsBothTiersForOneScript()
        {
            var vm = new ProblemsViewModel();
            vm.SetDiagnostics("a", ScriptDiagnosticSource.Editor,
                new[] { Diagnostic("e", ScriptDiagnosticSource.Editor) });
            vm.SetDiagnostics("a", ScriptDiagnosticSource.Compiler,
                new[] { Diagnostic("c", ScriptDiagnosticSource.Compiler) });

            vm.Clear("a");

            vm.Rows.Should().BeEmpty();
        }

        [Test]
        public void ErrorsSortAboveWarnings()
        {
            var vm = new ProblemsViewModel();
            vm.SetDiagnostics("a", ScriptDiagnosticSource.Editor, new[]
            {
                Diagnostic("a warning", ScriptDiagnosticSource.Editor, 5, ScriptDiagnosticSeverity.Warning),
                Diagnostic("an error", ScriptDiagnosticSource.Editor, 90)
            });

            // The list is scanned, not read in arrival order.
            vm.Rows[0].Message.Should().Be("an error");
        }

        [Test]
        public void CountsAndTitleTrackTheRows()
        {
            var vm = new ProblemsViewModel();
            vm.Title.Should().Be("Problems");

            vm.SetDiagnostics("a", ScriptDiagnosticSource.Editor, new[]
            {
                Diagnostic("e", ScriptDiagnosticSource.Editor),
                Diagnostic("w", ScriptDiagnosticSource.Editor, 2, ScriptDiagnosticSeverity.Warning)
            });

            vm.ErrorCount.Should().Be(1);
            vm.WarningCount.Should().Be(1);
            vm.Title.Should().Be("Problems 2");
        }

        [Test]
        public void SourceTagNamesTheTier()
        {
            var vm = new ProblemsViewModel();
            vm.SetDiagnostics("a", ScriptDiagnosticSource.Compiler,
                new[] { Diagnostic("c", ScriptDiagnosticSource.Compiler) });

            // Without the tag, a disagreement between the tiers reads as a compiler bug.
            vm.Rows[0].SourceTag.Should().Be("compiler");
        }

        [Test]
        public void ForeignIncludeDiagnosticNavigatesToTheIncludeButIsClearedByItsOwningCompile()
        {
            var vm = new ProblemsViewModel();
            var diagnostic = new ScriptAnalysisDiagnostic(
                "include failure", 0, 0, ScriptDiagnosticSeverity.Error,
                ScriptDiagnosticSource.Compiler, 12, "shared_inc");

            vm.SetDiagnostics("entry", ScriptDiagnosticSource.Compiler, new[] { diagnostic });

            vm.Rows.Should().ContainSingle();
            vm.Rows[0].ResRef.Should().Be("shared_inc");
            vm.Rows[0].OwnerResRef.Should().Be("entry");

            vm.SetDiagnostics(
                "entry", ScriptDiagnosticSource.Compiler, Array.Empty<ScriptAnalysisDiagnostic>());
            vm.Rows.Should().BeEmpty();
        }
    }
}
