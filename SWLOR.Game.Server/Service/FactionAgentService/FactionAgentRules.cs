using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.ShipComponentDefinition;
using SWLOR.Game.Server.Feature.ShipDefinition;
using SWLOR.Game.Server.Feature.ShipRewardDefinition;
using SWLOR.Game.Server.Feature.TradeDefinition;
using SWLOR.Game.Server.Service.CurrencyService;
using SWLOR.Game.Server.Service.FactionService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Service.FactionAgentService
{
    /// <summary>
    /// Faction agent decisions (plan section 29), free of the engine: the agents, each agent's daily board, what a mission
    /// pays, who may take it, what counts toward it, and the faction token stores. The numbers are FactionAgentTuning's
    /// (tools/SpaceRevampBalance/agents.py).
    /// </summary>
    public static class FactionAgentRules
    {
        private static readonly DateTime Epoch = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private const string OfferPrefix = "agent_";
        private const string ObjectivePrefix = "agent:";
        private static readonly ShipNpcRole[] KillRoles = { ShipNpcRole.Invalid, ShipNpcRole.Fighter, ShipNpcRole.Interceptor, ShipNpcRole.Gunship };

        // Agents

        public static FactionAgentDetail GetAgent(FactionAgentType type)
        {
            return FactionAgentTuning.Agents.FirstOrDefault(x => x.Type == type);
        }

        public static IReadOnlyList<FactionAgentDetail> GetAgentsAtDock(string dockId)
        {
            return FactionAgentTuning.Agents.Where(x => x.DockId == dockId).ToList();
        }

        // The board

        /// <summary>The board day: boards change at 00:00 UTC.</summary>
        public static int GetDay(DateTime now)
        {
            return (int)Math.Floor((now - Epoch).TotalDays);
        }

        public static DateTime GetNextBoardChange(DateTime now)
        {
            return Epoch.AddDays(GetDay(now) + 1);
        }

        public static string OfferId(FactionAgentType agent, int day, int slotIndex)
        {
            return $"{OfferPrefix}{(int)agent}_{day}_{slotIndex}";
        }

        /// <summary>Whether a quest id is an agent mission's.</summary>
        public static bool IsMissionId(string id) => id?.StartsWith(OfferPrefix, StringComparison.Ordinal) ?? false;

        /// <summary>The key of the counter a mission's quest objective uses.</summary>
        public static string ObjectiveKey(string missionId) => ObjectivePrefix + missionId;

        /// <summary>
        /// An agent's board for the day, the same for every player: <see cref="FactionAgentTuning.StandardOffers"/>
        /// offers at the dock's tier, <see cref="FactionAgentTuning.PriorityOffers"/> a tier higher for Trusted pilots, and
        /// an intel lead (a data heist a tier higher) for pilots holding agent intel. No mission type repeats among the
        /// standard and priority offers.
        /// </summary>
        public static List<AgentMission> GetBoard(FactionAgentType agentType, DateTime now)
        {
            var offers = new List<AgentMission>();
            var agent = GetAgent(agentType);
            if (agent == null)
                return offers;

            var day = GetDay(now);
            var random = new System.Random(unchecked(day * 7919 + (int)agentType * 104729));
            var weights = agent.MissionWeights.Where(x => x.Value > 0).ToDictionary(x => x.Key, x => x.Value);
            var index = 0;
            for (var slot = 0; slot < FactionAgentTuning.StandardOffers + FactionAgentTuning.PriorityOffers; slot++)
            {
                var type = PickType(weights, random);
                weights.Remove(type);
                var kind = slot < FactionAgentTuning.StandardOffers ? AgentMissionSlot.Standard : AgentMissionSlot.Priority;
                offers.Add(BuildOffer(agent, OfferId(agentType, day, index++), kind, type, random));
            }

            offers.Add(BuildOffer(agent, OfferId(agentType, day, index), AgentMissionSlot.Intel, AgentMissionType.DataHeist, random));
            return offers;
        }

        private static AgentMissionType PickType(Dictionary<AgentMissionType, int> weights, System.Random random)
        {
            if (weights.Count == 0)
                return AgentMissionType.Kill;

            var roll = random.Next(weights.Values.Sum());
            foreach (var (type, weight) in weights.OrderBy(x => x.Key))
            {
                if (roll < weight)
                    return type;
                roll -= weight;
            }

            return weights.Keys.Max();
        }

        public static int GetMissionTier(FactionAgentDetail agent, AgentMissionSlot slot)
        {
            return slot == AgentMissionSlot.Standard ? agent.DockTier : Math.Min(6, agent.DockTier + 1);
        }

        private static AgentMission BuildOffer(FactionAgentDetail agent, string id, AgentMissionSlot slot, AgentMissionType type, System.Random random)
        {
            var tier = GetMissionTier(agent, slot);
            var mission = new AgentMission
            {
                Id = id,
                Agent = agent.Type,
                Faction = agent.Faction,
                Slot = slot,
                Type = type,
                Tier = tier,
                MinimumStanding = slot == AgentMissionSlot.Priority
                    ? FactionAgentTuning.PriorityStanding
                    : slot == AgentMissionSlot.Intel ? FactionAgentTuning.IntelStanding : 0,
                Standing = slot switch
                {
                    AgentMissionSlot.Priority => FactionAgentTuning.StandingPriority,
                    AgentMissionSlot.Intel => FactionAgentTuning.StandingIntel,
                    _ => FactionAgentTuning.StandingStandard,
                },
            };

            if ((type is AgentMissionType.Kill or AgentMissionType.ConvoyEscort or AgentMissionType.ScanDown or AgentMissionType.DataHeist) &&
                agent.Orbits.TryGetValue(tier, out var orbits) && orbits.Length > 0)
            {
                var orbit = orbits[random.Next(orbits.Length)];
                mission.OrbitResref = orbit.AreaResref;
                mission.Hops = orbit.Hops;
            }

            switch (type)
            {
                case AgentMissionType.Kill:
                    mission.Role = KillRoles[random.Next(KillRoles.Length)];
                    mission.Quantity = FactionAgentTuning.KillCount[mission.Role];
                    break;
                case AgentMissionType.SealedCourier:
                    var destinations = TradeCatalog.Exchanges
                        .Where(x => x.Type != agent.Exchange && TradeLookup.GetHops(agent.Exchange, x.Type) >= 1)
                        .Select(x => x.Type)
                        .ToList();
                    mission.Destination = destinations.Count == 0 ? TradeExchangeType.Invalid : destinations[random.Next(destinations.Count)];
                    mission.Hops = mission.Destination == TradeExchangeType.Invalid ? 1 : TradeLookup.GetHops(agent.Exchange, mission.Destination);
                    mission.Volume = FactionAgentTuning.CourierVolume[tier];
                    mission.DeadlineMinutes = FreightContractRules.GetDeadlineMinutes(mission.Hops);
                    break;
                case AgentMissionType.MiningQuota:
                    mission.ItemResref = FactionAgentTuning.OreResref[tier];
                    mission.Quantity = FactionAgentTuning.OreQuantity[tier];
                    break;
                case AgentMissionType.ScanDown:
                    mission.Site = FactionAgentTuning.ScanDownSites[random.Next(FactionAgentTuning.ScanDownSites.Length)];
                    break;
                case AgentMissionType.DataHeist:
                    mission.Site = SignatureType.DataSite;
                    break;
            }

            mission.Credits = GetCredits(type, tier, mission.Hops, mission.Role, mission.Site);
            mission.Tokens = GetTokens(mission.Credits, slot == AgentMissionSlot.Intel);
            return mission;
        }

        // Payout

        /// <summary>The time a mission takes in minutes, as agents.py models it.</summary>
        public static double GetMinutes(AgentMissionType type, int tier, int hops, ShipNpcRole role, SignatureType site)
        {
            var travel = FactionAgentTuning.BaseMinutes + 2 * FactionAgentTuning.HopMinutes * hops;
            return type switch
            {
                AgentMissionType.Kill => travel + FactionAgentTuning.KillCount[role] * FactionAgentTuning.KillMinutes[role],
                AgentMissionType.SealedCourier => FactionAgentTuning.BaseMinutes + FactionAgentTuning.CourierStopMinutes +
                                                  FactionAgentTuning.HopMinutes * Math.Max(1, hops),
                AgentMissionType.MiningQuota => FactionAgentTuning.BaseMinutes + 2 * FactionAgentTuning.HopMinutes * 0 +
                                                FactionAgentTuning.OreQuantity[tier] / FactionAgentTuning.OrePerMinute[tier],
                AgentMissionType.ConvoyEscort => travel + FactionAgentTuning.ConvoyFlightMinutes +
                                                 FactionAgentTuning.ConvoyRaiders[tier] * FactionAgentTuning.RaiderKillMinutes,
                AgentMissionType.ScanDown => travel + FactionAgentTuning.ScanDownMinutes[site][tier],
                AgentMissionType.DataHeist => travel + FactionAgentTuning.HeistMinutes[tier],
                _ => 0,
            };
        }

        /// <summary>What the activity itself pays (kill loot, a site's cache, slicing), less the ore a quota gives up.</summary>
        public static double GetActivityValue(AgentMissionType type, int tier, ShipNpcRole role, SignatureType site)
        {
            return type switch
            {
                AgentMissionType.Kill => FactionAgentTuning.KillCount[role] * FactionAgentTuning.KillValue[tier],
                AgentMissionType.MiningQuota => -FactionAgentTuning.OreQuantity[tier] * FactionAgentTuning.OreValue[tier],
                AgentMissionType.ConvoyEscort => FactionAgentTuning.ConvoyRaiderValue[tier],
                AgentMissionType.ScanDown => FactionAgentTuning.ScanDownValue[site][tier],
                AgentMissionType.DataHeist => FactionAgentTuning.HeistValue[tier],
                _ => 0,
            };
        }

        /// <summary>
        /// A mission's credits: its time at the tier's agent rate, less what the activity already pays, never below
        /// <see cref="FactionAgentTuning.MinimumShare"/> of the time's worth, rounded to 25.
        /// </summary>
        public static int GetCredits(AgentMissionType type, int tier, int hops, ShipNpcRole role, SignatureType site)
        {
            tier = Math.Clamp(tier, 1, 6);
            var target = FactionAgentTuning.CreditsPerMinute[tier] * GetMinutes(type, tier, hops, role, site);
            var credits = Math.Max(target - GetActivityValue(type, tier, role, site), FactionAgentTuning.MinimumShare * target);
            return 25 * (int)Math.Floor(credits / 25 + 0.5);
        }

        /// <summary>Tokens on top of the credits: about a fifth of their worth, at least one; doubled for an intel lead.</summary>
        public static int GetTokens(int credits, bool intel)
        {
            var tokens = Math.Max(1, (int)Math.Floor(credits * FactionAgentTuning.TokenShare / FactionAgentTuning.TokenValue + 0.5));
            return intel ? tokens * FactionAgentTuning.IntelTokenMultiplier : tokens;
        }

        // Taking a mission

        /// <summary>Whether a player may take an offer, before any ship check a courier run needs.</summary>
        public static AgentRefusal CanAccept(
            AgentMission offer,
            int standing,
            IReadOnlyCollection<AgentMission> held,
            IReadOnlyCollection<string> taken,
            int intel)
        {
            if (offer == null)
                return AgentRefusal.OfferGone;
            if (TradeStandingRules.GetTier(standing) == TradeStandingTier.Hostile)
                return AgentRefusal.Hostile;
            if ((taken?.Contains(offer.Id) ?? false) || (held?.Any(x => x.Id == offer.Id) ?? false))
                return AgentRefusal.AlreadyTaken;
            if ((held?.Count ?? 0) >= FactionAgentTuning.MaximumActive)
                return AgentRefusal.TooManyMissions;
            if (standing < offer.MinimumStanding)
                return AgentRefusal.StandingTooLow;
            if (offer.Slot == AgentMissionSlot.Intel && intel < 1)
                return AgentRefusal.NoIntel;

            return AgentRefusal.None;
        }

        /// <summary>Keeps the taken offer ids of today and yesterday; older boards are gone anyway.</summary>
        public static void PruneTaken(List<string> taken, DateTime now)
        {
            if (taken == null)
                return;

            var today = GetDay(now);
            taken.RemoveAll(id =>
            {
                var parts = id.Split('_');
                return parts.Length < 4 || !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var day) || day < today - 1;
            });
        }

        public static string GetRefusalMessage(AgentRefusal refusal)
        {
            return refusal switch
            {
                AgentRefusal.OfferGone => "That mission is no longer offered.",
                AgentRefusal.AlreadyTaken => "You have already taken that mission.",
                AgentRefusal.TooManyMissions => $"You can hold at most {FactionAgentTuning.MaximumActive} agent missions.",
                AgentRefusal.Hostile => "The faction counts you as an enemy. Its agents will not deal with you.",
                AgentRefusal.StandingTooLow => "You need better standing with this faction for that.",
                AgentRefusal.NoIntel => "An intel lead needs agent intel. Slice data cans at data sites to find some.",
                AgentRefusal.NoShipDocked => "A courier run needs one of your ships with a cargo bay docked here.",
                AgentRefusal.NotEnoughRoom => "None of your ships docked here has room for the sealed load.",
                AgentRefusal.NotEnoughTokens => "You do not have enough tokens.",
                _ => string.Empty,
            };
        }

        // What counts toward a mission

        /// <summary>Whether destroying an NPC ship of this tier and role in this system counts toward a kill mission.</summary>
        public static bool IsKillCredit(AgentMission mission, string areaResref, int shipTier, ShipNpcRole role)
        {
            return mission != null &&
                   mission.Type == AgentMissionType.Kill &&
                   mission.OrbitResref == areaResref &&
                   mission.Tier == shipTier &&
                   (mission.Role == ShipNpcRole.Invalid || mission.Role == role);
        }

        /// <summary>Whether a courier delivery made now is late.</summary>
        public static bool IsCourierLate(AgentMission mission, DateTime now)
        {
            return mission.AcceptedAt != null && now > mission.AcceptedAt.Value.AddMinutes(mission.DeadlineMinutes);
        }

        // Text

        public static string GetFactionName(FactionType faction)
        {
            return faction == FactionType.Invalid ? string.Empty : faction.GetAttribute<FactionType, FactionAttribute>().Name;
        }

        public static string GetSystemName(string areaResref)
        {
            return TradeLookup.GetOrbit(areaResref)?.Name ?? areaResref;
        }

        public static string GetTypeName(AgentMissionType type)
        {
            return type switch
            {
                AgentMissionType.Kill => "Kill",
                AgentMissionType.SealedCourier => "Sealed Courier",
                AgentMissionType.MiningQuota => "Mining Quota",
                AgentMissionType.ConvoyEscort => "Convoy Escort",
                AgentMissionType.ScanDown => "Scan-Down",
                AgentMissionType.DataHeist => "Data Heist",
                _ => "Mission",
            };
        }

        private static string GetRoleName(ShipNpcRole role, int quantity)
        {
            var name = role switch
            {
                ShipNpcRole.Fighter => "fighter",
                ShipNpcRole.Interceptor => "interceptor",
                ShipNpcRole.Gunship => "gunship",
                _ => "ship",
            };
            return quantity == 1 ? name : name + "s";
        }

        private static string GetSiteName(SignatureType site)
        {
            return site switch
            {
                SignatureType.CombatSite => "combat site",
                SignatureType.CacheSite => "cache site",
                SignatureType.Derelict => "derelict",
                SignatureType.DataSite => "data site",
                _ => "site",
            };
        }

        /// <summary>The mission's quest name in the journal.</summary>
        public static string GetTitle(AgentMission mission)
        {
            var agent = GetAgent(mission.Agent);
            var faction = agent == null ? "Agent" : GetFactionName(agent.Faction);
            return $"{faction}: {GetTypeName(mission.Type)} (Tier {mission.Tier})";
        }

        /// <summary>What the mission asks, in one or two sentences.</summary>
        public static string Describe(AgentMission mission)
        {
            var system = GetSystemName(mission.OrbitResref);
            var enemy = FactionAgentTuning.TierFactionNames[Math.Clamp(mission.Tier, 1, 6)];
            return mission.Type switch
            {
                AgentMissionType.Kill =>
                    $"Destroy {mission.Quantity} {enemy} {GetRoleName(mission.Role, mission.Quantity)} (tier {mission.Tier}) in {system}. Kills by your party in the system count.",
                AgentMissionType.SealedCourier =>
                    $"Carry a sealed load of {mission.Volume} volume to {TradeLookup.GetExchange(mission.Destination)?.Name} within {mission.DeadlineMinutes} minutes " +
                    "and deliver it from the Freight tab of that dock's Trade Exchange. It loads into one of your ships docked here with room for it.",
                AgentMissionType.MiningQuota =>
                    $"Bring {mission.Quantity} {FactionAgentTuning.OreName[Math.Clamp(mission.Tier, 1, 6)]} to this agent and hand it in.",
                AgentMissionType.ConvoyEscort =>
                    $"Fly to {system}. A friendly convoy meets you there and makes for a starlane; pirate raiders will strike at it. It must reach the starlane.",
                AgentMissionType.ScanDown =>
                    $"Fly to {system} and scan down the {GetSiteName(mission.Site)} this agent marks for your party (a Probe Launcher), then clear it and open its site cache.",
                AgentMissionType.DataHeist =>
                    $"Fly to {system} and scan down a data site held by {enemy} ships. Slice its Target Data Can with a Data Analyzer.",
                _ => string.Empty,
            };
        }

        public static string DescribeReward(AgentMission mission)
        {
            return $"{mission.Credits} credits, +{mission.Standing} standing and {mission.Tokens} {GetTokenName(mission.Faction)}";
        }

        // Faction tokens

        /// <summary>The currency a faction's agents pay and its store takes (one per faction, plan section 29).</summary>
        public static CurrencyType GetTokenCurrency(FactionType faction)
        {
            return faction switch
            {
                FactionType.RepublicMilitary => CurrencyType.RepublicMilitaryToken,
                FactionType.Czerka => CurrencyType.CzerkaToken,
                FactionType.Smuggler => CurrencyType.SmugglerToken,
                FactionType.HuttCartel => CurrencyType.HuttCartelToken,
                FactionType.Mandalorian => CurrencyType.MandalorianToken,
                FactionType.SithMilitary => CurrencyType.SithMilitaryToken,
                _ => CurrencyType.Invalid,
            };
        }

        public static string GetTokenName(FactionType faction)
        {
            var currency = GetTokenCurrency(faction);
            return currency == CurrencyType.Invalid ? "tokens" : currency.GetAttribute<CurrencyType, CurrencyAttribute>().Name;
        }

        // Faction token stores

        /// <summary>
        /// A faction's store: the missile variants, the faction maker's Hull Designs and Advanced ships' schematics (Wild
        /// Space only ones left out), its manufacturer component and the Rift Keys: tier 1 up to the faction's highest dock tier,
        /// each priced and gated by its own tier, and tier 5.5.
        /// </summary>
        public static List<FactionTokenStoreItem> GetStoreItems(FactionType faction)
        {
            var items = new List<FactionTokenStoreItem>();
            if (!FactionAgentTuning.FactionMakers.TryGetValue(faction, out var maker))
                return items;

            FactionTokenStoreItem Item(FactionTokenStoreCategory category, string resref, string name, int quantity = 1,
                ShipComponentTraitType trait = ShipComponentTraitType.None) => new()
            {
                Category = category,
                Resref = resref,
                Name = name ?? string.Empty,
                Quantity = quantity,
                Tokens = FactionAgentTuning.StorePrices[category],
                MinimumTier = FactionAgentTuning.StoreGates[category],
                Trait = trait,
            };

            var makers = RevampShipCatalog.Hulls.ToDictionary(x => x.Tag, x => x.Manufacturer);
            bool Made(string shipTag) => makers.TryGetValue(shipTag, out var m) && m == maker;

            items.AddRange(FactionAgentTuning.StoreAmmo.Select(x => Item(FactionTokenStoreCategory.Ammo, x, null, FactionAgentTuning.AmmoQuantity)));
            items.AddRange(RevampRewardCatalog.HullDesigns
                .Where(x => Made(x.ShipTag) && !x.WildSpaceOnly)
                .Select(x => Item(FactionTokenStoreCategory.DesignSchematic, x.Resref, $"Design Schematic: {x.ShipName} {x.Name}")));
            // The faction maker's component, then any black-market maker's (W11o: the Chiss Sensor Array at the Smugglers
            // and the Hutt Cartel).
            var componentMakers = new List<ShipManufacturer> { maker };
            if (FactionAgentTuning.BlackMarketMakers.TryGetValue(faction, out var blackMarket))
                componentMakers.AddRange(blackMarket);
            foreach (var componentMaker in componentMakers)
            {
                var component = RevampComponentCatalog.ManufacturerComponents.FirstOrDefault(x => x.Manufacturer == componentMaker);
                if (component != null)
                    items.Add(Item(FactionTokenStoreCategory.ManufacturerComponent, component.Resref, component.Name, 1, component.Trait));
            }
            if (FactionAgentTuning.StoreRiftKeys.TryGetValue(faction, out var keys))
            {
                items.AddRange(keys.Select(key =>
                {
                    var item = Item(FactionTokenStoreCategory.RiftKey, key, null);
                    item.Tokens = FactionAgentTuning.RiftKeyPrices[key];
                    item.MinimumTier = FactionAgentTuning.RiftKeyGates[key];
                    return item;
                }));
            }
            items.AddRange(RevampRewardCatalog.ShipSchematics
                .Where(x => Made(x.ShipTag) && !x.WildSpaceOnly)
                .Select(x => Item(FactionTokenStoreCategory.ShipSchematic, x.Resref, $"Ship Schematic: {x.ShipName}")));
            return items;
        }

        /// <summary>Whether a player of this standing tier holding these tokens may buy the item.</summary>
        public static AgentRefusal CanBuy(FactionTokenStoreItem item, TradeStandingTier tier, int tokens)
        {
            if (item == null)
                return AgentRefusal.OfferGone;
            if (tier == TradeStandingTier.Hostile)
                return AgentRefusal.Hostile;
            if (tier < item.MinimumTier)
                return AgentRefusal.StandingTooLow;
            if (tokens < item.Tokens)
                return AgentRefusal.NotEnoughTokens;

            return AgentRefusal.None;
        }
    }
}
