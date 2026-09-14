using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class VenomCoatingItemDefinition : IItemListDefinition
    {
        // Local variables set on the weapon. The combat-consumption side (owned elsewhere)
        // reads these three locals when resolving on-hit poison application.
        public const string PoisonCoatingTierVariable = "POISON_COATING_TIER";
        public const string PoisonCoatingChargesVariable = "POISON_COATING_CHARGES";
        public const string PoisonCoatingPotencyVariable = "POISON_COATING_POTENCY";

        private const int BaseCharges = 20;
        private const int ConcentratedCharges = 10;

        private static readonly Dictionary<int, string> _tierLabels = new()
        {
            [1] = "I", [2] = "II", [3] = "III", [4] = "IV", [5] = "V"
        };

        private readonly ItemBuilder _builder = new();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            CreateVial("poison_vial_1", 1);
            CreateVial("poison_vial_2", 2);
            CreateVial("poison_vial_3", 3);
            CreateVial("poison_vial_4", 4);
            CreateVial("poison_vial_5", 5);
            CreateVial("conc_poison_1", 1, true);
            CreateVial("conc_poison_2", 2, true);
            CreateVial("conc_poison_3", 3, true);
            CreateVial("conc_poison_4", 4, true);
            CreateVial("conc_poison_5", 5, true);

            return _builder.Build();
        }

        private void CreateVial(string tag, int tier, bool concentrated = false)
        {
            _builder.Create(tag)
                .Delay(2f)
                .PlaysAnimation(Animation.LoopingGetMid)
                .MaxDistance(0.0f)
                .ValidationAction((user, item, target, location, itemPropertyIndex) =>
                {
                    if (!GetIsPC(user) || GetIsDM(user))
                    {
                        return "Only players may use this coating.";
                    }

                    var baseItemType = GetBaseItemType(target);

                    if (Item.LightsaberBaseItemTypes.Contains(baseItemType) ||
                        Item.SaberstaffBaseItemTypes.Contains(baseItemType))
                    {
                        return "The coating will not adhere to an energy blade.";
                    }

                    if (!Item.WeaponBaseItemTypes.Contains(baseItemType) ||
                        Item.PistolBaseItemTypes.Contains(baseItemType) ||
                        Item.RifleBaseItemTypes.Contains(baseItemType))
                    {
                        return "Only melee or thrown weapons can be coated in venom.";
                    }

                    var existingTier = GetLocalInt(target, PoisonCoatingTierVariable);
                    if (existingTier > tier)
                    {
                        return $"This weapon is already coated with a stronger venom (Tier {_tierLabels[existingTier]}).";
                    }

                    return string.Empty;
                })
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    var potency = Stat.GetStatAdjustment(user, StatType.PoisonBonus) + (concentrated ? tier * 10 : 0);
                    var coatingDurationBonus = Stat.GetStatAdjustment(user, StatType.PoisonCoatingDurationPercent);
                    var charges = concentrated ? ConcentratedCharges : CalculateCharges(coatingDurationBonus);

                    SetLocalInt(target, PoisonCoatingTierVariable, tier);
                    SetLocalInt(target, PoisonCoatingChargesVariable, charges);
                    SetLocalInt(target, PoisonCoatingPotencyVariable, potency);

                    Item.ReduceItemStack(item, 1);

                    Log.Write(LogGroup.Crafting,
                        $"Player '{GetName(user)}' ({GetObjectUUID(user)}) applied Tier {_tierLabels[tier]}{(concentrated ? " concentrated" : string.Empty)} venom coating to '{GetName(target)}' (potency {potency}, {charges} charges).");
                    SendMessageToPC(user, $"You coat {GetName(target)} in Tier {_tierLabels[tier]}{(concentrated ? " concentrated" : string.Empty)} venom. ({charges} charges)");
                });
        }

        public static int CalculateCharges(int coatingDurationBonusPercent)
        {
            return BaseCharges * (100 + Math.Max(0, coatingDurationBonusPercent)) / 100;
        }
    }
}
