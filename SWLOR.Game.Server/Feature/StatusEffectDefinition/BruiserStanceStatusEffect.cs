using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BruiserStanceStatusEffect : StatusEffectBase, IEquipmentBoundStatusEffect
    {
        public override string Name => "Bruiser Stance";
        public override EffectIconType Icon => EffectIconType.BruiserStanceStatusEffect;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;

        public bool HasValidEquipment(uint creature) => EquipmentPredicates.HasUnarmedGrip(creature);

        public BruiserStanceStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = 10;
            StatGroup.Stats[StatType.TechniqueLine] = 2;
            StatGroup.Stats[StatType.TechniqueSetupBraceWindowSeconds] = 4;
            StatGroup.Stats[StatType.TechniqueSetupRetaliationPower] = 8;
            StatGroup.Stats[StatType.TechniqueSetupRetaliationCooldownSeconds] = 18;
        }
    }
}
