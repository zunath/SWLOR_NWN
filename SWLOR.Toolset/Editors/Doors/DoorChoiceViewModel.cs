using SWLOR.Toolset.Domain.Editors.Behaviors;

namespace SWLOR.Toolset.Editors.Doors
{
    /// <summary>One named option in an ordinary door choice row.</summary>
    public sealed class DoorChoiceViewModel
    {
        public BehaviorChoice Choice { get; }

        public long Value => Choice.Value;

        public string Display => Choice.Display;

        public DoorChoiceViewModel(BehaviorChoice choice)
        {
            Choice = choice;
        }

        public override string ToString() => Display;
    }
}
