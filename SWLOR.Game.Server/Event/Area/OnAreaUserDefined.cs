namespace SWLOR.Game.Server.Event.Area
{
    internal class OnAreaUserDefined: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            return true;

        }
    }
}
