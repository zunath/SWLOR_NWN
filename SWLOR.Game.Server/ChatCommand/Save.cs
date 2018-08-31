using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Saves your character.", CommandPermissionType.Player)]
    public class Save: IChatCommand
    {
        private readonly INWScript _;

        public Save(INWScript script)
        {
            _ = script;
        }

        public void DoAction(NWPlayer user, params string[] args)
        {
            _.ExportSingleCharacter(user.Object);
            _.SendMessageToPC(user.Object, "Character saved successfully.");
        }
    }
}
