using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    /// <summary>
    /// Design Schematics (plan section 7): using one unlocks its Hull Design for the character, so a starport Refit in
    /// Ship Management can change any of their ships of that model to it.
    /// </summary>
    public class DesignSchematicItemDefinition : IItemListDefinition
    {
        private readonly ItemBuilder _builder = new();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            _builder.Create("DESIGN_SCHEMATIC")
                .Delay(3f)
                .PlaysAnimation(Animation.LoopingGetMid)
                .ValidationAction((user, item, _, _, _) =>
                {
                    if (!GetIsPC(user) || GetIsDM(user))
                        return "Only players may use this item.";

                    return HullDesignRules.FindBySchematic(GetResRef(item)) == null
                        ? "This schematic has a configuration problem. Please inform a DM."
                        : string.Empty;
                })
                .ApplyAction((user, item, _, _, _) =>
                {
                    var design = HullDesignRules.FindBySchematic(GetResRef(item));
                    var dbPlayer = DB.Get<Player>(GetObjectUUID(user));
                    if (!HullDesignRules.Unlock(dbPlayer.UnlockedHullDesigns, design, DateTime.UtcNow))
                    {
                        SendMessageToPC(user, $"You already know the {design.ShipName} - {design.Name} Hull Design.");
                        return;
                    }

                    DB.Set(dbPlayer);
                    SendMessageToPC(user, ColorToken.Green(
                        $"You learn the {design.ShipName} - {design.Name} Hull Design. Refit a {design.ShipName} to it in Ship Management at a starport."));
                    Item.ReduceItemStack(item, 1);
                });

            return _builder.Build();
        }
    }
}
