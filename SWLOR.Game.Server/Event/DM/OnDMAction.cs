namespace SWLOR.Game.Server.NWN.Events.DM
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
