namespace SWLOR.Game.Server.Event.Player
{
    public class OnPlayerHeartbeat: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            return true;
        }
    }
}
