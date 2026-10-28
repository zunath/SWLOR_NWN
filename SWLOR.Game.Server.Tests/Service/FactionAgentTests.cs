using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.ShipComponentDefinition;
using SWLOR.Game.Server.Feature.ShipDefinition;
using SWLOR.Game.Server.Feature.ShipRewardDefinition;
using SWLOR.Game.Server.Feature.TradeDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CurrencyService;
using SWLOR.Game.Server.Service.FactionAgentService;
using SWLOR.Game.Server.Service.FactionService;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Faction agents (plan section 29, W15a): placement, the daily board and each mission type, what counts toward a
/// mission, the payouts and standing gates, and the faction token stores. The numbers come from
/// tools/SpaceRevampBalance/agents.py, which also writes the reference fixture these tests read.
/// </summary>
public class FactionAgentTests
{
    private static readonly DateTime Now = new(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);

    private static readonly FactionType[] AgentFactions =
    {
        FactionType.RepublicMilitary, FactionType.Czerka, FactionType.Smuggler, FactionType.HuttCartel, FactionType.Mandalorian,
        FactionType.SithMilitary,
    };

    private static JObject Reference() => JObject.Parse(File.ReadAllText(
        Path.Combine(RepositoryRoot(), "SWLOR.Game.Server.Tests", "Fixtures", "FactionAgents", "FactionAgentReference.json")));

    // Placement

    [Test]
    public void Placement_TwoAgentsPerFactionAtDifferentDocksAndNoDockHostsMoreThanTwo()
    {
        FactionAgentTuning.Agents.Should().HaveCount(12);
        FactionAgentTuning.Agents.Select(x => x.Type).Should().OnlyHaveUniqueItems();
        Enum.GetValues<FactionAgentType>().Where(x => x != FactionAgentType.Invalid).Should().BeEquivalentTo(FactionAgentTuning.Agents.Select(x => x.Type));

        foreach (var faction in AgentFactions)
        {
            var agents = FactionAgentTuning.Agents.Where(x => x.Faction == faction).ToList();
            agents.Should().HaveCount(2, faction.ToString());
            agents[0].DockId.Should().NotBe(agents[1].DockId, faction.ToString());
            agents[0].DockTier.Should().BeLessThanOrEqualTo(agents[1].DockTier, "a faction's first agent is at its lower-tier dock");
        }

        FactionAgentTuning.Agents.GroupBy(x => x.DockId).Should().OnlyContain(x => x.Count() <= 2);
    }

    [Test]
    public void Placement_FollowsThePlansDockTable()
    {
        string Dock(FactionAgentType type) => FactionAgentRules.GetAgent(type).DockId;

        Dock(FactionAgentType.RepublicVeles).Should().Be("velesinterior");
        Dock(FactionAgentType.RepublicDantooine).Should().Be("dan_centcolony");
        Dock(FactionAgentType.CzerkaCZ220).Should().Be("czs220_hangar");
        Dock(FactionAgentType.CzerkaHutlar).Should().Be("hutlar_outpost");
        Dock(FactionAgentType.SmugglerMonCala).Should().Be("moncaladaccityex");
        Dock(FactionAgentType.SmugglerStation).Should().Be("spacenarshaddung");
        Dock(FactionAgentType.HuttTatooine).Should().Be("tat_anc_astropor");
        Dock(FactionAgentType.HuttSmugglersMoon).Should().Be("pw_ar_nsshipyard");
        Dock(FactionAgentType.MandalorianHutlar).Should().Be("hutlar_outpost");
        Dock(FactionAgentType.MandalorianDathomir).Should().Be("dath_landingpad");
        Dock(FactionAgentType.SithDathomir).Should().Be("dath_landingpad");
        Dock(FactionAgentType.SithKorriban).Should().Be("korribanlandingp");
    }

    [Test]
    public void Placement_DockTierIsTheExchangesTierAndEveryOrbitFliesTheMissionTier()
    {
        foreach (var agent in FactionAgentTuning.Agents)
        {
            TradeLookup.GetExchange(agent.Exchange).OrbitTier.Should().Be(agent.DockTier, agent.Name);
            agent.Orbits.Keys.Should().BeEquivalentTo(new[] { agent.DockTier, Math.Min(6, agent.DockTier + 1) }.Distinct(), agent.Name);
            foreach (var (tier, orbits) in agent.Orbits)
            {
                orbits.Should().NotBeEmpty();
                foreach (var orbit in orbits)
                {
                    FactionAgentTuning.MissionOrbits[tier].Should().Contain(orbit.AreaResref, $"{agent.Name} T{tier}");
                    TradeLookup.GetOrbit(orbit.AreaResref).Should().NotBeNull(orbit.AreaResref);
                }
            }
        }
    }

