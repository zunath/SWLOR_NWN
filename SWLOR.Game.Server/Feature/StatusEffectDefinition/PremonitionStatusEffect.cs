using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public class PremonitionStatusEffect: IStatusEffectListDefinition
    {
        private readonly StatusEffectBuilder _builder = new();

        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            Premonition1();
            Premonition2();

            return _builder.Build();
        }

        private void Impact(uint source, int amount)
        {
            foreach (var member in Party.GetAllPartyMembersWithinRange(source, 10f))
            {
                // Caster does not get this bonus.
                if (source == member)
                    continue;

                var effect = EffectLinkEffects(
                    EffectConcealment(amount), 
                    EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Blue_Yellow));
                ApplyEffectToObject(DurationType.Temporary, effect, member, 6.1f);
            }

            Enmity.ModifyEnmityOnAll(source, 50 * amount);
            CombatPoint.AddCombatPointToAllTagged(source, SkillType.Force);
        }

        private void Premonition1()
        {
            _builder.Create(StatusEffectType.Premonition1)
                .Name("Premonition I")
                .EffectIcon(EffectIconType.ImmunityMind)
                .CannotReplace(StatusEffectType.Premonition2)
                .GrantAction((source, target, length, effectData) =>
                {
                    Impact(source, 15);
                })
                .TickAction((source, target, effectData) =>
                {
                    Impact(source, 15);
                });
        }

        private void Premonition2()
        {
            _builder.Create(StatusEffectType.Premonition2)
                .Name("Premonition II")
                .EffectIcon(EffectIconType.ImmunityMind)
                .Replaces(StatusEffectType.Premonition1)
                .GrantAction((source, target, length, effectData) =>
                {
                    Impact(source, 30);
                })
                .TickAction((source, target, effectData) =>
                {
                    Impact(source, 30);
                });
        }
    }
}
