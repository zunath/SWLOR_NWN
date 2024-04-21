using SWLOR.Core.Beamdog;
using SWLOR.Core.Feature.GuiDefinition.ViewModel;
using SWLOR.Core.Service.GuiService;

namespace SWLOR.Core.Feature.GuiDefinition
{
    public class PriceSelectionDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<PriceSelectionViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.PriceSelection)
                .SetIsResizable(true)
                .SetIsCollapsible(false)
                .SetInitialGeometry(0, 0, 400f, 240f)
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
