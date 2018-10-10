using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;


namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Saves your character.", CommandPermissionType.Player)]
    public class Save: IChatCommand
    {
        private readonly INWScript _;
        private readonly IPlayerService _player;

        public Save(INWScript script,
            IPlayerService player)
        {
            _ = script;
            _player = player;
        }

        /// <summary>
        /// Exports user's character bic file.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="args"></param>
        public void DoAction(NWPlayer user, params string[] args)
        {
            _player.SaveCharacter(user);
            _player.SaveLocation(user);
            _.ExportSingleCharacter(user.Object);
            _.SendMessageToPC(user.Object, "Character saved successfully.");
        }
    }
}
