using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class PropertyAreaDescriptionDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<PropertyAreaDescriptionViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.PropertyAreaDescription)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 326f, 300f)
                .SetTitle("Area Description")

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Shown to anyone entering this property.")
                            .SetHeight(20f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddTextEdit()
                            .SetPlaceholder("Description")
                            .BindValue(model => model.Description)
                            .SetIsMultiline(true)
                            .SetMaxLength(ManageApartmentViewModel.MaxDescriptionLength)
                            .SetHeight(160f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("Save")
                            .SetHeight(35f)
                            .BindOnClicked(model => model.OnClickSave());

                        row.AddButton()
                            .SetText("Cancel")
                            .SetHeight(35f)
                            .BindOnClicked(model => model.OnClickCancel());
                    });
                });

            return _builder.Build();
        }
    }
}
