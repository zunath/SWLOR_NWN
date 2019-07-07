using System;
using NWN;
using SWLOR.Game.Server.Bioware;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;


namespace SWLOR.Game.Server.Service
{
    public static class ComponentBonusService
    {
        
        public static void ApplyComponentBonus(NWItem product, ItemProperty sourceIP)
        {
            ComponentBonusType bonusType = (ComponentBonusType)_.GetItemPropertySubType(sourceIP);
            int amount = _.GetItemPropertyCostTableValue(sourceIP);
            ItemProperty prop = null;
            string sourceTag = string.Empty;
            int attackBonus = 0;

            // A note about the sourceTags:
            // It's not currently possible to create custom item properties on items. To get around this,
            // we look in an inaccessible container which holds the custom item properties. Then, we get the
            // item that has the item property we want. From there we take that item property and copy it to 
            // the crafted item.
            // This is a really roundabout way to do it, but it's the only option for now. Hopefully a feature to
            // directly add the item properties comes in to NWNX in the future.
            // 2019-06-12: Directly modifying item properties is possible now but I'm not going to do a refactor until later.
            //             Anyone interested in working on this let me know and I can point you in the right direction. - Z
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
                        var maxDur = DurabilityService.GetMaxDurability(product) + amount;
                        DurabilityService.SetMaxDurability(product, maxDur);
                        DurabilityService.SetDurability(product, maxDur);
                        break;
                    case ComponentBonusType.ChargesUp:
                        product.Charges += amount;
                        break;
                    case ComponentBonusType.ACUp:
                        product.CustomAC += amount;
                        break;
                    case ComponentBonusType.HarvestingUp:
                        product.HarvestingBonus += amount;
                        break;
                    case ComponentBonusType.CooldownRecoveryUp:
                        product.CooldownRecovery += amount;
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
                    case ComponentBonusType.LuckUp:
                        product.LuckBonus += amount;
                        break;
                    case ComponentBonusType.MeditateUp:
                        product.MeditateBonus += amount;
                        break;
                    case ComponentBonusType.RestUp:
                        product.RestBonus += amount;
                        break;
                    case ComponentBonusType.MedicineUp:
                        product.MedicineBonus += amount;
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
                    case ComponentBonusType.StructureBonusUp:
                        product.StructureBonus += amount;
                        break;
                    case ComponentBonusType.StrengthUp:
                        product.StrengthBonus += amount;
                        break;
                    case ComponentBonusType.DexterityUp:
                        product.DexterityBonus += amount;
                        break;
                    case ComponentBonusType.ConstitutionUp:
                        product.ConstitutionBonus += amount;
                        break;
                    case ComponentBonusType.WisdomUp:
                        product.WisdomBonus += amount;
                        break;
                    case ComponentBonusType.IntelligenceUp:
                        product.IntelligenceBonus += amount;
                        break;
                    case ComponentBonusType.CharismaUp:
                        product.CharismaBonus += amount;
                        break;
                    case ComponentBonusType.AttackBonusUp:
                        attackBonus += amount;
                        break;
                    case ComponentBonusType.DurationUp:
                        product.DurationBonus += amount;
                        break;
                    case ComponentBonusType.ScanningUp:
                        product.ScanningBonus += amount;
                        break;
                    case ComponentBonusType.ScavengingUp:
                        product.ScavengingBonus += amount;
                        break;
                    case ComponentBonusType.PilotingUp:
                        product.PilotingBonus += amount;
                        break;

                    // Legacy and other component bonus types won't do anything.
                    default: 
                        return;
                }

                if (!string.IsNullOrWhiteSpace(sourceTag))
                {
                    prop = ItemService.GetCustomItemPropertyByItemTag(sourceTag);
                }

                if (prop == null) return;

                BiowareXP2.IPSafeAddItemProperty(product, prop, 0.0f, AddItemPropertyPolicy.IgnoreExisting, true, true);
            }
            
            // Attack bonus is aggregated into one item property, ensuring that the amount doesn't go over 20.
            if (attackBonus > 0)
            {
                // Look for existing properties, get the value and add it. Then remove that item property.
                foreach (var ip in product.ItemProperties)
                {
                    if (_.GetItemPropertyType(ip) == _.ITEM_PROPERTY_ATTACK_BONUS)
                    {
                        amount = _.GetItemPropertyCostTableValue(ip);
                        attackBonus += amount;

                        _.RemoveItemProperty(product, ip);
                    }
                }

                // Clamp bonus to 20.
                if (attackBonus > 20) attackBonus = 20;

                // Time to add the new item property.
                prop = _.ItemPropertyAttackBonus(attackBonus);
                BiowareXP2.IPSafeAddItemProperty(product, prop, 0.0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            }
        }
    }
}
