using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class RejuvenationStatusEffect : AuraStatusEffectBase
    {
        public override string Name => "Rejuvenation";
        public override EffectIconType Icon => EffectIconType.Rejuvenation;
        public override float Frequency => 6f;

        protected override void Tick(uint creature)
        {
            var level = Perk.GetPerkLevel(Source, PerkType.Rejuvenation);
            Stat.RestoreStamina(creature, level);
        }
    }
}
