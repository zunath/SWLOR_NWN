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
//
// COMPONENT COLUMNS
// ------------------
// A column doesn't have to be a bound text label. AddComponentColumn takes a
// cell-builder callback instead of a bound expression, so a column can host
// a button, toggle, image, or any other GuiExpandableComponent widget - the
// same thing MarketBuyDefinition/AchievementsDefinition already do by hand
// with raw AddList/AddCell blocks (row identity for a per-row click handler
// is recovered via the engine-native NuiGetEventArrayIndex(), same as those).
// A table's column list is no longer required to contain a text column at
// all: SetShowHeader(false) drops the header row (some lists, like
// MarketBuy's item list, never had one), and BindRowCount(...) lets the
// caller name an explicit row-count source instead of relying on "the first
// column happens to have a bound text expression". A column can also opt
// into being width-flexible via isVariable regardless of position - by
// default only the last column is flexible, matching the old behavior.
// ============================================================================

using SWLOR.Game.Server.Core.Beamdog;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Service.GuiService.Component
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

        /// <summary>
        /// When set, the cell is built by this callback instead of the default bound
        /// label (see AddComponentColumn). Null for ordinary text columns.
        /// </summary>
        public Action<GuiTemplateCell<TViewModel>> CellBuilder { get; }

        /// <summary>
        /// Null means "auto": variable only if this is the last column, matching the
        /// original behavior. Non-null lets a column opt in/out explicitly, since more
        /// than one column in a row may need to flex (see MarketBuy's item list).
        /// </summary>
        public bool? IsVariable { get; }

        public GuiTableColumn(
            string header,
            float width,
            Expression<Func<TViewModel, GuiBindingList<string>>> valueExpression,
            Expression<Func<TViewModel, GuiBindingList<string>>> tooltipExpression,
            string headerTooltip,
            Action<GuiTemplateCell<TViewModel>> cellBuilder = null,
            bool? isVariable = null)
        {
            Header = header;
            Width = width;
            ValueExpression = valueExpression;
            TooltipExpression = tooltipExpression;
            HeaderTooltip = headerTooltip;
            CellBuilder = cellBuilder;
            IsVariable = isVariable;
        }
    }

    public class GuiTableBuilder<TViewModel> where TViewModel : IGuiViewModel
    {
        private readonly List<GuiTableColumn<TViewModel>> _columns = new();
        private float _rowHeight = 24f;
        private bool _showHeader = true;
        private float? _padding;
        private Expression<Func<TViewModel, GuiBindingList<string>>> _rowCountExpression;

        /// <summary>
        /// Adds a bound text column. Unless BindRowCount is used, the row-count
        /// source is the first column (in declared order) with a value expression.
        /// </summary>
        public GuiTableBuilder<TViewModel> AddColumn(
            string header,
            float width,
            Expression<Func<TViewModel, GuiBindingList<string>>> valueExpression,
            Expression<Func<TViewModel, GuiBindingList<string>>> tooltipExpression = null,
            string headerTooltip = null,
            bool? isVariable = null)
        {
            _columns.Add(new GuiTableColumn<TViewModel>(header, width, valueExpression, tooltipExpression, headerTooltip, null, isVariable));
            return this;
        }

        /// <summary>
        /// Adds a text column with the player-facing header tooltip placed next
        /// to the header declaration. This overload keeps table definitions easy
        /// to scan when cells do not require their own tooltip binding.
        /// </summary>
        public GuiTableBuilder<TViewModel> AddColumn(
            string header,
            float width,
            string headerTooltip,
            Expression<Func<TViewModel, GuiBindingList<string>>> valueExpression,
            Expression<Func<TViewModel, GuiBindingList<string>>> tooltipExpression = null,
            bool? isVariable = null)
        {
            return AddColumn(header, width, valueExpression, tooltipExpression, headerTooltip, isVariable);
        }

        /// <summary>
        /// Adds a column whose cell is built by the given callback instead of a bound
        /// label - e.g. a button, toggle button, image, or any other
        /// GuiExpandableComponent widget. Row identity for a per-row click handler is
        /// recovered the same way any hand-rolled AddList/AddCell button already does:
        /// via NuiGetEventArrayIndex() inside the bound method.
        /// </summary>
        public GuiTableBuilder<TViewModel> AddComponentColumn(
            string header,
            float width,
            Action<GuiTemplateCell<TViewModel>> cellBuilder,
            string headerTooltip = null,
            bool? isVariable = null)
        {
            _columns.Add(new GuiTableColumn<TViewModel>(header, width, null, null, headerTooltip, cellBuilder, isVariable));
            return this;
        }

        public GuiTableBuilder<TViewModel> SetRowHeight(float height)
        {
            _rowHeight = height;
            return this;
        }

        /// <summary>
        /// Shows or hides the header row. Defaults to true. Some lists (e.g.
        /// MarketBuy's item list) never had a header row at all.
        /// </summary>
        public GuiTableBuilder<TViewModel> SetShowHeader(bool showHeader)
        {
            _showHeader = showHeader;
            return this;
        }

        public GuiTableBuilder<TViewModel> SetPadding(float padding)
        {
            _padding = padding;
            return this;
        }

        /// <summary>
        /// Explicitly names the row-count source instead of relying on "the first
        /// column with a bound text expression" - required when no text column
        /// exists, and recommended whenever the natural row-count column isn't the
        /// first one declared, so a later column reorder can't silently change it.
        /// </summary>
        public GuiTableBuilder<TViewModel> BindRowCount(Expression<Func<TViewModel, GuiBindingList<string>>> expression)
        {
            _rowCountExpression = expression;
            return this;
        }

        internal void Build(GuiColumn<TViewModel> col)
        {
            if (_columns.Count == 0)
                throw new InvalidOperationException("GuiTable requires at least one column.");

            // A column that resolves to fixed-width (not variable) with no positive width
            // produces a template cell the NUI solver can neither size nor grow. Fail at
            // build time with the column named rather than as a client-side draw error.
            for (var i = 0; i < _columns.Count; i++)
            {
                var column = _columns[i];
                var isVariable = column.IsVariable ?? i == _columns.Count - 1;

                if (!isVariable && column.Width <= 0f)
                {
                    var headerLabel = string.IsNullOrWhiteSpace(column.Header) ? $"index {i}" : $"'{column.Header}'";
                    throw new InvalidOperationException(
                        $"GuiTable column {headerLabel} resolves to a fixed width of {column.Width}. " +
                        "Fixed columns must declare a positive width; pass isVariable: true if the column should flex. " +
                        "Note that only the last column defaults to variable.");
                }
            }

            if (_showHeader)
            {
                col.AddRow(headerRow =>
                {
                    for (var i = 0; i < _columns.Count; i++)
                    {
                        var column = _columns[i];
                        var variable = column.IsVariable ?? i == _columns.Count - 1;
                        // Variable columns get width 0 (fill remaining space), matching
                        // the existing AddTableHeader convention in CharacterSheetDefinition.
                        var width = variable ? 0f : column.Width;
                        AddTableHeader(headerRow, column.Header, width, column.HeaderTooltip);
                    }
                });
            }

            var rowCountExpression = _rowCountExpression
                ?? _columns.Select(c => c.ValueExpression).FirstOrDefault(e => e != null)
                ?? throw new InvalidOperationException("GuiTable requires a row-count source: either a text column (AddColumn) or an explicit BindRowCount(...) call.");

            col.AddRow(row =>
            {
                row.AddList(template =>
                {
                    for (var i = 0; i < _columns.Count; i++)
                    {
                        var column = _columns[i];
                        var variable = column.IsVariable ?? i == _columns.Count - 1;

                        template.AddCell(cell =>
                        {
                            if (!variable)
                            {
                                cell.SetIsVariable(false);
                                cell.SetWidth(column.Width);
                            }

                            if (column.CellBuilder != null)
                            {
                                column.CellBuilder(cell);
                            }
                            else
                            {
                                var label = cell.AddLabel()
                                    .BindText(column.ValueExpression)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left);

                                if (column.TooltipExpression != null)
                                    label.BindTooltip(column.TooltipExpression);
                            }
                        });
                    }

                    if (_padding.HasValue)
                        template.SetPadding(_padding.Value);
                })
                    .BindRowCount(rowCountExpression)
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
    /// Owns the column mappings for a table and rebuilds the bound
    /// GuiBindingList&lt;TValue&gt; properties from a single sequence of row
    /// DTOs in one call, instead of a hand-rolled loop with N separate
    /// .Add() calls that must stay in lockstep (see RefreshCharacterStatsList,
    /// RefreshResistances, RefreshCraftingStats for the current duplicated
    /// pattern this replaces). Columns aren't limited to string: any bound
    /// value type (bool for a per-row enabled flag, etc.) is supported.
    /// </summary>
    public class GuiTableSource<TViewModel, TRow>
    {
        private sealed class ColumnBinding
        {
            public Action<TViewModel, IEnumerable<TRow>> Refresh;

            /// <summary>Null if this column's Column(...) call didn't supply a getter - see RemoveRowAt.</summary>
            public Action<TViewModel, int> Remove;
            public Func<TViewModel, int, bool> CanRemove;
        }

        private readonly List<ColumnBinding> _columns = new();

        /// <summary>
        /// Registers a bound column: which ViewModel property receives the
        /// values, and how to pull that column's value out of a row DTO.
        /// The optional getter lets RemoveRowAt find this column's current
        /// live bound list to remove a single row from it directly (a
        /// GuiBindingList&lt;T&gt; mutation propagates to the client without
        /// reassigning the property) - only needed if RemoveRowAt will be used.
        /// </summary>
        public GuiTableSource<TViewModel, TRow> Column<TValue>(
            Action<TViewModel, GuiBindingList<TValue>> setter,
            Func<TRow, TValue> valueSelector,
            Func<TViewModel, GuiBindingList<TValue>> getter = null)
        {
            _columns.Add(new ColumnBinding
            {
                Refresh = (viewModel, rows) =>
                {
                    var list = new GuiBindingList<TValue>();
                    foreach (var row in rows)
                        list.Add(valueSelector(row));
                    setter(viewModel, list);
                },
                Remove = getter == null ? null : (viewModel, index) => getter(viewModel).RemoveAt(index),
                CanRemove = getter == null
                    ? null
                    : (viewModel, index) =>
                    {
                        var values = getter(viewModel);
                        return values != null && index >= 0 && index < values.Count;
                    }
            });
            return this;
        }

        /// <summary>
        /// Rebuilds every bound column from the given rows in one pass.
        /// Guarantees all columns stay the same length - the exact class of
        /// bug the hand-rolled parallel-list pattern doesn't protect against.
        /// Returns the row list that was refreshed from, so the caller can
        /// hold onto it (e.g. for NuiGetEventArrayIndex() row lookups) the
        /// same way it would hold any other per-instance state.
        /// </summary>
        public IList<TRow> Refresh(TViewModel viewModel, IEnumerable<TRow> rows)
        {
            var rowList = rows as IList<TRow> ?? rows.ToList();

            foreach (var column in _columns)
                column.Refresh(viewModel, rowList);

            return rowList;
        }

        /// <summary>
        /// Removes a single row: from the given row list, and from every
        /// registered column's bound list. Throws if any column was
        /// registered without a getter, since that column's bound list would
        /// otherwise silently drift out of length-sync with the rest.
        /// </summary>
        public void RemoveRowAt(TViewModel viewModel, IList<TRow> rows, int index)
        {
            if (index < 0 || index >= rows.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            // Validate every bound list before any mutation.
            for (var i = 0; i < _columns.Count; i++)
            {
                if (_columns[i].Remove == null)
                    throw new InvalidOperationException($"Column {i} has no getter registered; cannot RemoveRowAt without leaving it out of sync. Pass a getter to Column(...) for every bound column before calling RemoveRowAt.");

                if (!_columns[i].CanRemove(viewModel, index))
                    throw new InvalidOperationException($"Column {i} does not contain row {index}; cannot RemoveRowAt without leaving the table out of sync. Refresh all bound columns before removing a row.");
            }

            rows.RemoveAt(index);
            for (var i = 0; i < _columns.Count; i++)
            {
                var column = _columns[i];
                column.Remove(viewModel, index);
            }
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