    [Test]
    public void Placement_EveryAgentStandsBesideItsDocksDockMaster()
    {
        foreach (var dock in FactionAgentTuning.Agents.GroupBy(x => x.DockId))
        {
            var area = JObject.Parse(File.ReadAllText(Path.Combine(RepositoryRoot(), "Module", "git", $"{dock.Key}.git.json"),
                System.Text.Encoding.Latin1));
            var creatures = area["Creature List"]!["value"]!.ToList();
            static string Local(JToken creature, string name) => creature["VarTable"]?["value"]?
                .FirstOrDefault(x => x["Name"]!["value"]!.Value<string>() == name)?["Value"]!["value"]!.ToString();

            var master = creatures.First(x => Local(x, "CONVERSATION") == "StarportDialog");
            foreach (var agent in dock)
            {
                var placed = creatures.Where(x => Local(x, "CONVERSATION") == "FactionAgentDialog" &&
                                                  Local(x, FactionAgents.AgentVariable) == ((int)agent.Type).ToString()).ToList();
                placed.Should().ContainSingle(agent.Name);
                var npc = placed[0];
                npc["FirstName"]!["value"]!["0"]!.Value<string>().Should().Be(agent.Name);
                npc["Plot"]!["value"]!.Value<int>().Should().Be(1);
                npc["ScriptDialogue"]!["value"]!.Value<string>().Should().Be(master["ScriptDialogue"]!["value"]!.Value<string>());
                var dx = npc["XPosition"]!["value"]!.Value<double>() - master["XPosition"]!["value"]!.Value<double>();
                var dy = npc["YPosition"]!["value"]!.Value<double>() - master["YPosition"]!["value"]!.Value<double>();
                Math.Sqrt(dx * dx + dy * dy).Should().BeInRange(0.5, 1.5, $"{agent.Name} stands about a meter from the Dock Master");
            }

            creatures.Where(x => Local(x, "CONVERSATION") == "FactionAgentDialog").Select(x => x["Tag"]!["value"]!.Value<string>())
                .Should().OnlyHaveUniqueItems();
        }
    }

    // The board and mission generation

    [Test]
    public void Board_IsTheSameForEveryoneThroughTheDayAndChangesTheNext()
    {
        foreach (var agent in FactionAgentTuning.Agents)
        {
            var board = FactionAgentRules.GetBoard(agent.Type, Now);
            var later = FactionAgentRules.GetBoard(agent.Type, Now.AddHours(11));
            board.Select(Describe).Should().Equal(later.Select(Describe), agent.Name);
            board.Select(x => x.Id).Should().OnlyHaveUniqueItems();
            FactionAgentRules.GetBoard(agent.Type, Now.AddDays(1)).Select(x => x.Id).Should().NotIntersectWith(board.Select(x => x.Id));
        }

        FactionAgentTuning.Agents.Any(a => FactionAgentRules.GetBoard(a.Type, Now).Select(Describe)
                .SequenceEqual(FactionAgentRules.GetBoard(a.Type, Now.AddDays(1)).Select(Describe)))
            .Should().BeFalse("the board rotates daily");
        FactionAgentRules.GetNextBoardChange(Now).Should().Be(new DateTime(2026, 10, 29, 0, 0, 0, DateTimeKind.Utc));
    }

