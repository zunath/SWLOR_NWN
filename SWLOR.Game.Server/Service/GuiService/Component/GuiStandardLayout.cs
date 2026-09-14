// ============================================================================
// GuiStandardLayout.cs
//
// PROBLEM THIS SOLVES
// --------------------
// Only ONE root layout shape has been proven to correctly track window
// geometry as the client resizes a window: a single root column containing
// ONE row (the "main row"), whose first column is variable-width content
// (an optional fixed-height borderless Auto-scroll group hosting tab-bar
// rows, followed by a borderless Auto-scroll group hosting the swappable
// tab-content partial) and whose remaining columns are fixed-width side
// rails. CharacterSheetDefinition.cs's root block is the origin of this
// shape.
//
// Deviating from it - most notably, putting the tab-bar rows as SIBLING
// ROWS of the body row directly in the root column instead of nesting both
// under one shared row - froze the content region at a constant width
// regardless of window resizing. That mistake cost two failed iterations of
// a debug gallery window before the shape above was confirmed correct by
// direct comparison against CharacterSheet. See Readmes/NuiLayoutRules.md
// (rule R5) for the layout-rule writeup.
//
// This helper exists so a new window author can declare "these are my tab
// rows, this is my content partial, these are my side columns" and get the
// proven-correct nesting automatically, instead of hand-rolling the root
// block and risking that same regression again.
// ============================================================================

