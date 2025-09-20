using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.FactionService;
using SWLOR.Game.Server.Service.SnippetService;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Log;
using SWLOR.Shared.Core.Log.LogGroup;

namespace SWLOR.Game.Server.Feature.SnippetDefinition
{
    public class FactionSnippetDefinition: ISnippetListDefinition
    {
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();
        private ILogger _logger = ServiceContainer.GetService<ILogger>();

        private readonly SnippetBuilder _builder = new SnippetBuilder();

        public Dictionary<string, SnippetDetail> BuildSnippets()
        {
            // Conditions
            ConditionHasStanding();
            ConditionHasPoints();

            // Actions
            ActionGivePoints();
            ActionTakePoints();
            ActionGiveStanding();
            ActionTakeStanding();


            return _builder.Build();
        }

        private void ConditionHasStanding()
        {
            _builder.Create("condition-has-faction-standing")
                .Description("Checks whether a player has standing greater than or equal to an amount with a particular faction.")
                .AppearsWhenAction((player, args) =>
                {
                    if(args.Length < 2)
                    {
                        const string Error = "'condition-has-faction-standing' requires factionId and amount arguments";
                        SendMessageToPC(player, Error);
                        _logger.Write<ErrorLogGroup>(Error);
                        return false;
                    }
                    
                    // Try to find by faction Id
                    if (!int.TryParse(args[0], out var factionId))
                    {
                        const string Error = "'condition-has-faction-standing' has an invalid argument for 'factionId'. Must be an Id mapped to the FactionType enumeration.";
                        SendMessageToPC(player, Error);
                        _logger.Write<ErrorLogGroup>(Error);
                        return false;
                    }

                    // Try to read second argument as the amount
                    if (!int.TryParse(args[1], out var amount))
                    {
                        const string Error = "'condition-has-faction-standing' has an invalid argument for 'amount'. Must be a number.";
                        SendMessageToPC(player, Error);
                        _logger.Write<ErrorLogGroup>(Error);
                        return false;
                    }

                    var factionType = (FactionType) factionId;
                    var playerId = GetObjectUUID(player);
                    var dbPlayer = _db.Get<Player>(playerId);
                    var factionStanding = 0;

                    if (dbPlayer.Factions.ContainsKey(factionType))
                        factionStanding = dbPlayer.Factions[factionType].Standing;

                    return factionStanding >= amount;
                });
        }

        private void ConditionHasPoints()
        {
            _builder.Create("condition-has-faction-points")
                .Description("Checks whether a player has faction points greater than or equal with a particular faction.")
                .AppearsWhenAction((player, args) =>
                {
                    if (args.Length < 2)
                    {
                        const string Error = "'condition-has-faction-points' requires factionId and amount arguments";
                        SendMessageToPC(player, Error);
                        _logger.Write<ErrorLogGroup>(Error);
                        return false;
                    }

                    // Try to find by faction Id
                    if (!int.TryParse(args[0], out var factionId))
                    {
                        const string Error = "'condition-has-faction-points' has an invalid argument for 'factionId'. Must be an Id mapped to the FactionType enumeration.";
                        SendMessageToPC(player, Error);
                        _logger.Write<ErrorLogGroup>(Error);
                        return false;
                    }

                    // Try to read second argument as the amount
                    if (!int.TryParse(args[1], out var amount))
                    {
                        const string Error = "'condition-has-faction-points' has an invalid argument for 'amount'. Must be a number.";
                        SendMessageToPC(player, Error);
                        _logger.Write<ErrorLogGroup>(Error);
                        return false;
                    }

                    var factionType = (FactionType)factionId;
                    var playerId = GetObjectUUID(player);
                    var dbPlayer = _db.Get<Player>(playerId);
                    var factionStanding = 0;

                    if (dbPlayer.Factions.ContainsKey(factionType))
                        factionStanding = dbPlayer.Factions[factionType].Points;

                    return factionStanding >= amount;
                });
        }


        private void ActionGivePoints()
        {
            _builder.Create("action-give-faction-points")
                .Description("Gives faction points toward a particular faction to a player.")
                .ActionsTakenAction((player, args) =>
                {
                    if (args.Length < 2)
                    {
                        const string Error = "'action-give-faction-points' requires factionId and amount arguments";
                        SendMessageToPC(player, Error);
                        _logger.Write<ErrorLogGroup>(Error);
                        return;
                    }

                    // Try to find by faction Id
                    if (!int.TryParse(args[0], out var factionId))
                    {
                        const string Error = "'action-give-faction-points' has an invalid argument for 'factionId'. Must be an Id mapped to the FactionType enumeration.";
                        SendMessageToPC(player, Error);
                        _logger.Write<ErrorLogGroup>(Error);
                        return;
                    }

                    // Try to read second argument as the amount
                    if (!int.TryParse(args[1], out var amount))
                    {
                        const string Error = "'action-give-faction-points' has an invalid argument for 'amount'. Must be a number.";
                        SendMessageToPC(player, Error);
                        _logger.Write<ErrorLogGroup>(Error);
                        return;
                    }

                    var factionType = (FactionType)factionId;
                    amount = Math.Abs(amount);
                    Faction.AdjustPlayerFactionPoints(player, factionType, amount);

                });
        }