    [Test]
    public void Board_HasStandardPriorityAndIntelSlotsAtTheRightTiers()
    {
        foreach (var agent in FactionAgentTuning.Agents)
        {
            for (var day = 0; day < 30; day++)
            {
                var board = FactionAgentRules.GetBoard(agent.Type, Now.AddDays(day));
                board.Should().HaveCount(FactionAgentTuning.StandardOffers + FactionAgentTuning.PriorityOffers + 1);
                var standard = board.Where(x => x.Slot == AgentMissionSlot.Standard).ToList();
                var priority = board.Where(x => x.Slot == AgentMissionSlot.Priority).ToList();
                var intel = board.Single(x => x.Slot == AgentMissionSlot.Intel);

                standard.Should().HaveCount(FactionAgentTuning.StandardOffers).And.OnlyContain(x => x.Tier == agent.DockTier && x.MinimumStanding == 0);
                priority.Should().HaveCount(FactionAgentTuning.PriorityOffers)
                    .And.OnlyContain(x => x.Tier == Math.Min(6, agent.DockTier + 1) && x.MinimumStanding == FactionAgentTuning.PriorityStanding);
                intel.Type.Should().Be(AgentMissionType.DataHeist);
                intel.Tier.Should().Be(Math.Min(6, agent.DockTier + 1));
                standard.Concat(priority).Select(x => x.Type).Should().OnlyHaveUniqueItems("no type repeats on one board");
                board.Should().OnlyContain(x => x.Agent == agent.Type && x.Faction == agent.Faction);
                standard.Should().OnlyContain(x => x.Standing == FactionAgentTuning.StandingStandard);
                priority.Should().OnlyContain(x => x.Standing == FactionAgentTuning.StandingPriority);
                intel.Standing.Should().Be(FactionAgentTuning.StandingIntel);
            }
        }
    }

    [Test]
    public void Board_OffersEveryMissionTypeOverAMonth()
    {
        foreach (var agent in FactionAgentTuning.Agents)
        {
            var types = Enumerable.Range(0, 60).SelectMany(day => FactionAgentRules.GetBoard(agent.Type, Now.AddDays(day))).Select(x => x.Type).ToHashSet();
            types.Should().BeEquivalentTo(agent.MissionWeights.Where(x => x.Value > 0).Select(x => x.Key), agent.Name);
        }
    }

    [Test]
    public void Missions_EachTypeCarriesItsTerms()
    {
        var missions = FactionAgentTuning.Agents
            .SelectMany(agent => Enumerable.Range(0, 60).SelectMany(day => FactionAgentRules.GetBoard(agent.Type, Now.AddDays(day))))
            .ToList();

        foreach (var mission in missions)
        {
            var agent = FactionAgentRules.GetAgent(mission.Agent);
            switch (mission.Type)
            {
                case AgentMissionType.Kill:
                    agent.Orbits[mission.Tier].Select(x => x.AreaResref).Should().Contain(mission.OrbitResref);
                    mission.Quantity.Should().Be(FactionAgentTuning.KillCount[mission.Role]);
                    break;
                case AgentMissionType.SealedCourier:
                    mission.Destination.Should().NotBe(agent.Exchange).And.NotBe(TradeExchangeType.Invalid);
                    mission.Hops.Should().Be(TradeLookup.GetHops(agent.Exchange, mission.Destination)).And.BeGreaterThan(0);
                    mission.Volume.Should().Be(FactionAgentTuning.CourierVolume[mission.Tier]);
                    mission.DeadlineMinutes.Should().Be(FreightContractRules.GetDeadlineMinutes(mission.Hops));
                    mission.OrbitResref.Should().BeEmpty();
                    break;
                case AgentMissionType.MiningQuota:
                    mission.ItemResref.Should().Be(FactionAgentTuning.OreResref[mission.Tier]);
                    mission.Quantity.Should().Be(FactionAgentTuning.OreQuantity[mission.Tier]);
                    File.Exists(Path.Combine(RepositoryRoot(), "Module", "uti", $"{mission.ItemResref}.uti.json")).Should().BeTrue();
                    break;
                case AgentMissionType.ConvoyEscort:
                    agent.Orbits[mission.Tier].Select(x => x.AreaResref).Should().Contain(mission.OrbitResref);
                    mission.Quantity.Should().Be(1);
                    break;
                case AgentMissionType.ScanDown:
                    agent.Orbits[mission.Tier].Select(x => x.AreaResref).Should().Contain(mission.OrbitResref);
                    FactionAgentTuning.ScanDownSites.Should().Contain(mission.Site);
                    break;
                case AgentMissionType.DataHeist:
                    agent.Orbits[mission.Tier].Select(x => x.AreaResref).Should().Contain(mission.OrbitResref);
                    mission.Site.Should().Be(SignatureType.DataSite);
                    break;
                default:
                    Assert.Fail($"unexpected type {mission.Type}");
                    break;
            }

            mission.Credits.Should().Be(FactionAgentRules.GetCredits(mission.Type, mission.Tier, mission.Hops, mission.Role, mission.Site));
            mission.Tokens.Should().Be(FactionAgentRules.GetTokens(mission.Credits, mission.Slot == AgentMissionSlot.Intel));
            FactionAgentRules.Describe(mission).Should().NotBeNullOrWhiteSpace();
            FactionAgentRules.IsMissionId(mission.Id).Should().BeTrue();
        }

        missions.Where(x => x.Type == AgentMissionType.Kill).Select(x => x.Role).Distinct().Should()
            .BeEquivalentTo(new[] { ShipNpcRole.Invalid, ShipNpcRole.Fighter, ShipNpcRole.Interceptor, ShipNpcRole.Gunship });
        missions.Where(x => x.Type == AgentMissionType.ScanDown).Select(x => x.Site).Distinct().Should().BeEquivalentTo(FactionAgentTuning.ScanDownSites);
    }

