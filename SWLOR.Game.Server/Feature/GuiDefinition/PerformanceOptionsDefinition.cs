using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PerformanceService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>The Perform window the Options button on the performance hot bar opens.</summary>
    public class PerformanceOptionsDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<PerformanceOptionsViewModel> _builder = new();

        private const float TabRowHeight = 28f;
        private const float TabPanelHeight = 48f;
        private const float ContentPanelWidth = 460f;
        private const float ListRowHeight = 28f;
        // A list row takes its height plus the list's row spacing.
        private const float ListRowPitch = ListRowHeight + 4f;
        private const float NoteWidth = 240f;
        private const float ApplyWidth = 100f;
        private const float PairWidth = (ContentPanelWidth - 28f) / 2f;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.PerformanceOptions)
                .SetInitialGeometry(620f, 20f, 510f, 666f)
                .SetTitle("Perform")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .BindOnClosed(model => model.OnWindowClosed())
                .DefinePartialView(PerformanceOptionsViewModel.DanceTabPartial, AddDanceTab)
                .DefinePartialView(PerformanceOptionsViewModel.InstrumentTabPartial, AddInstrumentTab);

            window.AddStandardLayout(layout =>
            {
                layout.SetTabPanelHeight(TabPanelHeight);
                layout.AddTabRow(row =>
                {
                    row.SetHeight(TabRowHeight);
                    row.AddToggles()
                        .AddOption("Dance")
                        .AddOption("Instrument")
                        .BindSelectedValue(model => model.TabToggleValue)
                        .SetWidth(232f)
                        .SetHeight(TabRowHeight);
                });
                layout.SetContentPartialElement(PerformanceOptionsViewModel.TabContentElement);
            });

            return _builder.Build();
        }

        private static void AddDanceTab(GuiGroup<PerformanceOptionsViewModel> host)
        {
            AddTab(host, content =>
            {
                AddCaption(content, "ROUTINE");
                AddChoiceList(content, model => model.RoutineNames, model => model.RoutineNotes,
                    model => model.RoutineSelected, model => model.RoutineEnabled, model => model.OnSelectRoutine(),
                    "Locked until you unlock the routine's perk.", 5);

                AddCaption(content, "SECOND ROUTINE");
                AddSecondPick(content, model => model.SecondRoutineOptions, model => model.SecondRoutineIndex,
                    model => model.SecondRoutineEnabled, "A second routine needs the Showstopper perk.");

                AddSharedSections(content);
            });
        }

        private static void AddInstrumentTab(GuiGroup<PerformanceOptionsViewModel> host)
        {
            AddTab(host, content =>
            {
                AddCaption(content, "SET");
                AddChoiceList(content, model => model.SetNames, model => model.SetNotes,
                    model => model.SetSelected, model => model.SetEnabled, model => model.OnSelectSet(),
                    "Locked until you unlock the set's perk.", 6);

                // Second set and song share a row so the tab fits a 720 px screen without scrolling.
                content.AddRow(row =>
                {
                    AddPairCaption(row, "SECOND SET");
                    AddPairCaption(row, "SONG");
                });
                content.AddRow(row =>
                {
                    AddPick(row, model => model.SecondSetOptions, model => model.SecondSetIndex,
                        model => model.SecondSetEnabled, "A second set needs the Virtuoso perk.").SetWidth(PairWidth);
                    AddPick(row, model => model.SongOptions, model => model.SongIndex,
                        model => model.SongEnabled, "Collect a Cantina song to choose one.").SetWidth(PairWidth);
                });

                AddSharedSections(content);
            });
        }

        // Each tab is a fixed-width borderless panel; scrolling comes from the standard layout's content host (rule R5).
        private static void AddTab(GuiGroup<PerformanceOptionsViewModel> host, Action<GuiColumn<PerformanceOptionsViewModel>> body)
        {
            host.AddColumn(col => col.AddRow(row => row.AddGroup(panel =>
            {
                panel.SetShowBorder(false);
                panel.SetScrollbars(NuiScrollbars.None);
                panel.AddColumn(body);
            }).SetWidth(ContentPanelWidth)));
        }

        private static void AddPairCaption(GuiRow<PerformanceOptionsViewModel> row, string text)
        {
            row.AddLabel()
                .SetText(text)
                .SetColor(PerformanceWindowColors.Dim)
                .SetWidth(PairWidth)
                .SetHeight(22f)
                .SetHorizontalAlign(NuiHorizontalAlign.Left);
        }

        private static void AddCaption(GuiColumn<PerformanceOptionsViewModel> content, string text)
        {
            content.AddRow(row => row.AddLabel()
                .SetText(text)
                .SetColor(PerformanceWindowColors.Dim)
                .SetHeight(22f)
                .SetHorizontalAlign(NuiHorizontalAlign.Left));
        }

        // One row per routine or set: a toggle button with its name and a note on what it gives or what unlocks it.
        private static void AddChoiceList(
            GuiColumn<PerformanceOptionsViewModel> content,
            Expression<Func<PerformanceOptionsViewModel, GuiBindingList<string>>> names,
            Expression<Func<PerformanceOptionsViewModel, GuiBindingList<string>>> notes,
            Expression<Func<PerformanceOptionsViewModel, GuiBindingList<bool>>> selected,
            Expression<Func<PerformanceOptionsViewModel, GuiBindingList<bool>>> enabled,
            Expression<Func<PerformanceOptionsViewModel, Action>> onSelect,
            string lockedTooltip,
            int rowCount)
        {
            content.AddRow(row => row.AddList(template =>
                {
                    template.AddCell(cell => cell.AddToggleButton()
                        .BindText(names)
                        .BindIsToggled(selected)
                        .BindIsEnabled(enabled)
                        .SetDisabledTooltip(lockedTooltip)
                        .BindOnClicked(onSelect));

                    template.AddCell(cell =>
                    {
                        cell.SetIsVariable(false);
                        cell.SetWidth(NoteWidth);
                        cell.AddLabel()
                            .BindText(notes)
                            .SetColor(PerformanceWindowColors.Dim)
                            .SetHorizontalAlign(NuiHorizontalAlign.Right);
                    });
                })
                .BindRowCount(names)
                .SetRowHeight(ListRowHeight)
                .SetScrollbars(NuiScrollbars.None)
                .SetHeight(ListRowPitch * rowCount + 6f));
        }

        // A combo box that stays disabled, showing what is needed, until the player has the perk (or a song) it needs.
        private static void AddSecondPick(
            GuiColumn<PerformanceOptionsViewModel> content,
            Expression<Func<PerformanceOptionsViewModel, GuiBindingList<GuiComboEntry>>> options,
            Expression<Func<PerformanceOptionsViewModel, int>> selectedIndex,
            Expression<Func<PerformanceOptionsViewModel, bool>> enabled,
            string disabledTooltip)
        {
            // No row height: a row holding a combo box derives its height from the combo (layout rule R2c).
            content.AddRow(row => AddPick(row, options, selectedIndex, enabled, disabledTooltip));
        }

        private static GuiComboBox<PerformanceOptionsViewModel> AddPick(
            GuiRow<PerformanceOptionsViewModel> row,
            Expression<Func<PerformanceOptionsViewModel, GuiBindingList<GuiComboEntry>>> options,
            Expression<Func<PerformanceOptionsViewModel, int>> selectedIndex,
            Expression<Func<PerformanceOptionsViewModel, bool>> enabled,
            string disabledTooltip)
        {
            return row.AddComboBox()
                .BindOptions(options)
                .BindSelectedIndex(selectedIndex)
                .BindIsEnabled(enabled)
                .SetDisabledTooltip(disabledTooltip)
                .SetHeight(32f);
        }

        private static void AddSharedSections(GuiColumn<PerformanceOptionsViewModel> content)
        {
            // The note is a tooltip rather than a row, so the Instrument tab fits a 720 px screen.
            AddCaption(content, "TEMPO");
            content.AddRow(row => row.AddOptions()
                .SetDirection(NuiDirection.Horizontal)
                .AddOption(PerformanceTempo.Relaxed.GetLabel())
                .AddOption(PerformanceTempo.Lively.GetLabel())
                .AddOption(PerformanceTempo.Upbeat.GetLabel())
                .BindSelectedValue(model => model.TempoValue)
                .SetTooltip("Faster tempo: more Performance XP for you. Same reward for your audience.")
                .SetWidth(ContentPanelWidth - 20f)
                .SetHeight(30f));

            content.AddRow(row => row.AddLabel()
                .BindText(model => model.VenueHeaderText)
                .SetColor(PerformanceWindowColors.Dim)
                .SetHeight(22f)
                .SetHorizontalAlign(NuiHorizontalAlign.Left));
            content.AddRow(row => row.AddLabel()
                .BindText(model => model.VenueAreaText)
                .SetHeight(22f)
                .SetHorizontalAlign(NuiHorizontalAlign.Left));
            content.AddRow(row => row.AddLabel()
                .BindText(model => model.VenueFloorText)
                .SetHeight(22f)
                .SetHorizontalAlign(NuiHorizontalAlign.Left));

            // The audience line is kept short enough to sit beside the button.
            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.AudienceText)
                    .SetHeight(32f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .SetVerticalAlign(NuiVerticalAlign.Middle);
                row.AddButton()
                    .SetText("Apply")
                    .SetWidth(ApplyWidth)
                    .SetHeight(32f)
                    .BindOnClicked(model => model.OnApply());
            });
        }
    }
}
