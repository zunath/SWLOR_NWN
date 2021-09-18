using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiSliderInt : GuiWidget
    {
        public int Value { get; set; }
        public string ValueBindName { get; set; }
        public bool IsValueBound => !string.IsNullOrWhiteSpace(ValueBindName);

        public int Minimum { get; set; }
        public string MinimumBindName { get; set; }
        public bool IsMinimumBound => !string.IsNullOrWhiteSpace(MinimumBindName);

        public int Maximum { get; set; }
        public string MaximumBindName { get; set; }
        public bool IsMaximumBound => !string.IsNullOrWhiteSpace(MaximumBindName);

        public int StepSize { get; set; }
        public string StepSizeBindName { get; set; }
        public bool IsStepSizeBound => !string.IsNullOrWhiteSpace(StepSizeBindName);

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
