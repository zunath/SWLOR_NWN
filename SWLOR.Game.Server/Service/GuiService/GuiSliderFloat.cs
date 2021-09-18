using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiSliderFloat: GuiWidget
    {
        public float Value { get; set; }
        public string ValueBindName { get; set; }
        public bool IsValueBound => !string.IsNullOrWhiteSpace(ValueBindName);

        public float Minimum { get; set; }
        public string MinimumBindName { get; set; }
        public bool IsMinimumBound => !string.IsNullOrWhiteSpace(MinimumBindName);

        public float Maximum { get; set; }
        public string MaximumBindName { get; set; }
        public bool IsMaximumBound => !string.IsNullOrWhiteSpace(MaximumBindName);

        public float StepSize { get; set; }
        public string StepSizeBindName { get; set; }
        public bool IsStepSizeBound => !string.IsNullOrWhiteSpace(StepSizeBindName);

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
