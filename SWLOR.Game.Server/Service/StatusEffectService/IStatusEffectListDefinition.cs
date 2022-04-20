using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.StatusEffectService
{
    public interface IStatusEffectListDefinition
    {
        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects();
    }
}
