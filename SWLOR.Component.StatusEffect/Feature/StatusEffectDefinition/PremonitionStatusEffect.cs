using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.Component.StatusEffect.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Combat.ValueObjects;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Domain.Skill.Enums;
using SWLOR.Shared.Domain.StatusEffect.Enums;
using SWLOR.Shared.Domain.StatusEffect.ValueObjects;

namespace SWLOR.Component.StatusEffect.Feature.StatusEffectDefinition
{
    public class PremonitionStatusEffect: IStatusEffectListDefinition
    {
        private readonly StatusEffectBuilder _builder = new();
        private readonly ICombatPointService _combatPointService;
        private readonly IEnmityService _enmityService;
        private readonly IPartyService _partyService;

        public PremonitionStatusEffect(
            ICombatPointService combatPointService, 
            IEnmityService enmityService,
            IPartyService partyService)
        {
            _combatPointService = combatPointService;
            _enmityService = enmityService;
            _partyService = partyService;
        }

        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            Premonition1();
            Premonition2();

            return _builder.Build();
        }

        private void Impact(uint source, int amount)
        {
            foreach (var member in _partyService.GetAllPartyMembersWithinRange(source, 10f))
            {
                // Caster does not get this bonus.
                if (source == member)
                    continue;

                var effect = EffectLinkEffects(
                    EffectConcealment(amount), 
                    EffectVisualEffect(VisualEffectType.Vfx_Dur_Aura_Pulse_Blue_Yellow));
                ApplyEffectToObject(DurationType.Temporary, effect, member, 6.1f);
            }

            _enmityService.ModifyEnmityOnAll(source, 50 * amount);
            _combatPointService.AddCombatPointToAllTagged(source, SkillType.Force);
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
                    Impact(source, 25);
                })
                .TickAction((source, target, effectData) =>
                {
                    Impact(source, 25);
                });
        }
    }
}
