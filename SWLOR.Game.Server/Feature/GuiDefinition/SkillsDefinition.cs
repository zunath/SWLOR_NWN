using System;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class SkillsDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<SkillsViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {

            _builder.CreateWindow(GuiWindowType.CharacterSheet)
                .BindGeometry(model => model.Geometry)
                .BindOnOpened(model => model.OnLoadWindow())
                .SetIsResizable(false)
                .SetGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Skills");

            return _builder.Build();
        }
    }
}
