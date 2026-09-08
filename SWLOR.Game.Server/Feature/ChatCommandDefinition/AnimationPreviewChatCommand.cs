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
            if (!Clips.TryGetValue(name, out var clip))
            {
                SendMessageToPC(controller, "Usage: /animtest <name>. Available: " + string.Join(", ", Clips.Keys.OrderBy(key => key)));
                return;
            }
            NamedAnimation.Play(user, clip);
            SendMessageToPC(controller, $"Playing {name} ({clip.Duration:0.##} seconds).");
        })
        .Build();
}
