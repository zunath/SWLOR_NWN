using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class Antitoxin1StatusEffect : StatusEffectBase
    {
        public override string Name => "Antitoxin";
        public override EffectIconType Icon => EffectIconType.Antitoxin1StatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public Antitoxin1StatusEffect()
        {
            StatGroup.Resists[ResistanceType.Poison] = 50;
        }
    }
}
