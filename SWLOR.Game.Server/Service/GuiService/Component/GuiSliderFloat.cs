using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiSliderFloat: GuiWidget
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

        public GuiSliderFloat SetValue(float value)
        {
            Value = value;
            return this;
        }

        public GuiSliderFloat BindValue(string bindName)
        {
            ValueBindName = bindName;
            return this;
        }

        public GuiSliderFloat SetMinimum(float minimum)
        {
            Minimum = minimum;
            return this;
        }

        public GuiSliderFloat BindMinimum(string bindName)
        {
            MinimumBindName = bindName;
            return this;
        }


        public GuiSliderFloat SetMaximum(float maximum)
        {
            Maximum = maximum;
            return this;
        }

        public GuiSliderFloat BindMaximum(string bindName)
        {
            MaximumBindName = bindName;
            return this;
        }

        public GuiSliderFloat SetStepSize(float stepSize)
        {
            StepSize = stepSize;
            return this;
        }

        public GuiSliderFloat BindStepSize(string bindName)
        {
            StepSizeBindName = bindName;
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
