using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.DroidService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Derelict boarding (plan section 28): which derelicts can be boarded or are contested, who may dock, what an interior
    /// holds at a tier, the contested waves, their loot step-up and reinforcements, and droid salvage teams. Pure rules;
    /// numbers come from <see cref="DerelictTuning"/>.
    /// </summary>
    public static class DerelictRules
    {
        /// <summary>Whether a new derelict signature can be boarded. <paramref name="roll"/> is 0-99.</summary>
        public static bool RollsBoardable(int roll) => roll < DerelictTuning.BoardablePercent;

        /// <summary>Whether a boardable derelict is contested. <paramref name="roll"/> is 0-99.</summary>
        public static bool RollsContested(int roll) => roll < DerelictTuning.ContestedPercent;

        /// <summary>The content tier of a derelict: its area's space tier (1-6), or 7 in Wild Space (tier 5.5).</summary>
        public static int TierIndex(int spaceTier, bool wildSpace) =>
            wildSpace ? DerelictTuning.WildSpaceTierIndex : Math.Clamp(spaceTier, 1, 6);

        /// <summary>The tier of an interior's slicing terminals and lockboxes: ground slicing stops at 5.</summary>
        public static int SlicingTier(int tierIndex) => Math.Clamp(tierIndex, 1, 5);

        /// <summary>The ground enemies an interior of the content tier draws from.</summary>
        public static IReadOnlyList<string> Roster(int tierIndex) =>
            DerelictTuning.Rosters[Math.Clamp(tierIndex, 1, DerelictTuning.WildSpaceTierIndex)];

        public static string TerminalResref(int tierIndex) => $"slice_term_{SlicingTier(tierIndex)}";

        public static string LockboxResref(int tierIndex) => $"lockbox_t{SlicingTier(tierIndex)}";

        /// <summary>The Ship Scrap a salvage team brings back: the tier's, tier 5 at most (Wild Space too).</summary>
        public static string ScrapResref(int tierIndex) =>
            $"ship_scrap_{(tierIndex == DerelictTuning.WildSpaceTierIndex ? 5 : Math.Clamp(tierIndex, 1, 5))}";

        /// <summary>The interior template a roll picks. <paramref name="roll"/> is 0 to the template count - 1.</summary>
        public static int PickTemplate(int roll) => Math.Clamp(roll, 0, DerelictTuning.TemplateResrefs.Length - 1);

        /// <summary>
        /// What an interior of the content tier holds: a template, the tier's ground enemies, slicing terminals of its
        /// slicing tier, debris and the strongbox. <paramref name="roll"/> returns 0 to max - 1.
        /// </summary>
        public static DerelictInteriorPlan PlanInterior(int tierIndex, Func<int, int> roll)
        {
            var template = PickTemplate(roll(DerelictTuning.TemplateResrefs.Length));
            var roster = Roster(tierIndex);
            return new DerelictInteriorPlan
            {
                TemplateResref = DerelictTuning.TemplateResrefs[template],
                Name = DerelictTuning.TemplateNames[template],
                Enemies = Enumerable.Range(0, DerelictTuning.InteriorEnemies).Select(_ => roster[Math.Clamp(roll(roster.Count), 0, roster.Count - 1)]).ToList(),
                Terminals = Enumerable.Repeat(TerminalResref(tierIndex), DerelictTuning.InteriorTerminals).ToList(),
                Dressing = Enumerable.Range(0, DerelictTuning.DressingCount)
                    .Select(_ => DerelictTuning.DressingResrefs[Math.Clamp(roll(DerelictTuning.DressingResrefs.Length), 0, DerelictTuning.DressingResrefs.Length - 1)])
                    .ToList(),
                StrongboxResref = DerelictTuning.StrongboxResref,
            };
        }

        // Boarding and salvage gates

        /// <summary>Why the pilot cannot board the derelict with <c>/board</c>, or <see cref="DerelictRefusal.None"/>.</summary>
        public static DerelictRefusal GetBoardRefusal(DerelictRequest request)
        {
            var common = GetCommonRefusal(request);
            if (common != DerelictRefusal.None)
                return common;
            if (!ShipDockingRules.CanVisit(request.ShipSize))
                return DerelictRefusal.VisitorTooLarge;
            if (request.Distance > ShipDockingRules.DockRange)
                return DerelictRefusal.OutOfRange;
            if (request.InCombat)
                return DerelictRefusal.InCombat;
            if (request.DefensesLeft)
                return DerelictRefusal.DefensesLeft;
            if (request.Underway == DerelictMode.SalvageTeam)
                return DerelictRefusal.SalvageUnderway;
            if (request.Underway == DerelictMode.Boarding && !request.SameParty)
                return DerelictRefusal.OtherParty;
            if (request.Underway == DerelictMode.Boarding && request.DockedCount >= DerelictTuning.DockCapacity)
                return DerelictRefusal.Full;

            return DerelictRefusal.None;
        }

        /// <summary>
        /// Why the pilot cannot send a salvage team with <c>/salvage</c>, or <see cref="DerelictRefusal.None"/>. Any hull
        /// may: the ship does not dock, it holds beside the derelict while its posted droids work.
        /// </summary>
        public static DerelictRefusal GetSalvageRefusal(DerelictRequest request)
        {
            var common = GetCommonRefusal(request);
            if (common != DerelictRefusal.None)
                return common;
            if (request.Distance > ShipDockingRules.DockRange)
                return DerelictRefusal.OutOfRange;
            if (request.InCombat)
                return DerelictRefusal.InCombat;
            if (request.DefensesLeft)
                return DerelictRefusal.DefensesLeft;
            if (request.Underway == DerelictMode.Boarding)
                return DerelictRefusal.BoardingUnderway;
            if (request.Underway == DerelictMode.SalvageTeam)
                return DerelictRefusal.SalvageUnderway;
            if (request.PostedDroids <= 0)
                return DerelictRefusal.NoDroids;

            return DerelictRefusal.None;
        }

        private static DerelictRefusal GetCommonRefusal(DerelictRequest request)
        {
            if (!request.IsDerelict)
                return DerelictRefusal.NotADerelict;
            if (!request.Boardable)
                return DerelictRefusal.NotBoardable;
            if (request.Looted)
                return DerelictRefusal.Looted;
            if (!request.PartyLocated)
                return DerelictRefusal.NotLocated;

            return DerelictRefusal.None;
        }

        /// <summary>The message a pilot sees for a refusal.</summary>
        public static string DescribeRefusal(DerelictRefusal refusal) => refusal switch
        {
            DerelictRefusal.NotADerelict => "Target a derelict's wreck to board it or send a salvage team.",
            DerelictRefusal.NotBoardable => "This derelict's hull is too far gone to board.",
            DerelictRefusal.Looted => "This derelict has already been stripped.",
            DerelictRefusal.NotLocated => "Your party must scan this derelict to 100% before working it.",
            DerelictRefusal.VisitorTooLarge => "Only Small and Medium ships can dock with a derelict.",
            DerelictRefusal.OutOfRange => $"You must be within {ShipDockingRules.DockRange:0} meters of the derelict.",
            DerelictRefusal.InCombat => "You cannot do that while in combat.",
            DerelictRefusal.DefensesLeft => "The derelict's defenses are still active. Destroy them first.",
            DerelictRefusal.OtherParty => "Another party has boarded this derelict.",
            DerelictRefusal.Full => $"{DerelictTuning.DockCapacity} ships are already docked at this derelict.",
            DerelictRefusal.SalvageUnderway => "A salvage team is already stripping this derelict.",
            DerelictRefusal.BoardingUnderway => "A party has boarded this derelict. Board it with them instead.",
            DerelictRefusal.NoDroids => "Post a droid at one of your ship's stations or its droid bay to send a salvage team.",
            _ => string.Empty,
        };

        // Lifetime

        /// <summary>A boarding or salvage team begins: the first wave's time and the hold on the site are set from now.</summary>
        public static void Begin(DerelictBoarding boarding, DateTime now)
        {
            boarding.StartedAt = now;
            boarding.NextWaveAt = now.AddSeconds(DerelictTuning.WaveFirstSeconds);
        }

        /// <summary>Until when a worked derelict keeps its site open: the hold from when work began.</summary>
        public static DateTime HoldUntil(DateTime startedAt) => startedAt.AddMinutes(DerelictTuning.BoardingHoldMinutes);

        /// <summary>Whether a boarding or salvage team has run out its hold and must end.</summary>
        public static bool IsExpired(DerelictBoarding boarding, DateTime now) => now >= HoldUntil(boarding.StartedAt);

        /// <summary>
        /// A boarding is over once no ship is docked and nobody is aboard: everyone went home. A salvage team ends on its
        /// own (finished, recalled or cut off).
        /// </summary>
        public static bool IsBoardingAbandoned(DerelictBoarding boarding, int playersAboard) =>
            boarding.Mode == DerelictMode.Boarding && boarding.DockedShipIds.Count == 0 && playersAboard == 0;

        /// <summary>
        /// Where a leaving boarder goes: their ship's interior when it can take them, otherwise the dock it is towed to,
        /// otherwise the starting location.
        /// </summary>
        public static DerelictReturn ChooseReturn(bool shipInteriorReady, bool dockReachable)
        {
            if (shipInteriorReady)
                return DerelictReturn.ShipInterior;

            return dockReachable ? DerelictReturn.ShipDock : DerelictReturn.Start;
        }

        /// <summary>
        /// The ship a boarder returns to: the one they came aboard with, otherwise one they own, otherwise the first ship
        /// docked. Null when no ship is docked.
        /// </summary>
        public static string ChooseShip(DerelictBoarding boarding, string playerId, IReadOnlyDictionary<string, string> ownerByShipId)
        {
            foreach (var shipId in boarding.DockedShipIds)
            {
                if (boarding.CrewByShipId.TryGetValue(shipId, out var crew) && crew.Contains(playerId))
                    return shipId;
            }

            foreach (var shipId in boarding.DockedShipIds)
            {
                if (ownerByShipId != null && ownerByShipId.TryGetValue(shipId, out var owner) && owner == playerId)
                    return shipId;
            }

            return boarding.DockedShipIds.FirstOrDefault();
        }

        // Contested waves

        /// <summary>
        /// Whether the next wave arrives now: the derelict is contested and not yet stripped, no wave is out, fewer than
        /// <see cref="DerelictTuning.MaximumWaves"/> came, and its time has come.
        /// </summary>
        public static bool WaveDue(DerelictBoarding boarding, DateTime now) =>
            boarding.Contested && !boarding.Looted && !boarding.WaveOut &&
            boarding.WavesSpawned < DerelictTuning.MaximumWaves && now >= boarding.NextWaveAt;

        /// <summary>A wave arrives.</summary>
        public static void RecordWaveSpawned(DerelictBoarding boarding)
        {
            boarding.WavesSpawned++;
            boarding.WaveOut = true;
        }

        /// <summary>The wave's last ship is down: one more wave cleared, and the next comes after the gap.</summary>
        public static void RecordWaveCleared(DerelictBoarding boarding, DateTime now)
        {
            boarding.WaveOut = false;
            if (!boarding.Looted)
                boarding.WavesCleared++;
            boarding.NextWaveAt = now.AddSeconds(DerelictTuning.WaveGapSeconds);
        }

        /// <summary>The strongbox's step-up per wave cleared at the content tier, in percent.</summary>
        public static int WaveStepPercent(int tierIndex) =>
            DerelictTuning.WaveStepPercent[Math.Clamp(tierIndex, 1, DerelictTuning.WildSpaceTierIndex)];

        /// <summary>
        /// Whether enemy boarders breach the hull now: a contested boarding, not yet stripped, with a wave out, nobody
        /// defending within <see cref="DerelictTuning.DefenseRadius"/>, someone aboard, fewer than the maximum sent and the
        /// last breach long enough ago.
        /// </summary>
        public static bool ShouldReinforce(DerelictBoarding boarding, int defenders, int playersAboard, DateTime now) =>
            boarding.Mode == DerelictMode.Boarding && boarding.Contested && !boarding.Looted && boarding.WaveOut &&
            defenders == 0 && playersAboard > 0 &&
            boarding.Reinforcements < DerelictTuning.MaximumReinforcements &&
            now >= boarding.LastReinforcementAt.AddSeconds(DerelictTuning.ReinforcementSeconds);

        /// <summary>How many enemy boarders the next breach sends: the count, never past the maximum.</summary>
        public static int ReinforcementSize(DerelictBoarding boarding) =>
            Math.Max(0, Math.Min(DerelictTuning.ReinforcementCount, DerelictTuning.MaximumReinforcements - boarding.Reinforcements));

        // Rewards

        /// <summary>The strongbox's credits for each boarder: the tier's base, stepped up by every wave cleared.</summary>
        public static int StrongboxCredits(int tierIndex, int wavesCleared)
        {
            var baseCredits = DerelictTuning.StrongboxCredits[Math.Clamp(tierIndex, 1, DerelictTuning.WildSpaceTierIndex)];
            var waves = Math.Clamp(wavesCleared, 0, DerelictTuning.MaximumWaves);
            return (int)Math.Round(baseCredits * (1 + WaveStepPercent(tierIndex) * waves / 100.0), MidpointRounding.AwayFromZero);
        }

        /// <summary>A defender's share of the strongbox: each party member flying within range when it opens.</summary>
        public static int DefenderCredits(int tierIndex, int wavesCleared) =>
            (int)Math.Round(StrongboxCredits(tierIndex, wavesCleared) * DerelictTuning.DefenderSharePercent / 100.0,
                MidpointRounding.AwayFromZero);

        /// <summary>The ground lockboxes each boarder finds in the strongbox: one more per wave cleared.</summary>
        public static int StrongboxLockboxes(int wavesCleared) =>
            DerelictTuning.StrongboxLockboxes + Math.Clamp(wavesCleared, 0, DerelictTuning.MaximumWaves);

        /// <summary>Whether the strongbox opens: every enemy aboard is down.</summary>
        public static bool CanOpenStrongbox(int enemiesLeft) => enemiesLeft <= 0;

        // Salvage teams

        /// <summary>A salvage team's Industry: its posted droids' summed, capped at one best droid's.</summary>
        public static int TeamIndustry(IEnumerable<int> droidIndustry) =>
            Math.Min(DerelictTuning.SalvageIndustryCap, DroidIndustryRules.SumIndustry(droidIndustry));

        /// <summary>The team's timer: the base, shortened by <see cref="DroidIndustryRules.SalvageTeamSpeedPercent"/>.</summary>
        public static int SalvageSeconds(int industry) =>
            (int)Math.Round(DerelictTuning.SalvageBaseSeconds / (1 + DroidIndustryRules.SalvageTeamSpeedPercent(industry) / 100.0),
                MidpointRounding.AwayFromZero);

        /// <summary>The team's credits: the tier's base, raised by <see cref="DroidIndustryRules.SalvageTeamYieldPercent"/>.</summary>
        public static int SalvageCredits(int tierIndex, int industry) =>
            DroidIndustryRules.ApplyPercent(DerelictTuning.SalvageCredits[Math.Clamp(tierIndex, 1, DerelictTuning.WildSpaceTierIndex)],
                DroidIndustryRules.SalvageTeamYieldPercent(industry));

        /// <summary>The Ship Scrap the team brings back, raised by its yield percent.</summary>
        public static int SalvageScrap(int industry) =>
            DroidIndustryRules.ApplyPercent(DerelictTuning.SalvageScrap, DroidIndustryRules.SalvageTeamYieldPercent(industry));

        /// <summary>The team works <paramref name="seconds"/> more, unless a wave is out: the droids take cover until it is cleared.</summary>
        public static void AdvanceSalvage(DerelictBoarding boarding, double seconds)
        {
            if (boarding.Mode != DerelictMode.SalvageTeam || boarding.WaveOut || seconds <= 0)
                return;

            boarding.SalvageProgress = Math.Min(boarding.SalvageSecondsNeeded, boarding.SalvageProgress + seconds);
        }

        public static bool IsSalvageDone(DerelictBoarding boarding) =>
            boarding.Mode == DerelictMode.SalvageTeam && boarding.SalvageProgress >= boarding.SalvageSecondsNeeded;

        /// <summary>Whether the salvage ship is still close enough for its team to keep working.</summary>
        public static bool IsTethered(float distance) => distance <= DerelictTuning.SalvageTetherMeters;

        /// <summary>Whether a ship this far from the derelict defends it.</summary>
        public static bool IsDefending(float distance) => distance <= DerelictTuning.DefenseRadius;
    }
}
