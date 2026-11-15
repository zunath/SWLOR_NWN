using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public abstract class SocialScalingStatusEffectBase : StatusEffectBase
    {
        protected int ScaleBySourceSocial(int baseValue, int maximumValue)
        {
            return AbilityEffectScaling.ScaleValueBySourceSocial(Source, baseValue, maximumValue);
        }
    }
}
