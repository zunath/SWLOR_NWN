using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.AppearanceDefinition.Species;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Associate;
using Player = SWLOR.Game.Server.Entity.Player;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Companions;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    /// <summary>
    /// Appearance and movement commands: customization, armor visibility, disguises, head scale and walking.
    /// </summary>
    public class CharacterAppearanceChatCommand: IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new ChatCommandBuilder();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            Customize();
            ArmorVisibility();
            Disguises();
            HeadScale();
            AlwaysWalk();

            return _builder.Build();
        }

        private void Disguises()
        {
            _builder.Create("disguise", "disguises")
                .Description("Toggles the disguises menu.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) =>
                {
                    Gui.TogglePlayerWindow(user, GuiWindowType.Disguises);
                });
        }

        private void ArmorVisibility()
        {
            _builder.Create("helm", "helmet")
                .Description("Shows or hides your helmet.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) => ToggleArmorVisibility(user, true));

            _builder.Create("cloak")
                .Description("Shows or hides your cloak.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) => ToggleArmorVisibility(user, false));
        }

        private static void ToggleArmorVisibility(uint user, bool helmet)
        {
            var item = helmet ? "helmet" : "cloak";
            if (!ArmorDisplay.Toggle(user, helmet, out var shown))
            {
                SendMessageToPC(user, ColorToken.Red($"Your {item} setting could not be saved."));
                return;
            }

            SendMessageToPC(user, shown ? $"Your {item} is now shown." : $"Your {item} is now hidden.");
        }

        private void Customize()
        {
            _builder.Create("customize", "customise")
                .Description("Opens the appearance editor window.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) =>
                {
                    var payload = new AppearanceEditorPayload(user);

                    // A DM's window can still be open on the avatar or a creature they possessed before; DMPresence
                    // moves it to the creature they control now instead of rebinding it under the open copy.
                    if (DMPresence.IsControlledByDM(user))
                        DMPresence.ToggleWindow(user, GuiWindowType.AppearanceEditor, payload);
                    else
                        Gui.TogglePlayerWindow(user, GuiWindowType.AppearanceEditor, payload);
                });
        }

        private void HeadScale()
        {
            const float Increment = 0.01f;

            _builder.Create("headscale")
                .Description("Adjusts your head size separately from body height. Usage: /headscale [value|+/-]. Omit value to view current size. Limits depend on your race.")
                .Permissions(AuthorizationLevel.All)
                .Validate((user, args) =>
                {
                    if (GetIsDM(user) || GetIsDMPossessed(user))
                    {
                        return "This command can only be used by players.";
                    }

                    if (!RacialAppearanceRegistry.TryGet(GetAppearanceType(user), out var appearance))
                    {
                        return "Your appearance type does not support head scaling.";
                    }

                    if (args.Length <= 0)
                        return string.Empty;

                    var arg = args[0];
                    if (arg == "+" || arg == "-" || arg.Equals("up", StringComparison.OrdinalIgnoreCase) ||
                        arg.Equals("down", StringComparison.OrdinalIgnoreCase))
                    {
                        return string.Empty;
                    }

                    if (!float.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                        !float.IsFinite(value))
                    {
                        return $"Please specify a value between {appearance.MinimumHeadScale} and {appearance.MaximumHeadScale}, or use + / -.";
                    }

                    if (value < appearance.MinimumHeadScale || value > appearance.MaximumHeadScale)
                    {
                        return $"Please specify a value between {appearance.MinimumHeadScale} and {appearance.MaximumHeadScale}.";
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    if (!RacialAppearanceRegistry.TryGet(GetAppearanceType(user), out var appearance))
                    {
                        SendMessageToPC(user, "Your appearance type does not support head scaling.");
                        return;
                    }

                    var minScale = appearance.MinimumHeadScale;
                    var maxScale = appearance.MaximumHeadScale;
                    var playerId = GetObjectUUID(user);
                    var dbPlayer = DB.Get<Player>(playerId);
                    var current = dbPlayer.HeadAppearanceScale <= 0f ? 1.0f : dbPlayer.HeadAppearanceScale;
                    float newScale;

                    if (args.Length <= 0)
                    {
                        SendMessageToPC(user, $"Head Size: {current:0.##} (range {minScale}-{maxScale}). Use /headscale <value>, /headscale +, or /headscale -.");
                        return;
                    }

                    var arg = args[0];
                    if (arg == "+" || arg.Equals("up", StringComparison.OrdinalIgnoreCase))
                    {
                        newScale = Math.Min(maxScale, current + Increment);
                    }
                    else if (arg == "-" || arg.Equals("down", StringComparison.OrdinalIgnoreCase))
                    {
                        newScale = Math.Max(minScale, current - Increment);
                    }
                    else
                    {
                        newScale = float.Parse(arg, NumberStyles.Float, CultureInfo.InvariantCulture);
                    }

                    if (Math.Abs(newScale - current) < 0.0001f &&
                        (arg == "+" || arg == "-" || arg.Equals("up", StringComparison.OrdinalIgnoreCase) ||
                         arg.Equals("down", StringComparison.OrdinalIgnoreCase)))
                    {
                        SendMessageToPC(user, newScale >= maxScale
                            ? "You cannot increase your head size any further."
                            : "You cannot decrease your head size any further.");
                        return;
                    }

                    dbPlayer.HeadAppearanceScale = newScale;
                    DB.Set(dbPlayer);

                    HelmetModelRenderer.SetHeadScale(user, newScale);

                    SendMessageToPC(user, $"Head Size: {newScale:0.##}");
                });
        }

        private void AlwaysWalk()
        {
            _builder.Create("alwayswalk", "walk")
                .Description("Toggles forced walking at normal walking speed.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, _, _, _) =>
                {
                    var isWalking = CreatureStat.ToggleWalkMode(user);
                    var status = isWalking ? ColorToken.Green("enabled") : ColorToken.Red("disabled");
                    SendMessageToPC(user, $"Walk mode {status}.");
                });
        }
    }
}
