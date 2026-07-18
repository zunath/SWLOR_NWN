using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Active while a player moves in stealth. Carries the Stealth perk rank's effectiveness bonus,
    /// drains stamina every tick (stealth breaks when stamina runs out), and awards Espionage XP for
    /// time spent hidden near hostile NPCs.
    /// </summary>
    public sealed class StealthStatusEffect : StatusEffectBase
    {
        private const int StaminaDrainPerTick = 2;
        private const float BaseFrequencySeconds = 6f;

        // XP is granted once per detection window (30s) rather than every tick, so the reward is
        // paced to "another check survived while hidden" rather than raw seconds in stealth.
        private const int TicksPerXpGrant = 5;
        private const float HostileScanRadiusMeters = 15f;
        private const string TickCounterVariable = "ESPIONAGE_STEALTH_XP_TICKS";

        private float _frequency = BaseFrequencySeconds;

        public override string Name => "Stealth";
        public override EffectIconType Icon => EffectIconType.StealthStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override float Frequency => _frequency;
        public override bool PersistsOnLogout => false;

        protected override void Apply(uint creature, int durationTicks)
        {
            DeleteLocalInt(creature, TickCounterVariable);

            // Drain-slowing stats stretch the tick interval rather than shrinking the
            // per-tick amount so small reductions are not lost to integer rounding.
            var drainReduction = Stat.GetStatAdjustment(creature, StatType.StealthStaminaDrainReductionPercent);
            if (drainReduction > 0)
            {
                _frequency = BaseFrequencySeconds * (100 + drainReduction) / 100f;
            }
        }

        protected override void Tick(uint creature)
        {
            if (!GetIsPC(creature) || GetIsDM(creature))
                return;

            GrantTimeInStealthXP(creature);

            Stat.ReduceStamina(creature, StaminaDrainPerTick);

            if (Stat.GetCurrentStamina(creature) > 0)
                return;

            SendMessageToPC(creature, ColorToken.Red("You are too exhausted to remain hidden."));
            AssignCommand(creature, () =>
            {
                SetActionMode(creature, ActionMode.Stealth, false);
            });
        }

        /// <summary>
        /// Awards Espionage XP for staying hidden near hostiles. Only non-player hostiles count, so
        /// the grant cannot be farmed off another player, and the amount is a level-vs-rank delta so
        /// sneaking past trivial enemies stops paying once the skill outgrows them.
        /// </summary>
        private static void GrantTimeInStealthXP(uint creature)
        {
            var ticks = GetLocalInt(creature, TickCounterVariable) + 1;
            if (ticks < TicksPerXpGrant)
            {
                SetLocalInt(creature, TickCounterVariable, ticks);
                return;
            }

            DeleteLocalInt(creature, TickCounterVariable);

            var highestHostileLevel = CombatAreaPulses
                .GetHostileCreatures(creature, GetLocation(creature), HostileScanRadiusMeters)
                .Where(hostile => !GetIsPC(hostile) && !GetIsDM(hostile))
                .Select(hostile => Stat.GetNPCStats(hostile).Level)
                .DefaultIfEmpty(0)
                .Max();

            if (highestHostileLevel <= 0)
                return;

            var playerId = GetObjectUUID(creature);
            var dbPlayer = DB.Get<Player>(playerId);
            var rank = dbPlayer.Skills[SkillType.Espionage].Rank;
            var xp = Skill.GetDeltaXP(highestHostileLevel - rank);

            if (xp <= 0)
                return;

            Skill.GiveSkillXP(creature, SkillType.Espionage, xp, false, false);
        }
    }
}
