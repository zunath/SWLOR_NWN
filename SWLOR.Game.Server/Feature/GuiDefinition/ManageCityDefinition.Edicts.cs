using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public partial class ManageCityDefinition
    {
        private const float EdictLabelHeight = 20f;
        private const float EdictNoteHeight = 44f;
        private const float HeldEdictRowHeight = 44f;
        private const float HeldEdictListHeight = 140f;
        private const float OfferedEdictRowHeight = 28f;
        private const float OfferedEdictListHeight = 150f;
        private const float TargetCityRowHeight = 28f;
        private const float TargetCityListHeight = 84f;
        private const float EdictDescriptionHeight = 72f;
        private const float RepealButtonWidth = 76f;
        private const float EdictButtonHeight = 35f;

        /// <summary>
        /// The Edicts tab: the city's slots, the Edicts it holds with their costs and locks, and a list of Edicts to enact.
        /// </summary>
        private static void AddEdictsTab(GuiGroup<ManageCityViewModel> host)
        {
            AddPanel(host, content =>
            {
                content.AddRow(row =>
                {
                    row.AddLabel()
                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                        .SetVerticalAlign(NuiVerticalAlign.Middle)
                        .SetHeight(25f)
                        .BindText(model => model.Instructions)
                        .BindColor(model => model.InstructionsColor);
                });

                content.AddRow(row =>
                {
                    row.AddLabel()
                        .BindText(model => model.EdictSlotsText)
                        .SetColor(GuiColor.Cyan)
                        .SetHeight(EdictLabelHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                content.AddRow(row =>
                {
                    row.AddText()
                        .BindText(model => model.EdictFundsText)
                        .SetShowBorder(false)
                        .SetScrollbars(NuiScrollbars.None)
                        .SetHeight(EdictNoteHeight);
                });

                content.AddRow(row =>
                {
                    row.AddLabel()
                        .SetText("Edicts in force")
                        .SetHeight(EdictLabelHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                content.AddRow(row =>
                {
                    row.AddLabel()
                        .SetText("The city holds no Edicts.")
                        .BindIsVisible(model => model.IsNoHeldEdictsVisible)
                        .SetHeight(EdictLabelHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                content.AddRow(row =>
                {
                    row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddText()
                                    .BindText(model => model.HeldEdictTexts)
                                    .BindTooltip(model => model.HeldEdictTooltips)
                                    .SetShowBorder(false)
                                    .SetScrollbars(NuiScrollbars.None)
                                    .SetHeight(HeldEdictRowHeight - 4f);
                            });
                            template.AddCell(cell =>
                            {
                                cell.SetIsVariable(false);
                                cell.SetWidth(RepealButtonWidth);
                                cell.AddButton()
                                    .SetText("Repeal")
                                    .BindIsEnabled(model => model.HeldEdictRepealEnabled)
                                    .BindTooltip(model => model.HeldEdictRepealTooltips)
                                    .BindOnClicked(model => model.RepealEdict())
                                    .SetWidth(RepealButtonWidth);
                            });
                        })
                        .BindRowCount(model => model.HeldEdictTexts)
                        .SetRowHeight(HeldEdictRowHeight)
                        .SetHeight(HeldEdictListHeight);
                });

                content.AddRow(row =>
                {
                    row.AddLabel()
                        .SetText("Enact an Edict")
                        .SetHeight(EdictLabelHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                content.AddRow(row =>
                {
                    row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddToggleButton()
                                    .BindIsToggled(model => model.OfferedEdictToggles)
                                    .BindText(model => model.OfferedEdictNames)
                                    .BindTooltip(model => model.OfferedEdictTooltips)
                                    .BindOnClicked(model => model.SelectOfferedEdict());
                            });
                        })
                        .BindRowCount(model => model.OfferedEdictNames)
                        .SetRowHeight(OfferedEdictRowHeight)
                        .SetHeight(OfferedEdictListHeight);
                });

                content.AddRow(row =>
                {
                    row.AddText()
                        .BindText(model => model.SelectedEdictText)
                        .SetShowBorder(false)
                        .SetScrollbars(NuiScrollbars.None)
                        .SetHeight(EdictDescriptionHeight);
                });

                content.AddRow(row =>
                {
                    row.BindIsVisible(model => model.IsTargetPickerVisible);
                    row.AddLabel()
                        .SetText("City to name")
                        .SetHeight(EdictLabelHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                content.AddRow(row =>
                {
                    row.BindIsVisible(model => model.IsTargetPickerVisible);
                    row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddToggleButton()
                                    .BindIsToggled(model => model.TargetCityToggles)
                                    .BindText(model => model.TargetCityNames)
                                    .BindOnClicked(model => model.SelectTargetCity());
                            });
                        })
                        .BindRowCount(model => model.TargetCityNames)
                        .SetRowHeight(TargetCityRowHeight)
                        .SetHeight(TargetCityListHeight);
                });

                content.AddRow(row =>
                {
                    row.AddSpacer();
                    row.AddButton()
                        .SetText("Enact")
                        .SetTooltip("Enact the chosen Edict. Its cost for the days left before the next upkeep check is paid from the treasury now, and nothing is refunded if it is repealed.")
                        .BindIsEnabled(model => model.IsEnactEnabled)
                        .BindOnClicked(model => model.EnactEdict())
                        .SetHeight(EdictButtonHeight);
                    row.AddSpacer();
                });
            });
        }
    }
}
