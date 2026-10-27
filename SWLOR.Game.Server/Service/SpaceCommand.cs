using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// What a pilot's Command perks add to a ship (plan section 6, Appendix A), ported from the ground Leadership
    /// Vanguard Command and Field Steward lines: the party ships and enemy ships within command range, the
    /// duration and range Command Presence adds, and Mark Target. Mark Target, Command Presence, Repair Efficiency
    /// and Crew Morale set space-only stats (<c>Space*</c>), because the ground stats of the same perks add up with
    /// a pilot's ground Leadership and First Aid perks.
    /// </summary>
    public static class SpaceCommand
    {
        private static readonly Dictionary<uint, DateTime> _markTargetReadyAt = new();
        private static bool _rangeBonusRegistered;

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void Subscribe()
        {
            SpaceCombatEvents.HostileAbilityUsed -= OnHostileAbilityUsed;
            SpaceCombatEvents.HostileAbilityUsed += OnHostileAbilityUsed;

            if (_rangeBonusRegistered)
                return;

            _rangeBonusRegistered = true;
            SpaceAuras.RegisterRangeBonus(GetRangeBonus);
        }

        // Leadership

        /// <summary>The highest Leadership rank.</summary>
        public const int LeadershipMaximumRank = 50;

        /// <summary>
        /// What Leadership 50 adds to the Command style: the relative bonus SOC 26 gives the ground Leadership orders
        /// over SOC 10 (Press the Attack II +8% to +10%, Decisive Command +12% to +15%, Mark Target +8% to +10%, the
        /// ground's direct-effect stat bonus of 25%). The ground scales Leadership by SOC only; space adds the rank on top.
        /// </summary>
        public const int LeadershipMaximumBonusPercent = 25;

        /// <summary>
        /// What Leadership 50 adds to the effectiveness of the crew aboard. The balance model (plan section 10) puts
        /// a player crew at 100%: a crewed Cruiser at 100% already matches five separate Fighters against a T5 Boss
        /// capital (94 s against 91 s), and at 105% it is level with them (90 s), so Leadership's share stops there.
        /// </summary>
        public const int LeadershipCrewMaximumBonusPercent = 5;

        /// <summary>
        /// The percent by which the captain's Leadership rank strengthens the Command style: its command range and
        /// the orders' and auras' strength (plan section 10). The captain is the pilot; an NPC ship has none.
        /// </summary>
        public static int GetLeadershipBonusPercent(uint ship) => LeadershipBonusPercent(GetCaptainLeadershipRank(ship));

        /// <summary>The percent the captain's Leadership rank adds to the effectiveness of the crew aboard.</summary>
        public static int GetLeadershipCrewBonusPercent(uint ship) => LeadershipCrewBonusPercent(GetCaptainLeadershipRank(ship));

        private static int GetCaptainLeadershipRank(uint ship)
        {
            if (!GetIsObjectValid(ship) || !GetIsPC(ship) || GetIsDM(ship))
                return 0;

            var dbPlayer = DB.Get<Player>(GetObjectUUID(ship));
            return dbPlayer?.Skills != null && dbPlayer.Skills.TryGetValue(SkillType.Leadership, out var leadership)
                ? leadership.Rank
                : 0;
        }

        /// <summary>The Leadership bonus to orders, auras and range for a rank: +0.5% per rank, up to +25% at rank 50.</summary>
        public static int LeadershipBonusPercent(int leadershipRank) => ScaleByLeadershipRank(leadershipRank, LeadershipMaximumBonusPercent);

        /// <summary>The Leadership bonus to crew effectiveness for a rank: +1% per 10 ranks, up to +5% at rank 50.</summary>
        public static int LeadershipCrewBonusPercent(int leadershipRank) => ScaleByLeadershipRank(leadershipRank, LeadershipCrewMaximumBonusPercent);

        private static int ScaleByLeadershipRank(int leadershipRank, int maximumPercent) =>
            (int)Math.Round(
                Math.Clamp(leadershipRank, 0, LeadershipMaximumRank) * maximumPercent / (double)LeadershipMaximumRank,
                MidpointRounding.AwayFromZero);

        /// <summary>
        /// A Command status value raised by its source's Leadership bonus when the source is a ship in space, so the
        /// orders and auras the ground scales by SOC alone (Focus Fire, Formation, Fleet Action) also scale with the
        /// captain's Leadership rank. Ground values are left alone.
        /// </summary>
        public static int ApplyLeadershipScaling(uint source, int value)
        {
            if (value == 0 || source == 0 || source == OBJECT_INVALID || !Space.IsShipInSpace(source))
                return value;

            return SpaceCombatMath.ScaleByPercent(value, GetLeadershipBonusPercent(source));
        }

        // Range

        private static float GetRangeBonus(uint ship)
        {
            return Stat.GetStatAdjustment(ship, StatType.SpaceCommandRangeBonusMeters) +
                   SpaceRadii.CommandRange * GetLeadershipBonusPercent(ship) / 100f;
        }

        /// <summary>The ships in the commander's party within command range, the commander's own ship included.</summary>
        public static List<uint> GetPartyShipsInRange(uint commander)
        {
            return Party.GetAllPartyMembersWithinRange(commander, SpaceAuras.GetCommandRange(commander))
                .Where(x => Space.IsShipInSpace(x) && !GetIsDead(x))
                .ToList();
        }

        /// <summary>The enemy ships within command range of the commander's ship.</summary>
        public static List<uint> GetEnemyShipsInRange(uint commander)
        {
            return Space.GetEnemyShipsNear(commander, GetLocation(commander), SpaceAuras.GetCommandRange(commander));
        }

        /// <summary>Whether the target is the commander's own ship or a party ship within command range.</summary>
        public static bool IsPartyShipInRange(uint commander, uint target)
        {
            if (!GetIsObjectValid(target) || !Space.IsShipInSpace(target) || GetIsDead(target))
                return false;

            if (target == commander)
                return true;

            return Party.IsInParty(commander, target) &&
                   GetArea(commander) == GetArea(target) &&
                   GetDistanceBetween(commander, target) <= SpaceAuras.GetCommandRange(commander);
        }

        // Orders

        /// <summary>
        /// How long a non-capstone order lasts: the base seconds plus Command Presence's bonus, which Social raises
        /// toward its maximum.
        /// </summary>
        public static float ApplyDurationBonus(uint commander, float seconds)
        {
            var baseBonus = Stat.GetStatAdjustment(commander, StatType.SpaceCommandDurationBonusBaseSeconds);
            var maximumBonus = Stat.GetStatAdjustment(commander, StatType.SpaceCommandDurationBonusMaximumSeconds);
            if (maximumBonus <= 0)
                return seconds;

            var bonus = maximumBonus > baseBonus
                ? AbilityEffectScaling.ScaleValueBySourceSocial(commander, baseBonus, maximumBonus)
                : baseBonus;
            return seconds + bonus;
        }

        /// <summary>
        /// Gives a party order's status to every party ship within command range, the commander's own included.
        /// Returns the ships that took it. The status scales with the commander's Social, as the ground orders do.
        /// </summary>
        public static List<uint> ApplyOrder(uint commander, Type statusEffect, float seconds, bool capstone)
        {
            var duration = capstone ? seconds : ApplyDurationBonus(commander, seconds);
            var affected = new List<uint>();

            foreach (var ship in GetPartyShipsInRange(commander))
            {
                if (StatusEffect.ApplyStatusEffect(commander, ship, statusEffect, duration))
                    Ability.PlaySuccessfulImpactVisualEffect(commander, ship);

                // Command's crew bonuses reach everyone aboard (plan section 10): crew members fire the ship's
                // station modules with their own stats.
                foreach (var crewMember in SpaceCrew.GetCrew(ship))
                    StatusEffect.ApplyStatusEffect(commander, crewMember, statusEffect, duration);

                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Holy_Aid), ship);
                affected.Add(ship);
            }

            if (affected.Count > 0)
                CombatPoint.AddSupportCombatPoint(commander, affected, SkillType.Piloting, capstone ? 3 : 2);

            return affected;
        }

        // Mark Target

        private static void OnHostileAbilityUsed(SpaceAbilityEvent e)
        {
            if (e.Impact == null || e.Impact.AttributedDamage <= 0)
                return;

            TryMarkTarget(e.Activator);
        }

        /// <summary>
        /// Mark Target: when a hostile ability damages or debuffs an enemy, the party ships within command range
        /// gain a damage (and later Accuracy) bonus. It triggers once per ability use, at most once per cooldown. An
        /// ability that damages is seen from the event for every hostile space ability; one that only debuffs calls
        /// this itself.
        /// </summary>
        public static void TryMarkTarget(uint activator)
        {
            if (!GetIsObjectValid(activator) || !Space.IsShipInSpace(activator))
                return;

            var baseDamage = Stat.GetStatAdjustment(activator, StatType.SpaceMarkTargetDamagePercent);
            var baseAccuracy = Stat.GetStatAdjustment(activator, StatType.SpaceMarkTargetAccuracyPercent);
            var baseDuration = Stat.GetStatAdjustment(activator, StatType.SpaceMarkTargetDurationSeconds);
            var cooldown = Stat.GetStatAdjustment(activator, StatType.SpaceMarkTargetCooldownSeconds);
            var nameStrRef = Stat.GetStatAdjustment(activator, StatType.SpaceMarkTargetNameStrRef);
            var icon = (EffectIconType)Stat.GetStatAdjustment(activator, StatType.SpaceMarkTargetIcon);
            if ((baseDamage <= 0 && baseAccuracy <= 0) || baseDuration <= 0 || cooldown <= 0 ||
                nameStrRef <= 0 || icon == EffectIconType.Invalid)
            {
                return;
            }

            var now = DateTime.UtcNow;
            if (_markTargetReadyAt.TryGetValue(activator, out var readyAt) && !IsMarkTargetReady(readyAt, now))
                return;

            _markTargetReadyAt[activator] = now.AddSeconds(cooldown);

            var leadership = GetLeadershipBonusPercent(activator);
            var damagePercent = SpaceCombatMath.ScaleByPercent(
                AbilityEffectScaling.ScaleValueBySourceSocial(
                    activator, baseDamage, Stat.GetStatAdjustment(activator, StatType.SpaceMarkTargetMaximumDamagePercent)),
                leadership);
            var accuracyPercent = SpaceCombatMath.ScaleByPercent(
                AbilityEffectScaling.ScaleValueBySourceSocial(
                    activator, baseAccuracy, Stat.GetStatAdjustment(activator, StatType.SpaceMarkTargetMaximumAccuracyPercent)),
                leadership);
            var duration = ApplyDurationBonus(activator, baseDuration);

            foreach (var ship in GetPartyShipsInRange(activator))
            {
                StatusEffect.ApplyStatusEffect(
                    activator,
                    ship,
                    new HostileAbilityPartyBuffStatusEffect(damagePercent, accuracyPercent, nameStrRef, icon),
                    duration);

                foreach (var crewMember in SpaceCrew.GetCrew(ship))
                {
                    StatusEffect.ApplyStatusEffect(
                        activator,
                        crewMember,
                        new HostileAbilityPartyBuffStatusEffect(damagePercent, accuracyPercent, nameStrRef, icon),
                        duration);
                }
            }
        }

        /// <summary>Whether Mark Target's cooldown, which began at <paramref name="readyAt"/> minus its length, has ended.</summary>
        public static bool IsMarkTargetReady(DateTime readyAt, DateTime now) => now >= readyAt;

        /// <summary>Forgets the pilot's Mark Target cooldown when the pilot leaves space.</summary>
        public static void Clear(uint ship)
        {
            _markTargetReadyAt.Remove(ship);
        }
    }
}
