using SWLOR.Component.Inventory.Contracts;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Dialog.Contracts;
using SWLOR.Shared.Dialog.Model;
using SWLOR.Shared.Dialog.Service;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.Inventory.Feature.SnippetDefinition
{
    public class KeyItemSnippetDefinition: ISnippetListDefinition
    {
        private readonly ILogger _logger;
        private readonly IKeyItemService _keyItemService;
        private readonly SnippetBuilder _builder = new();

        public KeyItemSnippetDefinition(ILogger logger, IKeyItemService keyItemService)
        {
            _logger = logger;
            _keyItemService = keyItemService;
        }

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
                            type = _keyItemService.GetKeyItemTypeById(argId);
                        }
                        // Couldn't parse an integer. Look by name.
                        else
                        {
                            type = _keyItemService.GetKeyItemTypeByName(arg);
                        }

                        // Type is invalid, log an error and end.
                        if (type == KeyItemType.Invalid)
                        {
                            _logger.Write<ErrorLogGroup>($"{arg} is not a valid KeyItemType");
                            return false;
                        }

                        // Player doesn't have the specified key item.
                        if (!_keyItemService.HasKeyItem(player, type))
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
                            type = _keyItemService.GetKeyItemTypeById(argId);
                        }
                        // Couldn't parse an integer. Look by name.
                        else
                        {
                            type = _keyItemService.GetKeyItemTypeByName(arg);
                        }

                        // Type is invalid, log an error and end.
                        if (type == KeyItemType.Invalid)
                        {
                            _logger.Write<ErrorLogGroup>($"{arg} is not a valid KeyItemType");
                            return;
                        }

                        _keyItemService.GiveKeyItem(player, type);
                    }
                });
        }
    }
}
