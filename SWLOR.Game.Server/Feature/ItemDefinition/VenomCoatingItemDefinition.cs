using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class VenomCoatingItemDefinition: IItemListDefinition
    {
        // Local variables set on the weapon. The combat-consumption side (owned elsewhere)
        // reads these three locals when resolving on-hit poison application.
        public const string PoisonCoatingTierVariable = "POISON_COATING_TIER";
        public const string PoisonCoatingChargesVariable = "POISON_COATING_CHARGES";
        public const string PoisonCoatingPotencyVariable = "POISON_COATING_POTENCY";

        private const int BaseCharges = 20;

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

            return _builder.Build();
        }

        private void CreateVial(string tag, int tier)
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
                    var potency = Stat.GetStatAdjustment(user, StatType.PoisonBonus);

                    SetLocalInt(target, PoisonCoatingTierVariable, tier);
                    var coatingDurationBonus = Stat.GetStatAdjustment(user, StatType.PoisonCoatingDurationPercent);
            var charges = BaseCharges * (100 + coatingDurationBonus) / 100;
            SetLocalInt(target, PoisonCoatingChargesVariable, charges);
                    SetLocalInt(target, PoisonCoatingPotencyVariable, potency);

                    Item.ReduceItemStack(item, 1);

                    SendMessageToPC(user, $"You coat {GetName(target)} in Tier {_tierLabels[tier]} venom. ({BaseCharges} charges)");
                });
        }
    }
}
