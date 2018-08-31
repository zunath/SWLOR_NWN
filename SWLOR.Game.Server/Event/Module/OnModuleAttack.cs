namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleAttack : IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            return true;
        }
    }
}
