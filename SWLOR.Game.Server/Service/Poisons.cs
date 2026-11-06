using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using System;

namespace SWLOR.Game.Server.Service
{
    /// <summary>Consumes weapon poison coatings on weapon hits. The logic lives in <see cref="IPoisonsService"/>; the event handlers stay here and every member forwards to the container's instance.</summary>
    public static class Poisons
    {
        public const string CoatingTierVariable = "POISON_COATING_TIER";
        public const string CoatingChargesVariable = "POISON_COATING_CHARGES";
        public const string CoatingPotencyVariable = "POISON_COATING_POTENCY";
        public const int InternalCooldownSeconds = 6;

        private static IPoisonsService Instance => FacadeAccessor.Resolve<IPoisonsService>();

        [NWNEventHandler(ScriptName.OnSWLORDamage)]
        public static void ConsumePoisonCoating() => Instance.ConsumePoisonCoating();

        public static int GetCoatingVenomDamagePerTick(int tier) => Instance.GetCoatingVenomDamagePerTick(tier);

        public static float GetVenomDurationSeconds(int tier) => Instance.GetVenomDurationSeconds(tier);
    }
}
