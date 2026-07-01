using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum.Associate;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public abstract class BeastBondStatusEffect : StatusEffectBase
    {
        private const float BeastRange = 15f;

        private uint _linkedBeast = OBJECT_INVALID;

        protected abstract Type BeastStatusEffectType { get; }

        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;

        public override float Frequency => 1f;

        protected override void Apply(uint creature, int durationTicks)
        {
            SyncBeastStatus(creature);
        }

        protected override void Reapply(uint creature)
        {
            SyncBeastStatus(creature);
        }

        protected override void Tick(uint creature)
        {
            SyncBeastStatus(creature);
        }

        protected override void Remove(uint creature)
        {
            RemoveLinkedBeastStatus();

            var currentBeast = GetAssociate(AssociateType.Henchman, creature);
            if (BeastMastery.IsPlayerBeast(currentBeast))
            {
                StatusEffect.RemoveStatusEffect(currentBeast, BeastStatusEffectType, Source, false);
            }
        }

        private void SyncBeastStatus(uint player)
        {
            var beast = GetAssociate(AssociateType.Henchman, player);
            var hasValidBeast = BeastMastery.IsPlayerBeast(beast) &&
                                GetDistanceBetween(beast, player) < BeastRange;

            if (!hasValidBeast)
            {
                RemoveLinkedBeastStatus();
                return;
            }

            if (_linkedBeast != beast)
            {
                RemoveLinkedBeastStatus();
                _linkedBeast = beast;
            }

            if (!StatusEffect.HasStatusEffect(beast, BeastStatusEffectType, player))
            {
                StatusEffect.ApplyStatusEffect(player, beast, BeastStatusEffectType, 0f);
            }
        }

        private void RemoveLinkedBeastStatus()
        {
            if (!GetIsObjectValid(_linkedBeast))
            {
                _linkedBeast = OBJECT_INVALID;
                return;
            }

            StatusEffect.RemoveStatusEffect(_linkedBeast, BeastStatusEffectType, Source, false);
            _linkedBeast = OBJECT_INVALID;
        }
    }
}
