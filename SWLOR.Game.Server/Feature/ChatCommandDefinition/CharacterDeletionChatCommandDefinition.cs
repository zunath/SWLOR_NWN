using System;
using System.Globalization;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Service;
using Player = NWN.FinalFantasy.Entity.Player;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class CharacterDeletionChatCommandDefinition : ChatCommand.ChatCommandDefinition
    {
        public CharacterDeletionChatCommandDefinition()
            : base(
            "Permanently deletes your character.",
            ChatCommand.CommandPermissionType.Player | ChatCommand.CommandPermissionType.DM | ChatCommand.CommandPermissionType.Admin,
            HandleAction,
            HandleArgumentValidation,
            false)
        {
        }

        private static string HandleArgumentValidation(uint user, params string[] args)
        {
            if (!GetIsPC(user) || GetIsDM(user))
                return "You can only delete a player character.";

            string cdKey = GetPCPublicCDKey(user);
            string enteredCDKey = args.Length > 0 ? args[0] : string.Empty;

            if (cdKey != enteredCDKey)
            {
                return "Invalid CD key entered. Please enter the command as follows: \"/delete <CD Key>\". You can retrieve your CD key with the /CDKey chat command.";
            }

            return string.Empty;
        }

        private static void HandleAction(uint user, uint target, Location targetLocation, params string[] args)
        {
            string lastSubmission = GetLocalString(user, "DELETE_CHARACTER_LAST_SUBMISSION");
            bool isFirstSubmission = true;

            // Check for the last submission, if any.
            if (!string.IsNullOrWhiteSpace(lastSubmission))
            {
                // Found one, parse it.
                DateTime dateTime = DateTime.Parse(lastSubmission);
                if (DateTime.UtcNow <= dateTime.AddSeconds(30))
                {
                    // Player submitted a second request within 30 seconds of the last one. 
                    // This is a confirmation they want to delete.
                    isFirstSubmission = false;
                }
            }

            // Player hasn't submitted or time has elapsed
            if (isFirstSubmission)
            {
                SetLocalString(user, "DELETE_CHARACTER_LAST_SUBMISSION", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));
                FloatingTextStringOnCreature("Please confirm your deletion by entering another \"/delete <CD Key>\" command within 30 seconds.", user, false);
            }
            else
            {
                var playerID = GetObjectUUID(user);
                var entity = DB.Get<Player>(playerID);
                entity.IsDeleted = true;
                DB.Set(playerID, entity);
                
                BootPC(user, "Your character has been deleted.");
                Administration.DeletePlayerCharacter(user, true);

            }
        }
    }
}
