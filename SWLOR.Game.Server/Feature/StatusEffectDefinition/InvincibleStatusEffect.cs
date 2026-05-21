using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class InvincibleStatusEffect : StatusEffectBase
    {
        public override string Name => "Invincible";
        public override EffectIconType Icon => EffectIconType.InvincibleStatusEffect;

        public InvincibleStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment] = -50;
        }
    }
}
