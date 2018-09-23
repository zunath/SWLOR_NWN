namespace SWLOR.Game.Server.Event
{
    public interface IRegisteredEvent
    {
        bool Run(params object[] args);
    }
}
