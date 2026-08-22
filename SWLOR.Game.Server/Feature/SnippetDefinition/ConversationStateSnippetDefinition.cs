using System.Collections.Generic;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SnippetService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.SnippetDefinition
{
    /// <summary>
    /// General conversation building blocks for small stateful interactions which do not warrant a
    /// bespoke gameplay service. These operations also replace the equivalent one-off NWScript used
    /// by older module conversations.
    /// </summary>
    public sealed class ConversationStateSnippetDefinition : ISnippetListDefinition
    {
        private readonly SnippetBuilder _builder = new SnippetBuilder();

        public Dictionary<string, SnippetDetail> BuildSnippets()
        {
            LocalNumberCondition();
            PlayerCreditsCondition();
            PlayerAbilityCondition();
            PlayerClassCondition();
            RandomChanceCondition();
            SetLocalNumberAction();
            AdjustLocalNumberAction();
            TakePlayerCreditsAction();
            HealPlayerAction();
            NotifyPlayerAction();
            OpenTrainingStoreAction();
            OpenStatRebuildAction();
            PurchaseFullRebuildAction();
            return _builder.Build();
        }

        private void LocalNumberCondition()
        {
            _builder.Create("condition-local-number")
                .Description("Compares a number stored on the player, NPC, area, or module.")
                .Phrase("{scope} value {variable} is {comparison} {value}")
                .NegatedPhrase("{scope} value {variable} is not {comparison} {value}")
                .Argument("scope", SnippetArgumentType.Text)
                .Argument("variable", SnippetArgumentType.Text)
                .Argument("comparison", SnippetArgumentType.Text)
                .Argument("value", SnippetArgumentType.Amount)
                .AppearsWhenAction((player, args) =>
                {
                    if (!TryReadLocalArguments(player, args, out var target, out var variable, out var comparison, out var expected))
                        return false;

                    var actual = GetLocalInt(target, variable);
                    return comparison switch
                    {
                        "=" or "==" or "equal" => actual == expected,
                        "!=" or "not-equal" => actual != expected,
                        ">" => actual > expected,
                        ">=" => actual >= expected,
                        "<" => actual < expected,
                        "<=" => actual <= expected,
                        _ => LogInvalidComparison(comparison)
                    };
                });
        }

        private void PlayerCreditsCondition()
        {
            _builder.Create("condition-player-credits")
                .Description("Checks whether the player has at least the specified credits.")
                .Phrase("the player has at least {amount} credits")
                .NegatedPhrase("the player has fewer than {amount} credits")
                .Argument("amount", SnippetArgumentType.Amount)
                .AppearsWhenAction((player, args) =>
                    TryReadAmount("condition-player-credits", args, out var amount) && GetGold(player) >= amount);
        }

        private void PlayerAbilityCondition()
        {
            _builder.Create("condition-player-ability")
                .Description("Checks a player's current ability score.")
                .Phrase("the player's {ability} is at least {amount}")
                .NegatedPhrase("the player's {ability} is below {amount}")
                .Argument("ability", SnippetArgumentType.Text)
                .Argument("amount", SnippetArgumentType.Amount)
                .AppearsWhenAction((player, args) =>
                {
                    if (args.Length < 2 || !Enum.TryParse(args[0], true, out AbilityType ability) ||
                        !int.TryParse(args[1], out var amount))
                    {
                        Log.Write(LogGroup.Error,
                            "'condition-player-ability' requires an ability name and a numeric minimum.");
                        return false;
                    }

                    return GetAbilityScore(player, ability) >= amount;
                });
        }

        private void RandomChanceCondition()
        {
            _builder.Create("condition-random-chance")
                .Description("Passes randomly at the specified percentage chance.")
                .Phrase("a random roll succeeds at {amount} percent")
                .NegatedPhrase("a random roll fails at {amount} percent")
                .Argument("amount", SnippetArgumentType.Amount)
                .AppearsWhenAction((_, args) =>
                    TryReadAmount("condition-random-chance", args, out var chance) &&
                    SWLOR.Game.Server.Service.Random.D100(1) <= Math.Clamp(chance, 0, 100));
        }

        private void PlayerClassCondition()
        {
            _builder.Create("condition-player-class")
                .Description("Checks whether the player has at least one level in a class.")
                .Phrase("the player has levels in {class}")
                .NegatedPhrase("the player has no levels in {class}")
                .Argument("class", SnippetArgumentType.Text)
                .AppearsWhenAction((player, args) =>
                    args.Length > 0 &&
                    Enum.TryParse(args[0], true, out ClassType classType) &&
                    GetLevelByClass(classType, player) > 0);
        }

        private void SetLocalNumberAction()
        {
            _builder.Create("action-set-local-number")
                .Description("Stores a number on the player, NPC, area, or module.")
                .Phrase("sets {scope} value {variable} to {value}")
                .Argument("scope", SnippetArgumentType.Text)
                .Argument("variable", SnippetArgumentType.Text)
                .Argument("value", SnippetArgumentType.Amount)
                .ActionsTakenAction((player, args) =>
                {
                    if (!TryWriteLocalArguments(player, args, out var target, out var variable, out var value))
                        return false;
                    SetLocalInt(target, variable, value);
                    return true;
                });
        }

        private void AdjustLocalNumberAction()
        {
            _builder.Create("action-adjust-local-number")
                .Description("Adds to a number stored on the player, NPC, area, or module.")
                .Phrase("changes {scope} value {variable} by {value}")
                .Argument("scope", SnippetArgumentType.Text)
                .Argument("variable", SnippetArgumentType.Text)
                .Argument("value", SnippetArgumentType.Amount)
                .ActionsTakenAction((player, args) =>
                {
                    if (!TryWriteLocalArguments(player, args, out var target, out var variable, out var value))
                        return false;
                    SetLocalInt(target, variable, Math.Max(0, GetLocalInt(target, variable) + value));
                    return true;
                });
        }

        private void TakePlayerCreditsAction()
        {
            _builder.Create("action-take-player-credits")
                .Description("Takes credits from the player when they can afford the amount.")
                .Phrase("takes {amount} credits from the player")
                .Argument("amount", SnippetArgumentType.Amount)
                .ActionsTakenAction((player, args) =>
                {
                    if (!TryReadAmount("action-take-player-credits", args, out var amount) || GetGold(player) < amount)
                        return false;
                    TakeGoldFromCreature(amount, player, true);
                    return true;
                });
        }

        private void HealPlayerAction()
        {
            _builder.Create("action-heal-player")
                .Description("Fully heals the player. An optional credit cost may be supplied.")
                .Phrase("fully heals the player for {amount} credits")
                .Argument("amount", SnippetArgumentType.Amount, isOptional: true)
                .ActionsTakenAction((player, args) =>
                {
                    var cost = 0;
                    if (args.Length > 0 && (!int.TryParse(args[0], out cost) || cost < 0))
                    {
                        Log.Write(LogGroup.Error, "'action-heal-player' received an invalid credit cost.");
                        return false;
                    }
                    if (GetGold(player) < cost)
                        return false;
                    if (cost > 0)
                        TakeGoldFromCreature(cost, player, true);
                    ApplyEffectToObject(DurationType.Instant, EffectHeal(GetMaxHitPoints(player)), player);
                    return true;
                });
        }

        private void NotifyPlayerAction()
        {
            _builder.Create("action-notify-player")
                .Description("Sends a message to the player.")
                .Phrase("tells the player {message}")
                .Argument("message", SnippetArgumentType.Text)
                .ActionsTakenAction((player, args) =>
                {
                    if (args.Length == 0)
                        return false;
                    SendMessageToPC(player, args[0]);
                    return true;
                });
        }

        private void OpenTrainingStoreAction()
        {
            _builder.Create("action-open-training-store")
                .Description("Opens the training store for the player.")
                .Phrase("opens the training store")
                .ActionsTakenAction((player, _) =>
                    TrainingStoreViewModel.OpenTrainingStore(player, Snippet.GetExecutionOwner()));
        }

        private void OpenStatRebuildAction()
        {
            _builder.Create("action-open-stat-rebuild")
                .Description("Opens the stat rebuild window for the player.")
                .Phrase("opens the stat rebuild window")
                .ActionsTakenAction((player, _) =>
                    CharacterStatRebuildViewModel.OpenCharacterStatRebuild(
                        player,
                        Snippet.GetExecutionOwner()));
        }

        private void PurchaseFullRebuildAction()
        {
            _builder.Create("action-purchase-full-rebuild")
                .Description("Consumes a rebuild token and sends the player to the rebuild area.")
                .Phrase("purchases a full character rebuild")
                .ActionsTakenAction((player, _) => PlaceableScripts.PurchaseRebuild(player));
        }

        private static bool TryReadLocalArguments(
            uint player,
            string[] args,
            out uint target,
            out string variable,
            out string comparison,
            out int expected)
        {
            target = OBJECT_INVALID;
            variable = string.Empty;
            comparison = string.Empty;
            expected = 0;
            if (args.Length < 4 || !TryResolveScope(player, args[0], out target) ||
                string.IsNullOrWhiteSpace(args[1]) || !int.TryParse(args[3], out expected))
            {
                Log.Write(LogGroup.Error,
                    "'condition-local-number' requires scope, variable, comparison, and numeric value arguments.");
                return false;
            }

            variable = args[1];
            comparison = args[2].Trim().ToLowerInvariant();
            return true;
        }

        private static bool TryWriteLocalArguments(
            uint player,
            string[] args,
            out uint target,
            out string variable,
            out int value)
        {
            target = OBJECT_INVALID;
            variable = string.Empty;
            value = 0;
            if (args.Length < 3 || !TryResolveScope(player, args[0], out target) ||
                string.IsNullOrWhiteSpace(args[1]) || !int.TryParse(args[2], out value))
            {
                Log.Write(LogGroup.Error,
                    "A local-number action requires scope, variable, and numeric value arguments.");
                return false;
            }

            variable = args[1];
            return true;
        }

        private static bool TryResolveScope(uint player, string scope, out uint target)
        {
            var owner = Snippet.GetExecutionOwner();
            target = scope?.Trim().ToLowerInvariant() switch
            {
                "player" => player,
                "owner" or "npc" => owner,
                "area" => GetArea(GetIsObjectValid(owner) ? owner : player),
                "module" => GetModule(),
                _ => OBJECT_INVALID
            };

            if (GetIsObjectValid(target))
                return true;

            Log.Write(LogGroup.Error, $"Conversation local-number scope '{scope}' is invalid.");
            return false;
        }

        private static bool TryReadAmount(string key, string[] args, out int amount)
        {
            amount = 0;
            if (args.Length > 0 && int.TryParse(args[0], out amount) && amount >= 0)
                return true;
            Log.Write(LogGroup.Error, $"'{key}' requires a non-negative numeric amount.");
            return false;
        }

        private static bool LogInvalidComparison(string comparison)
        {
            Log.Write(LogGroup.Error, $"Conversation number comparison '{comparison}' is invalid.");
            return false;
        }
    }
}
