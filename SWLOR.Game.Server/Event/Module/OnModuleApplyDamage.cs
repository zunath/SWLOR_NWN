using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Event.Module
{
    public class OnModuleApplyDamage: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            ModService.OnModuleApplyDamage();
            CombatService.OnModuleApplyDamage();
            return true;
        }
    }
}
