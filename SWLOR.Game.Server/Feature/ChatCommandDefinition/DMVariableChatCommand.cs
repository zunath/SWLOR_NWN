using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Async;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Domain.Gameplay;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.World;
using ChatChannel = SWLOR.Game.Server.Core.NWNX.Enum.ChatChannel;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Composition;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    /// <summary>
    /// Local variable commands: reading and writing local variables on a target.
    /// </summary>
    public class DMVariableChatCommand: IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new ChatCommandBuilder();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            GetLocalVariable();
            SetLocalFloatCommand();
            SetLocalIntCommand();
            SetLocalStringCommand();

            return _builder.Build();
        }

        private void GetLocalVariable()
        {
            _builder.Create("getlocalfloat")
                .Description("Gets a local float on a target.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .RequiresTarget()
                .Validate((user, args) =>
                {
                    if (args.Length < 1)
                    {
                        return "Missing arguments. Format should be: /GetLocalFloat Variable_Name. Example: /GetLocalFloat MY_VARIABLE";
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    if (!GetIsObjectValid(target))
                    {
                        SendMessageToPC(user, "Target is invalid. Targeting area instead.");
                        target = GetArea(user);
                    }

                    var variableName = Convert.ToString(args[0]);
                    var value = GetLocalFloat(target, variableName);

                    SendMessageToPC(user, variableName + " = " + value);
                });

            _builder.Create("getlocalint")
                .Description("Gets a local integer on a target.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .RequiresTarget()
                .Validate((user, args) =>
                {
                    if (args.Length < 1)
                    {
                        return "Missing arguments. Format should be: /GetLocalInt Variable_Name. Example: /GetLocalInt MY_VARIABLE";
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    if (!GetIsObjectValid(target))
                    {
                        SendMessageToPC(user, "Target is invalid. Targeting area instead.");
                        target = GetArea(user);
                    }

                    var variableName = Convert.ToString(args[0]);
                    var value = GetLocalInt(target, variableName);

                    SendMessageToPC(user, variableName + " = " + value);
                });

            _builder.Create("getlocalstring")
                .Description("Gets a local string on a target.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .RequiresTarget()
                .Validate((user, args) =>
                {
                    if (args.Length < 1)
                    {
                        return "Missing arguments. Format should be: /GetLocalString Variable_Name. Example: /GetLocalString MY_VARIABLE";
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    if (!GetIsObjectValid(target))
                    {
                        SendMessageToPC(user, "Target is invalid. Targeting area instead.");
                        target = GetArea(user);
                    }

                    var variableName = Convert.ToString(args[0]);
                    var value = GetLocalString(target, variableName);

                    SendMessageToPC(user, variableName + " = " + value);
                });
        }

        private void SetLocalFloatCommand()
        {
            _builder.Create("setlocalfloat")
                .Description("Sets a local float on a target.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .RequiresTarget()
                .Validate((user, args) =>
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
                })
                .Action((user, target, location, args) =>
                {
                    if (!GetIsObjectValid(target))
                    {
                        SendMessageToPC(user, "Target is invalid. Targeting area instead.");
                        target = GetArea(user);
                    }

                    var variableName = args[0];
                    var value = float.Parse(args[1]);

                    SetLocalFloat(target, variableName, value);

                    SendMessageToPC(user, "Local float set: " + variableName + " = " + value);
                });
        }

        private void SetLocalIntCommand()
        {
            _builder.Create("setlocalint")
                .Description("Sets a local int on a target.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .RequiresTarget()
                .Validate((user, args) =>
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
                })
                .Action((user, target, location, args) =>
                {
                    if (!GetIsObjectValid(target))
                    {
                        SendMessageToPC(user, "Target is invalid. Targeting area instead.");
                        target = GetArea(user);
                    }

                    var variableName = args[0];
                    var value = Convert.ToInt32(args[1]);

                    SetLocalInt(target, variableName, value);

                    SendMessageToPC(user, "Local integer set: " + variableName + " = " + value);
                });
        }

        private void SetLocalStringCommand()
        {
            _builder.Create("setlocalstring")
                .Description("Sets a local string on a target.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .RequiresTarget()
                .Validate((user, args) =>
                {
                    if (args.Length < 1)
                    {
                        return "Missing arguments. Format should be: /SetLocalString Variable_Name <VALUE>. Example: /SetLocalString MY_VARIABLE My Text";
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    if (!GetIsObjectValid(target))
                    {
                        SendMessageToPC(user, "Target is invalid. Targeting area instead.");
                        target = GetArea(user);
                    }

                    var variableName = Convert.ToString(args[0]);
                    var value = string.Empty;

                    for (var x = 1; x < args.Length; x++)
                    {
                        value += " " + args[x];
                    }

                    value = value.Trim();

                    SetLocalString(target, variableName, value);

                    SendMessageToPC(user, "Local string set: " + variableName + " = " + value);
                });
        }
    }
}
