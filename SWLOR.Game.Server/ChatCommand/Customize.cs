using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Customizes your character appearance. Only available for use in the entry area.", CommandPermissionType.Player)]
    public class Customize: IChatCommand
    {
        private readonly IDialogService _dialog;

        public Customize(IDialogService dialog)
        {
            _dialog = dialog;
        }

        /// <summary>
        /// Opens the character customization menu.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="target"></param>
        /// <param name="targetLocation"></param>
        /// <param name="args"></param>
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            _dialog.StartConversation(user, user, "CharacterCustomization");
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            string areaResref = user.Area.Resref;

            if (areaResref != "ooc_area")
            {
                return "Customization can only occur in the starting area. You can't use this command any more.";
            }

            return string.Empty;
        }

        public bool RequiresTarget => false;
    }
}