    [Test]
    public void Missions_BecomeOneStateRuntimeQuests()
    {
        var board = FactionAgentTuning.Agents.SelectMany(a => Enumerable.Range(0, 30).SelectMany(d => FactionAgentRules.GetBoard(a.Type, Now.AddDays(d))))
            .GroupBy(x => x.Type).Select(x => x.First()).ToList();
        board.Should().HaveCount(6);

        foreach (var mission in board)
        {
            var quest = AgentMissionQuest.Build(mission);
            quest.QuestId.Should().Be(mission.Id);
            quest.IsRepeatable.Should().BeFalse("a board offer is taken once");
            quest.CountsTowardAchievements.Should().BeFalse();
            quest.AllowRewardSelection.Should().BeFalse();
            quest.States.Keys.Should().Equal(1);
            quest.Rewards.Should().ContainSingle().Which.Should().BeOfType<AgentMissionReward>().Which.MissionId.Should().Be(mission.Id);
            quest.OnAbandonActions.Should().ContainSingle();
            quest.OnCompleteActions.Should().ContainSingle();
            quest.States[1].JournalText.Should().Contain(FactionAgentRules.Describe(mission));

            var objective = quest.States[1].GetObjectives().Should().ContainSingle().Subject;
            if (mission.Type == AgentMissionType.MiningQuota)
            {
                var collect = objective.Should().BeOfType<CollectItemObjective>().Subject;
                collect.Resref.Should().Be(mission.ItemResref);
                collect.Quantity.Should().Be(mission.Quantity);
            }
            else
            {
                var counter = objective.Should().BeOfType<CombatObjective>().Subject;
                counter.Key.Should().Be(FactionAgentRules.ObjectiveKey(mission.Id));
                counter.Amount.Should().Be(mission.Quantity);
            }
        }
    }

    // Completion checks

    [Test]
    public void Completion_KillsCountOnlyInTheNamedSystemAtTheMissionTierAndRole()
    {
        var mission = new AgentMission { Type = AgentMissionType.Kill, OrbitResref = "moncalaorbit", Tier = 2, Role = ShipNpcRole.Interceptor };
        FactionAgentRules.IsKillCredit(mission, "moncalaorbit", 2, ShipNpcRole.Interceptor).Should().BeTrue();
        FactionAgentRules.IsKillCredit(mission, "moncalaorbit", 2, ShipNpcRole.Fighter).Should().BeFalse();
        FactionAgentRules.IsKillCredit(mission, "moncalaorbit", 1, ShipNpcRole.Interceptor).Should().BeFalse();
        FactionAgentRules.IsKillCredit(mission, "viscaraorbit", 2, ShipNpcRole.Interceptor).Should().BeFalse();

        mission.Role = ShipNpcRole.Invalid;
        FactionAgentRules.IsKillCredit(mission, "moncalaorbit", 2, ShipNpcRole.Gunship).Should().BeTrue("any ship of the tier");
        mission.Type = AgentMissionType.ConvoyEscort;
        FactionAgentRules.IsKillCredit(mission, "moncalaorbit", 2, ShipNpcRole.Gunship).Should().BeFalse();
    }

    [Test]
    public void Completion_CourierIsLateAfterItsDeadline()
    {
        var mission = new AgentMission { Type = AgentMissionType.SealedCourier, DeadlineMinutes = 40, AcceptedAt = Now };
        FactionAgentRules.IsCourierLate(mission, Now.AddMinutes(39)).Should().BeFalse();
        FactionAgentRules.IsCourierLate(mission, Now.AddMinutes(41)).Should().BeTrue();
    }

