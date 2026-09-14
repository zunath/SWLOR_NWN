using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public sealed class ConversationWindowDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<ConversationViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Conversation)
                .SetIsResizable(true)
                .SetIsCollapsible(false)
                .SetIsClosable(true)
                .SetInitialGeometry(0f, 0f, 650f, 520f)
                .SetTitle("Conversation")
                .BindOnClosed(model => model.OnWindowClosed())
                .AddColumn(root =>
                {
                    root.AddRow(header =>
                    {
                        header.AddColumn(portrait =>
                        {
                            portrait.AddRow(row =>
                            {
                                row.AddLabel()
                                    .BindText(model => model.SpeakerName)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                    .SetVerticalAlign(NuiVerticalAlign.Middle)
                                    .SetHeight(28f);
                            });
                            portrait.AddRow(row =>
                            {
                                row.AddImage()
                                    .BindResref(model => model.PortraitResref)
                                    .BindIsVisible(model => model.HasPortrait)
                                    .SetAspect(NuiAspect.Fit)
                                    .SetWidth(128f)
                                    .SetHeight(200f);
                            });
                        }).SetWidth(140f);

                        header.AddColumn(dialogue =>
                        {
                            dialogue.AddRow(row =>
                            {
                                row.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.AddText()
                                            .BindText(model => model.LineTexts)
                                            .BindColor(model => model.LineColors)
                                            .SetShowBorder(false)
                                            .SetScrollbars(NuiScrollbars.None)
                                            .SetPadding(4f)
                                            .SetHeight(24f);
                                    });
                                })
                                    .BindRowCount(model => model.LineTexts)
                                    .SetRowHeight(28f)
                                    .SetShowBorders(false)
                                    .SetScrollbars(NuiScrollbars.Y);
                            });
                        });
                    });

                    root.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Your response")
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Middle)
                            .SetColor(169, 169, 169)
                            .SetHeight(24f);
                    });

                    root.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddButton()
                                    .BindText(model => model.ChoiceTexts)
                                    .BindColor(model => model.ChoiceColors)
                                    .BindTooltip(model => model.ChoiceTexts)
                                    .BindOnClicked(model => model.OnClickChoice())
                                    .SetHeight(38f);
                            });
                        })
                            .BindRowCount(model => model.ChoiceTexts)
                            .SetRowHeight(42f)
                            .SetShowBorders(false)
                            .SetScrollbars(NuiScrollbars.Y);
                    });
                });

            return _builder.Build();
        }
    }
}
