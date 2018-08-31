namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleUserDefined : IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            return true;

        }
    }
}