    [Test]
    public void Completion_AgentLeadsAnswerOnlyTheHoldersPartyAndNeverRespawn()
    {
        var lead = SignatureSiteRules.CreateAgentLead("sig", "moncalaorbit", 2, SignatureType.CombatSite, 1, 2, 3, Now, "agent_1_666_0", "holder");
        lead.IsAgentLead.Should().BeTrue();
        lead.Tier.Should().Be(2);
        lead.SignalRequired.Should().Be(ScanningRules.SignalRequired(SignatureType.CombatSite, 2, false));
        SignatureSiteRules.CanScan(lead, new HashSet<string> { "holder", "friend" }).Should().BeTrue();
        SignatureSiteRules.CanScan(lead, new HashSet<string> { "stranger" }).Should().BeFalse();

        var normal = SignatureSiteRules.Create("n", "moncalaorbit", 2, SignatureType.CombatSite, 1, 2, 3, Now);
        normal.IsAgentLead.Should().BeFalse();
        SignatureSiteRules.CanScan(normal, new HashSet<string> { "stranger" }).Should().BeTrue();

        var scanning = ServerSource("Service", "SpaceScanning.cs");
        scanning.Should().Contain("if (!signature.IsAgentLead)\n                    respawns.Add(");
        scanning.Should().Contain("signatures.Count(x => x.Type != SignatureType.Rift && !x.IsAgentLead)");
        scanning.Should().Contain("signatures.Where(x => SignatureSiteRules.CanScan(x, partyIds))");
    }

    [Test]
    public void Completion_EachMissionsEventCreditsIt()
    {
        var events = ServerSource("Service", "FactionAgentEvents.cs");
        events.Should().Contain("[NWNEventHandler(ScriptName.OnCreatureDeathBefore)]\n        public static void CreditKillMissions()");
        events.Should().Contain("FactionAgentRules.IsKillCredit(x, areaResref, tier, role)");
        events.Should().Contain("[NWNEventHandler(ScriptName.OnAreaEnter)]\n        public static void StartMissionsInSystem()");
        events.Should().Contain("[NWNEventHandler(ScriptName.OnCreatureDeathAfter)]\n        public static void FailLostEscort()");
        events.Should().Contain("Space.PlaceSignature(signature);");

        ServerSource("Service", "TradeConvoys.cs").Should().Contain("FactionAgents.OnConvoyArrived(hauler);");
        ServerSource("Service", "SpaceScanSites.cs").Should().Contain("FactionAgents.OnSiteCompleted(signature, user, cache);");
        ServerSource("Service", "SpaceScanSites.cs").Should().Contain("FactionAgents.HoldHeistSite(signature, area, center);");
        ServerSource("Service", "SpaceSlicing.cs").Should().Contain("FactionAgents.OnHeistTargetSliced(slicer, target);");
        ServerSource("Service", "SpaceSlicing.cs").Should().Contain("FactionAgents.OnHeistTargetLost(target);");
        var freight = ServerSource("Service", "TradeFreight.cs");
        freight.Should().Contain("FactionAgents.OnCourierDelivered(player, contract.Id, result.IsLate);");
        freight.Should().Contain("FactionAgents.OnCourierLost(ship.OwnerPlayerId, contract.Id);");
        freight.Should().Contain("FactionAgents.Abandon(player, contract.Id);");
        ServerSource("Service", "SpaceNpcRoles.cs").Should().Contain("Trade.IsLawfulShip(creature)", "convoys and customs ships never count as kills");

        var agents = ServerSource("Service", "FactionAgents.cs");
        agents.Should().Contain("[NWNEventHandler(ScriptName.OnModuleEnter)]\n        public static void RegisterHeldMissions()");
        agents.Should().Contain("Quest.RegisterRuntimeQuest(AgentMissionQuest.Build(mission));");
        agents.Should().Contain("Quest.RecordCombatObjective(player, FactionAgentRules.ObjectiveKey(missionId), source);");
    }

    [Test]
    public void Courier_RunsAreFreightContractsOutsideTheFreightLimit()
    {
        var courier = FreightContractRules.AgentCourier("agent_1_666_0", TradeExchangeType.Viscara, TradeExchangeType.MonCala, "ship", 10, 30, Now);
        courier.IsAgentCourier.Should().BeTrue();
        courier.Fee.Should().Be(0);
        courier.Deposit.Should().Be(0);
        courier.Deadline.Should().Be(Now.AddMinutes(30));
        FreightContractRules.GetSealedLot(courier).Volume.Should().Be(10);

        var held = Enumerable.Range(0, TradeTuning.FreightMaximumActive)
            .Select(i => FreightContractRules.AgentCourier($"agent_1_666_{i}", TradeExchangeType.Viscara, TradeExchangeType.MonCala, "ship", 10, 30, Now))
            .ToList();
        FreightContractRules.CountFreight(held).Should().Be(0);
        var offer = FreightContractRules.GetOffers(TradeExchangeType.Viscara, Now)[0];
        FreightContractRules.CanAccept(offer, held, new List<string>(), 500, 500, 100000).Should().Be(FreightRefusal.None);
    }

