namespace SWLOR.Game.Server.Scripting.Contracts
{
    public interface IScript
    {
        void SubscribeEvents();
        void UnsubscribeEvents();
        void Main();
    }
}
