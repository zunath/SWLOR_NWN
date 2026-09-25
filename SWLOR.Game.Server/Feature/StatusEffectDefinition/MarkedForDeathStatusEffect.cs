using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class MarkedForDeathStatusEffect : StatusEffectBase
    {
        public const int MaxDamageBonus = 12;
        public const int MinNPCDamageBonus = 2;
        private const int FullDamageBonusNPCLevel = 50;
        private const int AttackLimit = 3;

        private int _remainingAttacks = AttackLimit;
        private bool _isApplyingBonusDamage;

        public override string Name => "Marked for Death";
        public override EffectIconType Icon => EffectIconType.MarkedForDeathStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Trauma;
        public int RemainingAttacks => _remainingAttacks;

        private int DamageBonus => GetIsPC(Source)
            ? MaxDamageBonus
            : GetNPCDamageBonus(Stat.GetNPCStats(Source).Level);

        /// <summary>
        /// NPC marks scale with the source's level so a flat bonus tuned for level 50 cannot
        /// remove most of a new character's HP. Sources without an NPC level keep the full bonus.
        /// </summary>
        public static int GetNPCDamageBonus(int npcLevel)
        {
            if (npcLevel <= 0 || npcLevel >= FullDamageBonusNPCLevel)
                return MaxDamageBonus;

            var bonus = (int)Math.Ceiling(npcLevel * MaxDamageBonus / (float)FullDamageBonusNPCLevel);
            return Math.Clamp(bonus, MinNPCDamageBonus, MaxDamageBonus);
        }

        protected override void OnDamageTaken(uint defender, uint attacker, int damage, CombatDamageType damageType)
        {
            if (_isApplyingBonusDamage ||
                _remainingAttacks <= 0 ||
                !GetIsObjectValid(Source) ||
                attacker != Source)
            {
                return;
            }

            _remainingAttacks--;
            _isApplyingBonusDamage = true;
            try
            {
                Combat.ApplyTriggeredDamage(Source, defender, DamageBonus, damageType);
            }
            finally
            {
                _isApplyingBonusDamage = false;
            }

            if (_remainingAttacks <= 0)
            {
                StatusEffect.RemoveStatusEffect(defender, typeof(MarkedForDeathStatusEffect), Source);
            }
        }
    }
}
