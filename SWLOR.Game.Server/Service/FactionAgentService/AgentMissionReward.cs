using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Service.FactionAgentService
{
    /// <summary>An agent mission's pay: its credits, standing and faction tokens, from the terms on the player's held mission.</summary>
    public class AgentMissionReward : IQuestReward
    {
        public string MissionId { get; }
        public bool IsSelectable => false;
        public string MenuName => "Agent payment";

        public AgentMissionReward(string missionId)
        {
            MissionId = missionId;
        }

        public void GiveReward(uint player)
        {
            FactionAgents.PayMission(player, MissionId);
        }
    }
}
