using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiColorPicker<T> : GuiWidget<T, GuiColorPicker<T>>
        where T: IGuiViewModel
    {
        private GuiColor SelectedColor { get; set; }
        private string SelectedColorBindName { get; set; }
        private bool IsSelectedColorBound => !string.IsNullOrWhiteSpace(SelectedColorBindName);

        public GuiColorPicker()
        {
            SelectedColor = new GuiColor(0, 0, 0);
        }

        public GuiColorPicker<T> SetSelectedColor(GuiColor selectedColor)
        {
            SelectedColor = selectedColor;
            return this;
        }

        public GuiColorPicker<T> BindSelectedColor<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            SelectedColorBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public override Json BuildElement()
        {
            var selectedColor = IsSelectedColorBound ? Nui.Bind(SelectedColorBindName) : SelectedColor.ToJson();

            return Nui.ColorPicker(selectedColor);
        }
    }
}
