// ============================================================================
// GuiTable.cs
//
// PROBLEM THIS SOLVES
// --------------------
// CharacterSheetDefinition.cs currently repeats this shape 3 times (Stats,
// Resistances, Crafting tabs): a manually-built header row (AddTableHeader
// per column, width typed twice - once in the header, once in the cell),
// an AddList/AddCell template with one GuiBindingList<string> per column,
// and a BindRowCount pointing at an arbitrary "first" column as a row-count
// proxy.
//
// CharacterSheetViewModel.cs mirrors this with 3 near-identical Refresh*
// methods that build N parallel GuiBindingList<string> instances and .Add()
// to all of them in lockstep inside a loop - nothing stops these from
// drifting out of sync in length.
//
// This file does NOT change the underlying wire protocol. GuiViewModelBase
// already has real, working machinery for GuiBindingList<T> (MaxSize
// high-water-mark + ListItemVisibility bookkeeping to work around the
// Beamdog/nwn-issues/427 bug, see OnPropertyChanged). NUI's AddList/AddCell
// model binds each cell in the row template to a *separate top-level
// property* (row index is implicit) - so column-per-property is a hard
// framework constraint, not a design choice we're free to change here.
//
// Instead, this collapses the *boilerplate* around that constraint:
//   - Definition side: one AddTable(...) call generates the header row +
//     the AddList/AddCell block + BindRowCount, from column definitions.
//   - ViewModel side: one GuiTableSource<TRow> holds the column mappings
//     and does the parallel-list rebuild in one method, so a window author
//     works with a single list of row DTOs instead of N hand-synced lists.
// ============================================================================

