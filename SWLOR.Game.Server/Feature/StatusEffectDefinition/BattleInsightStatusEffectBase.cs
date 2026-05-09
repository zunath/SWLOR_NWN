using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public abstract class BattleInsightStatusEffectBase : StatusEffectBase
    {
        public override EffectIconType Icon => EffectIconType.Dazed;
        public override float Frequency => 6f;
        public override bool PersistsOnLogout => false;

        protected abstract int EnmityAmount { get; }
        protected abstract Type SelfModifierStatusEffectType { get; }
        protected abstract Type PartyModifierStatusEffectType { get; }

        protected override void Apply(uint creature, int durationTicks)
        {
            ApplyPulse();
        }

        protected override void Tick(uint creature)
        {
            if (IsFlaggedForRemoval)
                return;

            ApplyPulse();
        }

        private void ApplyPulse()
        {
            StatusEffect.ApplyStatusEffect(Source, Source, SelfModifierStatusEffectType, 6f);

            var party = Party.GetAllPartyMembersWithinRange(Source, RadiusSize.Medium);

            foreach (var player in party)
            {
                if (player == Source)
                    continue;

                StatusEffect.ApplyStatusEffect(Source, player, PartyModifierStatusEffectType, 6f);
            }

            Enmity.ModifyEnmityOnAll(Source, EnmityAmount);
            CombatPoint.AddCombatPointToAllTagged(Source, SkillType.Force, 3);
        }
    }
}
