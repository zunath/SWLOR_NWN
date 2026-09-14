using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Rendering;
using SWLOR.Toolset.Domain.Script.Syntax;

namespace SWLOR.Toolset.Editors.Script
{
    /// <summary>
    /// Draws wavy underlines beneath diagnostics.
    /// </summary>
    /// <remarks>
    /// Colour carries severity, matching the rest of the toolset: errors use a red reserved for this,
    /// warnings reuse <c>AmberBrush</c>, which already means "needs attention" everywhere else in the
    /// app. Findings with zero length are skipped — compiler diagnostics arrive with a line number
    /// but no column, so they belong in the Problems list rather than under a specific word.
    /// </remarks>
    public sealed class DiagnosticSquiggleRenderer : IBackgroundRenderer
    {
        private static readonly IPen ErrorPen = new Pen(new SolidColorBrush(Color.Parse("#E05A5A")), 1.2);
        private static readonly IPen WarningPen = new Pen(new SolidColorBrush(Color.Parse("#D9A155")), 1.2);

        private IReadOnlyList<ScriptAnalysisDiagnostic> _diagnostics = Array.Empty<ScriptAnalysisDiagnostic>();

        public KnownLayer Layer => KnownLayer.Selection;

        public void SetDiagnostics(IReadOnlyList<ScriptAnalysisDiagnostic> diagnostics) =>
            _diagnostics = diagnostics;

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (_diagnostics.Count == 0 || textView.VisualLines.Count == 0)
                return;

            var documentLength = textView.Document?.TextLength ?? 0;

            foreach (var diagnostic in _diagnostics)
            {
                if (diagnostic.Length <= 0)
                    continue;

                var start = Math.Clamp(diagnostic.Start, 0, documentLength);
                var end = Math.Clamp(diagnostic.End, start, documentLength);
                if (start >= end)
                    continue;

                var segment = new AvaloniaEdit.Document.TextSegment { StartOffset = start, EndOffset = end };
                var pen = diagnostic.Severity == ScriptDiagnosticSeverity.Error ? ErrorPen : WarningPen;

                foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
                    DrawSquiggle(drawingContext, pen, rect);
            }
        }

        private static void DrawSquiggle(DrawingContext context, IPen pen, Rect rect)
        {
            const double step = 3.0;
            var y = rect.Bottom - 1.5;
            var geometry = new StreamGeometry();

            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(new Point(rect.Left, y), false);

                var up = true;
                for (var x = rect.Left + step; x < rect.Right; x += step)
                {
                    ctx.LineTo(new Point(x, up ? y - 2.0 : y));
                    up = !up;
                }
            }

            context.DrawGeometry(null, pen, geometry);
        }
    }
}
