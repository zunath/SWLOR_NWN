namespace SWLOR.Game.Server.Event.DM
{
    public class OnDMAction
    {
        public int ActionID { get; set; }

        public OnDMAction(int actionID)
        {
            ActionID = actionID;
        }
    }
}
