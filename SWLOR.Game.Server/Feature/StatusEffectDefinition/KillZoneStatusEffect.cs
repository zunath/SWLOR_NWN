using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class KillZoneStatusEffect : StatusEffectBase
    {
        public override string Name => "Kill Zone";
        public override EffectIconType Icon => EffectIconType.KillZoneStatusEffect;
        public KillZoneStatusEffect()
        {
            StatGroup.Stats[StatType.RepeatedTargetDamageSkillType] = (int)SkillType.Rifle;
            StatGroup.Stats[StatType.RepeatedTargetDamagePercentPerHit] = 4;
            StatGroup.Stats[StatType.RepeatedTargetDamagePercentMax] = 20;
        }

    }
}