using SWLOR.Game.Server.Core.Beamdog;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Component
{
    // ------------------------------------------------------------------
    // DEFINITION-SIDE: declarative column list -> header row + list template
    // ------------------------------------------------------------------

    public class GuiTableColumn<TViewModel> where TViewModel : IGuiViewModel
    {
        public string Header { get; }
        public float Width { get; }
        public string HeaderTooltip { get; }
        public Expression<Func<TViewModel, GuiBindingList<string>>> ValueExpression { get; }
        public Expression<Func<TViewModel, GuiBindingList<string>>> TooltipExpression { get; }

        public GuiTableColumn(
            string header,
            float width,
            Expression<Func<TViewModel, GuiBindingList<string>>> valueExpression,
            Expression<Func<TViewModel, GuiBindingList<string>>> tooltipExpression,
            string headerTooltip)
        {
            Header = header;
            Width = width;
            ValueExpression = valueExpression;
            TooltipExpression = tooltipExpression;
            HeaderTooltip = headerTooltip;
        }
    }

    public class GuiTableBuilder<TViewModel> where TViewModel : IGuiViewModel
    {
        private readonly List<GuiTableColumn<TViewModel>> _columns = new();
        private float _rowHeight = 24f;

        /// <summary>
        /// Adds a column. The FIRST column added becomes the canonical
        /// row-count source (mirrors current BindRowCount usage, but makes
        /// the choice explicit and singular instead of implicit per-tab).
        /// </summary>
        public GuiTableBuilder<TViewModel> AddColumn(
            string header,
            float width,
            Expression<Func<TViewModel, GuiBindingList<string>>> valueExpression,
            Expression<Func<TViewModel, GuiBindingList<string>>> tooltipExpression = null,
            string headerTooltip = null)
        {
            _columns.Add(new GuiTableColumn<TViewModel>(header, width, valueExpression, tooltipExpression, headerTooltip));
            return this;
        }

        public GuiTableBuilder<TViewModel> SetRowHeight(float height)
        {
            _rowHeight = height;
            return this;
        }

        internal void Build(GuiColumn<TViewModel> col)
        {
            if (_columns.Count == 0)
                throw new InvalidOperationException("GuiTable requires at least one column.");

            col.AddRow(headerRow =>
            {
                for (var i = 0; i < _columns.Count; i++)
                {
                    var column = _columns[i];
                    // Last column gets width 0 (fill remaining space), matching
                    // the existing AddTableHeader convention in CharacterSheetDefinition.
                    var width = i == _columns.Count - 1 ? 0f : column.Width;
                    AddTableHeader(headerRow, column.Header, width, column.HeaderTooltip);
                }
            });

            col.AddRow(row =>
            {
                row.AddList(template =>
                {
                    for (var i = 0; i < _columns.Count; i++)
                    {
                        var column = _columns[i];
                        var isLast = i == _columns.Count - 1;

                        template.AddCell(cell =>
                        {
                            if (!isLast)
                            {
                                cell.SetIsVariable(false);
                                cell.SetWidth(column.Width);
                            }

                            var label = cell.AddLabel()
                                .BindText(column.ValueExpression)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left);

                            if (column.TooltipExpression != null)
                                label.BindTooltip(column.TooltipExpression);
                        });
                    }
                })
                    // First column is the canonical row-count source - explicit
                    // and single, instead of an arbitrary implicit choice per tab.
                    .BindRowCount(_columns[0].ValueExpression)
                    .SetRowHeight(_rowHeight)
                    .SetShowBorders(false)
                    .SetScrollbars(NuiScrollbars.Y);
            });
        }

        private static void AddTableHeader(GuiRow<TViewModel> row, string text, float width, string tooltip)
        {
            var label = row.AddLabel()
                .SetText(text)
                .SetHeight(22f)
                .SetHorizontalAlign(NuiHorizontalAlign.Left);

            if (width > 0f)
                label.SetWidth(width);

            if (!string.IsNullOrWhiteSpace(tooltip))
                label.SetTooltip(tooltip);
        }
    }

    public static class GuiTableExtensions
    {
        /// <summary>
        /// Declares a table: header row + scrollable row-templated list, wired
        /// to the given columns. Replaces the manual AddTableHeader + AddList/
        /// AddCell + BindRowCount block previously duplicated per tab.
        ///
        /// Example (Stats tab, was ~35 lines, becomes):
        ///
        ///   tableCol.AddTable&lt;CharacterSheetViewModel&gt;(t => t
        ///       .AddColumn("STAT", 190f, m => m.StatNames, m => m.StatTooltips, "Character stat.")
        ///       .AddColumn("VALUE", 0f, m => m.StatValues, m => m.StatTooltips, "Current value.")
        ///       .SetRowHeight(24f));
        /// </summary>
        public static void AddTable<TViewModel>(
            this GuiColumn<TViewModel> col,
            Action<GuiTableBuilder<TViewModel>> configure)
            where TViewModel : IGuiViewModel
        {
            var builder = new GuiTableBuilder<TViewModel>();
            configure(builder);
            builder.Build(col);
        }
    }

    // ------------------------------------------------------------------
    // VIEWMODEL-SIDE: one row-DTO list -> N parallel GuiBindingList<string>
    // ------------------------------------------------------------------

    /// <summary>
    /// Owns the column mappings for a table and rebuilds the parallel
    /// GuiBindingList&lt;string&gt; properties from a single sequence of row
    /// DTOs in one call, instead of a hand-rolled loop with N separate
    /// .Add() calls that must stay in lockstep (see RefreshCharacterStatsList,
    /// RefreshResistances, RefreshCraftingStats for the current duplicated
    /// pattern this replaces).
    /// </summary>
    public class GuiTableSource<TViewModel, TRow>
    {
        private readonly List<(Action<TViewModel, GuiBindingList<string>> Setter, Func<TRow, string> Selector)> _columns = new();

        /// <summary>
        /// Registers a bound column: which ViewModel property receives the
        /// values, and how to pull that column's value out of a row DTO.
        /// </summary>
        public GuiTableSource<TViewModel, TRow> Column(
            Action<TViewModel, GuiBindingList<string>> setter,
            Func<TRow, string> valueSelector)
        {
            _columns.Add((setter, valueSelector));
            return this;
        }

        /// <summary>
        /// Rebuilds every bound column from the given rows in one pass.
        /// Guarantees all columns stay the same length - the exact class of
        /// bug the hand-rolled parallel-list pattern doesn't protect against.
        /// </summary>
        public void Refresh(TViewModel viewModel, IEnumerable<TRow> rows)
        {
            var rowList = rows as IList<TRow> ?? rows.ToList();
            var lists = _columns.Select(_ => new GuiBindingList<string>()).ToList();

            foreach (var row in rowList)
            {
                for (var i = 0; i < _columns.Count; i++)
                    lists[i].Add(_columns[i].Selector(row));
            }

            for (var i = 0; i < _columns.Count; i++)
                _columns[i].Setter(viewModel, lists[i]);
        }
    }
}

// ============================================================================
// USAGE EXAMPLE - how CharacterSheetViewModel's stats table would change
// ============================================================================
//
// Row DTO (new):
//
//   public readonly record struct StatEntry(string Name, string Value, string Tooltip);
//
// Static table source, defined once:
//
//   private static readonly GuiTableSource<CharacterSheetViewModel, StatEntry> StatsTable =
//       new GuiTableSource<CharacterSheetViewModel, StatEntry>()
//           .Column((m, v) => m.StatNames = v, r => r.Name)
//           .Column((m, v) => m.StatValues = v, r => r.Value)
//           .Column((m, v) => m.StatTooltips = v, r => r.Tooltip);
//
// RefreshCharacterStatsList becomes:
//
//   private void RefreshCharacterStatsList()
//   {
//       var rows = new List<StatEntry>
//       {
//           new("HP Regen", GetHPRegenValue().ToString(), "Amount of HP restored automatically by natural regeneration."),
//           new("FP Regen", GetFPRegenValue().ToString(), "Amount of FP restored automatically by natural regeneration."),
//           // ... rest of the AddStat(...) calls become record entries here
//       };
//
//       StatsTable.Refresh(this, rows);
//   }
//
// The same GuiTableSource pattern applies directly to RefreshResistances and
// RefreshCraftingStats - each becomes: build a List<TRow>, call .Refresh().
