using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// The DM toolbar pinned to the top-left corner, where the native DM taskbar used to be.
    /// </summary>
    public class DMToolbarDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<DMToolbarViewModel> _builder = new();

        private const float ButtonWidth = 110f;
        private const float ButtonHeight = 32f;

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.DMToolbar)
                .SetInitialGeometry(DMToolbarViewModel.ToolbarX, DMToolbarViewModel.ToolbarY,
                    DMToolbarViewModel.ToolbarWidth, DMToolbarViewModel.ToolbarHeight)
                .SetTitle(null)
                .SetIsClosable(false)
                .SetIsResizable(false)
                .SetIsCollapsible(false)
                .SetShowBorder(false)
                .SetKeepsOpenOnAreaChange(true)
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("Chooser")
                            .SetTooltip("Search areas and objects")
                            .SetWidth(ButtonWidth)
                            .SetHeight(ButtonHeight)
                            .BindOnClicked(model => model.OnClickChooser());

                        row.AddButton()
                            .SetText("Creator")
                            .SetTooltip("Search and spawn blueprints")
                            .SetWidth(ButtonWidth)
                            .SetHeight(ButtonHeight)
                            .BindOnClicked(model => model.OnClickCreator());

                        row.AddButton()
                            .SetText("Players")
                            .SetTooltip("Search online and offline characters")
                            .SetWidth(ButtonWidth)
                            .SetHeight(ButtonHeight)
                            .BindOnClicked(model => model.OnClickPlayers());

                        row.AddButton()
                            .BindText(model => model.AppearText)
                            .BindTooltip(model => model.AppearTooltip)
                            .SetWidth(ButtonWidth)
                            .SetHeight(ButtonHeight)
                            .BindOnClicked(model => model.OnClickToggleAppear());
                    });
                });

            return _builder.Build();
        }
    }
}
