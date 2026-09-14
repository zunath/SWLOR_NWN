using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.ChatCommandService;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition;

public class AnimationPreviewChatCommand : IChatCommandListDefinition
{
    public static IReadOnlyDictionary<string, AnimationClip> Clips => AnimationPreviewCatalog.Clips;

    public Dictionary<string, ChatCommandDetail> BuildChatCommands() => new ChatCommandBuilder()
        .Create("animtest")
        .Description("Preview an installed animation: /animtest ShieldBash. No argument lists available clips.")
        .Permissions(AuthorizationLevel.DM | AuthorizationLevel.Admin)
        .AvailableToAllOnTestEnvironment()
        .Action((user, target, location, args) =>
        {
            var controller = AnimationPreviewCatalog.GetController(user);
            var name = string.Concat(args);
            var entry = AnimationPreviewCatalog.Entries.SingleOrDefault(candidate =>
                candidate.Id.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (entry == null)
            {
                SendMessageToPC(controller, "Usage: /animtest <name>. Available: " + string.Join(", ", Clips.Keys.OrderBy(key => key)));
                return;
            }
            entry.Play(user);
            SendMessageToPC(controller, $"Playing {name} ({entry.DurationText}).");
        })
        .Build();
}
