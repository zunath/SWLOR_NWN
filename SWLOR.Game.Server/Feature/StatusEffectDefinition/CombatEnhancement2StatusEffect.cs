using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CombatEnhancement2StatusEffect : AbilityEnhancementStatusEffectBase
    {
        public override string Name => "Combat Enhancement II";
        public override EffectIconType Icon => EffectIconType.AbilityIncreaseSTR;

        public CombatEnhancement2StatusEffect()
        {
            StatGroup.Abilities[AbilityType.Might] = 2;
            StatGroup.Abilities[AbilityType.Perception] = 2;
            StatGroup.Abilities[AbilityType.Vitality] = 2;
        }
    }
}
