using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Bioware;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Scripts.Delayed
{
    public class CraftCreateItem: IScript
    {
        private Guid _eventID;

        public void SubscribeEvents()
        {
            _eventID = MessageHub.Instance.Subscribe<OnCreateCraftedItem>(OnCreateCraftedItem);
        }

        private void OnCreateCraftedItem(OnCreateCraftedItem data)
        {
            using (new Profiler(nameof(CraftCreateItem)))
            {
                try
                {
                    RunCreateItem(data.Player);
                    data.Player.IsBusy = false;
                }
                catch (Exception ex)
                {
                    LoggingService.LogError(ex);
                }
            }
        }

        public void UnsubscribeEvents()
        {
            MessageHub.Instance.Unsubscribe(_eventID);
        }

        public void Main()
        {
        }
        
        private void RunCreateItem(NWPlayer player)
        {
            foreach (var effect in player.Effects)
            {
                if (NWScript.GetEffectTag(effect) == "CRAFTING_IMMOBILIZATION")
                {
                    NWScript.RemoveEffect(player, effect);
                }
            }

            var model = CraftService.GetPlayerCraftingData(player);

            var blueprint = DataService.CraftBlueprint.GetByID(model.BlueprintID);
            var baseStructure = blueprint.BaseStructureID == null ? null : DataService.BaseStructure.GetByID(Convert.ToInt32(blueprint.BaseStructureID));
            var pcSkill = SkillService.GetPCSkill(player, blueprint.SkillID);

            var pcEffectiveLevel = CraftService.CalculatePCEffectiveLevel(player, pcSkill.Rank, (SkillType)blueprint.SkillID);
            var itemLevel = model.AdjustedLevel;
            var atmosphereBonus = CraftService.CalculateAreaAtmosphereBonus(player.Area);
            var chance = CalculateBaseChanceToAddProperty(pcEffectiveLevel, itemLevel, atmosphereBonus);
            var equipmentBonus = CalculateEquipmentBonus(player, (SkillType)blueprint.SkillID);

            if (chance <= 1.0f)
            {
                player.FloatingText(ColorTokenService.Red("Critical failure! You don't have enough skill to create that item. All components were lost."));
                CraftService.ClearPlayerCraftingData(player, true);
                return;
            }

            var luckyBonus = PerkService.GetCreaturePerkLevel(player, PerkType.Lucky);
            var craftedItems = new List<NWItem>();
            NWItem craftedItem = (NWScript.CreateItemOnObject(blueprint.ItemResref, player.Object, blueprint.Quantity));
            craftedItem.IsIdentified = true;
            craftedItems.Add(craftedItem);

            // If item isn't stackable, loop through and create as many as necessary.
            if (craftedItem.StackSize < blueprint.Quantity)
            {
                for (var x = 2; x <= blueprint.Quantity; x++)
                {
                    craftedItem = (NWScript.CreateItemOnObject(blueprint.ItemResref, player.Object));
                    craftedItem.IsIdentified = true;
                    craftedItems.Add(craftedItem);
                }
            }

            // Recommended level gets set regardless if all item properties make it on the final product.
            // Also mark who crafted the item. This is later used for display on the item's examination event.
            foreach (var item in craftedItems)
            {
                item.RecommendedLevel = itemLevel < 0 ? 0 : itemLevel;
                item.SetLocalString("CRAFTER_PLAYER_ID", player.GlobalID.ToString());

                BaseService.ApplyCraftedItemLocalVariables(item, baseStructure);
            }

            if(RandomService.Random(1, 100) <= luckyBonus)
            {
                chance += RandomService.Random(1, luckyBonus);
            }
            
            var successAmount = 0;
            foreach (var component in model.MainComponents)
            {
                var result = RunComponentBonusAttempt(player, component, equipmentBonus, chance, craftedItems);
                successAmount += result.Item1;
                chance = result.Item2;
            }
            foreach (var component in model.SecondaryComponents)
            {
                var result = RunComponentBonusAttempt(player, component, equipmentBonus, chance, craftedItems);
                successAmount += result.Item1;
                chance = result.Item2;
            }
            foreach (var component in model.TertiaryComponents)
            {
                var result = RunComponentBonusAttempt(player, component, equipmentBonus, chance, craftedItems);
                successAmount += result.Item1;
                chance = result.Item2;
            }
            foreach (var component in model.EnhancementComponents)
            {
                var result = RunComponentBonusAttempt(player, component, equipmentBonus, chance, craftedItems);
                successAmount += result.Item1;
                chance = result.Item2;
            }

            // Structures gain increased durability based on the blueprint
            if (baseStructure != null)
            {
                foreach (var item in craftedItems)
                {
                    var maxDur = DurabilityService.GetMaxDurability(item);
                    maxDur += (float)baseStructure.Durability;
                    DurabilityService.SetMaxDurability(item, maxDur);
                    DurabilityService.SetDurability(item, maxDur);
                }
            }
            
            player.SendMessage("You created " + blueprint.Quantity + "x " + blueprint.ItemName + "!");
            var baseXP = 750 + successAmount * RandomService.Random(1, 50);
            var xp = SkillService.CalculateRegisteredSkillLevelAdjustedXP(baseXP, model.AdjustedLevel, pcSkill.Rank);

            var exists = DataService.PCCraftedBlueprint.ExistsByPlayerIDAndCraftedBlueprintID(player.GlobalID, blueprint.ID);
            if(!exists)
            {
                xp = xp * 1.50f;
                player.SendMessage("You receive an XP bonus for crafting this item for the first time.");

                var pcCraftedBlueprint = new PCCraftedBlueprint
                {
                    CraftBlueprintID = blueprint.ID,
                    DateFirstCrafted = DateTime.UtcNow,
                    PlayerID = player.GlobalID
                };

                DataService.SubmitDataChange(pcCraftedBlueprint, DatabaseActionType.Insert);
            }

            SkillService.GiveSkillXP(player, blueprint.SkillID, (int)xp);
            CraftService.ClearPlayerCraftingData(player, true);
            player.SetLocalInt("LAST_CRAFTED_BLUEPRINT_ID_" + blueprint.CraftDeviceID, blueprint.ID);
        }


        private Tuple<int, float> RunComponentBonusAttempt(NWPlayer player, NWItem component, float equipmentBonus, float chance, List<NWItem> itemSet)
        {
            var successAmount = 0;

            // Note - this line MUST be outside the foreach loop, as inspecting component properties will reset the component.ItemProperties counter.
            var componentLevel = component.LevelIncrease > 0 ? component.LevelIncrease : component.RecommendedLevel;
            if (componentLevel < 1) componentLevel = 1;
            foreach (var ip in component.ItemProperties)
            {
                var ipType = NWScript.GetItemPropertyType(ip);
                if (ipType != ItemPropertyType.ComponentBonus) continue;

                var bonusTypeID = NWScript.GetItemPropertySubType(ip);
                var tlkID = Convert.ToInt32(NWScript.Get2DAString("iprp_compbon", "Name", bonusTypeID));
                var amount = NWScript.GetItemPropertyCostTableValue(ip);
                var bonusName = NWScript.GetStringByStrRef(tlkID) + " " + amount;
                var random = RandomService.RandomFloat() * 100.0f;
                var modifiedEquipmentBonus = equipmentBonus * 0.25f;

                if (random <= chance + modifiedEquipmentBonus)
                {
                    foreach (var item in itemSet)
                    {
                        // If the target item is a component itself, we want to add the component bonuses instead of the
                        // actual item property bonuses.
                        // In other words, we want the custom item property "Component Bonus: AC UP" instead of the "AC Bonus" item property.
                        var componentIP = item.ItemProperties.FirstOrDefault(x => NWScript.GetItemPropertyType(x) == ItemPropertyType.ComponentType);
                        if (componentIP == null)
                            ComponentBonusService.ApplyComponentBonus(item, ip);
                        else
                            BiowareXP2.IPSafeAddItemProperty(item, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);

                    }
                    player.SendMessage(ColorTokenService.Green("Successfully applied component property: " + bonusName));

                    var bonusType = (ComponentBonusType)NWScript.GetItemPropertySubType(ip);
                    if (bonusType != ComponentBonusType.DurabilityUp)
                    {
                        // Durability bonuses don't increase the penalty.  Higher level components transfer multiple
                        // properties more easily (to balance the fact that you can fit fewer of them on an item).     
                        int penalty;
                        switch (componentLevel)
                        {
                            case 1:
                                penalty = RandomService.Random(1, 19);
                                break;
                            case 2:
                                penalty = RandomService.Random(1, 9);
                                break;
                            case 3:
                                penalty = RandomService.Random(1, 6);
                                break;
                            case 4:
                                penalty = RandomService.Random(1, 4);
                                break;
                            default:
                                penalty = RandomService.Random(1, 3);
                                break;
                        }
                        chance -=  penalty;
                        if (chance < 1) chance = 1;
                    }

                    successAmount++;
                }
                else
                {
                    player.SendMessage(ColorTokenService.Red("Failed to apply component property: " + bonusName));
                }
            }

            return new Tuple<int, float>(successAmount, chance);
        }


        private float CalculateEquipmentBonus(NWPlayer player, SkillType skillType)
        {
            var effectiveStats = PlayerStatService.GetPlayerItemEffectiveStats(player);
            var equipmentBonus = 0;
            var multiplier = 0.5f;
            var atmosphere = CraftService.CalculateAreaAtmosphereBonus(player.Area);

            if (atmosphere >= 150)
            {
                multiplier = 0.8f;
            }
            else if (atmosphere >= 75)
            {
                multiplier = 0.7f;
            }
            else if (atmosphere >= 25)
            {
                multiplier = 0.6f;
            }

            switch (skillType)
            {
                case SkillType.Armorsmith: equipmentBonus = effectiveStats.Armorsmith; break;
                case SkillType.Weaponsmith: equipmentBonus = effectiveStats.Weaponsmith; break;
                case SkillType.Cooking: equipmentBonus = effectiveStats.Cooking; break;
                case SkillType.Engineering: equipmentBonus = effectiveStats.Engineering; break;
                case SkillType.Fabrication: equipmentBonus = effectiveStats.Fabrication; break;
                case SkillType.Medicine: equipmentBonus = effectiveStats.Medicine; break;
            }

            return equipmentBonus * multiplier; // +0.5%, +0.6%, +0.7%, or 0.8% per equipment bonus
        }

        private float CalculateBaseChanceToAddProperty(int pcLevel, int blueprintLevel, int atmosphereBonus)
        {
            var delta = pcLevel - blueprintLevel;
            var percentage = 0.0f;

            if (delta <= -5)
            {
                percentage = 0.0f;
            }
            else if (delta >= 4)
            {
                percentage = 90.0f;
            }
            else
            {
                switch (delta)
                {
                    case -4:
                        percentage = 10.0f;
                        break;
                    case -3:
                        percentage = 15.0f;
                        break;
                    case -2:
                        percentage = 25.0f;
                        break;
                    case -1:
                        percentage = 35.0f;
                        break;
                    case 0:
                        percentage = 50.0f;
                        break;
                    case 1:
                        percentage = 65.0f;
                        break;
                    case 2:
                        percentage = 75.0f;
                        break;
                    case 3:
                        percentage = 85.0f;
                        break;
                }
            }

            if (atmosphereBonus >= 60)
            {
                percentage += 4;
            }
            else if(atmosphereBonus >= 15)
            {
                percentage += 2;
            }

            if (percentage > 90)
                percentage = 90;
            
            return percentage;
        }
    }
}
