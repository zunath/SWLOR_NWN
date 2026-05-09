using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public abstract class PremonitionStatusEffectBase : StatusEffectBase
    {
        public override EffectIconType Icon => EffectIconType.ImmunityMind;
        public override float Frequency => 6f;
        public override bool PersistsOnLogout => false;

        protected abstract int Concealment { get; }

        protected override void Apply(uint creature, int durationTicks)
        {
            Impact();
        }

        protected override void Tick(uint creature)
        {
            Impact();
        }

        private void Impact()
        {
            foreach (var member in Party.GetAllPartyMembersWithinRange(Source, 10f))
            {
                if (Source == member)
                    continue;

                var effect = EffectLinkEffects(
                    EffectConcealment(Concealment),
                    EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Blue_Yellow));
                ApplyEffectToObject(DurationType.Temporary, effect, member, 6.1f);
            }

            Enmity.ModifyEnmityOnAll(Source, 50 * Concealment);
            CombatPoint.AddCombatPointToAllTagged(Source, SkillType.Force);
        }
    }
}
