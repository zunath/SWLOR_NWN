using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class TrapKitItemDefinition: IItemListDefinition
    {
        private static readonly Dictionary<int, string> _tierLabels = new()
        {
            [1] = "I", [2] = "II", [3] = "III", [4] = "IV", [5] = "V"
        };

        private readonly ItemBuilder _builder = new();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            CreateKit("trap_kit_1", 1);
            CreateKit("trap_kit_2", 2);
            CreateKit("trap_kit_3", 3);
            CreateKit("trap_kit_4", 4);
            CreateKit("trap_kit_5", 5);

            return _builder.Build();
        }

        private void CreateKit(string tag, int tier)
        {
            _builder.Create(tag)
                .ActivationSpell(CastSpell.UNIQUE_POWER_SELF_ONLY)
                .Delay(2f)
                .PlaysAnimation(Animation.LoopingGetMid)
                .ValidationAction((user, item, target, location, itemPropertyIndex) =>
                {
                    if (!GetIsObjectValid(item) || GetItemPossessor(item) != user)
                        return "The trap kit must be in your inventory.";

                    if (!GetIsPC(user) || GetIsDM(user))
                    {
                        return "Only players may deploy trap kits.";
                    }

                    if (!HasRequiredTrapcraft(user, tier))
                    {
                        return tier >= 5
                            ? "You lack the Master Saboteur expertise to deploy this kit."
                            : "You lack the Trapcraft expertise to deploy this kit.";
                    }

                    return string.Empty;
                })
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    var placed = Traps.TryPlaceKitTrap(user, GetLocation(user), tier);

                    if (!placed)
                    {
                        return;
                    }

                    Item.ReduceItemStack(item, 1);

                    Log.Write(LogGroup.Crafting,
                        $"Player '{GetName(user)}' ({GetObjectUUID(user)}) deployed a Tier {_tierLabels[tier]} trap kit.");
                    SendMessageToPC(user, $"You deploy a Tier {_tierLabels[tier]} trap. It will arm itself after a moment.");
                });
        }

        /// <summary>
        /// Trapcraft only grants tiers 1-4 (Trapcraft I-IV). Tier 5 kits are gated behind the
        /// Master Saboteur capstone perk instead, since Trapcraft has no fifth level.
        /// </summary>
        private static bool HasRequiredTrapcraft(uint user, int tier)
        {
            return EspionageProgression.CanUseTrapTier(
                Perk.GetPerkLevel(user, PerkType.Trapcraft),
                Perk.GetPerkLevel(user, PerkType.MasterSaboteur) >= 1,
                tier);
        }
    }
}
