using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.ChatCommandService;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition;

public class AnimationPreviewChatCommand : IChatCommandListDefinition
{
    public static IReadOnlyDictionary<string, AnimationClip> Clips { get; } =
        typeof(AuthoredAnimation).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(field => field.FieldType == typeof(AnimationClip))
            .ToDictionary(field => field.Name, field => (AnimationClip)field.GetValue(null), StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, ChatCommandDetail> BuildChatCommands() => new ChatCommandBuilder()
        .Create("animtest")
        .Description("Preview an installed animation: /animtest ShieldBash. No argument lists available clips.")
        .Permissions(AuthorizationLevel.Admin)
        .AvailableToAllOnTestEnvironment()
        .Action((user, target, location, args) =>
        {
            var name = string.Concat(args).Replace("_", "");
            if (!Clips.TryGetValue(name, out var clip))
            {
                SendMessageToPC(user, "Usage: /animtest <name>. Available: " + string.Join(", ", Clips.Keys.OrderBy(key => key)));
                return;
            }
            NamedAnimation.Play(user, clip);
            SendMessageToPC(user, $"Playing {name} ({clip.Duration:0.##} seconds).");
        })
        .Build();
}
