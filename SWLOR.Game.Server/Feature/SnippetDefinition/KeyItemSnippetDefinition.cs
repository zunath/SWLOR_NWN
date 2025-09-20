using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.SnippetService;
using SWLOR.Shared.Core.Log;
using SWLOR.Shared.Core.Log.LogGroup;

namespace SWLOR.Game.Server.Feature.SnippetDefinition
{
    public class KeyItemSnippetDefinition: ISnippetListDefinition
    {
        private ILogger _logger = ServiceContainer.GetService<ILogger>();
        private readonly SnippetBuilder _builder = new SnippetBuilder();

        public Dictionary<string, SnippetDetail> BuildSnippets()
        {
            // Conditions
            ConditionAllKeyItems();

            // Actions
            ActionGiveKeyItem();

            return _builder.Build();
        }

        private void ConditionAllKeyItems()
        {
            _builder.Create("condition-all-key-items")
                .Description("Checks whether a player has all of the specified key items.")
                .AppearsWhenAction((player, args) =>
                {
                    if (args.Length <= 0)
                    {
                        const string Error = "'condition-all-key-items' requires a keyItemId argument.";
                        SendMessageToPC(player, Error);
                        _logger.Write<ErrorLogGroup>(Error);
                        return false;
                    }

                    foreach (var arg in args)
                    {
                        KeyItemType type;

                        // Try searching by Id first.
                        if (int.TryParse(arg, out var argId))
                        {
                            type = KeyItem.GetKeyItemTypeById(argId);
                        }
                        // Couldn't parse an integer. Look by name.
                        else
                        {
                            type = KeyItem.GetKeyItemTypeByName(arg);
                        }

                        // Type is invalid, log an error and end.
                        if (type == KeyItemType.Invalid)
                        {
                            _logger.Write<ErrorLogGroup>($"{arg} is not a valid KeyItemType");
                            return false;
                        }

                        // Player doesn't have the specified key item.
                        if (!KeyItem.HasKeyItem(player, type))
                        {
                            return false;
                        }
                    }

                    return true;
                });
                    
        }

        private void ActionGiveKeyItem()
        {
            _builder.Create("action-give-key-items")
                .Description("Gives a one or more key items to the player.")
                .ActionsTakenAction((player, args) =>
                {
                    if (args.Length <= 0)
                    {
                        const string Error = "'action-give-key-items' requires a keyItemId argument.";
                        SendMessageToPC(player, Error);
                        _logger.Write<ErrorLogGroup>(Error);
                        return;
                    }

                    foreach (var arg in args)
                    {
                        KeyItemType type;

                        // Try searching by Id first.
                        if (int.TryParse(arg, out var argId))
                        {
                            type = KeyItem.GetKeyItemTypeById(argId);
                        }
                        // Couldn't parse an integer. Look by name.
                        else
                        {
                            type = KeyItem.GetKeyItemTypeByName(arg);
                        }

                        // Type is invalid, log an error and end.
                        if (type == KeyItemType.Invalid)
                        {
                            _logger.Write<ErrorLogGroup>($"{arg} is not a valid KeyItemType");
                            return;
                        }

                        KeyItem.GiveKeyItem(player, type);
                    }
                });
        }
    }
}
