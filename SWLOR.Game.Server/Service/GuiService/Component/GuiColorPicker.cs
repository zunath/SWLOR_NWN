using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiColorPicker: GuiWidget
    {
        private GuiColor SelectedColor { get; set; }
        private string SelectedColorBindName { get; set; }
        private bool IsSelectedColorBound => !string.IsNullOrWhiteSpace(SelectedColorBindName);

        public GuiColorPicker SetSelectedColor(GuiColor selectedColor)
        {
            SelectedColor = selectedColor;
            return this;
        }

        public GuiColorPicker BindSelectedColor(string bindName)
        {
            SelectedColorBindName = bindName;
            return this;
        }

        public override Json BuildElement()
        {
            var selectedColor = IsSelectedColorBound ? Nui.Bind(SelectedColorBindName) : SelectedColor.ToJson();

            return Nui.ColorPicker(selectedColor);
        }
    }
}
