using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Domain.UI
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
