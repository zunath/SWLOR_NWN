namespace SWLOR.Game.Server.Threading.Contracts
{
    public interface IBackgroundThread
    {
        void Start();
        void Exit();
    }
}