    // Rewards and standing gates

    [Test]
    public void Rewards_MatchTheBalanceModel()
    {
        foreach (var token in Reference()["Cases"]!)
        {
            var type = Enum.Parse<AgentMissionType>(token["Type"]!.Value<string>()!);
            var role = Enum.Parse<ShipNpcRole>(token["Role"]!.Value<string>()!);
            var siteText = token["Site"]!.Value<string>();
            var site = string.IsNullOrEmpty(siteText) ? SignatureType.Invalid : Enum.Parse<SignatureType>(siteText);
            var tier = token["Tier"]!.Value<int>();
            var credits = FactionAgentRules.GetCredits(type, tier, token["Hops"]!.Value<int>(), role, site);
            var label = $"{type} T{tier} hops {token["Hops"]} {role} {site}";

            credits.Should().Be(token["Credits"]!.Value<int>(), label);
            FactionAgentRules.GetTokens(credits, false).Should().Be(token["Tokens"]!.Value<int>(), label);
            if (token["IntelTokens"] != null)
                FactionAgentRules.GetTokens(credits, true).Should().Be(token["IntelTokens"]!.Value<int>(), label);
            credits.Should().BePositive().And.Match(x => x % 25 == 0);
        }
    }

    [Test]
    public void Rewards_PayAbout100To115PercentOfTheTiersCombat()
    {
        foreach (var (tier, rate) in (JObject)Reference()["Rates"]!)
        {
            rate!["Percent"]!.Value<double>().Should().BeInRange(100, 115, $"T{tier}");
        }

        for (var tier = 1; tier <= 6; tier++)
        {
            var rate = FactionAgentTuning.CreditsPerMinute[tier] * 60;
            rate.Should().BeGreaterThan(FactionAgentTuning.KillValue[tier] * 60, "the agent rate sits above the kill rate it is built on");
        }
    }

    [Test]
    public void Rewards_HigherTiersPayMore()
    {
        for (var tier = 2; tier <= 6; tier++)
        {
            if (tier == 4)
                continue; // tier 4 combat pays less than tier 3 (trade.py's loot baseline), and agents follow it
            FactionAgentRules.GetCredits(AgentMissionType.SealedCourier, tier, 2, ShipNpcRole.Invalid, SignatureType.Invalid)
                .Should().BeGreaterThan(FactionAgentRules.GetCredits(AgentMissionType.SealedCourier, tier - 1, 2, ShipNpcRole.Invalid, SignatureType.Invalid));
        }

        FactionAgentRules.GetCredits(AgentMissionType.SealedCourier, 3, 3, ShipNpcRole.Invalid, SignatureType.Invalid)
            .Should().BeGreaterThan(FactionAgentRules.GetCredits(AgentMissionType.SealedCourier, 3, 1, ShipNpcRole.Invalid, SignatureType.Invalid), "longer runs pay more");
        FactionAgentRules.GetTokens(10, false).Should().Be(1, "every mission pays at least one token");
        FactionAgentRules.GetTokens(1000, true).Should().Be(2 * FactionAgentRules.GetTokens(1000, false));
    }

