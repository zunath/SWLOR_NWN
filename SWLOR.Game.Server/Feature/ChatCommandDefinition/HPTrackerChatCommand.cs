using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ChatCommandService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    /// <summary>
    /// Chat commands for a temporary, narrative HP tracker shown as a bar above a creature.
    /// See <see cref="HPTracker"/> for the display/lifecycle logic. These commands never
    /// affect a creature's real combat hit points.
    ///
    /// All four require the caller to click a creature target. Regular players may only
    /// manage a tracker on themselves; DMs and Admins may manage any creature (PC or NPC),
    /// mirroring the self-vs-staff gate used by /description and /rename.
    /// </summary>
    public class HPTrackerChatCommand : IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new ChatCommandBuilder();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            SetHP();
            IncreaseHP();
            DecreaseHP();
            DeleteHP();

            return _builder.Build();
        }

        private void SetHP()
        {
            _builder.Create("hpset", "sethp")
                .Description("Creates/updates a temporary HP tracker shown above a target. Usage: /hpset <hp> (starts full) or /hpset <current> <max>. Players may only target themselves; DMs/Admins may target any creature.")
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

                    HPTracker.Set(target, current, max);
                    BroadcastNearby(target, ColorToken.Green($"{GetName(target)}'s HP tracker set to {current}/{max}."));
                });
        }

        private void IncreaseHP()
        {
            _builder.Create("hpinc", "inchp")
                .Description("Increases the current HP on a target's HP tracker. Usage: /hpinc [amount] (default 1).")
                .Permissions(AuthorizationLevel.All)
                .RequiresTarget(ObjectType.Creature)
                .Validate((user, args) => ValidateAmount(args))
                .Action((user, target, _, args) => AdjustHP(user, target, ParseAmount(args)));
        }

        private void DecreaseHP()
        {
            _builder.Create("hpdec", "dechp")
                .Description("Decreases the current HP on a target's HP tracker. Usage: /hpdec [amount] (default 1).")
                .Permissions(AuthorizationLevel.All)
                .RequiresTarget(ObjectType.Creature)
                .Validate((user, args) => ValidateAmount(args))
                .Action((user, target, _, args) => AdjustHP(user, target, -ParseAmount(args)));
        }

        private void DeleteHP()
        {
            _builder.Create("hpdel", "delhp")
                .Description("Removes a target's HP tracker entirely. Usage: /hpdel.")
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

                    HPTracker.Remove(target);
                    SendMessageToPC(user, ColorToken.Green($"HP tracker removed from {GetName(target)}."));
                });
        }

        // ---- Helpers ----

        /// <summary>
        /// Validates the optional single amount argument for /hpinc and /hpdec.
        /// An empty argument list is valid (defaults to 1).
        /// </summary>
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

            HPTracker.Adjust(target, delta);
            var (current, max) = HPTracker.Get(target);
            BroadcastNearby(target, ColorToken.Green($"{GetName(target)}'s HP tracker is now {current}/{max}."));
        }

        /// <summary>
        /// Players may only manage their own tracker; DMs and Admins may manage any creature.
        /// Sends an error to the user and returns false if the target is invalid or not permitted.
        /// </summary>
        private static bool CanTarget(uint user, uint target)
        {
            if (!GetIsObjectValid(target) || GetObjectType(target) != ObjectType.Creature)
            {
                SendMessageToPC(user, ColorToken.Red("You must target a creature."));
                return false;
            }

            var level = Authorization.GetAuthorizationLevel(user);
            var isStaff = level == AuthorizationLevel.DM || level == AuthorizationLevel.Admin;

            if (!isStaff && target != user)
            {
                SendMessageToPC(user, ColorToken.Red("You can only manage an HP tracker on yourself."));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Sends a message to every player and DM in the same area as the origin creature
        /// (matching where the HP bar itself is visible).
        /// </summary>
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
