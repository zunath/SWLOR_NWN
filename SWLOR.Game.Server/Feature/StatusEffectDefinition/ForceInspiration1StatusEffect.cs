using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceInspiration1StatusEffect : AbilityEnhancementStatusEffectBase
    {
        public override string Name => "Force Inspiration I";
        public override EffectIconType Icon => EffectIconType.AbilityIncreaseWIS;

        public ForceInspiration1StatusEffect()
        {
            StatGroup.Abilities[AbilityType.Willpower] = 1;
            StatGroup.Abilities[AbilityType.Agility] = 1;
            StatGroup.Abilities[AbilityType.Might] = 1;
        }
    }
}
