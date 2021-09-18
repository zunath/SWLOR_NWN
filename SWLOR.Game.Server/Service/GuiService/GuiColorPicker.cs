using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiColorPicker: GuiWidget
    {
        public GuiColor SelectedColor { get; set; }
        public string SelectedColorBindName { get; set; }
        public bool IsSelectedColorBound => !string.IsNullOrWhiteSpace(SelectedColorBindName);

        public override Json BuildElement()
        {
            var selectedColor = IsSelectedColorBound ? Nui.Bind(SelectedColorBindName) : SelectedColor.ToJson();

            return Nui.ColorPicker(selectedColor);
        }
    }
}
