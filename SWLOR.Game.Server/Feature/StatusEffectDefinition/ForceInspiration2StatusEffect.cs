using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceInspiration2StatusEffect : AbilityEnhancementStatusEffectBase
    {
        public override string Name => "Force Inspiration II";
        public override EffectIconType Icon => EffectIconType.AbilityIncreaseWIS;

        public ForceInspiration2StatusEffect()
        {
            StatGroup.Abilities[AbilityType.Willpower] = 2;
            StatGroup.Abilities[AbilityType.Agility] = 2;
            StatGroup.Abilities[AbilityType.Might] = 2;
        }
    }
}
