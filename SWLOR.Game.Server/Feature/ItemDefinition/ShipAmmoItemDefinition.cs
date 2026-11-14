using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.ShipRewardDefinition;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    /// <summary>
    /// Launcher ammo variants (plan section 14): using one loads it into its launchers, so they fire it before plain
    /// ammo; using the loaded variant again unloads it.
    /// </summary>
    public class ShipAmmoItemDefinition : IItemListDefinition
    {
        private readonly ItemBuilder _builder = new();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            var tags = RevampRewardCatalog.AmmoVariants.Select(x => x.Resref).ToArray();
            _builder.Create(tags[0], tags.Skip(1).ToArray())
                .ValidationAction((user, _, _, _, _) =>
                    GetIsPC(user) && !GetIsDM(user) ? string.Empty : "Only players may use this item.")
                .ApplyAction((user, item, _, _, _) =>
                {
                    var variant = ShipAmmoRules.Variant(GetResRef(item));
                    if (variant == null)
                        return;

                    var variable = ShipAmmoRules.LoadedVariable(variant.Launcher);
                    if (GetLocalString(user, variable) == variant.Resref)
                    {
                        DeleteLocalString(user, variable);
                        SendMessageToPC(user, $"Your launchers go back to standard ammo instead of {variant.Name}s.");
                        return;
                    }

                    SetLocalString(user, variable, variant.Resref);
                    SendMessageToPC(user, $"Your launchers now fire {variant.Name}s first.");
                });

            return _builder.Build();
        }
    }
}
