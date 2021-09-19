using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiSliderFloat<T> : GuiWidget<T, GuiSliderFloat<T>>
        where T: IGuiDataModel
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

        public GuiSliderFloat<T> SetValue(float value)
        {
            Value = value;
            return this;
        }

        public GuiSliderFloat<T> BindValue<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            ValueBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiSliderFloat<T> SetMinimum(float minimum)
        {
            Minimum = minimum;
            return this;
        }

        public GuiSliderFloat<T> BindMinimum<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            MinimumBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }


        public GuiSliderFloat<T> SetMaximum(float maximum)
        {
            Maximum = maximum;
            return this;
        }

        public GuiSliderFloat<T> BindMaximum<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            MaximumBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiSliderFloat<T> SetStepSize(float stepSize)
        {
            StepSize = stepSize;
            return this;
        }

        public GuiSliderFloat<T> BindStepSize<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            StepSizeBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

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
