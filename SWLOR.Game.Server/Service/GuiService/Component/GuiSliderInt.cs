using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiSliderInt : GuiWidget
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

        public GuiSliderInt SetValue(int value)
        {
            Value = value;
            return this;
        }

        public GuiSliderInt BindValue(string bindName)
        {
            ValueBindName = bindName;
            return this;
        }

        public GuiSliderInt SetMinimum(int minimum)
        {
            Minimum = minimum;
            return this;
        }

        public GuiSliderInt BindMinimum(string bindName)
        {
            MinimumBindName = bindName;
            return this;
        }


        public GuiSliderInt SetMaximum(int maximum)
        {
            Maximum = maximum;
            return this;
        }

        public GuiSliderInt BindMaximum(string bindName)
        {
            MaximumBindName = bindName;
            return this;
        }

        public GuiSliderInt SetStepSize(int stepSize)
        {
            StepSize = stepSize;
            return this;
        }

        public GuiSliderInt BindStepSize(string bindName)
        {
            StepSizeBindName = bindName;
            return this;
        }

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
