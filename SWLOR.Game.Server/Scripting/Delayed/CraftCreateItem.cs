using System;
using System.Collections.Generic;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Bioware;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;
using AddItemPropertyPolicy = SWLOR.Game.Server.Enumeration.AddItemPropertyPolicy;
using Skill = SWLOR.Game.Server.Enumeration.Skill;

namespace SWLOR.Game.Server.Scripting.Delayed
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
                if (_.GetEffectTag(effect) == "CRAFTING_IMMOBILIZATION")
                {
                    _.RemoveEffect(player, effect);
                }
            }

            var model = CraftService.GetPlayerCraftingData(player);

            var blueprint = CraftService.GetBlueprintByID(model.Blueprint);
            var baseStructure = BaseService.GetBaseStructure(blueprint.BaseStructureID);
            var dbPlayer = DataService.Player.GetByID(player.GlobalID);
            var pcSkill = dbPlayer.Skills[blueprint.Skill];

            int pcEffectiveLevel = CraftService.CalculatePCEffectiveLevel(player, pcSkill.Rank, blueprint.Skill);
            int itemLevel = model.AdjustedLevel;
            int atmosphereBonus = CraftService.CalculateAreaAtmosphereBonus(player.Area);
            float chance = CalculateBaseChanceToAddProperty(pcEffectiveLevel, itemLevel, atmosphereBonus);
            float equipmentBonus = CalculateEquipmentBonus(player, blueprint.Skill);

            if (chance <= 1.0f)
            {
                player.FloatingText(ColorTokenService.Red("Critical failure! You don't have enough skill to create that item. All components were lost."));
                CraftService.ClearPlayerCraftingData(player, true);
                return;
            }

            int luckyBonus = PerkService.GetCreaturePerkLevel(player, PerkType.Lucky);
            var craftedItems = new List<NWItem>();
            NWItem craftedItem = (_.CreateItemOnObject(blueprint.Resref, player.Object, blueprint.Quantity));
            craftedItem.IsIdentified = true;
            craftedItems.Add(craftedItem);

            // If item isn't stackable, loop through and create as many as necessary.
            if (craftedItem.StackSize < blueprint.Quantity)
            {
                for (int x = 2; x <= blueprint.Quantity; x++)
                {
                    craftedItem = (_.CreateItemOnObject(blueprint.Resref, player.Object));
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

                BaseService.ApplyCraftedItemLocalVariables(item, blueprint.BaseStructureID);
            }

            if(RandomService.Random(1, 100) <= luckyBonus)
            {
                chance += RandomService.Random(1, luckyBonus);
            }
            
            int successAmount = 0;
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
                    maxDur += baseStructure.Durability;
                    DurabilityService.SetMaxDurability(item, maxDur);
                    DurabilityService.SetDurability(item, maxDur);
                }
            }
            
            player.SendMessage("You created " + blueprint.Quantity + "x " + blueprint.ItemName + "!");
            int baseXP = 750 + successAmount * RandomService.Random(1, 50);
            float xp = SkillService.CalculateRegisteredSkillLevelAdjustedXP(baseXP, model.AdjustedLevel, pcSkill.Rank);

            bool exists = dbPlayer.CraftedBlueprints.Contains(model.Blueprint);
            if(!exists)
            {
                xp = xp * 1.50f;
                player.SendMessage("You receive an XP bonus for crafting this item for the first time.");

                dbPlayer.CraftedBlueprints.Add(model.Blueprint);
                DataService.Set(dbPlayer);
            }

            SkillService.GiveSkillXP(player, blueprint.Skill, (int)xp);
            CraftService.ClearPlayerCraftingData(player, true);
            player.SetLocalInt("LAST_CRAFTED_BLUEPRINT_ID_" + blueprint.CraftDevice, (int)model.Blueprint);
        }


        private Tuple<int, float> RunComponentBonusAttempt(NWPlayer player, NWItem component, float equipmentBonus, float chance, List<NWItem> itemSet)
        {
            int successAmount = 0;

            // Note - this line MUST be outside the foreach loop, as inspecting component properties will reset the component.ItemProperties counter.
            int componentLevel = component.LevelIncrease > 0 ? component.LevelIncrease : component.RecommendedLevel;
            if (componentLevel < 1) componentLevel = 1;
            foreach (var ip in component.ItemProperties)
            {
                var ipType = _.GetItemPropertyType(ip);
                if (ipType != ItemPropertyType.ComponentBonus) continue;

                int bonusTypeID = _.GetItemPropertySubType(ip);
                int tlkID = Convert.ToInt32(_.Get2DAString("iprp_compbon", "Name", bonusTypeID));
                int amount = _.GetItemPropertyCostTableValue(ip);
                string bonusName = _.GetStringByStrRef(tlkID) + " " + amount;
                float random = RandomService.RandomFloat() * 100.0f;
                float modifiedEquipmentBonus = equipmentBonus * 0.25f;

                if (random <= chance + modifiedEquipmentBonus)
                {
                    foreach (var item in itemSet)
                    {
                        // If the target item is a component itself, we want to add the component bonuses instead of the
                        // actual item property bonuses.
                        // In other words, we want the custom item property "Component Bonus: AC UP" instead of the "AC Bonus" item property.
                        var componentIP = item.ItemProperties.FirstOrDefault(x => _.GetItemPropertyType(x) == ItemPropertyType.ComponentType);
                        if (componentIP == null)
                            ComponentBonusService.ApplyComponentBonus(item, ip);
                        else
                            BiowareXP2.IPSafeAddItemProperty(item, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);

                    }
                    player.SendMessage(ColorTokenService.Green("Successfully applied component property: " + bonusName));

                    ComponentBonusType bonusType = (ComponentBonusType)_.GetItemPropertySubType(ip);
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


        private float CalculateEquipmentBonus(NWPlayer player, Skill skill)
        {
            var effectiveStats = PlayerStatService.GetPlayerItemEffectiveStats(player);
            int equipmentBonus = 0;
            float multiplier = 0.5f;
            int atmosphere = CraftService.CalculateAreaAtmosphereBonus(player.Area);

            if (atmosphere >= 75)
            {
                multiplier = 0.7f;
            }
            else if (atmosphere >= 25)
            {
                multiplier = 0.6f;
            }

            switch (skill)
            {
                case Skill.Armorsmith: equipmentBonus = effectiveStats.Armorsmith; break;
                case Skill.Weaponsmith: equipmentBonus = effectiveStats.Weaponsmith; break;
                case Skill.Cooking: equipmentBonus = effectiveStats.Cooking; break;
                case Skill.Engineering: equipmentBonus = effectiveStats.Engineering; break;
                case Skill.Fabrication: equipmentBonus = effectiveStats.Fabrication; break;
                case Skill.Medicine: equipmentBonus = effectiveStats.Medicine; break;
            }

            return equipmentBonus * multiplier; // +0.5%, +0.6%, or +0.7% per equipment bonus
        }

        private float CalculateBaseChanceToAddProperty(int pcLevel, int blueprintLevel, int atmosphereBonus)
        {
            int delta = pcLevel - blueprintLevel;
            float percentage = 0.0f;

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
