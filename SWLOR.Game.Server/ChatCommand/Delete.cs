﻿using System;
using System.Globalization;
using System.Linq;
using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Displays your public CD key.", CommandPermissionType.Player)]
    public class Delete : IChatCommand
    {
        private readonly INWScript _;
        private readonly INWNXAdmin _admin;
        private readonly IDataService _data;

        public Delete(
            INWScript script,
            INWNXAdmin admin,
            IDataService data)
        {
            _ = script;
            _admin = admin;
            _data = data;
        }

        /// <summary>
        /// Deletes a player's character. Player must submit the command twice within 30 seconds.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="target"></param>
        /// <param name="targetLocation"></param>
        /// <param name="args"></param>
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            string lastSubmission = user.GetLocalString("DELETE_CHARACTER_LAST_SUBMISSION");
            bool isFirstSubmission = true;

            // Check for the last submission, if any.
            if (!string.IsNullOrWhiteSpace(lastSubmission))
            {
                // Found one, parse it.
                DateTime dateTime = DateTime.Parse(lastSubmission);
                if(DateTime.UtcNow <= dateTime.AddSeconds(30))
                {
                    // Player submitted a second request within 30 seconds of the last one. 
                    // This is a confirmation they want to delete.
                    isFirstSubmission = false;
                }
            }

            // Player hasn't submitted or time has elapsed
            if (isFirstSubmission)
            {
                user.SetLocalString("DELETE_CHARACTER_LAST_SUBMISSION", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));
                user.FloatingText("Please confirm your deletion by entering another \"/delete <CD Key>\" command within 30 seconds.");
            }
            else
            {
                Player dbPlayer = _data.Get<Player>(user.GlobalID);
                dbPlayer.IsDeleted = true;
                _data.SubmitDataChange(dbPlayer, DatabaseActionType.Update);

                _.BootPC(user, "Your character has been deleted.");
                _admin.DeletePlayerCharacter(user, true);

            }
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            string cdKey = _.GetPCPublicCDKey(user);
            string enteredCDKey = args.Length > 0 ? args[0] : string.Empty;

            if (cdKey != enteredCDKey)
            {
                return "Invalid CD key entered. Please enter the command as follows: \"/delete <CD Key>\". You can retrieve your CD key with the /CDKey chat command.";
            }
            
            return string.Empty;
        }

        public bool RequiresTarget => false;
    }
}
