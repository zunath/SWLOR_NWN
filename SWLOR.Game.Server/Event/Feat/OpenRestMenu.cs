namespace SWLOR.Game.Server.Event.Feat
{
    public class OpenRestMenu: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            return true;
        }
    }
}
