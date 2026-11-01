using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Service.FactionAgentService
{
    /// <summary>
    /// Builds the runtime quest of an agent mission (plan section 29, <see cref="Quest.RegisterRuntimeQuest"/>). One
    /// state: a counter the mission's event credits (pays the moment it is done), or the ore a mining quota hands in.
    /// </summary>
    public static class AgentMissionQuest
    {
        public static QuestDetail Build(AgentMission mission)
        {
            var quest = new QuestDetail
            {
                QuestId = mission.Id,
                Name = FactionAgentRules.GetTitle(mission),
                IsRepeatable = false,
                AllowRewardSelection = false,
                // Missions come from a daily board, so they would farm quest achievements.
                CountsTowardAchievements = false,
            };

            var missionId = mission.Id;
            quest.Rewards.Add(new AgentMissionReward(missionId));
            quest.OnAbandonActions.Add(player => FactionAgents.OnMissionAbandoned(player, missionId));
            quest.OnCompleteActions.Add((player, _) => FactionAgents.OnMissionCompleted(player, missionId));

            var state = new QuestStateDetail
            {
                JournalText = $"{FactionAgentRules.Describe(mission)}\n\nReward: {FactionAgentRules.DescribeReward(mission)}.",
            };
            state.AddObjective(mission.Type == AgentMissionType.MiningQuota
                ? new CollectItemObjective(mission.ItemResref, mission.Quantity)
                : new CombatObjective(FactionAgentRules.ObjectiveKey(mission.Id), GetObjectiveText(mission), mission.Quantity));
            quest.States[1] = state;
            return quest;
        }

        public static string GetObjectiveText(AgentMission mission)
        {
            return mission.Type switch
            {
                AgentMissionType.Kill => "ships destroyed",
                AgentMissionType.SealedCourier => "sealed load delivered",
                AgentMissionType.ConvoyEscort => "convoy brought through",
                AgentMissionType.ScanDown => "site cleared",
                AgentMissionType.DataHeist => "target data can sliced",
                _ => "done",
            };
        }
    }
}
