using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class TestingDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<TestingViewModel> _builder = new();
        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Testing)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Testing Window")

                .DefinePartialView("partial1", partial =>
                {
                    partial.AddLabel()
                        .SetText("partial 1 showing now");
                })

                .DefinePartialView("partial2", partial =>
                {
                    partial.AddLabel()
                        .SetText("yo this is partial 2");
                })

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddPartialView("partialview");
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .BindOnClicked(model => model.TogglePartial())
                            .SetText("Change Partial");
                    });

                });

            return _builder.Build();
        }
    }
}
