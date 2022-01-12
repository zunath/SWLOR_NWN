using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class RenameDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<RenameItemViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.RenameItem)
                .SetIsResizable(true)
                .SetInitialGeometry(200f, 200f, 500f, 300f)
                .SetTitle("Rename Item")
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.Header)
                            .SetColor(255, 0, 0)
                            .SetIsVisible(true)
                            .SetWidth(300f)
                            .SetHeight(20f);

                    });

                    col.AddRow(row =>
                    {
                        row.AddColumn(col =>
                        {
                            col.AddRow(row =>
                            {
                                row.AddLabel()
                                    .SetText("Original Name: ")
                                    .SetIsVisible(true)
                                    .SetWidth(100f)
                                    .SetHeight(20f);
                            });

                            col.AddRow(row =>
                            {
                                row.AddLabel()
                                    .SetText("Current Name: ")
                                    .SetIsVisible(true)
                                    .SetWidth(100f)
                                    .SetHeight(20f);
                            });

                        });

                        row.AddColumn(col =>
                        {
                            col.AddRow(row =>
                            {
                                row.AddLabel()
                                    .BindText(model => model.OriginalName)
                                    .SetColor(0, 255, 0)
                                    .SetIsVisible(true)
                                    .SetWidth(200f)
                                    .SetHeight(20f);
                            });

                            col.AddRow(row =>
                            {
                                row.AddLabel()
                                    .BindText(model => model.CurrentName)
                                    .SetColor(0, 255, 0)
                                    .SetIsVisible(true)
                                    .SetWidth(200f)
                                    .SetHeight(20f);
                            });

                        });
                    });

                    col.AddRow(row =>
                    {
                        row.AddTextEdit()
                            .SetIsMultiline(false)
                            .SetMaxLength(63)
                            .BindValue(model => model.NewName)
                            .SetIsEnabled(true)
                            .SetHeight(20f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButton()
                            .BindOnClicked(model => model.OnClickSubmit())
                            .SetHeight(35f)
                            .SetText("Change Name")
                            .SetIsEnabled(true);

                        row.AddButton()
                            .BindOnClicked(model => model.OnClickReset())
                            .SetHeight(35f)
                            .SetText("Reset Name")
                            .SetIsEnabled(true);

                        row.AddButton()
                            .BindOnClicked(model => model.OnClickCancel())
                            .SetHeight(35f)
                            .SetText("Cancel")
                            .SetIsEnabled(true);

                        row.AddSpacer();

                    });
                });
        
            return _builder.Build();
        }
    }
}
