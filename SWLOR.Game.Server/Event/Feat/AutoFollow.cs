namespace SWLOR.Game.Server.Event.Feat
{
    public class AutoFollow: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            return true;
        }
    }
}
