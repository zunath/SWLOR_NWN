using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.ChatCommandService;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition;

public sealed class QuickCastChatCommand : IChatCommandListDefinition
{
    public Dictionary<string, ChatCommandDetail> BuildChatCommands()
    {
        var builder = new ChatCommandBuilder();
        AddCastCommand(builder, QuickCastTargetType.Self);
        AddCastCommand(builder, QuickCastTargetType.CurrentEnemy);
        builder.Create("castbind")
            .Description("Optional quick-cast hotkey: /castbind <empty slot 1–36> <self|enemy> <exact ability name or ID>. Enable that mode first. Native feat buttons keep manual targeting.")
            .Permissions(AuthorizationLevel.All)
            .Action((user, target, location, args) =>
            {
                if (args.Length < 3 || !int.TryParse(args[0], out var slot) ||
                    !TryParseTargetType(args[1], out var targetType))
                {
                    SendMessageToPC(user, "Usage: /castbind <empty slot 1–36> <self|enemy> <exact ability name or ID>. /castunbind <slot> releases the shortcut during this login.");
                    return;
                }

                if (!QuickCast.TryBind(user, slot, targetType, string.Join(" ", args.Skip(2)), out var error))
                    SendMessageToPC(user, error);
                else
                    SendMessageToPC(user, $"Quick-cast command bound to slot {slot}. Use /castunbind {slot} to release the shortcut during this login.");
            });
        builder.Create("castunbind")
            .Description("Release a quickbar slot assigned by /castbind during this login: /castunbind <slot 1–36>.")
            .Permissions(AuthorizationLevel.All)
            .Action((user, target, location, args) =>
            {
                if (args.Length != 1 || !int.TryParse(args[0], out var slot))
                {
                    SendMessageToPC(user, "Usage: /castunbind <slot 1–36>.");
                    return;
                }
                if (!QuickCast.TryUnbind(user, slot, out var error))
                    SendMessageToPC(user, error);
                else
                    SendMessageToPC(user, "Binding released. The slot is cleared if you have not replaced the quick-cast command yourself.");
            });
        return builder.Build();
    }

    private static void AddCastCommand(ChatCommandBuilder builder, QuickCastTargetType targetType)
    {
        var command = QuickCast.CommandName(targetType);
        builder.Create(command)
            .Description($"Optional cursor-free {(targetType == QuickCastTargetType.Self ? "self-support" : "current-enemy")} cast: /{command} on|off|list|<exact ability name or ID>. Off by default each login. Native feat buttons retain manual targeting; aimed areas are excluded.")
            .Permissions(AuthorizationLevel.All)
            .Action((user, target, location, args) =>
            {
                var input = string.Join(" ", args).Trim();
                if (input.Equals("on", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("off", StringComparison.OrdinalIgnoreCase))
                {
                    var enabled = input.Equals("on", StringComparison.OrdinalIgnoreCase);
                    QuickCast.SetEnabled(user, targetType, enabled);
                    SendMessageToPC(user, $"/{command} is {(enabled ? "on" : "off")} for this login. Native feat buttons retain manual targeting. Use /{command} list for eligible abilities.");
                    return;
                }
                if (input.Equals("list", StringComparison.OrdinalIgnoreCase))
                {
                    var abilities = QuickCast.GetAvailableAbilities(user, targetType).ToArray();
                    SendMessageToPC(user, abilities.Length == 0
                        ? "You have no eligible abilities for this quick-cast mode."
                        : string.Join("\n", abilities));
                    return;
                }
                if (string.IsNullOrWhiteSpace(input))
                {
                    SendMessageToPC(user, $"/{command} is {(QuickCast.IsEnabled(user, targetType) ? "on" : "off")}. Use /{command} on|off|list|<exact ability name or ID>. Modes start off each login. Use /castbind <empty slot> <self|enemy> <ability> for a hotkey, or the normal feat button for manual targeting.");
                    return;
                }
                if (!QuickCast.TryCast(user, targetType, input, out var error) && !string.IsNullOrWhiteSpace(error))
                    SendMessageToPC(user, error);
            });
    }

    private static bool TryParseTargetType(string input, out QuickCastTargetType targetType)
    {
        targetType = QuickCastTargetType.Self;
        if (input.Equals("self", StringComparison.OrdinalIgnoreCase))
            return true;
        targetType = QuickCastTargetType.CurrentEnemy;
        return input.Equals("enemy", StringComparison.OrdinalIgnoreCase);
    }
}
