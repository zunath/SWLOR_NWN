using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// Chooses what one extra hotbar slot does: an ability, an inventory item or a chat command.
    /// </summary>
    public class ExtraHotbarSlotPickerDefinition : IGuiWindowDefinition
    {
        private const float PanelWidth = 380f;
        private const float ControlHeight = 32f;

        private readonly GuiWindowBuilder<ExtraHotbarSlotPickerViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.ExtraHotbarSlotPicker)
                .SetInitialGeometry(0, 0, 420f, 620f)
                .BindTitle(model => model.Title)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .DefinePartialView(ExtraHotbarSlotPickerViewModel.MainPartial, AddBody);

            window.AddStandardLayout(layout =>
            {
                layout.SetContentPartialElement(ExtraHotbarSlotPickerViewModel.ContentElement);
            });

            return _builder.Build();
        }

        private static void AddBody(GuiGroup<ExtraHotbarSlotPickerViewModel> host)
        {
            host.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddGroup(panel =>
                    {
                        panel.SetShowBorder(false);
                        panel.SetScrollbars(NuiScrollbars.None);
                        panel.AddColumn(content =>
                        {
                            content.AddRow(r =>
                            {
                                r.AddComboBox()
                                    .AddOption("Abilities", ExtraHotbarSlotPickerViewModel.AbilitiesCategory)
                                    .AddOption("Items", ExtraHotbarSlotPickerViewModel.ItemsCategory)
                                    .AddOption("Chat Commands", ExtraHotbarSlotPickerViewModel.CommandsCategory)
                                    .AddOption("Combat Modes", ExtraHotbarSlotPickerViewModel.ActionModesCategory)
                                    .BindSelectedIndex(model => model.SelectedCategory)
                                    .SetWidth(150f)
                                    .SetHeight(ControlHeight);
                                r.AddTextEdit()
                                    .SetPlaceholder("Search")
                                    .BindValue(model => model.SearchText)
                                    .SetHeight(ControlHeight);
                            });

                            content.AddRow(r =>
                            {
                                r.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.AddGroup(group =>
                                        {
                                            group.SetShowBorder(false);
                                            group.SetScrollbars(NuiScrollbars.None);
                                            group.AddImage()
                                                .BindResref(model => model.EntryIcons)
                                                .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                                .SetVerticalAlign(NuiVerticalAlign.Middle)
                                                .SetAspect(NuiAspect.Stretch)
                                                .SetWidth(32f)
                                                .SetHeight(32f);
                                        });
                                        cell.SetWidth(40f);
                                        cell.SetIsVariable(false);
                                    });
                                    template.AddCell(cell =>
                                    {
                                        cell.AddButton()
                                            .BindText(model => model.EntryNames)
                                            .BindTooltip(model => model.EntryTooltips)
                                            .BindOnClicked(model => model.OnClickEntry());
                                    });
                                })
                                    .SetRowHeight(40f)
                                    .SetScrollbars(NuiScrollbars.Y)
                                    .BindRowCount(model => model.EntryNames)
                                    .SetHeight(330f);
                            });

                            content.AddRow(r =>
                            {
                                r.AddTextEdit()
                                    .SetPlaceholder("/command and arguments")
                                    .BindValue(model => model.CommandText)
                                    .BindIsVisible(model => model.IsCommandMode)
                                    .SetHeight(ControlHeight);
                                r.AddButton()
                                    .SetText("Assign")
                                    .SetTooltip("Puts this chat command on the slot.")
                                    .BindIsVisible(model => model.IsCommandMode)
                                    .SetWidth(90f)
                                    .SetHeight(ControlHeight)
                                    .BindOnClicked(model => model.OnClickAssignCommand());
                            });

                            content.AddRow(r => r.AddLabel()
                                .BindText(model => model.StatusText)
                                .BindColor(model => model.StatusColor)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetHeight(ControlHeight));

                            content.AddRow(r =>
                            {
                                r.AddSpacer();
                                r.AddButton()
                                    .SetText("Clear Slot")
                                    .SetWidth(110f)
                                    .SetHeight(ControlHeight)
                                    .BindOnClicked(model => model.OnClickClear());
                            });
                        });
                    })
                        .SetWidth(PanelWidth);
                });
            });
        }
    }
}
