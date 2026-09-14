using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beastmaster
{
    internal static class BeastBreathVisuals
    {
        public static void Play(uint activator, uint target, Location targetLocation, VisualEffect visualEffect)
        {
            // Head-attached breath models follow the beast's facing; match the damage cone's aim.
            if (GetIsObjectValid(target) && target != activator)
                SetFacingPoint(GetPosition(target), activator);
            else if (GetIsObjectValid(GetAreaFromLocation(targetLocation)))
                SetFacingPoint(GetPositionFromLocation(targetLocation), activator);

            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(visualEffect), activator);
        }
    }
}
