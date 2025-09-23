using SWLOR.Component.Quest.UI.ViewModel;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Quest.UI.View
{
    public class QuestsDefinition : IGuiWindowDefinition
    {
        private readonly IGuiService _guiService;
        private readonly GuiWindowBuilder<QuestsViewModel> _builder;

        public QuestsDefinition(IGuiService guiService)
        {
            _guiService = guiService;
            _builder = new GuiWindowBuilder<QuestsViewModel>(_guiService);
        }

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Quests)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Quests")

                .AddRow(row =>
                {
                    row.AddColumn(col =>
                    {
                        col.AddRow(row2 =>
                        {
                            row2.AddTextEdit()
                                .BindValue(model => model.SearchText)
                                .SetPlaceholder("Search by Name");

                            row2.AddButton()
                                .SetText("X")
                                .SetHeight(35f)
                                .SetWidth(35f)
                                .BindOnClicked(model => model.OnClickClearSearch());

                            row2.AddButton()
                                .SetText("Search")
                                .SetHeight(35f)
                                .SetWidth(60f)
                                .BindOnClicked(model => model.OnClickSearch());
                        });

                        col.AddRow(row2 =>
                        {
                            row2.AddList(template =>
                            {
                                template.AddCell(cell =>
                                {
                                    cell.AddToggleButton()
                                        .BindText(model => model.QuestNames)
                                        .BindIsToggled(model => model.QuestToggles)
                                        .BindOnClicked(model => model.OnClickQuest())
                                        .BindTooltip(model => model.QuestNames);
                                });
                            })
                                .BindRowCount(model => model.QuestNames);
                        });
                    });

                    row.AddColumn(col =>
                    {
                        col.AddRow(row2 =>
                        {
                            row2.AddLabel()
                                .BindText(model => model.ActiveQuestName)
                                .SetHeight(20f)
                                .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                .SetVerticalAlign(NuiVerticalAlign.Top);
                        });

                        col.AddRow(row2 =>
                        {
                            row2.AddText()
                                .BindText(model => model.ActiveQuestDescription);
                        });

                        col.AddRow(row2 =>
                        {
                            row2.AddSpacer();

                            row2.AddButton()
                                .SetText("Abandon Quest")
                                .BindOnClicked(model => model.OnClickAbandonQuest())
                                .SetHeight(32f)
                                .BindIsEnabled(model => model.IsAbandonQuestEnabled);

                            row2.AddSpacer();
                        });
                    });
                })

                ;

            return _builder.Build();
        }
    }
}
