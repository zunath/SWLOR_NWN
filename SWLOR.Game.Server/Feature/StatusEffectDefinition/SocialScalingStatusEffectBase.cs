using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public abstract class SocialScalingStatusEffectBase : StatusEffectBase
    {
        protected int ScaleBySourceSocial(int baseValue, int maximumValue)
        {
            // A ship in space also scales the value by its captain's Leadership rank (plan section 10).
            return SpaceCommand.ApplyLeadershipScaling(
                Source,
                AbilityEffectScaling.ScaleValueBySourceSocial(Source, baseValue, maximumValue));
        }
    }
}
