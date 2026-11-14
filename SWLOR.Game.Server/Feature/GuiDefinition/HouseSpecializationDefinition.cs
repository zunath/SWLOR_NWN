using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// A house's specialization: what it is now, what each choice does, and when it can change next. Anyone in the house can read it;
    /// only the owner can pick a Workshop (with its crafting skill), a Storehouse or a Greenhouse, once every 30 days.
    /// </summary>
    public class HouseSpecializationDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<HouseSpecializationViewModel> _builder = new();

        private const float ContentPanelWidth = 460f;
        private const float LabelHeight = 20f;
        private const float MessageHeight = 48f;
        private const float InfoHeight = 76f;
        private const float RowHeight = 32f;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.HouseSpecialization)
                .SetInitialGeometry(0, 0, 520f, 560f)
                .SetTitle("House Specialization")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .DefinePartialView(HouseSpecializationViewModel.MainContentPartial, AddMainContent);

            window.AddStandardLayout(layout => layout.SetContentPartialElement(HouseSpecializationViewModel.MainContentElement));

            return _builder.Build();
        }

        private static void AddMainContent(GuiGroup<HouseSpecializationViewModel> host)
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
                            AddLine(content, model => model.HeaderText, GuiColor.Cyan);
                            AddLine(content, model => model.CurrentText);
                            AddLine(content, model => model.NextChangeText);

                            content.AddRow(line =>
                            {
                                line.AddText()
                                    .BindText(model => model.EffectText)
                                    .SetShowBorder(false)
                                    .SetScrollbars(NuiScrollbars.None)
                                    .SetHeight(InfoHeight);
                            });

                            content.AddRow(line =>
                            {
                                line.AddText()
                                    .BindText(model => model.MessageText)
                                    .SetShowBorder(false)
                                    .SetScrollbars(NuiScrollbars.None)
                                    .SetHeight(MessageHeight);
                            });

                            AddLine(content, model => model.SkillHeaderText, GuiColor.Cyan);

                            content.AddRow(line =>
                            {
                                line.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.AddToggleButton()
                                            .BindText(model => model.SkillNames)
                                            .BindIsToggled(model => model.SkillToggles)
                                            .BindOnClicked(model => model.OnClickSkill());
                                    });
                                })
                                    .BindRowCount(model => model.SkillNames)
                                    .SetRowHeight(RowHeight);
                            });

                            content.AddRow(line =>
                            {
                                line.AddButton()
                                    .SetText("Make a Workshop")
                                    .BindIsEnabled(model => model.CanSetWorkshop)
                                    .BindDisabledTooltip(model => model.WorkshopTooltip)
                                    .BindOnClicked(model => model.OnClickWorkshop());
                            });

                            content.AddRow(line =>
                            {
                                line.AddButton()
                                    .SetText("Make a Storehouse")
                                    .BindIsEnabled(model => model.CanSetStorehouse)
                                    .BindDisabledTooltip(model => model.StorehouseTooltip)
                                    .BindOnClicked(model => model.OnClickStorehouse());
                            });

                            content.AddRow(line =>
                            {
                                line.AddButton()
                                    .SetText("Make a Greenhouse")
                                    .BindIsEnabled(model => model.CanSetGreenhouse)
                                    .BindDisabledTooltip(model => model.GreenhouseTooltip)
                                    .BindOnClicked(model => model.OnClickGreenhouse());
                            });
                        });
                    })
                        .SetWidth(ContentPanelWidth);
                });
            });
        }

        private static void AddLine(
            GuiColumn<HouseSpecializationViewModel> content,
            System.Linq.Expressions.Expression<System.Func<HouseSpecializationViewModel, string>> text,
            GuiColor color = null)
        {
            content.AddRow(line =>
            {
                var label = line.AddLabel()
                    .BindText(text)
                    .SetHeight(LabelHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);

                if (color != null)
                    label.SetColor(color);
            });
        }
    }
}
