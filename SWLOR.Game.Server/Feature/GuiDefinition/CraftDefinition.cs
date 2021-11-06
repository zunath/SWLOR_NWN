using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class CraftDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<CraftViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Craft)
                .SetIsResizable(true)
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .BindTitle(model => model.WindowTitle)

                .AddPartialView("SetUpPartial", view =>
                {
                    view.AddLabel()
                        .SetText("this is the SetUpPartial view");
                })

                .AddPartialView("CraftPartial", view =>
                {
                    view.AddLabel()
                        .SetText("this is the Craft view");
                })


                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("Change to partial 1")
                            .BindOnClicked(model => model.ChangeToSetUpPartial());
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("Change to partial 2")
                            .BindOnClicked(model => model.ChangeToCraftPartial());
                    });

                    col.AddRow(row =>
                    {
                        row.AddGroup(group =>
                        {
                            group.SetId("MyView");
                        });
                    });
                })
                ;

            return _builder.Build();
        }
    }
}
