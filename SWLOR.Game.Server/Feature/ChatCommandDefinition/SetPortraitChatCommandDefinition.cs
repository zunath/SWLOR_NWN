using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class SetPortraitChatCommandDefinition : ChatCommand.ChatCommandDefinition
    {
        public SetPortraitChatCommandDefinition()
            : base(
                "Sets portrait of the target player using the string specified. (Remember to add po_ to the portrait)",
                ChatCommand.CommandPermissionType.DM | ChatCommand.CommandPermissionType.Admin,
                HandleAction,
                HandleArgumentValidation,
                true)
        {
        }

        private static string HandleArgumentValidation(uint user, params string[] args)
        {
            if (args.Length <= 0)
            {
                return "Please enter the name of the portrait and try again. Example: /SetPortrait po_myportrait";
            }

            if (args[0].Length > 16)
            {
                return "The portrait you entered is too long. Portrait names should be between 1 and 16 characters.";
            }


            return string.Empty;
        }

        private static void HandleAction(uint user, uint target, Location targetLocation, params string[] args)
        {
            if (!GetIsObjectValid(target) || GetObjectType(target) != ObjectType.Creature)
            {
                SendMessageToPC(user, "Only creatures may be targeted with this command.");
                return;
            }

            SetPortraitResRef(target, args[0]);
            FloatingTextStringOnCreature("Your portrait has been changed.", target, false);
        }
    }
}
