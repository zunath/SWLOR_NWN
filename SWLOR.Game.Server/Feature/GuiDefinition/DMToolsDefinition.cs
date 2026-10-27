using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class DMToolsDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<DMToolsViewModel> _builder = new();

        private const float TabRowHeight = 28f;
        private const float TabPanelHeight = 48f;
        private const float ContentPanelWidth = 530f;
        private const float SpaceLabelWidth = 90f;
        private const float SpaceFieldWidth = 300f;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.DMTools)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 575f, 600f)
                .SetTitle("DM Tools")
                .BindOnClosed(model => model.OnCloseWindow())
                .DefinePartialView(DMToolsViewModel.PlaceablesTabPartial, AddPlaceablesTab)
                .DefinePartialView(DMToolsViewModel.SpaceTabPartial, AddSpaceTab);

            window.AddStandardLayout(layout =>
            {
                layout.SetTabPanelHeight(TabPanelHeight);
                layout.AddTabRow(row =>
                {
                    row.SetHeight(TabRowHeight);
                    row.AddToggles()
                        .AddOption("Placeables")
                        .AddOption("Space")
                        .BindSelectedValue(model => model.TabToggleValue)
                        .SetWidth(232f)
                        .SetHeight(TabRowHeight);
                });
                layout.SetContentPartialElement(DMToolsViewModel.TabContentElement);
            });

            return _builder.Build();
        }

        private static void AddPanel(GuiGroup<DMToolsViewModel> host, System.Action<GuiColumn<DMToolsViewModel>> content)
        {
            host.AddColumn(col => col.AddRow(row => row.AddGroup(panel =>
            {
                panel.SetShowBorder(false);
                panel.SetScrollbars(NuiScrollbars.None);
                panel.AddColumn(content);
            }).SetWidth(ContentPanelWidth)));
        }

        private static void AddPlaceablesTab(GuiGroup<DMToolsViewModel> host)
        {
            AddPanel(host, content =>
            {
                content.AddRow(row =>
                {
                    row.AddColumn(col =>
                    {
                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .BindText(model => model.Instructions)
                                .BindColor(model => model.InstructionColor)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetVerticalAlign(NuiVerticalAlign.Middle)
                                .SetHeight(20f);
                        });

                        col.AddRow(row =>
                        {
                            row.AddList(template =>
                            {
                                template.AddCell(cell =>
                                {
                                    cell.AddToggleButton()
                                        .BindText(model => model.PlaceableNames)
                                        .BindOnClicked(model => model.OnSelectPlaceable())
                                        .BindIsToggled(model => model.PlaceableToggles);
                                });
                            })
                            .BindRowCount(model => model.PlaceableNames);

                            row.SetHeight(300f);
                        });

                        col.AddPagination(
                            model => model.PageNumbers,
                            model => model.SelectedPageIndex,
                            model => model.OnPreviousPage(),
                            model => model.OnNextPage());

                        col.AddRow(row =>
                        {
                            row.AddButton()
                                .SetText("Refresh")
                                .BindOnClicked(model => model.OnRefreshPlaceables())
                                .SetHeight(35f);

                            row.AddButton()
                                .SetText("Save Changes")
                                .BindOnClicked(model => model.OnSaveChanges())
                                .BindIsEnabled(model => model.IsPlaceableSelected)
                                .SetHeight(35f);

                            row.AddButton()
                                .SetText("Delete Placeable")
                                .BindOnClicked(model => model.OnDeletePlaceable())
                                .BindIsEnabled(model => model.IsPlaceableSelected)
                                .SetHeight(35f);
                        });

                        col.AddRow(row =>
                        {
                            row.AddButton()
                                .SetText("Battle Grid")
                                .SetTooltip("Project a sizable grid over this area for battles")
                                .BindOnClicked(model => model.OnOpenBattleGrid())
                                .SetHeight(35f);
                        });
                    });

                    row.AddColumn(col =>
                    {
                        col.AddRow(row =>
                        {
                            row.AddTextEdit()
                                .BindValue(model => model.SearchText)
                                .SetPlaceholder("Search placeables...");
                        });

                        col.AddRow(row =>
                        {
                            row.AddButton()
                                .SetText("Search")
                                .BindOnClicked(model => model.OnSearch())
                                .SetHeight(35f);
                            row.AddButton()
                                .SetText("Clear")
                                .BindOnClicked(model => model.OnClearSearch())
                                .SetHeight(35f);
                        });

                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .SetText("Position")
                                .SetHeight(20f);
                        });

                        col.AddRow(row =>
                        {
                            row.AddSpacer();

                            row.AddButtonImage()
                                .SetImageResref("arrow_up")
                                .SetTooltip("Y-Axis +")
                                .BindOnClicked(model => model.OnYAxisUp())
                                .SetWidth(32f)
                                .SetHeight(32f)
                                .BindIsEnabled(model => model.IsPlaceableSelected);

                            row.AddButtonImage()
                                .SetImageResref("arrow_down")
                                .SetTooltip("Y-Axis -")
                                .BindOnClicked(model => model.OnYAxisDown())
                                .SetWidth(32f)
                                .SetHeight(32f)
                                .BindIsEnabled(model => model.IsPlaceableSelected);

                            row.AddButtonImage()
                                .SetImageResref("arrow_right")
                                .SetTooltip("X-Axis +")
                                .BindOnClicked(model => model.OnXAxisUp())
                                .SetWidth(32f)
                                .SetHeight(32f)
                                .BindIsEnabled(model => model.IsPlaceableSelected);

                            row.AddButtonImage()
                                .SetImageResref("arrow_left")
                                .SetTooltip("X-Axis -")
                                .BindOnClicked(model => model.OnXAxisDown())
                                .SetWidth(32f)
                                .SetHeight(32f)
                                .BindIsEnabled(model => model.IsPlaceableSelected);

                            row.AddButtonImage()
                                .SetImageResref("arrow_zup")
                                .SetTooltip("Z-Axis +")
                                .BindOnClicked(model => model.OnZAxisUp())
                                .SetWidth(32f)
                                .SetHeight(32f)
                                .BindIsEnabled(model => model.IsPlaceableSelected);

                            row.AddButtonImage()
                                .SetImageResref("arrow_zdown")
                                .SetTooltip("Z-Axis -")
                                .BindOnClicked(model => model.OnZAxisDown())
                                .SetWidth(32f)
                                .SetHeight(32f)
                                .BindIsEnabled(model => model.IsPlaceableSelected);

                            row.AddButtonImage()
                                .SetImageResref("level")
                                .SetTooltip("Z-Axis Reset")
                                .BindOnClicked(model => model.OnZAxisReset())
                                .SetWidth(32f)
                                .SetHeight(32f)
                                .BindIsEnabled(model => model.IsPlaceableSelected);

                            row.AddSpacer();
                        });

                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .SetText("Facing")
                                .SetHeight(20f);
                        });

                        col.AddRow(row =>
                        {
                            row.AddSpacer();

                            row.AddButtonImage()
                                .SetImageResref("clockwise")
                                .SetTooltip("Clockwise")
                                .BindOnClicked(model => model.OnRotateClockwise())
                                .SetWidth(32f)
                                .SetHeight(32f)
                                .BindIsEnabled(model => model.IsPlaceableSelected);

                            row.AddButtonImage()
                                .SetImageResref("counterclockwise")
                                .SetTooltip("Counter-Clockwise")
                                .BindOnClicked(model => model.OnRotateCounterClockwise())
                                .SetWidth(32f)
                                .SetHeight(32f)
                                .BindIsEnabled(model => model.IsPlaceableSelected);

                            row.AddButtonImage()
                                .SetImageResref("north")
                                .SetTooltip("North")
                                .BindOnClicked(model => model.OnSetFacingNorth())
                                .SetWidth(32f)
                                .SetHeight(32f)
                                .BindIsEnabled(model => model.IsPlaceableSelected);

                            row.AddButtonImage()
                                .SetImageResref("south")
                                .SetTooltip("South")
                                .BindOnClicked(model => model.OnSetFacingSouth())
                                .SetWidth(32f)
                                .SetHeight(32f)
                                .BindIsEnabled(model => model.IsPlaceableSelected);

                            row.AddButtonImage()
                                .SetImageResref("east")
                                .SetTooltip("East")
                                .BindOnClicked(model => model.OnSetFacingEast())
                                .SetWidth(32f)
                                .SetHeight(32f)
                                .BindIsEnabled(model => model.IsPlaceableSelected);

                            row.AddButtonImage()
                                .SetImageResref("west")
                                .SetTooltip("West")
                                .BindOnClicked(model => model.OnSetFacingWest())
                                .SetWidth(32f)
                                .SetHeight(32f)
                                .BindIsEnabled(model => model.IsPlaceableSelected);

                            row.AddSpacer();
                        });

                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .SetText("Layout Presets (This Map)")
                                .SetHeight(20f);
                        });

                        col.AddRow(row =>
                        {
                            row.AddTextEdit()
                                .BindValue(model => model.LayoutName)
                                .SetPlaceholder("Layout Name")
                                .SetMaxLength(32);
                        });

                        col.AddRow(row =>
                        {
                            row.AddList(template =>
                            {
                                template.AddCell(cell =>
                                {
                                    cell.AddToggleButton()
                                        .BindText(model => model.LayoutNames)
                                        .BindOnClicked(model => model.OnSelectLayout())
                                        .BindIsToggled(model => model.LayoutToggles);
                                });
                            })
                            .BindRowCount(model => model.LayoutNames);

                            row.SetHeight(110f);
                        });

                        col.AddRow(row =>
                        {
                            row.AddButton()
                                .SetText("Save Layout")
                                .BindOnClicked(model => model.OnSaveLayout())
                                .SetHeight(35f);
                            row.AddButton()
                                .SetText("Load Layout")
                                .BindOnClicked(model => model.OnLoadLayout())
                                .BindIsEnabled(model => model.IsLayoutSelected)
                                .SetHeight(35f);
                            row.AddButton()
                                .SetText("Delete")
                                .BindOnClicked(model => model.OnDeleteLayout())
                                .BindIsEnabled(model => model.IsLayoutSelected)
                                .SetHeight(35f);
                        });
                    });
                });
            });
        }

        /// <summary>
        /// Spawns an enemy squad of one faction tier and role near the DM, or a trade convoy of that tier. Incursions
        /// and derelicts come with their own systems (plan sections 28 and 30) and are shown disabled until then.
        /// </summary>
        private static void AddSpaceTab(GuiGroup<DMToolsViewModel> host)
        {
            AddPanel(host, content =>
            {
                content.AddRow(row =>
                {
                    row.AddLabel()
                        .SetText("Spawn an enemy squad around you. You must be in a space area.")
                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                        .SetHeight(20f);
                });

                content.AddRow(row =>
                {
                    row.AddLabel()
                        .SetText("Faction")
                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                        .SetWidth(SpaceLabelWidth);
                    row.AddComboBox()
                        .BindOptions(model => model.SpaceFactionOptions)
                        .BindSelectedIndex(model => model.SpaceFactionIndex)
                        .SetWidth(SpaceFieldWidth);
                });

                content.AddRow(row =>
                {
                    row.AddLabel()
                        .SetText("Role")
                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                        .SetWidth(SpaceLabelWidth);
                    row.AddComboBox()
                        .BindOptions(model => model.SpaceRoleOptions)
                        .BindSelectedIndex(model => model.SpaceRoleIndex)
                        .SetWidth(SpaceFieldWidth);
                });

                content.AddRow(row =>
                {
                    row.AddLabel()
                        .SetText("Ships")
                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                        .SetWidth(SpaceLabelWidth);
                    row.AddComboBox()
                        .BindOptions(model => model.SpaceCountOptions)
                        .BindSelectedIndex(model => model.SpaceCountIndex)
                        .SetWidth(SpaceFieldWidth);
                });

                content.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("Spawn Squad")
                        .SetTooltip("Spawn the ships around you")
                        .BindOnClicked(model => model.OnSpawnSpaceSquad())
                        .SetHeight(35f);
                });

                content.AddRow(row =>
                {
                    row.AddLabel()
                        .BindText(model => model.SpaceStatus)
                        .BindColor(model => model.SpaceStatusColor)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                        .SetHeight(20f);
                });

                content.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("Start Incursion")
                        .SetTooltip("Comes with incursions")
                        .SetIsEnabled(false)
                        .SetHeight(35f);
                    row.AddButton()
                        .SetText("Start Convoy")
                        .SetTooltip("Start a trade convoy of the picked faction's tier around you")
                        .BindOnClicked(model => model.OnStartConvoy())
                        .SetHeight(35f);
                    row.AddButton()
                        .SetText("Place Derelict")
                        .SetTooltip("Comes with derelicts")
                        .SetIsEnabled(false)
                        .SetHeight(35f);
                });
            });
        }
    }
}
