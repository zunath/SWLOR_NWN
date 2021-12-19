using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class OutfitDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<OutfitViewModel> _builder = new();
        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Outfits)
                .SetIsResizable(true)
                .SetInitialGeometry(0, 0, 641.7894f, 396.3158f)
                .SetTitle("Outfits")
                
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddToggleButton()
                                    .BindIsToggled(model => model.SlotToggles)
                                    .BindText(model => model.SlotNames)
                                    .BindOnClicked(model => model.OnClickSlot());
                            });
                        })
                            .BindRowCount(model => model.SlotNames);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("New")
                            .BindOnClicked(model => model.OnClickNew())
                            .SetHeight(35f);

                        row.AddButton()
                            .SetText("Delete")
                            .BindOnClicked(model => model.OnClickDelete())
                            .SetHeight(35f);
                    });
                })
                
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddTextEdit()
                            .SetPlaceholder("Name")
                            .BindValue(model => model.Name)
                            .SetMaxLength(32);

                        row.AddButton()
                            .SetText("Save")
                            .SetHeight(35f)
                            .BindIsEnabled(model => model.IsSaveEnabled)
                            .BindOnClicked(model => model.OnClickSave());

                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddText()
                            .BindText(model => model.Details);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddButton()
                            .SetText("Store Outfit")
                            .BindOnClicked(model => model.OnClickStoreOutfit())
                            .SetHeight(35f);

                        row.AddButton()
                            .SetText("Load Outfit")
                            .BindIsEnabled(model => model.IsLoadEnabled)
                            .BindOnClicked(model => model.OnClickLoadOutfit())
                            .SetHeight(35f);

                        row.AddSpacer();
                    });

                    col.BindIsVisible(model => model.IsSlotLoaded);
                });

            return _builder.Build();
        }
    }
}
