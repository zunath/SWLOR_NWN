using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class ExamineItemDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<ExamineItemViewModel> _builder = new();
        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.ExamineItem)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 430f, 460f)
                .BindTitle(model => model.WindowTitle)
                .DefinePartialView(ExamineItemViewModel.ContentPartial, host =>
                    host.AddColumn(col =>
                        col.AddRow(row =>
                            row.AddGroup(panel =>
                            {
                                panel.SetShowBorder(false)
                                    .SetScrollbars(NuiScrollbars.None);

                                panel.AddColumn(body =>
                                {
                                    body.AddRow(row =>
                                        row.AddLabel()
                                            .SetText("Description")
                                            .SetHeight(26f));

                                    body.AddRow(row =>
                                        row.AddText()
                                            .BindText(model => model.Description)
                                            .SetHeight(230f));

                                    body.AddRow(row =>
                                        row.AddLabel()
                                            .SetText("Item Properties")
                                            .SetHeight(26f));

                                    body.AddRow(row =>
                                        row.AddText()
                                            .BindText(model => model.ItemProperties)
                                            .SetHeight(160f));
                                });
                            })
                                .SetWidth(390f))));
            window.AddStandardLayout(layout =>
                layout.SetContentPartialElement(ExamineItemViewModel.ContentElement));
            return _builder.Build();
        }
    }
}
