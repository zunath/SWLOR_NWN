using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.NWN.API;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiSliderInt<T> : GuiWidget<T, GuiSliderInt<T>>
        where T: IGuiViewModel
    {
        private int Value { get; set; }
        private string ValueBindName { get; set; }
        private bool IsValueBound => !string.IsNullOrWhiteSpace(ValueBindName);
        
        private int Minimum { get; set; }
        private string MinimumBindName { get; set; }
        private bool IsMinimumBound => !string.IsNullOrWhiteSpace(MinimumBindName);
        
        private int Maximum { get; set; }
        private string MaximumBindName { get; set; }
        private bool IsMaximumBound => !string.IsNullOrWhiteSpace(MaximumBindName);
        
        private int StepSize { get; set; }
        private string StepSizeBindName { get; set; }
        private bool IsStepSizeBound => !string.IsNullOrWhiteSpace(StepSizeBindName);

        /// <summary>
        /// Sets a static value for the slider.
        /// </summary>
        /// <param name="value">The value to set.</param>
        public GuiSliderInt<T> SetValue(int value)
        {
            Value = value;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the slider.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiSliderInt<T> BindValue<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            ValueBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static minimum value for the slider.
        /// </summary>
        /// <param name="minimum">The minimum value to set.</param>
        public GuiSliderInt<T> SetMinimum(int minimum)
        {
            Minimum = minimum;
            return this;
        }

        /// <summary>
        /// Binds a dynamic minimum value for the slider.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiSliderInt<T> BindMinimum<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            MinimumBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }


        /// <summary>
        /// Sets the maximum value for the slider.
        /// </summary>
        /// <param name="maximum">The maximum value to set.</param>
        public GuiSliderInt<T> SetMaximum(int maximum)
        {
            Maximum = maximum;
            return this;
        }

        /// <summary>
        /// Binds a dynamic maximum value for the slider.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiSliderInt<T> BindMaximum<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            MaximumBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for the step size for the slider.
        /// </summary>
        /// <param name="stepSize">The step size to set.</param>
        public GuiSliderInt<T> SetStepSize(int stepSize)
        {
            StepSize = stepSize;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the step size for the slider.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiSliderInt<T> BindStepSize<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            StepSizeBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Builds the GuiSliderInt element.
        /// </summary>
        /// <returns>Json representing the integer slider element.</returns>
        public override Json BuildElement()
        {
            var value = IsValueBound ? Nui.Bind(ValueBindName) : JsonInt(Value);
            var minimum = IsMinimumBound ? Nui.Bind(MinimumBindName) : JsonInt(Minimum);
            var maximum = IsMaximumBound ? Nui.Bind(MaximumBindName) : JsonInt(Maximum);
            var stepSize = IsStepSizeBound ? Nui.Bind(StepSizeBindName) : JsonInt(StepSize);

            return Nui.Slider(value, minimum, maximum, stepSize);
        }
    }
}
