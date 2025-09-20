using System;
using System.Linq.Expressions;
using SWLOR.NWN.API.Engine;
using SWLOR.Shared.Core.Beamdog;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiProgressBar<T> : GuiWidget<T, GuiProgressBar<T>>
        where T: IGuiViewModel
    {
        private float Value { get; set; }
        private string ValueBindName { get; set; }
        private bool IsValueBound => !string.IsNullOrWhiteSpace(ValueBindName);

        /// <summary>
        /// Sets a static value for the progress bar.
        /// </summary>
        /// <param name="value">The value to set.</param>
        public GuiProgressBar<T> SetValue(float value)
        {
            Value = value;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value to the progress bar.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiProgressBar<T> BindValue<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            ValueBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Builds the GuiProgressBar element.
        /// </summary>
        /// <returns>Json representing the progress bar element.</returns>
        public override Json BuildElement()
        {
            var value = IsValueBound ? Nui.Bind(ValueBindName) : JsonFloat(Value);

            return Nui.Progress(value);
        }
    }
}