using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    /// <summary>
    /// Fluent configuration surface for <see cref="GuiStandardLayout.AddStandardLayout{T}"/>.
    /// Collects tab rows, the swappable content partial, and side columns,
    /// then hands them to the helper to emit in the proven-correct nesting.
    /// </summary>
    public class GuiStandardLayoutConfig<T> where T : IGuiViewModel
    {
        private readonly List<Action<GuiRow<T>>> _tabRows = new();
        private readonly List<(Action<GuiColumn<T>> BuildColumn, float? FixedWidth)> _leadingColumns = new();
        private readonly List<(Action<GuiColumn<T>> BuildColumn, float? FixedWidth)> _sideColumns = new();

        internal float? TabPanelHeightValue { get; private set; }
        internal string ContentElementId { get; private set; }
        internal IReadOnlyList<Action<GuiRow<T>>> TabRows => _tabRows;
        internal IReadOnlyList<(Action<GuiColumn<T>> BuildColumn, float? FixedWidth)> LeadingColumns => _leadingColumns;
        internal IReadOnlyList<(Action<GuiColumn<T>> BuildColumn, float? FixedWidth)> SideColumns => _sideColumns;

        /// <summary>
        /// Sets the fixed height of the tab-bar panel (the group that hosts
        /// every row added via <see cref="AddTabRow"/>). Required if any tab
        /// rows are added.
        /// </summary>
        public GuiStandardLayoutConfig<T> SetTabPanelHeight(float height)
        {
            TabPanelHeightValue = height;
            return this;
        }

        /// <summary>
        /// Adds one row to the tab-bar panel. The row-building action is
        /// passed through untouched - the caller is responsible for its own
        /// SetHeight/AddToggles/etc. calls, exactly as if building a row
        /// directly.
        /// </summary>
        public GuiStandardLayoutConfig<T> AddTabRow(Action<GuiRow<T>> buildRow)
        {
            _tabRows.Add(buildRow);
            return this;
        }

        /// <summary>
        /// Sets the partial-view element id hosted by the content region.
        /// Required.
        /// </summary>
        public GuiStandardLayoutConfig<T> SetContentPartialElement(string elementId)
        {
            ContentElementId = elementId;
            return this;
        }

        /// <summary>
        /// Adds a rail before the variable-width content column.
        /// </summary>
        public GuiStandardLayoutConfig<T> AddLeadingColumn(Action<GuiColumn<T>> buildColumn, float? fixedWidth = null)
        {
            _leadingColumns.Add((buildColumn, fixedWidth));
            return this;
        }

        /// <summary>
        /// Adds a side column to the main row, after the content column.
        /// Side columns are emitted in the order they are added. Pass
        /// <paramref name="fixedWidth"/> to pin the column's width, matching
        /// the fixed-width rail columns (e.g. an event log) used alongside
        /// the variable-width content column.
        /// </summary>
        public GuiStandardLayoutConfig<T> AddSideColumn(Action<GuiColumn<T>> buildColumn, float? fixedWidth = null)
        {
            _sideColumns.Add((buildColumn, fixedWidth));
            return this;
        }
    }

    /// <summary>
    /// Emits the only root layout shape proven to track window geometry
    /// correctly: root column -> single row -> [variable-width content
    /// column | fixed-width side column(s)]. The content column holds an
    /// optional fixed-height borderless Auto-scroll group for tab-bar rows,
    /// then a borderless Auto-scroll group hosting the swappable tab-content
    /// partial. Deviating from this shape - e.g. putting tab rows as SIBLING
    /// ROWS of the body row in the root column - freezes the content region
    /// at a constant width regardless of window resizing (see
    /// Readmes/NuiLayoutRules.md rule R5; CharacterSheet is the origin of
    /// this shape).
    /// </summary>
    public static class GuiStandardLayout
    {
        /// <summary>
        /// Normalizes every window's authored root elements into the shared root shape
        /// before the main partial is serialized. GuiWindowBuilder applies this to the
        /// complete window corpus; specialized layouts still compose their content with
        /// <see cref="AddStandardLayout{T}"/> first.
        /// </summary>
        internal static GuiWindow<T> DefineStandardMainPartial<T>(
            this GuiWindow<T> window,
            IReadOnlyList<IGuiWidget> authoredElements)
            where T : IGuiViewModel
        {
            return window.DefinePartialView("%%WINDOW_MAIN%%", group =>
            {
                group.AddColumn(root =>
                {
                    root.AddRow(mainRow =>
                    {
                        mainRow.Elements.AddRange(authoredElements);
                    });
                });
            });
        }

        public static GuiWindow<T> AddStandardLayout<T>(this GuiWindow<T> window, Action<GuiStandardLayoutConfig<T>> configure)
            where T : IGuiViewModel
        {
            var config = new GuiStandardLayoutConfig<T>();
            configure(config);

            if (string.IsNullOrEmpty(config.ContentElementId))
            {
                throw new InvalidOperationException(
                    "GuiStandardLayout requires SetContentPartialElement to be called.");
            }

            if (config.TabRows.Count > 0 && !config.TabPanelHeightValue.HasValue)
            {
                throw new InvalidOperationException(
                    "GuiStandardLayout requires SetTabPanelHeight to be called when one or more AddTabRow rows are configured.");
            }

            window.AddColumn(root =>
            {
                root.AddRow(mainRow =>
                {
                    AddColumns(mainRow, config.LeadingColumns);

                    mainRow.AddColumn(contentCol =>
                    {
                        if (config.TabRows.Count > 0)
                        {
                            contentCol.AddRow(tabPanelRow =>
                            {
                                tabPanelRow.AddGroup(tabPanel =>
                                {
                                    tabPanel.SetShowBorder(false);
                                    tabPanel.SetScrollbars(NuiScrollbars.Auto);
                                    tabPanel.AddColumn(tabCol =>
                                    {
                                        foreach (var buildRow in config.TabRows)
                                        {
                                            tabCol.AddRow(buildRow);
                                        }
                                    });
                                })
                                    .SetHeight(config.TabPanelHeightValue.Value);
                            });
                        }

                        contentCol.AddRow(contentRow =>
                        {
                            contentRow.AddGroup(host =>
                            {
                                host.SetShowBorder(false);
                                host.SetScrollbars(NuiScrollbars.Auto);
                                host.AddColumn(hostCol =>
                                {
                                    hostCol.AddRow(hostRow =>
                                    {
                                        hostRow.AddPartialView(config.ContentElementId);
                                    });
                                });
                            });
                        });
                    });

                    AddColumns(mainRow, config.SideColumns);
                });
            });

            return window;
        }

        private static void AddColumns<T>(
            GuiRow<T> row,
            IReadOnlyList<(Action<GuiColumn<T>> BuildColumn, float? FixedWidth)> columns)
            where T : IGuiViewModel
        {
            foreach (var configuredColumn in columns)
            {
                var column = row.AddColumn(configuredColumn.BuildColumn);
                if (configuredColumn.FixedWidth.HasValue)
                    column.SetWidth(configuredColumn.FixedWidth.Value);
            }
        }
    }
}
