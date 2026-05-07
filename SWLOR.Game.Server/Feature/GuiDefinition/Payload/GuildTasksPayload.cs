using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class GuildTasksPayload: GuiPayloadBase
    {
        public GuildType Guild { get; }
        public uint GuildMaster { get; }

        public GuildTasksPayload(GuildType guild, uint guildMaster)
        {
            Guild = guild;
            GuildMaster = guildMaster;
        }
    }
}
