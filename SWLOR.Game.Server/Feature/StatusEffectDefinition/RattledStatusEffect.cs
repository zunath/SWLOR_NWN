using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class RattledStatusEffect : StatusEffectBase
    {
        public override string Name => "Rattled";
        public override StatusEffectActivationType ActivationType => StatusEffectActivationType.Passive;
        public override EffectIconType Icon => EffectIconType.RattledStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Trauma;
        public override StatusEffectStackType StackingType => StatusEffectStackType.StackFromMultipleSources;
        public override bool PersistsOnLogout => false;

        public RattledStatusEffect() : this(10) { }

        public RattledStatusEffect(int penalty)
        {
            StatGroup.Stats[StatType.OrdinaryWeaponAccuracyPenalty] = penalty;
        }

        public override IStatusEffect Clone() => new RattledStatusEffect(StatGroup.Stats[StatType.OrdinaryWeaponAccuracyPenalty]);
    }
}
