using System;
using SWLOR.Game.Server.Core;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class GetLocalFloatChatCommandDefinition : ChatCommand.ChatCommandDefinition
    {
        public GetLocalFloatChatCommandDefinition()
            : base(
            "Gets a local float on a target.",
            ChatCommand.CommandPermissionType.DM | ChatCommand.CommandPermissionType.Admin,
            HandleAction,
            HandleArgumentValidation,
            true)
        {
        }

        private static string HandleArgumentValidation(uint user, params string[] args)
        {
            if (args.Length < 1)
            {
                return "Missing arguments. Format should be: /GetLocalFloat Variable_Name. Example: /GetLocalFloat MY_VARIABLE";
            }

            return string.Empty;
        }

        private static void HandleAction(uint user, uint target, Location targetLocation, params string[] args)
        {
            if (!GetIsObjectValid(target))
            {
                SendMessageToPC(user, "Target is invalid. Targeting area instead.");
                target = GetArea(user);
            }

            string variableName = Convert.ToString(args[0]);
            float value = GetLocalFloat(target, variableName);

            SendMessageToPC(user, variableName + " = " + value);
        }
    }


    public class GetLocalIntChatCommandDefinition : ChatCommand.ChatCommandDefinition
    {
        public GetLocalIntChatCommandDefinition()
            : base(
                "Gets a local integer on a target.",
                ChatCommand.CommandPermissionType.DM | ChatCommand.CommandPermissionType.Admin,
                HandleAction,
                HandleArgumentValidation,
                true)
        {
        }

        private static string HandleArgumentValidation(uint user, params string[] args)
        {
            if (args.Length < 1)
            {
                return "Missing arguments. Format should be: /GetLocalInt Variable_Name. Example: /GetLocalInt MY_VARIABLE";
            }

            return string.Empty;
        }

        private static void HandleAction(uint user, uint target, Location targetLocation, params string[] args)
        {
            if (!GetIsObjectValid(target))
            {
                SendMessageToPC(user, "Target is invalid. Targeting area instead.");
                target = GetArea(user);
            }

            string variableName = Convert.ToString(args[0]);
            int value = GetLocalInt(target, variableName);

            SendMessageToPC(user, variableName + " = " + value);
        }
    }


    public class GetLocalStringChatCommandDefinition : ChatCommand.ChatCommandDefinition
    {
        public GetLocalStringChatCommandDefinition()
            : base(
                "Gets a local string on a target.",
                ChatCommand.CommandPermissionType.DM | ChatCommand.CommandPermissionType.Admin,
                HandleAction,
                HandleArgumentValidation,
                true)
        {
        }

        private static string HandleArgumentValidation(uint user, params string[] args)
        {
            if (args.Length < 1)
            {
                return "Missing arguments. Format should be: /GetLocalString Variable_Name. Example: /GetLocalString MY_VARIABLE";
            }

            return string.Empty;
        }

        private static void HandleAction(uint user, uint target, Location targetLocation, params string[] args)
        {
            if (!GetIsObjectValid(target))
            {
                SendMessageToPC(user, "Target is invalid. Targeting area instead.");
                target = GetArea(user);
            }

            string variableName = Convert.ToString(args[0]);
            string value = GetLocalString(target, variableName);

            SendMessageToPC(user, variableName + " = " + value);
        }
    }


    public class SetLocalFloatChatCommandDefinition : ChatCommand.ChatCommandDefinition
    {
        public SetLocalFloatChatCommandDefinition()
            : base(
                "Sets a local float on a target.",
                ChatCommand.CommandPermissionType.DM | ChatCommand.CommandPermissionType.Admin,
                HandleAction,
                HandleArgumentValidation,
                true)
        {
        }

        private static string HandleArgumentValidation(uint user, params string[] args)
        {
            if (args.Length < 2)
            {
                return "Missing arguments. Format should be: /SetLocalFloat Variable_Name <VALUE>. Example: /SetLocalFloat MY_VARIABLE 6.9";
            }

            if (!float.TryParse(args[1], out var value))
            {
                return "Invalid value entered. Please try again.";
            }
            return string.Empty;
        }

        private static void HandleAction(uint user, uint target, Location targetLocation, params string[] args)
        {
            if (!GetIsObjectValid(target))
            {
                SendMessageToPC(user, "Target is invalid. Targeting area instead.");
                target = GetArea(user);
            }

            string variableName = args[0];
            float value = float.Parse(args[1]);

            SetLocalFloat(target, variableName, value);

            SendMessageToPC(user, "Local float set: " + variableName + " = " + value);
        }
    }

    public class SetLocalIntChatCommandDefinition : ChatCommand.ChatCommandDefinition
    {
        public SetLocalIntChatCommandDefinition()
            : base(
                "Sets a local float on a target.",
                ChatCommand.CommandPermissionType.DM | ChatCommand.CommandPermissionType.Admin,
                HandleAction,
                HandleArgumentValidation,
                true)
        {
        }

        private static string HandleArgumentValidation(uint user, params string[] args)
        {
            if (args.Length < 2)
            {
                return "Missing arguments. Format should be: /SetLocalInt Variable_Name <VALUE>. Example: /SetLocalInt MY_VARIABLE 69";
            }

            if (!int.TryParse(args[1], out var value))
            {
                return "Invalid value entered. Please try again.";
            }

            return string.Empty;
        }

        private static void HandleAction(uint user, uint target, Location targetLocation, params string[] args)
        {
            if (!GetIsObjectValid(target))
            {
                SendMessageToPC(user, "Target is invalid. Targeting area instead.");
                target = GetArea(user);
            }

            string variableName = args[0];
            int value = Convert.ToInt32(args[1]);

            SetLocalInt(target, variableName, value);

            SendMessageToPC(user, "Local integer set: " + variableName + " = " + value);
        }
    }

    public class SetLocalStringChatCommandDefinition : ChatCommand.ChatCommandDefinition
    {
        public SetLocalStringChatCommandDefinition()
            : base(
                "Sets a local string on a target.",
                ChatCommand.CommandPermissionType.DM | ChatCommand.CommandPermissionType.Admin,
                HandleAction,
                HandleArgumentValidation,
                true)
        {
        }

        private static string HandleArgumentValidation(uint user, params string[] args)
        {
            if (args.Length < 1)
            {
                return "Missing arguments. Format should be: /SetLocalString Variable_Name <VALUE>. Example: /SetLocalString MY_VARIABLE My Text";
            }

            return string.Empty;
        }

        private static void HandleAction(uint user, uint target, Location targetLocation, params string[] args)
        {
            if (!GetIsObjectValid(target))
            {
                SendMessageToPC(user, "Target is invalid. Targeting area instead.");
                target = GetArea(user);
            }

            string variableName = Convert.ToString(args[0]);
            string value = string.Empty;

            for (int x = 1; x < args.Length; x++)
            {
                value += " " + args[x];
            }

            value = value.Trim();

            SetLocalString(target, variableName, value);

            SendMessageToPC(user, "Local string set: " + variableName + " = " + value);
        }
    }
}
