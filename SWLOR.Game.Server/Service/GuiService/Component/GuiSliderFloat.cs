using System;
using System.Linq.Expressions;
using SWLOR.NWN.API.Engine;
using SWLOR.Shared.Core.Beamdog;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiSliderFloat<T> : GuiWidget<T, GuiSliderFloat<T>>
        where T: IGuiViewModel
    {
        private float Value { get; set; }
        private string ValueBindName { get; set; }
        private bool IsValueBound => !string.IsNullOrWhiteSpace(ValueBindName);
        
        private float Minimum { get; set; }
        private string MinimumBindName { get; set; }
        private bool IsMinimumBound => !string.IsNullOrWhiteSpace(MinimumBindName);
        
        private float Maximum { get; set; }
        private string MaximumBindName { get; set; }
        private bool IsMaximumBound => !string.IsNullOrWhiteSpace(MaximumBindName);
        
        private float StepSize { get; set; }
        private string StepSizeBindName { get; set; }
        private bool IsStepSizeBound => !string.IsNullOrWhiteSpace(StepSizeBindName);

        /// <summary>
        /// Sets a static value for the slider.
        /// </summary>
        /// <param name="value">The value to set.</param>
        public GuiSliderFloat<T> SetValue(float value)
        {
            Value = value;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the slider.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiSliderFloat<T> BindValue<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            ValueBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static minimum value for the slider.
        /// </summary>
        /// <param name="minimum">The minimum value to set.</param>
        public GuiSliderFloat<T> SetMinimum(float minimum)
        {
            Minimum = minimum;
            return this;
        }

        /// <summary>
        /// Binds a dynamic minimum value for the slider.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiSliderFloat<T> BindMinimum<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            MinimumBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets the maximum value for the slider.
        /// </summary>
        /// <param name="maximum">The maximum value to set.</param>
        public GuiSliderFloat<T> SetMaximum(float maximum)
        {
            Maximum = maximum;
            return this;
        }

        /// <summary>
        /// Binds a dynamic maximum value for the slider.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiSliderFloat<T> BindMaximum<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            MaximumBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for the step size for the slider.
        /// </summary>
        /// <param name="stepSize">The step size to set.</param>
        public GuiSliderFloat<T> SetStepSize(float stepSize)
        {
            StepSize = stepSize;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the step size for the slider.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiSliderFloat<T> BindStepSize<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            StepSizeBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Builds the GuiSliderFloat element.
        /// </summary>
        /// <returns>Json representing the float slider element.</returns>
        public override Json BuildElement()
        {
            var value = IsValueBound ? Nui.Bind(ValueBindName) : JsonFloat(Value);
            var minimum = IsMinimumBound ? Nui.Bind(MinimumBindName) : JsonFloat(Minimum);
            var maximum = IsMaximumBound ? Nui.Bind(MaximumBindName) : JsonFloat(Maximum);
            var stepSize = IsStepSizeBound ? Nui.Bind(StepSizeBindName) : JsonFloat(StepSize);

            return Nui.SliderFloat(value, minimum, maximum, stepSize);
        }
    }
}
