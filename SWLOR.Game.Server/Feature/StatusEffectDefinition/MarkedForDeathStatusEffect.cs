using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class MarkedForDeathStatusEffect : StatusEffectBase
    {
        private const int DamageBonus = 12;
        private const int AttackLimit = 3;

        private int _remainingAttacks = AttackLimit;
        private bool _isApplyingBonusDamage;

        public override string Name => "Marked for Death";
        public override EffectIconType Icon => EffectIconType.MarkedForDeathStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Trauma;
        public int RemainingAttacks => _remainingAttacks;

        protected override void OnDamageTaken(uint defender, uint attacker, int damage, CombatDamageType damageType)
        {
            if (_isApplyingBonusDamage ||
                _remainingAttacks <= 0 ||
                attacker != Source)
            {
                return;
            }

            _remainingAttacks--;
            AssignCommand(Source, () =>
            {
                _isApplyingBonusDamage = true;
                ApplyEffectToObject(DurationType.Instant, EffectDamage(DamageBonus), defender);
                _isApplyingBonusDamage = false;
            });

            if (_remainingAttacks <= 0)
            {
                StatusEffect.RemoveStatusEffect(defender, typeof(MarkedForDeathStatusEffect), Source);
            }
        }
    }
}
