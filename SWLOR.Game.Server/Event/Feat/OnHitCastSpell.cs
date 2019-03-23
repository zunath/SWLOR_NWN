using NWN;
using SWLOR.Game.Server.Event.Legacy;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Event.Feat
{
    public class OnHitCastSpell: IRegisteredEvent
    {
        
        private readonly IDurabilityService _durability;
        
        
        

        public OnHitCastSpell(
            
            IDurabilityService durability
            
            )
        {
            
            _durability = durability;
            
            
            
        }

        public bool Run(params object[] args)
        {
            NWPlayer player = (Object.OBJECT_SELF);

            if (player.IsValid)
            {
                _durability.OnHitCastSpell(player);
                AbilityService.OnHitCastSpell(player);
                SkillService.OnHitCastSpell(player);
                PerkService.OnHitCastSpell(player);
                HandleItemSpecificCastSpell();
            }
            return true;
        }
        
        private void HandleItemSpecificCastSpell()
        {
            NWObject oSpellOrigin = (_.GetSpellCastItem());
            // Item specific
            string script = oSpellOrigin.GetLocalString("JAVA_SCRIPT");

            if (!string.IsNullOrWhiteSpace(script))
            {
                App.RunEvent<LegacyJVMItemEvent>(script);
            }
        }
    }
}