    [Test]
    public void Gates_StandingIntelAndLimits()
    {
        var board = FactionAgentRules.GetBoard(FactionAgentType.CzerkaHutlar, Now);
        var standard = board.First(x => x.Slot == AgentMissionSlot.Standard);
        var priority = board.First(x => x.Slot == AgentMissionSlot.Priority);
        var intel = board.First(x => x.Slot == AgentMissionSlot.Intel);
        var none = new List<AgentMission>();
        var taken = new List<string>();

        FactionAgentRules.CanAccept(standard, 0, none, taken, 0).Should().Be(AgentRefusal.None);
        FactionAgentRules.CanAccept(standard, TradeTuning.StandingHostile, none, taken, 0).Should().Be(AgentRefusal.Hostile);
        FactionAgentRules.CanAccept(priority, 0, none, taken, 0).Should().Be(AgentRefusal.StandingTooLow);
        FactionAgentRules.CanAccept(priority, FactionAgentTuning.PriorityStanding, none, taken, 0).Should().Be(AgentRefusal.None);
        FactionAgentRules.CanAccept(intel, 0, none, taken, 0).Should().Be(AgentRefusal.NoIntel);
        FactionAgentRules.CanAccept(intel, 0, none, taken, 1).Should().Be(AgentRefusal.None);
        FactionAgentRules.CanAccept(standard, 0, none, new List<string> { standard.Id }, 0).Should().Be(AgentRefusal.AlreadyTaken);
        FactionAgentRules.CanAccept(standard, 0, new List<AgentMission> { standard }, taken, 0).Should().Be(AgentRefusal.AlreadyTaken);
        var full = Enumerable.Range(0, FactionAgentTuning.MaximumActive).Select(i => new AgentMission { Id = $"x{i}" }).ToList();
        FactionAgentRules.CanAccept(standard, 0, full, taken, 0).Should().Be(AgentRefusal.TooManyMissions);
        FactionAgentRules.CanAccept(null, 0, none, taken, 0).Should().Be(AgentRefusal.OfferGone);
        FactionAgentTuning.PriorityStanding.Should().Be(TradeTuning.StandingTierMinimums[0], "priority missions open at Trusted");
    }

    [Test]
    public void Gates_TakenOffersArePrunedAfterADay()
    {
        var today = FactionAgentRules.GetDay(Now);
        var taken = new List<string>
        {
            FactionAgentRules.OfferId(FactionAgentType.RepublicVeles, today, 0),
            FactionAgentRules.OfferId(FactionAgentType.RepublicVeles, today - 1, 1),
            FactionAgentRules.OfferId(FactionAgentType.RepublicVeles, today - 2, 2),
            "junk",
        };
        FactionAgentRules.PruneTaken(taken, Now);
        taken.Should().Equal(FactionAgentRules.OfferId(FactionAgentType.RepublicVeles, today, 0),
            FactionAgentRules.OfferId(FactionAgentType.RepublicVeles, today - 1, 1));
    }

    // Token stores

    [Test]
    public void Tokens_AreOneCurrencyPerFaction()
    {
        var currencies = AgentFactions.Select(FactionAgentRules.GetTokenCurrency).ToList();
        currencies.Should().OnlyHaveUniqueItems().And.NotContain(CurrencyType.Invalid);
        foreach (var currency in currencies)
        {
            var detail = currency.GetAttribute<CurrencyType, CurrencyAttribute>();
            detail.Name.Should().EndWith("Tokens");
            detail.IconResref.Should().StartWith("iit_coin_", "token icons reuse the existing coin icons");
        }

        FactionAgentRules.GetTokenCurrency(FactionType.Jedi).Should().Be(CurrencyType.Invalid);
    }

    [Test]
    public void Stores_SellRiftKeysComponentsSchematicsAndAmmo()
    {
        var reference = Reference();
        var makers = RevampShipCatalog.Hulls.ToDictionary(x => x.Tag, x => x.Manufacturer);
        foreach (var faction in AgentFactions)
        {
            var items = FactionAgentRules.GetStoreItems(faction);
            var maker = FactionAgentTuning.FactionMakers[faction];
            items.Select(x => (x.Category.ToString(), x.Resref, x.Quantity)).Should().Equal(
                reference["Stores"]![faction.ToString()]!.Select(x => (x["Category"]!.Value<string>(), x["Resref"]!.Value<string>(), x["Quantity"]!.Value<int>())),
                faction.ToString());

            items.Should().Contain(x => x.Category == FactionTokenStoreCategory.RiftKey && x.Resref == "rift_key_t55");
            items.Where(x => x.Category == FactionTokenStoreCategory.Ammo).Should().HaveCount(3);
            var component = items.Single(x => x.Category == FactionTokenStoreCategory.ManufacturerComponent);
            var source = RevampComponentCatalog.ManufacturerComponents.Single(x => x.Manufacturer == maker);
            component.Resref.Should().Be(source.Resref);
            component.Trait.Should().Be(source.Trait);
            items.Where(x => x.Category == FactionTokenStoreCategory.DesignSchematic).Should().NotBeEmpty(faction.ToString())
                .And.OnlyContain(x => RevampRewardCatalog.HullDesigns.Any(d => d.Resref == x.Resref && makers[d.ShipTag] == maker && !d.WildSpaceOnly));
            items.Where(x => x.Category == FactionTokenStoreCategory.ShipSchematic)
                .Should().OnlyContain(x => RevampRewardCatalog.ShipSchematics.Any(s => s.Resref == x.Resref && makers[s.ShipTag] == maker && !s.WildSpaceOnly));
            foreach (var item in items)
            {
                if (item.Category == FactionTokenStoreCategory.RiftKey)
                {
                    var key = reference["RiftKeys"]![item.Resref]!;
                    item.Tokens.Should().Be(key["Tokens"]!.Value<int>(), item.Resref);
                    item.MinimumTier.Should().Be(Enum.Parse<TradeStandingTier>(key["MinimumTier"]!.Value<string>()!), item.Resref);
                    continue;
                }

                item.Tokens.Should().Be(FactionAgentTuning.StorePrices[item.Category]);
                item.MinimumTier.Should().Be(FactionAgentTuning.StoreGates[item.Category]);
                File.Exists(Path.Combine(RepositoryRoot(), "Module", "uti", $"{item.Resref}.uti.json")).Should().BeTrue(item.Resref);
            }
        }

        foreach (var (category, price) in (JObject)reference["StorePrices"]!)
            FactionAgentTuning.StorePrices[Enum.Parse<FactionTokenStoreCategory>(category)].Should().Be(price!.Value<int>());
    }

