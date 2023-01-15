using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class HoloNetDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<HoloNetViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.HoloNet)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 829f, 453f)
                .SetTitle("Holonet Broadcast")

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Please type in your HoloNet broadcast below (OOC: remember, server rules apply!)")
                            .SetIsVisible(true)
                            .SetWidth(800f);

                        row.SetHeight(20f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddTextEdit()
                            .SetIsMultiline(true)
                            .SetMaxLength(HoloNetViewModel.MaxHoloNetTextLength)
                            .BindValue(model => model.HoloNetText)
                            .SetIsEnabled(true)
                            .SetHeight(300f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButton()
                            .BindOnClicked(model => model.OnClickSubmit())
                            .SetHeight(35f)
                            .SetText($"Broadcast ({HoloNetViewModel.BroadcastPrice}cr)")
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
