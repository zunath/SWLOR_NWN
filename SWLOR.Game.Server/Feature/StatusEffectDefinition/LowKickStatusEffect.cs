using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class LowKickStatusEffect : StatusEffectBase
    {
        public override string Name => "Low Kick";
        public override StatusEffectActivationType ActivationType => StatusEffectActivationType.Passive;
        public override EffectIconType Icon => EffectIconType.LowKickStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Trauma;
        public override StatusEffectStackType StackingType => StatusEffectStackType.StackFromMultipleSources;
        public override bool PersistsOnLogout => false;

        public LowKickStatusEffect() : this(10) { }

        public LowKickStatusEffect(int penalty)
        {
            StatGroup.Stats[StatType.OrdinaryPhysicalDamagePenaltyPercent] = penalty;
        }

        public override IStatusEffect Clone() => new LowKickStatusEffect(StatGroup.Stats[StatType.OrdinaryPhysicalDamagePenaltyPercent]);
    }
}
