using SWLOR.Toolset.Domain.Editors.Behaviors;

namespace SWLOR.Toolset.Editors.Waypoints
{
    public sealed class WaypointChoiceViewModel
    {
        public BehaviorChoice Choice { get; }

        public long Value => Choice.Value;

        public string? StringValue => Choice.StringValue;

        public string Display => Choice.Display;

        public WaypointChoiceViewModel(BehaviorChoice choice)
        {
            Choice = choice;
        }

        public override string ToString() => Display;
    }
}
