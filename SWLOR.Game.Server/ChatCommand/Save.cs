using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;


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

        /// <summary>
        /// Exports user's character bic file.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="args"></param>
        public void DoAction(NWPlayer user, params string[] args)
        {
            _.ExportSingleCharacter(user.Object);
            _.SendMessageToPC(user.Object, "Character saved successfully.");
        }
    }
}
