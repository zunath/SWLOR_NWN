using System.Collections.Generic;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Droid crew numbers and rules (plan section 10, "Droid crew"). Every constant names its source.
    /// </summary>
    public static class DroidCrewRules
    {
        public const int MinimumTier = 1;
        public const int MaximumTier = 5;

        /// <summary>
        /// A droid with no Industry (any combat droid) at any seat. Plan section 10, "Droid seat effectiveness (W0
        /// balance)": a tier-1 combat droid works any seat at 25%; section 27: station effectiveness comes from Industry.
        /// </summary>
        public const double NoIndustryEffectiveness = DroidIndustryTuning.NoIndustryEffectiveness;

        /// <summary>The best droid at the Engineer or Sensors seat. Plan section 10, "Droid seat effectiveness (W0 balance)".</summary>
        public const double BestSupportEffectiveness = DroidIndustryTuning.BestSupportEffectiveness;

        /// <summary>
        /// The best droid at a Gunner seat. Plan section 10, "Droid seat effectiveness (W0 balance)": at 75% on
        /// Gunner seats one pilot with droids beat a Fighter by about 2x.
        /// </summary>
        public const double BestGunnerEffectiveness = DroidIndustryTuning.BestGunnerEffectiveness;

        /// <summary>
        /// A droid switches a repairer, shield generator or hardener on once this share of shield or hull is missing,
        /// the threshold NPC ships use (<c>Space.NpcRepairThreshold</c>).
        /// </summary>
        public const float RepairThreshold = 0.25f;

        /// <summary>How often the droid crew loop checks that posted droids' controllers still exist.</summary>
        public const int ControllerCheckSeconds = 6;

        /// <summary>The number of droids that may follow their owner at once (<c>DroidControlItemDefinition</c>).</summary>
        public const int FollowLimit = 1;

        /// <summary>
        /// A player who takes a station a droid holds bumps the droid back to its owner, so players are never kept
        /// off a seat by a droid whose owner is offline.
        /// </summary>
        public const bool PlayerBumpsDroid = true;

        /// <summary>A tier held between <see cref="MinimumTier"/> and <see cref="MaximumTier"/>.</summary>
        public static int ClampTier(int tier) => Math.Clamp(tier, MinimumTier, MaximumTier);

        /// <summary>
        /// A droid's level: 5, 15, 25, 35 and 45 for tiers 1-5, as <c>Droid.CacheDroidLevels</c> sets them.
        /// </summary>
        public static int Level(int tier) => ClampTier(tier) * 10 - 5;

        /// <summary>
        /// The highest module rank a droid can switch on: its level. Modules come every 5 ranks (0-45, plan section
        /// 8), so each tier adds two rank bands, a tier-5 droid works rank 45 modules and the named rank-50 modules
        /// stay with players.
        /// </summary>
        public static int MaximumModuleRank(int tier) => Level(tier);

        /// <summary>Whether a droid of this tier can switch the module on (its RequiresSkill rank against the tier's rank).</summary>
        public static bool MeetsRank(IReadOnlyDictionary<SkillType, int> requiredSkills, int tier) =>
            ShipCrewSeats.GetRequiredRank(requiredSkills) <= MaximumModuleRank(tier);

        /// <summary>
        /// The droid's attribute score for every attribute while it fires: the balance model's pilot at the droid's
        /// level (<c>tools/SpaceRevampBalance/model.py</c> <c>attr()</c>, 16 + 0.2 x rank). Combat parts do not
        /// make a better crew droid; its Industry rating (section 27) sets how well it works.
        /// </summary>
        public static int Attribute(int tier) =>
            (int)Math.Round(16 + 0.2 * Level(tier), MidpointRounding.AwayFromZero);

        /// <summary>
        /// The best droid's effectiveness at a seat, or 0 for the Pilot's seat (droids never fly) and the droid bay
        /// (a bay droid works no station slots; its Industry pays off through the bay, plan section 27).
        /// </summary>
        public static double BestEffectiveness(ShipCrewSeat seat) => seat switch
        {
            ShipCrewSeat.Gunner or ShipCrewSeat.SecondGunner => BestGunnerEffectiveness,
            ShipCrewSeat.Engineer or ShipCrewSeat.Sensors => BestSupportEffectiveness,
            _ => 0.0,
        };

        /// <summary>
        /// What a droid's Industry adds to its effectiveness at a seat: the share of the best droid's Industry times the
        /// gap between no Industry and the seat's best. Plan section 10: the best industrial droid works a station at
        /// about 75% of a player of matching rank (50% at a Gunner seat), and a combat droid far below.
        /// </summary>
        public static double IndustryBonus(ShipCrewSeat seat, int industry)
        {
            var best = BestEffectiveness(seat);
            return best <= 0 ? 0.0 : (best - NoIndustryEffectiveness) * DroidIndustryRules.Share(industry);
        }

        /// <summary>
        /// A droid's effectiveness at a seat before Crew Morale and Leadership: 25% with no Industry, rising with its
        /// Industry to the seat's best at 100 (five tier-5 industrial parts). Base = 0.25 + (best - 0.25) x Industry / 100.
        /// Its tier sets only the module ranks it can switch on.
        /// </summary>
        public static double BaseEffectiveness(ShipCrewSeat seat, int industry)
        {
            var best = BestEffectiveness(seat);
            return best <= 0 ? 0.0 : NoIndustryEffectiveness + IndustryBonus(seat, industry);
        }

        /// <summary>
        /// A droid's effectiveness at a seat: its base times the crew multiplier (Crew Morale and Leadership,
        /// <see cref="SpaceCrew.EffectivenessMultiplier"/>), capped at <see cref="ShipCrewSeats.MaximumSeatEffectiveness"/>.
        /// </summary>
        public static double SeatEffectiveness(ShipCrewSeat seat, int industry, double crewMultiplier) =>
            ShipCrewSeats.CapEffectiveness(BaseEffectiveness(seat, industry) * Math.Max(0.0, crewMultiplier));

        /// <summary>What a droid does with a module of this type and activation.</summary>
        public static DroidCrewTask GetTask(ShipModuleType type, ShipModuleActivation activation)
        {
            if (activation != ShipModuleActivation.Toggle)
                return DroidCrewTask.None;

            if (ShipToggleRules.IsOffensive(type))
                return DroidCrewTask.Weapon;

            return type switch
            {
                ShipModuleType.ShieldRepairer or ShipModuleType.BulwarkShieldGenerator or ShipModuleType.ShieldHardener
                    => DroidCrewTask.ShieldSupport,
                ShipModuleType.HullRepairer or ShipModuleType.RepairFieldGenerator => DroidCrewTask.HullSupport,
                ShipModuleType.ProbeLauncher => DroidCrewTask.Survey,
                _ => DroidCrewTask.None,
            };
        }

        /// <summary>
        /// Whether a droid switches a Probe Launcher on: it runs Survey Protocol (plan section 27), the launcher is off
        /// and there is something to scan where the ship is.
        /// </summary>
        public static bool RunsProbeLauncher(IReadOnlyDictionary<PerkType, int> protocols, bool launcherOn, bool somethingToScan) =>
            !launcherOn && somethingToScan &&
            DroidIndustryRules.GetProtocolValue(protocols, PerkType.SurveyProtocol) > 0;

        /// <summary>Whether a droid runs this module while shield is low.</summary>
        public static bool RunsWhenShieldLow(ShipModuleType type) =>
            GetTask(type, ShipModuleActivation.Toggle) == DroidCrewTask.ShieldSupport;

        /// <summary>Whether a droid runs this module while hull is low.</summary>
        public static bool RunsWhenHullLow(ShipModuleType type) =>
            GetTask(type, ShipModuleActivation.Toggle) == DroidCrewTask.HullSupport;

        /// <summary>
        /// Whether a droid may switch a weapon on against the pilot's target, given the target it last engaged with
        /// that weapon. It engages each pick of target once: a weapon that switched off (out of range, out of
        /// capacitor, the target lost) comes back on once the pilot picks a target, the same one included
        /// (<see cref="SpaceCrew.ForgetDroidEngagements"/>). The weapon routine checks that it is off, can fire and
        /// is in range.
        /// </summary>
        public static bool MayEngageAgain(uint target, uint engagedTarget) => target != engagedTarget;

        /// <summary>
        /// Whether a droid counts toward its owner's follow limit: only a droid that follows them does. A working droid
        /// (a ship's station or droid bay, and later a holding or station building) is posted and never spawned.
        /// </summary>
        public static bool CountsTowardFollowLimit(bool spawned, bool posted) => spawned && !posted;

        /// <summary>
        /// Why a droid's controller cannot activate it, or an empty string. A posted droid stays at its post; droids
        /// posted elsewhere do not count toward the follow limit.
        /// </summary>
        public static string GetActivationError(bool thisDroidPosted, int followers)
        {
            if (thisDroidPosted)
                return "This droid is posted aboard a ship. Recall it at the ship's crew console first.";

            return followers >= FollowLimit ? "Only one droid may be activated at a time." : string.Empty;
        }

        /// <summary>
        /// Why a droid cannot be posted at a seat or the droid bay (<paramref name="postName"/>: "the Gunner station",
        /// "the droid bay"), or an empty string. The captain (the owner or anyone with the crew permission) posts one of
        /// their own droids at an empty station. A seat holds a player or a droid.
        /// </summary>
        public static string GetPostingError(
            bool isCaptain,
            bool seatExists,
            bool seatHasPlayer,
            bool seatHasDroid,
            bool ownsDroid,
            bool droidPosted,
            bool droidFollowing,
            string postName)
        {
            if (!isCaptain)
                return "You do not have permission to crew this starship.";
            if (!seatExists)
                return "This ship has no such station.";
            if (!ownsDroid)
                return "You can post only a droid whose controller you carry.";
            if (droidPosted)
                return "That droid is already posted at a station.";
            if (droidFollowing)
                return "Dismiss that droid before you post it.";
            if (seatHasPlayer)
                return $"Someone works {postName}.";
            if (seatHasDroid)
                return $"A droid already works {postName}. Recall it first.";

            return string.Empty;
        }
    }
}
