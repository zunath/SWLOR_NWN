using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ChatCommandService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    /// <summary>
    /// Chat commands for the temporary, narrative HP tracker. The HP itself is displayed in the "HP
    /// Tracker" NUI window (/hptracker), which lists tracked creatures near the viewer; these commands add,
    /// adjust, and remove trackers. They never affect a creature's real combat hit points.
    ///
    /// Regular players may only manage a tracker on themselves; DMs/Admins may manage any non-DM creature.
    /// </summary>
    public class HPTrackerChatCommand : IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new ChatCommandBuilder();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            OpenWindow();
            SetHP();
            IncreaseHP();
            DecreaseHP();
            DeleteHP();

            return _builder.Build();
        }

        private void OpenWindow()
        {
            _builder.Create("hptracker")
                .Description("Opens the HP Tracker window, listing creatures with an HP tracker near you.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, _, _, _) =>
                {
                    // Standard window-open pattern (mirrors /dice, /dmtools). A regular PC opens it directly.
                    // A DM must be possessing a creature: the window then renders on that creature's client
                    // (uiTarget) while its state lives under the master DM. A bare, unpossessed DM avatar
                    // cannot host a NUI window, so nothing opens for it — same as every SWLOR NUI window.
                    var player = user;
                    var uiTarget = OBJECT_INVALID;
                    if (GetIsDMPossessed(player))
                    {
                        uiTarget = player;
                        player = GetMaster(player);
                    }

                    // A bare (unpossessed) DM avatar can't host a NUI window (this is the same guard
                    // TogglePlayerWindow uses). Explain how to proceed instead of silently doing nothing.
                    if (!GetIsPC(player) && uiTarget == OBJECT_INVALID)
                    {
                        SendMessageToPC(user, ColorToken.Red("The HP Tracker window can't open for an unpossessed DM. Possess a creature first, or use a player character."));
                        return;
                    }

                    Gui.TogglePlayerWindow(player, GuiWindowType.HpTracker, null, OBJECT_INVALID, uiTarget);
                });
        }

        private void SetHP()
        {
            _builder.Create("hpset", "sethp")
                .Description("Adds/updates a creature's HP tracker (shown in /hptracker). Usage: /hpset <hp> (starts full) or /hpset <current> <max>. Players may only target themselves; DMs/Admins may target any creature.")
                .Permissions(AuthorizationLevel.All)
                .RequiresTarget(ObjectType.Creature)
                .Validate((user, args) =>
                {
                    if (args.Length < 1 || args.Length > 2)
                        return "Usage: /hpset <hp>   or   /hpset <current> <max>";

                    if (args.Length == 1)
                    {
                        if (!int.TryParse(args[0], out var hp) || hp < 1)
                            return "HP must be a whole number of 1 or greater.";
                    }
                    else
                    {
                        if (!int.TryParse(args[0], out var current) || current < 0)
                            return "Current HP must be a whole number of 0 or greater.";
                        if (!int.TryParse(args[1], out var max) || max < 1)
                            return "Max HP must be a whole number of 1 or greater.";
                        if (current > max)
                            return "Current HP cannot be greater than max HP.";
                    }

                    return string.Empty;
                })
                .Action((user, target, _, args) =>
                {
                    if (!CanTarget(user, target)) return;

                    int current, max;
                    if (args.Length == 2)
                    {
                        current = int.Parse(args[0]);
                        max = int.Parse(args[1]);
                    }
                    else
                    {
                        current = int.Parse(args[0]);
                        max = current;
                    }

                    var before = HPTracker.Has(target) ? HPTracker.Get(target) : (Current: 0, Max: 0);
                    HPTracker.Set(target, current, max);
                    HpTrackerWindow.RefreshOpenWindows();
                    BroadcastNearby(target, ColorToken.Green($"{GetName(target)}'s HP is now {current}/{max}."));

                    if (HpTrackerWindow.IsStaff(user))
                        Log.Write(LogGroup.DM, $"HP tracker set: Actor={GetName(user)}, Target={GetName(target)}, Before={before.Current}/{before.Max}, After={current}/{max}");
                });
        }

        private void IncreaseHP()
        {
            _builder.Create("hpinc", "inchp")
                .Description("Increases a creature's tracked HP. Usage: /hpinc [amount] (default 1).")
                .Permissions(AuthorizationLevel.All)
                .RequiresTarget(ObjectType.Creature)
                .Validate((user, args) => ValidateAmount(args))
                .Action((user, target, _, args) => AdjustHP(user, target, ParseAmount(args)));
        }

        private void DecreaseHP()
        {
            _builder.Create("hpdec", "dechp")
                .Description("Decreases a creature's tracked HP. Usage: /hpdec [amount] (default 1).")
                .Permissions(AuthorizationLevel.All)
                .RequiresTarget(ObjectType.Creature)
                .Validate((user, args) => ValidateAmount(args))
                .Action((user, target, _, args) => AdjustHP(user, target, -ParseAmount(args)));
        }

        private void DeleteHP()
        {
            _builder.Create("hpdel", "delhp")
                .Description("Removes a creature's HP tracker. Usage: /hpdel.")
                .Permissions(AuthorizationLevel.All)
                .RequiresTarget(ObjectType.Creature)
                .Action((user, target, _, _) =>
                {
                    if (!CanTarget(user, target)) return;

                    if (!HPTracker.Has(target))
                    {
                        SendMessageToPC(user, ColorToken.Red($"{GetName(target)} does not have an HP tracker."));
                        return;
                    }

                    var (removedCurrent, removedMax) = HPTracker.Get(target);
                    HPTracker.Remove(target);
                    HpTrackerWindow.RefreshOpenWindows();
                    SendMessageToPC(user, ColorToken.Green($"HP tracker removed from {GetName(target)}."));

                    if (HpTrackerWindow.IsStaff(user))
                        Log.Write(LogGroup.DM, $"HP tracker removed: Actor={GetName(user)}, Target={GetName(target)}, Before={removedCurrent}/{removedMax}");
                });
        }

        // ---- Helpers ----

        private static string ValidateAmount(string[] args)
        {
            if (args.Length == 0)
                return string.Empty;
            if (args.Length > 1)
                return "Provide a single amount, e.g. /hpdec 3";
            if (!int.TryParse(args[0], out var amount) || amount < 1)
                return "Amount must be a whole number of 1 or greater.";
            return string.Empty;
        }

        private static int ParseAmount(string[] args)
        {
            if (args.Length >= 1 && int.TryParse(args[0], out var amount) && amount >= 1)
                return amount;
            return 1;
        }

        private void AdjustHP(uint user, uint target, int delta)
        {
            if (!CanTarget(user, target)) return;

            if (!HPTracker.Has(target))
            {
                SendMessageToPC(user, ColorToken.Red($"{GetName(target)} does not have an HP tracker. Use /hpset first."));
                return;
            }

            var (beforeCurrent, beforeMax) = HPTracker.Get(target);
            HPTracker.Adjust(target, delta);
            HpTrackerWindow.RefreshOpenWindows();
            var (current, max) = HPTracker.Get(target);
            BroadcastNearby(target, ColorToken.Green($"{GetName(target)}'s HP is now {current}/{max}."));

            if (HpTrackerWindow.IsStaff(user))
                Log.Write(LogGroup.DM, $"HP tracker adjusted: Actor={GetName(user)}, Target={GetName(target)}, Delta={delta}, Before={beforeCurrent}/{beforeMax}, After={current}/{max}");
        }

        /// <summary>
        /// Players may only manage their own tracker; DMs/Admins may manage any non-DM creature.
        /// Sends an error to the user and returns false if the target is invalid or not permitted.
        /// </summary>
        private static bool CanTarget(uint user, uint target)
        {
            if (!GetIsObjectValid(target) || GetObjectType(target) != ObjectType.Creature)
            {
                SendMessageToPC(user, ColorToken.Red("You must target a creature."));
                return false;
            }

            if (GetIsDM(target))
            {
                SendMessageToPC(user, ColorToken.Red("You can't place an HP tracker on a DM."));
                return false;
            }

            if (!HpTrackerWindow.IsStaff(user) && target != user)
            {
                SendMessageToPC(user, ColorToken.Red("You can only manage an HP tracker on yourself."));
                return false;
            }

            return true;
        }

        /// <summary>Broadcasts a message to every player and DM in the target creature's area.</summary>
        private static void BroadcastNearby(uint origin, string message)
        {
            var area = GetArea(origin);
            for (var pc = GetFirstPC(); GetIsObjectValid(pc); pc = GetNextPC())
            {
                if (GetArea(pc) == area)
                    SendMessageToPC(pc, message);
            }
        }
    }
}