        private void ActionTakePoints()
        {
            _builder.Create("action-take-faction-points")
                .Description("Takes faction points toward a particular faction from a player.")
                .ActionsTakenAction((player, args) =>
                {
                    if (args.Length < 2)
                    {
                        const string Error = "'action-take-faction-points' requires factionId and amount arguments";
                        SendMessageToPC(player, Error);
                        _logger.Write<ErrorLogGroup>(Error);
                        return;
                    }

                    // Try to find by faction Id
                    if (!int.TryParse(args[0], out var factionId))
                    {
                        const string Error = "'action-take-faction-points' has an invalid argument for 'factionId'. Must be an Id mapped to the FactionType enumeration.";
                        SendMessageToPC(player, Error);
                        _logger.Write<ErrorLogGroup>(Error);
                        return;
                    }

                    // Try to read second argument as the amount
                    if (!int.TryParse(args[1], out var amount))
                    {
                        const string Error = "'action-take-faction-points' has an invalid argument for 'amount'. Must be a number.";
                        SendMessageToPC(player, Error);
                        _logger.Write<ErrorLogGroup>(Error);
                        return;
                    }

                    var factionType = (FactionType)factionId;
                    amount = Math.Abs(amount);
                    Faction.AdjustPlayerFactionPoints(player, factionType, -amount);
                });
        }

        private void ActionGiveStanding()
        {
            _builder.Create("action-give-faction-standing")
                .Description("Gives faction standing toward a particular faction to a player.")
                .ActionsTakenAction((player, args) =>
                {
                    if (args.Length < 2)
                    {
                        const string Error = "'action-give-faction-standing' requires factionId and amount arguments";
                        SendMessageToPC(player, Error);
                        _logger.Write<ErrorLogGroup>(Error);
                        return;
                    }

                    // Try to find by faction Id
                    if (!int.TryParse(args[0], out var factionId))
                    {
                        const string Error = "'action-give-faction-standing' has an invalid argument for 'factionId'. Must be an Id mapped to the FactionType enumeration.";
                        SendMessageToPC(player, Error);
                        _logger.Write<ErrorLogGroup>(Error);
                        return;
                    }

                    // Try to read second argument as the amount
                    if (!int.TryParse(args[1], out var amount))
                    {
                        const string Error = "'action-give-faction-standing' has an invalid argument for 'amount'. Must be a number.";
                        SendMessageToPC(player, Error);
                        _logger.Write<ErrorLogGroup>(Error);
                        return;
                    }

                    var factionType = (FactionType)factionId;
                    amount = Math.Abs(amount);
                    Faction.AdjustPlayerFactionStanding(player, factionType, amount);
                });
        }

        private void ActionTakeStanding()
        {
            _builder.Create("action-take-faction-standing")
                .Description("Takes faction standing toward a particular faction from a player.")
                .ActionsTakenAction((player, args) =>
                {
                    if (args.Length < 2)
                    {
                        const string Error = "'action-take-faction-standing' requires factionId and amount arguments";
                        SendMessageToPC(player, Error);
                        _logger.Write<ErrorLogGroup>(Error);
                        return;
                    }

                    // Try to find by faction Id
                    if (!int.TryParse(args[0], out var factionId))
                    {
                        const string Error = "'action-take-faction-standing' has an invalid argument for 'factionId'. Must be an Id mapped to the FactionType enumeration.";
                        SendMessageToPC(player, Error);
                        _logger.Write<ErrorLogGroup>(Error);
                        return;
                    }

                    // Try to read second argument as the amount
                    if (!int.TryParse(args[1], out var amount))
                    {
                        const string Error = "'action-take-faction-standing' has an invalid argument for 'amount'. Must be a number.";
                        SendMessageToPC(player, Error);
                        _logger.Write<ErrorLogGroup>(Error);
                        return;
                    }

                    var factionType = (FactionType)factionId;
                    amount = Math.Abs(amount);
                    Faction.AdjustPlayerFactionStanding(player, factionType, -amount);
                });
        }

    }
}
