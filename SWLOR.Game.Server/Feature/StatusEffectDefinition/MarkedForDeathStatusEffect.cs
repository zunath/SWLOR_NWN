using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// The marker's next <see cref="AttackLimit"/> damaging hits on the target are amplified.
    /// The bonus is a percentage of each hit rather than a flat amount so it tracks the marker's
    /// own damage: a flat bonus tripled a level 1 droid's auto-attacks while barely registering
    /// on a level 50 elite.
    /// </summary>
    public sealed class MarkedForDeathStatusEffect : StatusEffectBase
    {
        public const int DamageTakenFromSourcePercent = 50;
        public const int AttackLimit = 3;

        private int _remainingAttacks = AttackLimit;

        public override string Name => "Marked for Death";
        public override EffectIconType Icon => EffectIconType.MarkedForDeathStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Trauma;
        public int RemainingAttacks => _remainingAttacks;

        public MarkedForDeathStatusEffect()
        {
            StatGroup.Stats[StatType.DamageTakenFromStatusSourcePercentAdjustment] = DamageTakenFromSourcePercent;
        }

        protected override void OnDamageTaken(uint defender, uint attacker, int damage, CombatDamageType damageType)
        {
            if (!GetIsObjectValid(Source))
                return;

            if (RegisterDamagingHit(attacker))
            {
                StatusEffect.RemoveStatusEffect(defender, typeof(MarkedForDeathStatusEffect), Source);
            }
        }

        /// <summary>
        /// Spends one charge when <paramref name="attacker"/> is the marker. Returns true when that
        /// hit used the last charge and the mark should be removed.
        /// </summary>
        public bool RegisterDamagingHit(uint attacker)
        {
            if (_remainingAttacks <= 0 || attacker != Source)
                return false;

            _remainingAttacks--;
            return _remainingAttacks <= 0;
        }
    }
}
