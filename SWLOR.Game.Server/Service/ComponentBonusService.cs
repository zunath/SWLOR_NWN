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
        
        private readonly IItemService _item;
        private readonly IBiowareXP2 _biowareXP2;
        private readonly IDurabilityService _durability;

        public ComponentBonusService(
            
            IItemService item,
            IBiowareXP2 biowareXP2,
            IDurabilityService durability)
        {
            
            _item = item;
            _biowareXP2 = biowareXP2;
            _durability = durability;
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
                        var maxDur = _durability.GetMaxDurability(product) + amount;
                        _durability.SetMaxDurability(product, maxDur);
                        _durability.SetDurability(product, maxDur);
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
                    case ComponentBonusType.DarkPotencyUp:
                        product.DarkPotencyBonus += amount;
                        break;
                    case ComponentBonusType.LightPotencyUp:
                        product.LightPotencyBonus += amount;
                        break;
                    case ComponentBonusType.MindPotencyUp:
                        product.MindPotencyBonus += amount;
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
                    case ComponentBonusType.DarkPotencyDown:
                        product.DarkPotencyBonus -= amount;
                        break;
                    case ComponentBonusType.LightPotencyDown:
                        product.LightPotencyBonus -= amount;
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
                        prop = _.ItemPropertyAttackBonus(amount);
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
                    case ComponentBonusType.MindPotencyDown:
                        product.MindPotencyBonus -= amount;
                        break;
                    case ComponentBonusType.ElectricalPotencyUp:
                        product.ElectricalPotencyBonus += amount;
                        break;
                    case ComponentBonusType.ElectricalPotencyDown:
                        product.ElectricalPotencyBonus -= amount;
                        break;
                    case ComponentBonusType.ForcePotencyUp:
                        product.ForcePotencyBonus += amount;
                        break;
                    case ComponentBonusType.ForcePotencyDown:
                        product.ForcePotencyBonus -= amount;
                        break;
                    case ComponentBonusType.ForceAccuracyUp:
                        product.ForceAccuracyBonus += amount;
                        break;
                    case ComponentBonusType.ForceAccuracyDown:
                        product.ForceAccuracyBonus -= amount;
                        break;
                    case ComponentBonusType.ForceDefenseUp:
                        product.ForceDefenseBonus += amount;
                        break;
                    case ComponentBonusType.ForceDefenseDown:
                        product.ForceDefenseBonus -= amount;
                        break;
                    case ComponentBonusType.ElectricalDefenseUp:
                        product.ElectricalDefenseBonus += amount;
                        break;
                    case ComponentBonusType.ElectricalDefenseDown:
                        product.ElectricalDefenseBonus -= amount;
                        break;
                    case ComponentBonusType.MindDefenseUp:
                        product.MindDefenseBonus += amount;
                        break;
                    case ComponentBonusType.MindDefenseDown:
                        product.MindDefenseBonus -= amount;
                        break;
                    case ComponentBonusType.LightDefenseUp:
                        product.LightDefenseBonus += amount;
                        break;
                    case ComponentBonusType.LightDefenseDown:
                        product.LightDefenseBonus -= amount;
                        break;
                    case ComponentBonusType.DarkDefenseUp:
                        product.DarkDefenseBonus += amount;
                        break;
                    case ComponentBonusType.DarkDefenseDown:
                        product.DarkDefenseBonus -= amount;
                        break;
                    case ComponentBonusType.PilotingUp:
                        product.PilotingBonus += amount;
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
