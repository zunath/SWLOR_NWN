// ReSharper disable once CheckNamespace
namespace NWN
{
    public partial class NWScript : INWScript
    {
        public void AssignCommand(Object oActionSubject, ActionDelegate aActionToAssign)
        {
            Internal.ClosureAssignCommand(oActionSubject, aActionToAssign);
        }

        public void DelayCommand(float fSeconds, ActionDelegate aActionToDelay)
        {
            Internal.ClosureDelayCommand(Object.OBJECT_SELF, fSeconds, aActionToDelay);
        }

        public void ActionDoCommand(ActionDelegate aActionToDelay)
        {
            Internal.ClosureActionDoCommand(Object.OBJECT_SELF, aActionToDelay);
        }
    }
}
