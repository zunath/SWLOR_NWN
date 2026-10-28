using System;
using System.Linq;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.FactionAgentService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    /// <summary>
    /// A faction agent (plan section 29): the daily mission board, the player's missions, and the faction's token store
    /// (requisitions). The NPC carries <see cref="FactionAgents.AgentVariable"/>.
    /// </summary>
    public class FactionAgentDialog : ConversationMenuDefinition
    {
        private class Model
        {
            public string OfferId { get; set; }
            public string MissionId { get; set; }
            public int StoreIndex { get; set; } = -1;
        }

        private const string MainPageId = "MAIN_PAGE";
        private const string BoardPageId = "BOARD_PAGE";
        private const string OfferPageId = "OFFER_PAGE";
        private const string MissionsPageId = "MISSIONS_PAGE";
        private const string MissionPageId = "MISSION_PAGE";
        private const string AbandonPageId = "ABANDON_PAGE";
        private const string StorePageId = "STORE_PAGE";
        private const string BuyPageId = "BUY_PAGE";

        public override ConversationMenuSpec Build()
        {
            return new ConversationMenuBuilder()
                .WithDataModel(new Model())
                .AddPage(MainPageId, MainPageInit)
                .AddPage(BoardPageId, BoardPageInit)
                .AddPage(OfferPageId, OfferPageInit)
                .AddPage(MissionsPageId, MissionsPageInit)
                .AddPage(MissionPageId, MissionPageInit)
                .AddPage(AbandonPageId, AbandonPageInit)
                .AddPage(StorePageId, StorePageInit)
                .AddPage(BuyPageId, BuyPageInit)
                .Build();
        }

        private FactionAgentDetail Agent => FactionAgents.GetAgent(Owner);

        private string StandingLine(FactionAgentDetail agent)
        {
            var standing = Trade.GetStanding(Player, agent.Faction);
            var tier = TradeStandingRules.GetTier(standing);
            return $"{FactionAgentRules.GetFactionName(agent.Faction)} standing: {TradeStandingRules.GetTierName(tier)} ({standing}). " +
                   $"{FactionAgentRules.GetTokenName(agent.Faction)}: {FactionAgents.GetTokens(Player, agent)}. Agent intel: {FactionAgents.GetIntel(Player)}.";
        }

        private void MainPageInit(ConversationMenuPage page)
        {
            var agent = Agent;
            if (agent == null)
            {
                page.Header = "This agent has nothing for you.";
                return;
            }

            if (TradeStandingRules.GetTier(Trade.GetStanding(Player, agent.Faction)) == TradeStandingTier.Hostile)
            {
                page.Header = $"{agent.Name}\n\nI do not deal with enemies of the {FactionAgentRules.GetFactionName(agent.Faction)}. Leave.";
                return;
            }

            page.Header = $"{agent.Name}, {FactionAgentRules.GetFactionName(agent.Faction)} agent\n\n{agent.Greeting}\n\n{StandingLine(agent)}";
            page.AddResponse("Mission board", () => GoToPage(BoardPageId));
            page.AddResponse($"Your missions ({FactionAgents.GetMissions(Player).Count} of {FactionAgentTuning.MaximumActive})", () => GoToPage(MissionsPageId));
            page.AddResponse("Requisitions", () => GoToPage(StorePageId));
        }

        private void BoardPageInit(ConversationMenuPage page)
        {
            var agent = Agent;
            var now = DateTime.UtcNow;
            page.Header = $"Mission board. New missions at {FactionAgentRules.GetNextBoardChange(now):HH:mm} UTC.\n\n{StandingLine(agent)}";
            foreach (var offer in FactionAgentRules.GetBoard(agent.Type, now))
            {
                var refusal = FactionAgents.GetRefusal(Player, agent, offer);
                var label = $"{(offer.Slot == AgentMissionSlot.Intel ? "Intel lead: " : offer.Slot == AgentMissionSlot.Priority ? "Priority: " : string.Empty)}" +
                            $"{FactionAgentRules.GetTypeName(offer.Type)} (Tier {offer.Tier}), {offer.Credits} cr, {offer.Tokens} tokens";
                if (refusal == AgentRefusal.AlreadyTaken)
                    label = ColorToken.Gray(label + " - taken");
                else if (refusal != AgentRefusal.None)
                    label = ColorToken.Red(label);

                var offerId = offer.Id;
                page.AddResponse(label, () =>
                {
                    Data<Model>().OfferId = offerId;
                    GoToPage(OfferPageId);
                });
            }
        }

        private void OfferPageInit(ConversationMenuPage page)
        {
            var agent = Agent;
            var offer = FactionAgentRules.GetBoard(agent.Type, DateTime.UtcNow).FirstOrDefault(x => x.Id == Data<Model>().OfferId);
            if (offer == null)
            {
                page.Header = FactionAgentRules.GetRefusalMessage(AgentRefusal.OfferGone);
                return;
            }

            var refusal = FactionAgents.GetRefusal(Player, agent, offer);
            var needs = offer.MinimumStanding > 0
                ? $"\nNeeds {TradeStandingRules.GetTierName(TradeStandingRules.GetTier(offer.MinimumStanding))} standing ({offer.MinimumStanding})."
                : string.Empty;
            if (offer.Slot == AgentMissionSlot.Intel)
                needs += "\nSpends one agent intel.";
            page.Header = $"{FactionAgentRules.GetTitle(offer)}\n\n{FactionAgentRules.Describe(offer)}\n\nReward: {FactionAgentRules.DescribeReward(offer)}.{needs}" +
                          (refusal == AgentRefusal.None ? string.Empty : $"\n\n{FactionAgentRules.GetRefusalMessage(refusal)}");
            if (refusal != AgentRefusal.None)
                return;

            page.AddResponse(ColorToken.Green("Accept"), () =>
            {
                FactionAgents.Accept(Player, Owner, offer.Id);
                GoToPage(MissionsPageId, false);
            });
        }

        private void MissionsPageInit(ConversationMenuPage page)
        {
            var missions = FactionAgents.GetMissions(Player);
            page.Header = missions.Count == 0
                ? "You hold no agent missions."
                : $"Your agent missions ({missions.Count} of {FactionAgentTuning.MaximumActive}). Missions pay the moment they are done, except a mining quota, which you hand in to the agent who gave it.";
            foreach (var mission in missions)
            {
                var agent = FactionAgentRules.GetAgent(mission.Agent);
                var missionId = mission.Id;
                page.AddResponse($"{FactionAgentRules.GetTitle(mission)} - {agent?.Name}", () =>
                {
                    Data<Model>().MissionId = missionId;
                    GoToPage(MissionPageId);
                });
            }
        }

        private void MissionPageInit(ConversationMenuPage page)
        {
            var mission = FactionAgents.GetMissions(Player).FirstOrDefault(x => x.Id == Data<Model>().MissionId);
            if (mission == null)
            {
                page.Header = "That mission is over.";
                return;
            }

            var giver = FactionAgentRules.GetAgent(mission.Agent);
            var quest = Quest.GetQuestByIdOrDefault(mission.Id);
            var progress = quest?.States[1].GetObjectives().FirstOrDefault()?.GetCurrentStateText(Player, mission.Id) ?? string.Empty;
            page.Header = $"{FactionAgentRules.GetTitle(mission)}, from {giver?.Name} at {giver?.DockName}\n\n{FactionAgentRules.Describe(mission)}\n\n" +
                          $"Progress: {progress}\nReward: {FactionAgentRules.DescribeReward(mission)}.";

            if (mission.Type == AgentMissionType.MiningQuota && mission.Agent == Agent.Type)
            {
                page.AddResponse(ColorToken.Green("Hand in the ore"), () =>
                {
                    FactionAgents.HandIn(Player, Owner, mission.Id);
                    Close();
                });
            }

            page.AddResponse(ColorToken.Red("Abandon this mission"), () => GoToPage(AbandonPageId));
        }

        private void AbandonPageInit(ConversationMenuPage page)
        {
            var missionId = Data<Model>().MissionId;
            page.Header = "Abandon this mission? You cannot take it again, and a courier run's load is taken off your ship.";
            page.AddResponse(ColorToken.Red("Abandon"), () =>
            {
                FactionAgents.Abandon(Player, missionId);
                GoToPage(MissionsPageId, false);
            });
        }

        private void StorePageInit(ConversationMenuPage page)
        {
            var agent = Agent;
            var tier = TradeStandingRules.GetTier(Trade.GetStanding(Player, agent.Faction));
            page.Header = $"{FactionAgentRules.GetFactionName(agent.Faction)} requisitions, paid in {FactionAgentRules.GetTokenName(agent.Faction)}.\n\n{StandingLine(agent)}";
            var items = FactionAgentRules.GetStoreItems(agent.Faction);
            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                var label = $"{(item.Quantity > 1 ? $"{item.Quantity}x " : string.Empty)}{FactionAgents.GetStoreItemName(item)} - {item.Tokens} tokens";
                if (tier < item.MinimumTier)
                    label = ColorToken.Gray($"{label} (needs {TradeStandingRules.GetTierName(item.MinimumTier)})");

                var itemIndex = index;
                page.AddResponse(label, () =>
                {
                    Data<Model>().StoreIndex = itemIndex;
                    GoToPage(BuyPageId);
                });
            }
        }

        private void BuyPageInit(ConversationMenuPage page)
        {
            var agent = Agent;
            var items = FactionAgentRules.GetStoreItems(agent.Faction);
            var index = Data<Model>().StoreIndex;
            var item = index >= 0 && index < items.Count ? items[index] : null;
            var tokens = FactionAgents.GetTokens(Player, agent);
            var refusal = FactionAgentRules.CanBuy(item, TradeStandingRules.GetTier(Trade.GetStanding(Player, agent.Faction)), tokens);
            if (item == null)
            {
                page.Header = FactionAgentRules.GetRefusalMessage(AgentRefusal.OfferGone);
                return;
            }

            page.Header = $"{item.Quantity}x {FactionAgents.GetStoreItemName(item)} for {item.Tokens} {FactionAgentRules.GetTokenName(agent.Faction)}. " +
                          $"You hold {tokens}.{(refusal == AgentRefusal.None ? string.Empty : $"\n\n{FactionAgentRules.GetRefusalMessage(refusal)}")}";
            if (refusal != AgentRefusal.None)
                return;

            page.AddResponse(ColorToken.Green("Requisition"), () => FactionAgents.Buy(Player, Owner, index));
        }
    }
}
