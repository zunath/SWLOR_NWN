using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class TreatmentKit3StatusEffect : StatusEffectBase
    {
        public override string Name => "Ailment Resistance";
        public override EffectIconType Icon => EffectIconType.TreatmentKit3StatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public TreatmentKit3StatusEffect()
        {
            StatGroup.Resists[ResistanceType.Fire] = 50;
            StatGroup.Resists[ResistanceType.Poison] = 50;
            StatGroup.Resists[ResistanceType.Electrical] = 50;
            StatGroup.Resists[ResistanceType.Ice] = 50;
            StatGroup.Resists[ResistanceType.Trauma] = 50;
        }
    }
}
