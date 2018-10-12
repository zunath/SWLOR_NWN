namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IMapService
    {
        void OnAreaEnter();
        void OnAreaExit();
        void OnModuleLeave();
    }
}