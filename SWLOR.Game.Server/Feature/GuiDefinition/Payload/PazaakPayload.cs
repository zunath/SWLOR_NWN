using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class PazaakPayload : GuiPayloadBase
    {
        public uint Table { get; set; }
        public string NpcProfileId { get; set; }
        public string NpcRewardId { get; set; }
        public string NpcDisplayName { get; set; }

        public PazaakPayload()
        {
            NpcProfileId = string.Empty;
            NpcRewardId = string.Empty;
            NpcDisplayName = string.Empty;
        }

        public PazaakPayload(uint table, string npcProfileId, string npcRewardId = "", string npcDisplayName = "")
        {
            Table = table;
            NpcProfileId = npcProfileId;
            NpcRewardId = npcRewardId;
            NpcDisplayName = npcDisplayName;
        }
    }
}
