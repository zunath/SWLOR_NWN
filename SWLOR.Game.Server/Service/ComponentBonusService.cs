using System;
using NWN;
using SWLOR.Game.Server.Bioware.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class ComponentBonusService : IComponentBonusService
    {
        private readonly INWScript _;
        private readonly IItemService _item;
        private readonly IBiowareXP2 _biowareXP2;

        public ComponentBonusService(
            INWScript script,
            IItemService item,
            IBiowareXP2 biowareXP2)
        {
            _ = script;
            _item = item;
            _biowareXP2 = biowareXP2;
        }

        public void ApplyComponentBonus(NWItem product, ItemProperty sourceIP)
        {
            ComponentBonusType bonusType = (ComponentBonusType)_.GetItemPropertySubType(sourceIP);
            int amount = _.GetItemPropertyCostTableValue(sourceIP);
            ItemProperty prop = null;
            string sourceTag = string.Empty;

            // A note about the sourceTags:
            // It's not currently possible to create custom item properties on items. To get around this,
            // we look in an inaccessible container which holds the custom item properties. Then, we get the
            // item that has the item property we want. From there we take that item property and copy it to 
            // the crafted item.
            // This is a really roundabout way to do it, but it's the only option for now. Hopefully a feature to
            // directly add the item properties comes in to NWNX in the future.
            for (int x = 1; x <= amount; x++)
            {
                switch (bonusType)
                {
                    case ComponentBonusType.ModSocketRed: 
                        sourceTag = "rslot_red"; 
                        break;
                    case ComponentBonusType.ModSocketBlue: 
                        sourceTag = "rslot_blue"; 
                        break;
                    case ComponentBonusType.ModSocketGreen: 
                        sourceTag = "rslot_green"; 
                        break;
                    case ComponentBonusType.ModSocketYellow: 
                        sourceTag = "rslot_yellow"; 
                        break;
                    case ComponentBonusType.ModSocketPrismatic: 
                        sourceTag = "rslot_prismatic"; 
                        break;
                    case ComponentBonusType.DurabilityUp:
                        product.MaxDurability += amount;
                        product.Durability = product.MaxDurability;
                        break;
                    case ComponentBonusType.ChargesUp:
                        product.Charges += amount;
                        break;
                    case ComponentBonusType.ACUp:
                        product.CustomAC += amount;
                        break;
                    case ComponentBonusType.LoggingUp:
                        product.LoggingBonus += amount;
                        break;
                    case ComponentBonusType.MiningUp:
                        product.MiningBonus += amount;
                        break;
                    case ComponentBonusType.CastingSpeedUp:
                        product.CastingSpeed += amount;
                        break;
                    case ComponentBonusType.ArmorsmithUp:
                        product.CraftBonusArmorsmith += amount;
                        break;
                    case ComponentBonusType.WeaponsmithUp:
                        product.CraftBonusWeaponsmith += amount;
                        break;
                    case ComponentBonusType.CookingUp:
                        product.CraftBonusCooking += amount;
                        break;
                    case ComponentBonusType.EngineeringUp:
                        product.CraftBonusEngineering += amount;
                        break;
                    case ComponentBonusType.FabricationUp:
                        product.CraftBonusFabrication += amount;
                        break;
                    case ComponentBonusType.HPUp:
                        product.HPBonus += amount;
                        break;
                    case ComponentBonusType.FPUp:
                        product.FPBonus += amount;
                        break;
                    case ComponentBonusType.EnmityUp:
                        product.EnmityRate += amount;
                        break;
                    case ComponentBonusType.EnmityDown:
                        product.EnmityRate -= amount;
                        break;
                    case ComponentBonusType.DarkAbilityUp:
                        product.DarkAbilityBonus += amount;
                        break;
                    case ComponentBonusType.LightAbilityUp:
                        product.LightAbilityBonus += amount;
                        break;
                    case ComponentBonusType.LuckUp:
                        product.LuckBonus += amount;
                        break;
                    case ComponentBonusType.MeditateUp:
                        product.MeditateBonus += amount;
                        break;
                    case ComponentBonusType.FirstAidUp:
                        product.FirstAidBonus += amount;
                        break;
                    case ComponentBonusType.HPRegenUp:
                        product.HPRegenBonus += amount;
                        break;
                    case ComponentBonusType.FPRegenUp:
                        product.FPRegenBonus += amount;
                        break;
                    case ComponentBonusType.BaseAttackBonusUp:
                        product.BaseAttackBonus += amount;
                        break;
                    case ComponentBonusType.SneakAttackUp:
                        product.SneakAttackBonus += amount;
                        break;
                    case ComponentBonusType.DamageUp:
                        product.DamageBonus += amount;
                        break;
                    case ComponentBonusType.DarkAbilityDown:
                        product.DarkAbilityBonus -= amount;
                        break;
                    case ComponentBonusType.LightAbilityDown:
                        product.LightAbilityBonus -= amount;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (!string.IsNullOrWhiteSpace(sourceTag))
                {
                    prop = _item.GetCustomItemPropertyByItemTag(sourceTag);
                }

                if (prop == null) return;

                _biowareXP2.IPSafeAddItemProperty(product, prop, 0.0f, AddItemPropertyPolicy.IgnoreExisting, true, true);
            }

        }
    }
}
