using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SlicingService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Slicing in space (plan section 24): the Data Analyzer starts the slicing mini-game on data cans, locked caches
    /// and wreck data cores within its range; customs scans and space lockboxes start it too. The pilot slices, or the
    /// crew member at the Sensors seat with their own Slicing perk. Rewards come from the space reward tables.
    /// </summary>
    public static partial class Space
    {
        /// <summary>The tag every data can shares, registered as a space object so a click targets it.</summary>
        public const string DataCanTag = "spc_datacan";

        /// <summary>Set on a site cache that must be sliced open.</summary>
        private const string LockedCacheVariable = "SPACE_LOCKED_CACHE";

        // The ship a slicer works from

        /// <summary>
        /// The ship a player slices from: their own ship while piloting it, the ship whose Sensors seat they hold,
        /// otherwise OBJECT_INVALID.
        /// </summary>
        public static uint GetSlicingShip(uint slicer)
        {
            if (!GetIsObjectValid(slicer))
                return OBJECT_INVALID;
            if (IsPlayerInSpaceMode(slicer))
                return slicer;

            return SpaceCrew.TryGetSeat(slicer, out var ship, out var seat) && seat == ShipCrewSeat.Sensors
                ? ship
                : OBJECT_INVALID;
        }

        /// <summary>Whether the ship a player slices from is in combat.</summary>
        public static bool IsSlicingShipInCombat(uint slicer)
        {
            var ship = GetSlicingShip(slicer);
            return GetIsObjectValid(ship) && (IsShipInCombat(ship) || GetIsInCombat(ship));
        }

        /// <summary>
        /// Who aboard a ship slices a target of a tier: the pilot, or the Sensors crew member when they have the
        /// Slicing rank and the pilot does not (or theirs is higher).
        /// </summary>
        public static uint PickSlicer(uint ship, int tier)
        {
            var crew = SpaceCrew.GetSeatOccupant(ship, ShipCrewSeat.Sensors);
            var hasCrew = GetIsObjectValid(crew) && GetIsPC(crew);
            var seat = SpaceSlicingRules.PickSlicer(
                SlicingSession.GetSlicingRank(ship),
                hasCrew,
                hasCrew ? SlicingSession.GetSlicingRank(crew) : 0,
                tier);
            return seat == ShipCrewSeat.Sensors ? crew : ship;
        }

        /// <summary>Why a slicer cannot reach a space target right now, or empty (SlicingSession.ValidateStart).</summary>
        public static string GetSlicingTargetError(uint slicer, uint target, SlicingSourceType source)
        {
            var ship = GetSlicingShip(slicer);
            if (!GetIsObjectValid(ship))
                return "You must be piloting the ship or crewing its Sensors station to slice this.";

            if (source == SlicingSourceType.CustomsScan)
                return Trade.GetCustomsScan(ship) == null ? "The customs patrol has no further business with you." : string.Empty;

            if (!SpaceSlicingRules.UsesDataAnalyzer(source))
                return "This slicing target has an invalid security profile.";

            if (GetArea(ship) != GetArea(target) || !SpaceSlicingRules.IsInAnalyzerRange(GetDistanceBetween(ship, target)))
                return $"The target is out of your Data Analyzer's range ({SpaceSlicingTuning.AnalyzerRange:0} m).";

            return GetSourceTargetError(ship, slicer, target, source);
        }

        private static string GetSourceTargetError(uint ship, uint slicer, uint target, SlicingSourceType source)
        {
            switch (source)
            {
                case SlicingSourceType.DataCan:
                case SlicingSourceType.LockedCache:
                {
                    var signature = FindSignature(GetLocalString(target, SiteSignatureVariable));
                    if (signature == null || signature.Completed)
                        return "This site has closed.";
                    if (ScanningRules.PartyPercent(signature, GetScanPartyIds(ship)) < ScanningTuning.InfoLocatedPercent)
                        return "This site is keyed to the ships that scanned it. Scan it yourself to slice it.";
                    return string.Empty;
                }
                case SlicingSourceType.WreckDataCore:
                {
                    var wreck = GetWreck(target);
                    if (wreck == null || !wreck.HasDataCore)
                        return "This wreck has no data core left.";

                    var now = DateTime.UtcNow;
                    var ids = new[] { GetObjectUUID(ship), GetObjectUUID(slicer) };
                    if (!SpaceSlicingRules.CanSliceDataCore(wreck.Claimants, ids, wreck.DestroyedAt, now))
                        return $"The ship's killers have first claim on this data core for {ShipSalvageRules.ClaimSecondsLeft(wreck.DestroyedAt, now)} more seconds.";
                    return string.Empty;
                }
                default:
                    return string.Empty;
            }
        }

        // Data Analyzer

        /// <summary>The space source and slicing tier of a Data Analyzer target, or Lockbox (none) when it cannot be sliced.</summary>
        private static (SlicingSourceType Source, int Tier) GetAnalyzerTarget(uint target)
        {
            if (!GetIsObjectValid(target) || GetObjectType(target) != ObjectType.Placeable)
                return (SlicingSourceType.Lockbox, 0);

            if (GetTag(target) == DataCanTag)
                return (SlicingSourceType.DataCan, GetLocalInt(target, SlicingSession.TierVariable));

            if (GetLocalBool(target, LockedCacheVariable))
                return (SlicingSourceType.LockedCache, GetLocalInt(target, SlicingSession.TierVariable));

            var wreck = GetWreck(target);
            if (wreck != null && wreck.HasDataCore)
                return (SlicingSourceType.WreckDataCore, SpaceSlicingRules.TargetTier(wreck.Tier, IsWildSpace(GetArea(target))));

            return (SlicingSourceType.Lockbox, 0);
        }

        /// <summary>Why the Data Analyzer cannot start on the target, or empty.</summary>
        public static string DataAnalyzerError(uint ship, uint target)
        {
            var (source, tier) = GetAnalyzerTarget(target);
            if (!SpaceSlicingRules.UsesDataAnalyzer(source))
                return "Only data cans, locked caches and wreck data cores can be sliced.";

            var slicer = SpaceCrew.GetStatSource(ship);
            if (!GetIsObjectValid(slicer))
                return "A droid cannot slice.";

            return SlicingSession.ValidateStart(slicer, target, source, tier);
        }

        /// <summary>A Data Analyzer press: whoever runs it (the pilot, or the Sensors crew member) starts slicing the target.</summary>
        public static void StartDataAnalyzer(uint ship, uint target)
        {
            var slicer = SpaceCrew.GetStatSource(ship);
            if (!GetIsObjectValid(slicer) || !GetIsPC(slicer))
                return;

            var (source, tier) = GetAnalyzerTarget(target);
            StartSlicing(slicer, target, source, tier);
        }

        /// <summary>Starts the slicing mini-game for a space target and opens its window, or tells the slicer why not.</summary>
        public static bool StartSlicing(uint slicer, uint target, SlicingSourceType source, int tier)
        {
            if (!SlicingSession.TryStart(slicer, target, source, tier, out var error))
            {
                SendMessageToPC(slicer, error);
                return false;
            }

            Gui.TogglePlayerWindow(slicer, GuiWindowType.Slicing, new SlicingPayload(target, source, tier));
            return true;
        }

        // Outcomes (SlicingSession calls these)

        /// <summary>A space target was sliced: its rewards go out and the target is spent.</summary>
        public static void CompleteSlicing(uint slicer, uint target, SlicingSourceType source, int tier)
        {
            if (source == SlicingSourceType.CustomsScan)
            {
                Trade.ResolveCustomsSpoof(slicer, true);
                return;
            }

            var rewards = SlicingReward.RollSpace(source, tier, max => Random.Next(max), AgentIntelAvailable);
            var ship = GetSlicingShip(slicer);
            switch (source)
            {
                case SlicingSourceType.SpaceLockbox:
                    GiveRewardsToPlayer(slicer, rewards);
                    DestroyObject(target);
                    break;
                case SlicingSourceType.DataCan:
                    DeliverRewardsToShip(slicer, ship, rewards, new List<string>());
                    DestroyObject(target);
                    break;
                case SlicingSourceType.LockedCache:
                    OpenLockedCache(target, slicer, ship, rewards);
                    break;
                case SlicingSourceType.WreckDataCore:
                    var wreck = GetWreck(target);
                    if (wreck != null)
                        wreck.HasDataCore = false;
                    DeliverRewardsToShip(slicer, ship, rewards, new List<string>());
                    SendMessageToPC(slicer, "The data core is drained. The wreck can still be salvaged.");
                    break;
            }

            Log.Write(LogGroup.Space, $"SPACE SLICING - {PlayerName.GetAuditName(slicer)} - {source} T{tier} - {DescribeRewards(rewards)}");
        }

        /// <summary>A space target was destroyed by a failed attempt.</summary>
        public static void OnSlicingTargetDestroyed(uint target, SlicingSourceType source)
        {
            switch (source)
            {
                case SlicingSourceType.WreckDataCore:
                    var wreck = GetWreck(target);
                    if (wreck != null)
                        wreck.HasDataCore = false;
                    break;
                case SlicingSourceType.LockedCache:
                    var signature = FindSignature(GetLocalString(target, SiteSignatureVariable));
                    if (signature != null)
                        signature.Completed = true;
                    DestroyObject(target);
                    break;
                case SlicingSourceType.CustomsScan:
                    break;
                default:
                    DestroyObject(target);
                    break;
            }
        }

        /// <summary>A customs spoof failed or was given up: it plays out as being caught.</summary>
        public static void FailCustomsSlicing(uint slicer)
        {
            Trade.ResolveCustomsSpoof(slicer, false);
        }

        /// <summary>
        /// Whether agent intel can be given (plan section 29). Faction agents arrive in W15; until then an agent intel
        /// pick gives a component with a rare trait instead.
        /// </summary>
        public static bool AgentIntelAvailable => false;

        /// <summary>The W15 hook for an agent intel reward: unlocks a special agent mission for the slicer.</summary>
        private static void GrantAgentIntel(uint slicer)
        {
        }

        // Reward delivery

        /// <summary>Space-site rewards: credits to the slicer, items into the ship's hold (the slicer's inventory with no ship).</summary>
        private static void DeliverRewardsToShip(uint slicer, uint ship, IReadOnlyList<SpaceSlicingReward> rewards, List<string> extraItems)
        {
            if (!GetIsObjectValid(ship))
            {
                GiveRewardsToPlayer(slicer, rewards);
                foreach (var resref in extraItems)
                    CreateItemOnObject(resref, slicer);
                return;
            }

            var items = new List<string>(extraItems);
            var components = new List<(string Resref, ShipComponentTraitType Trait)>();
            foreach (var reward in rewards)
            {
                switch (reward.Kind)
                {
                    case SpaceSlicingRewardKind.Credits:
                        GiveGoldToCreature(slicer, reward.Credits);
                        break;
                    case SpaceSlicingRewardKind.Component:
                    case SpaceSlicingRewardKind.RareComponent:
                        components.Add((reward.Resref, reward.Trait));
                        break;
                    case SpaceSlicingRewardKind.AgentIntel:
                        GrantAgentIntel(slicer);
                        break;
                    default:
                        items.AddRange(Enumerable.Repeat(reward.Resref, reward.Quantity));
                        break;
                }
            }

            SendMessageToPC(slicer, ColorToken.Green($"Slicing recovered: {DescribeRewards(rewards)}."));
            ShipHold.DeliverYield(ship, items, components);
        }

        /// <summary>Space lockbox rewards: everything goes to the player who sliced it.</summary>
        private static void GiveRewardsToPlayer(uint player, IReadOnlyList<SpaceSlicingReward> rewards)
        {
            foreach (var reward in rewards)
            {
                switch (reward.Kind)
                {
                    case SpaceSlicingRewardKind.Credits:
                        GiveGoldToCreature(player, reward.Credits);
                        break;
                    case SpaceSlicingRewardKind.AgentIntel:
                        GrantAgentIntel(player);
                        break;
                    default:
                        var item = CreateItemOnObject(reward.Resref, player, reward.Quantity);
                        if (reward.Trait != ShipComponentTraitType.None && GetIsObjectValid(item))
                            ShipComponents.SetTraits(item, new[] { reward.Trait });
                        break;
                }
            }

            SendMessageToPC(player, ColorToken.Green($"You recover {DescribeRewards(rewards)}."));
        }

        private static string DescribeRewards(IEnumerable<SpaceSlicingReward> rewards)
        {
            return string.Join(", ", rewards.Select(reward => reward.Kind switch
            {
                SpaceSlicingRewardKind.Credits => $"{reward.Credits} credits",
                SpaceSlicingRewardKind.AgentIntel => "agent intel",
                _ => $"{reward.Quantity}x {ShipTraitRules.DecoratedName(Cache.GetItemNameByResref(reward.Resref), reward.Trait == ShipComponentTraitType.None ? null : new[] { reward.Trait })}",
            }));
        }

        // Sites: data cans and locked caches

        /// <summary>A data site's cans, around its center; the site cache appears once every can is sliced or lost.</summary>
        private static void SpawnDataCans(ScanSignature signature, uint area, Vector3 center)
        {
            var tier = SpaceSlicingRules.TargetTier(signature.Tier, IsWildSpace(area));
            for (var index = 0; index < SpaceSlicingTuning.DataSiteCans; index++)
            {
                var (x, y) = SignatureSiteRules.RingOffset(index, SpaceSlicingTuning.DataSiteCans, SpaceSlicingTuning.DataCanRingMeters);
                var can = CreateObject(ObjectType.Placeable, SpaceSlicingTuning.DataCanResref,
                    Location(area, Vector3(center.X + x, center.Y + y, center.Z), Random.Next(360)));
                if (!GetIsObjectValid(can))
                    continue;

                SetLocalString(can, SiteSignatureVariable, signature.Id);
                SetLocalInt(can, SlicingSession.TierVariable, tier);
                signature.Objectives.Add(can);
            }
        }

        /// <summary>Makes a cache site's cache a locked cache: a click targets it for the Data Analyzer, and using it says so.</summary>
        private static void LockCache(uint cache, ScanSignature signature, uint area)
        {
            SetName(cache, "Locked Cache");
            SetDescription(cache, "A sealed pirate cache behind a security lock, keyed to the ships that scanned this site. " +
                                  "Slice it open with a Data Analyzer to take the site's best loot.");
            SetLocalBool(cache, LockedCacheVariable, true);
            SetLocalInt(cache, SlicingSession.TierVariable, SpaceSlicingRules.TargetTier(signature.Tier, IsWildSpace(area)));
            SetEventScript(cache, EventScript.Placeable_OnLeftClick, ScriptName.OnSpaceTarget);
        }

        /// <summary>Whether a site cache is locked; using a locked cache only tells the pilot to slice it.</summary>
        private static bool IsLockedCache(uint cache) => GetLocalBool(cache, LockedCacheVariable);

        /// <summary>A locked cache sliced open: the site's locked-cache credits, its elite pull and the slicing reward.</summary>
        private static void OpenLockedCache(uint cache, uint slicer, uint ship, IReadOnlyList<SpaceSlicingReward> rewards)
        {
            var signature = FindSignature(GetLocalString(cache, SiteSignatureVariable));
            var items = new List<string>();
            var credits = 0;
            if (signature != null && !signature.Completed)
            {
                signature.Completed = true;
                credits = SignatureSiteRules.LockedCacheCredits(signature.Tier);
                var table = Loot.GetLootTableByName(SignatureSiteRules.CacheLootTable(signature.Tier));
                items.AddRange(Enumerable.Range(0, ScanningTuning.CacheItemPulls).Select(_ => table.GetRandomItem().Resref));
            }

            var pilot = GetIsObjectValid(ship) ? ship : slicer;
            if (credits > 0)
                GiveGoldToCreature(pilot, credits);

            SendMessageToPC(slicer, ColorToken.Green($"The lock gives way. You open the {GetName(cache)}: {credits} credits."));
            DeliverRewardsToShip(slicer, ship, rewards, items);
            Log.Write(LogGroup.Space, $"LOCKED CACHE - {PlayerName.GetAuditName(slicer)} - {credits} credits, {string.Join(", ", items)}");
            DestroyObject(cache);
        }

        // Space lockboxes and data cores on NPC ship deaths

        /// <summary>
        /// A destroyed preset NPC ship sometimes drops a space lockbox (plan section 12) into its killer's hold: the
        /// killing blow's ship, otherwise the ship with the most threat.
        /// </summary>
        private static void RollSpaceLockbox(uint creature, WreckDifficulty difficulty, int shipTier)
        {
            if (!SpaceSlicingRules.RollsLockbox(difficulty, Random.Next(100)))
                return;

            var killer = GetLastKiller();
            if (!GetIsPC(killer))
                killer = Enmity.GetHighestEnmityTarget(creature);
            if (!GetIsObjectValid(killer) || !GetIsPC(killer) || GetIsDM(killer))
                return;

            var resref = SpaceSlicingRules.LockboxResref(shipTier);
            SendMessageToPC(killer, ColorToken.Green($"A sealed lockbox tumbles out of {GetName(creature)}."));
            ShipHold.DeliverYield(killer, new[] { resref });
        }
    }
}
