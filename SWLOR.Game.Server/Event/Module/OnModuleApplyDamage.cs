using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Module
{
    public class OnModuleApplyDamage: IRegisteredEvent
    {
        private readonly IModService _mod;
        private readonly ICombatService _combat;

        public OnModuleApplyDamage(
            IModService mod,
            ICombatService combat)
        {
            _mod = mod;
            _combat = combat;
        }

        public bool Run(params object[] args)
        {
            _mod.OnModuleApplyDamage();
            _combat.OnModuleApplyDamage();
            return true;
        }
    }
}
