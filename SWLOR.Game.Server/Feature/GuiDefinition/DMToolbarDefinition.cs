using System.Linq.Expressions;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// The DM toolbar pinned to the top-left corner, where the native DM taskbar used to be.
    /// </summary>
    public class DMToolbarDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<DMToolbarViewModel> _builder = new();

        private const float ButtonWidth = DMToolbarViewModel.ButtonSlotWidth;
        private const float ButtonHeight = 20f;

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
                    col.SetMargin(0f);
                    col.AddRow(row =>
                    {
                        row.SetMargin(0f);
                        AddButton(row, "Chooser", "Search areas and objects", model => model.OnClickChooser());
                        AddButton(row, "Creator", "Search the palettes and spawn blueprints", model => model.OnClickCreator());
                        AddButton(row, "Players", "Search online and offline characters", model => model.OnClickPlayers());
                        AddButton(row, "Creatures", "Creature Manager: save and spawn customized creatures", model => model.OnClickCreatureManager());
                        AddButton(row, "Placeables", "Placeable Tools: move, rotate and save placeable layouts", model => model.OnClickPlaceableTools());
                        AddButton(row, "Notes", "Area notes", model => model.OnClickAreaNotes());
                        AddButton(row, "Music", "Change the area music", model => model.OnClickMusic());

                        row.AddButton()
                            .BindText(model => model.AppearText)
                            .BindTooltip(model => model.AppearTooltip)
                            .SetWidth(ButtonWidth)
                            .SetHeight(ButtonHeight)
                            .SetMargin(0f)
                            .BindOnClicked(model => model.OnClickToggleAppear());

                        AddButton(row, "Settings", "Emote style, Comms filter, helmet and cloak", model => model.OnClickSettings());

                        // Must stay last: the toolbar is sized to end before it when it is hidden.
                        AddButton(row, "Admin", "Staff, bans and property diagnostics", model => model.OnClickAdmin())
                            .BindIsVisible(model => model.IsAdmin);
                    });
                });

            return _builder.Build();
        }

        private static GuiButton<DMToolbarViewModel> AddButton(
            GuiRow<DMToolbarViewModel> row,
            string text,
            string tooltip,
            Expression<Func<DMToolbarViewModel, Action>> onClick)
        {
            return row.AddButton()
                .SetText(text)
                .SetTooltip(tooltip)
                .SetWidth(ButtonWidth)
                .SetHeight(ButtonHeight)
                .SetMargin(0f)
                .BindOnClicked(onClick);
        }
    }
}