    [Test]
    public void Stores_GateByStandingAndTokens()
    {
        var item = new FactionTokenStoreItem { Category = FactionTokenStoreCategory.RiftKey, Tokens = 20, MinimumTier = TradeStandingTier.Honored };
        FactionAgentRules.CanBuy(item, TradeStandingTier.Honored, 20).Should().Be(AgentRefusal.None);
        FactionAgentRules.CanBuy(item, TradeStandingTier.Exalted, 25).Should().Be(AgentRefusal.None);
        FactionAgentRules.CanBuy(item, TradeStandingTier.Trusted, 100).Should().Be(AgentRefusal.StandingTooLow);
        FactionAgentRules.CanBuy(item, TradeStandingTier.Honored, 19).Should().Be(AgentRefusal.NotEnoughTokens);
        FactionAgentRules.CanBuy(item, TradeStandingTier.Hostile, 100).Should().Be(AgentRefusal.Hostile);
        FactionAgentRules.CanBuy(null, TradeStandingTier.Exalted, 100).Should().Be(AgentRefusal.OfferGone);
        FactionAgentTuning.StoreGates[FactionTokenStoreCategory.Ammo].Should().Be(TradeStandingTier.Neutral);
        FactionAgentTuning.StoreGates[FactionTokenStoreCategory.ShipSchematic].Should().Be(TradeStandingTier.Exalted);
    }

    // Persistence and wiring

    [Test]
    public void Persistence_HeldMissionsTakenOffersAndIntelLiveOnThePlayerRecord()
    {
        var player = new SWLOR.Game.Server.Entity.Player("id");
        player.AgentMissions.Should().BeEmpty();
        player.AgentOffersTaken.Should().BeEmpty();
        player.AgentIntel.Should().Be(0);

        var mission = FactionAgentRules.GetBoard(FactionAgentType.SithKorriban, Now)[0].Copy();
        mission.AcceptedAt = Now;
        player.AgentMissions.Add(mission);
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(player);
        var loaded = Newtonsoft.Json.JsonConvert.DeserializeObject<SWLOR.Game.Server.Entity.Player>(json)!;
        Describe(loaded.AgentMissions.Single()).Should().Be(Describe(mission));
        loaded.AgentMissions.Single().AcceptedAt.Should().Be(Now);

        ServerSource("Service", "SpaceSlicing.cs").Should().Contain("public static bool AgentIntelAvailable => true;");
        ServerSource("Feature", "DialogDefinition", "FactionAgentDialog.cs").Should().Contain("class FactionAgentDialog : ConversationMenuDefinition");
    }

    private static string Describe(AgentMission x) =>
        $"{x.Id}|{x.Type}|{x.Slot}|{x.Tier}|{x.OrbitResref}|{x.Hops}|{x.Role}|{x.Quantity}|{x.ItemResref}|{x.Destination}|{x.Site}|{x.Volume}|{x.Credits}|{x.Tokens}|{x.Standing}";

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory!.FullName;
    }

    private static string ServerSource(params string[] path)
    {
        var full = Path.Combine(new[] { RepositoryRoot(), "SWLOR.Game.Server" }.Concat(path).ToArray());
        return File.ReadAllText(full).Replace("\r\n", "\n");
    }
}
