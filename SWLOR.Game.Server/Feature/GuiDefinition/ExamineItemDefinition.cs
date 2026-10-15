using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class ExamineItemDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<ExamineItemViewModel> _builder = new();
        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.ExamineItem)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 440f, 480f)
                .BindTitle(model => model.WindowTitle);
            // The view model builds the content per item, sized to its text.
            window.AddStandardLayout(layout =>
                layout.SetContentPartialElement(ExamineItemViewModel.ContentElement));
            var result = _builder.Build();
            if (!GuiLayoutValidator.IsValidationOnlyBuild)
                result.Window = Nui.StyleForegroundColor(result.Window, Nui.Bind(nameof(ExamineItemViewModel.TitleColor)));
            return result;
        }
    }
}
