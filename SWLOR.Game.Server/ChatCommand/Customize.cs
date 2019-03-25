using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Customizes your character appearance. Only available for use in the entry area or DM customization area.", CommandPermissionType.Player)]
    public class Customize: IChatCommand
    {
        /// <summary>
        /// Opens the character customization menu.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="target"></param>
        /// <param name="targetLocation"></param>
        /// <param name="args"></param>
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            DialogService.StartConversation(user, user, "CharacterCustomization");
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            string areaResref = user.Area.Resref;

            if (areaResref != "ooc_area" && areaResref != "customize_char")
            {
                return "Customization can only occur in the starting area or the DM customization area. You can't use this command any more.";
            }

            return string.Empty;
        }

        public bool RequiresTarget => false;
    }
}
