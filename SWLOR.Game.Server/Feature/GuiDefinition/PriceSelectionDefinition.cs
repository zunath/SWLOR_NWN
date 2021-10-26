using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class PriceSelectionDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<PriceSelectionViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.PriceSelection)
                .SetIsResizable(true)
                .SetInitialGeometry(0, 0, 321f, 162f)
                .SetTitle("Change Price")
                
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.ItemName)
                            .SetHeight(26f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Top);
                    });

                    col.AddRow(row =>
                    {
                        row.AddTextEdit()
                            .BindValue(model => model.Price);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("Save")
                            .BindOnClicked(model => model.OnClickSave());

                        row.AddButton()
                            .SetText("Cancel")
                            .BindOnClicked(model => model.OnClickCancel());
                    });
                });


            return _builder.Build();
        }
    }
}
