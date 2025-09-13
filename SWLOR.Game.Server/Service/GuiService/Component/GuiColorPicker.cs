using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.NWN.API;

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

        /// <summary>
        /// Sets a static value for the selected color.
        /// </summary>
        /// <param name="selectedColor">The static color to set.</param>
        public GuiColorPicker<T> SetSelectedColor(GuiColor selectedColor)
        {
            SelectedColor = selectedColor;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the selected color.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiColorPicker<T> BindSelectedColor<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            SelectedColorBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Builds the GuiColorPicker element.
        /// </summary>
        /// <returns>Json representing the color picker element.</returns>
        public override Json BuildElement()
        {
            var selectedColor = IsSelectedColorBound ? Nui.Bind(SelectedColorBindName) : SelectedColor.ToJson();

            return Nui.ColorPicker(selectedColor);
        }
    }
}
