using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CombatEnhancement3StatusEffect : AbilityEnhancementStatusEffectBase
    {
        public override string Name => "Combat Enhancement III";
        public override EffectIconType Icon => EffectIconType.AbilityIncreaseSTR;

        public CombatEnhancement3StatusEffect()
        {
            StatGroup.Abilities[AbilityType.Might] = 3;
            StatGroup.Abilities[AbilityType.Perception] = 3;
            StatGroup.Abilities[AbilityType.Vitality] = 3;
        }
    }
}
