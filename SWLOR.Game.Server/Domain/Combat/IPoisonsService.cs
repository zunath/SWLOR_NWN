using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using System;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>Consumes weapon poison coatings on weapon hits.</summary>
    public interface IPoisonsService
    {
        void ConsumePoisonCoating();

        int GetCoatingVenomDamagePerTick(int tier);

        float GetVenomDurationSeconds(int tier);
    }
}
