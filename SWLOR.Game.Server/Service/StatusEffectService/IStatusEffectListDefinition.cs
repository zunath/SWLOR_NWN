using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.StatusEffectService
{
    public interface IStatusEffectListDefinition
    {
        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects();
    }
}
