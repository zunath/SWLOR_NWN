using System;
using NWN;
using SWLOR.Game.Server.Bioware;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;
using ComponentType = SWLOR.Game.Server.Data.Entity.ComponentType;

namespace SWLOR.Game.Server.Scripts.Placeable.MolecularReassembler
{
    public class ReassembleComplete: IScript
    {
        private ComponentType _componentType;
        private NWPlayer _player;
        private EffectiveItemStats _playerItemStats;
        private Guid _eventID;

        // New component items will be spawned and the appropriate properties from the base item will be transferred.
        public void SubscribeEvents()
        {
            _eventID = MessageHub.Instance.Subscribe<OnReassembleComplete>(OnReassembleComplete);
        }

        private void OnReassembleComplete(OnReassembleComplete data)
        {
            _player = data.Player;
            int xp = 100; // Always grant at least this much XP to player.

            // Remove the immobilization effect
            foreach (var effect in _player.Effects)
            {
                if (_.GetEffectTag(effect) == "CRAFTING_IMMOBILIZATION")
                {
                    _.RemoveEffect(_player, effect);
                }
            }

            // Check for a fuel cell in the player's inventory again. If it doesn't exist, we exit early with an error message.
            NWItem fuel = _.GetItemPossessedBy(_player, "ass_power");
            if (!fuel.IsValid)
            {
                _player.SendMessage(ColorTokenService.Red("A 'Reassembly Fuel Cell' was not found in your inventory. Reassembly failed."));
                return;
            }

            // Otherwise the fuel cell was found. Destroy it and continue on with the process.
            fuel.Destroy();

            _playerItemStats = PlayerStatService.GetPlayerItemEffectiveStats(_player);
            string serializedSalvageItem = data.SerializedSalvageItem;
            NWPlaceable tempStorage = _.GetObjectByTag("TEMP_ITEM_STORAGE");
            NWItem item = SerializationService.DeserializeItem(serializedSalvageItem, tempStorage);
            int salvageComponentTypeID = data.SalvageComponentTypeID;
            _componentType = DataService.ComponentType.GetByID(salvageComponentTypeID);

            // Create an item with no bonuses every time.
            _.CreateItemOnObject(_componentType.ReassembledResref, _player);

            // Now check specific custom properties which are stored as local variables on the item.
            xp += ProcessProperty(item.HarvestingBonus, 3, ComponentBonusType.HarvestingUp);
            xp += ProcessProperty(item.PilotingBonus, 3, ComponentBonusType.PilotingUp);
            xp += ProcessProperty(item.ScanningBonus, 3, ComponentBonusType.ScanningUp);
            xp += ProcessProperty(item.ScavengingBonus, 3, ComponentBonusType.ScavengingUp);
            xp += ProcessProperty(item.CooldownRecovery, 3, ComponentBonusType.CooldownRecoveryUp);
            xp += ProcessProperty(item.CraftBonusArmorsmith, 3, ComponentBonusType.ArmorsmithUp);
            xp += ProcessProperty(item.CraftBonusWeaponsmith, 3, ComponentBonusType.WeaponsmithUp);
            xp += ProcessProperty(item.CraftBonusCooking, 3, ComponentBonusType.CookingUp);
            xp += ProcessProperty(item.CraftBonusEngineering, 3, ComponentBonusType.EngineeringUp);
            xp += ProcessProperty(item.CraftBonusFabrication, 3, ComponentBonusType.FabricationUp);
            xp += ProcessProperty(item.HPBonus, 5, ComponentBonusType.HPUp, 0.5f);
            xp += ProcessProperty(item.FPBonus, 5, ComponentBonusType.FPUp, 0.5f);
            xp += ProcessProperty(item.EnmityRate, 3, ComponentBonusType.EnmityUp);

            xp += ProcessProperty(item.LuckBonus, 3, ComponentBonusType.LuckUp);
            xp += ProcessProperty(item.MeditateBonus, 3, ComponentBonusType.MeditateUp);
            xp += ProcessProperty(item.RestBonus, 3, ComponentBonusType.RestUp);
            xp += ProcessProperty(item.MedicineBonus, 3, ComponentBonusType.MedicineUp);
            xp += ProcessProperty(item.HPRegenBonus, 3, ComponentBonusType.HPRegenUp);
            xp += ProcessProperty(item.FPRegenBonus, 3, ComponentBonusType.FPRegenUp);
            xp += ProcessProperty(item.BaseAttackBonus, 3, ComponentBonusType.BaseAttackBonusUp, 6f);
            xp += ProcessProperty(item.StructureBonus, 3, ComponentBonusType.StructureBonusUp);
            xp += ProcessProperty(item.SneakAttackBonus, 3, ComponentBonusType.SneakAttackUp);
            xp += ProcessProperty(item.DamageBonus, 3, ComponentBonusType.DamageUp);
            xp += ProcessProperty(item.StrengthBonus, 3, ComponentBonusType.StrengthUp);
            xp += ProcessProperty(item.DexterityBonus, 3, ComponentBonusType.DexterityUp);
            xp += ProcessProperty(item.ConstitutionBonus, 3, ComponentBonusType.ConstitutionUp);
            xp += ProcessProperty(item.WisdomBonus, 3, ComponentBonusType.WisdomUp);
            xp += ProcessProperty(item.IntelligenceBonus, 3, ComponentBonusType.IntelligenceUp);
            xp += ProcessProperty(item.CharismaBonus, 3, ComponentBonusType.CharismaUp);
            xp += ProcessProperty(item.DurationBonus, 3, ComponentBonusType.DurationUp);

            item.Destroy();

            SkillService.GiveSkillXP(_player, SkillType.Harvesting, xp);

        }

        public void UnsubscribeEvents()
        {
            MessageHub.Instance.Unsubscribe(_eventID);
        }

        public void Main()
        {
        }

        private int ProcessProperty(int amount, int maxBonuses, ComponentBonusType bonus, float levelsPerBonus = 1.0f)
        {
            string resref = _componentType.ReassembledResref;
            int penalty = 0;
            int luck = PerkService.GetCreaturePerkLevel(_player, PerkType.Lucky) + (_playerItemStats.Luck / 3);
            int xp = 0;

            ItemPropertyUnpacked bonusIP = new ItemPropertyUnpacked
            {
                Property = (int)CustomItemPropertyType.ComponentBonus,
                SubType = (int)bonus,
                CostTable = 62,
                CostTableValue = 0,
                Param1 = 255,
                Param1Value = 0,
                UsesPerDay = 255,
                ChanceToAppear = 100,
                IsUseable = true,
                SpellID = -1
            };

            while (amount > 0)
            {
                int chanceToTransfer = CraftService.CalculateReassemblyChance(_player, penalty);
                // Roll to see if the item can be created.
                bool success = RandomService.Random(0, 100) <= chanceToTransfer;

                // Do a lucky roll if we failed the first time.
                if (!success && luck > 0 && RandomService.Random(0, 100) <= luck)
                {
                    _player.SendMessage("Lucky reassemble!");
                    success = true;
                }

                if (amount >= maxBonuses)
                {
                    if (success)
                    {
                        int levelIncrease = (int)(maxBonuses * levelsPerBonus);
                        // Roll succeeded. Create item.
                        bonusIP.CostTableValue = maxBonuses;
                        ItemProperty bonusIPPacked = NWNXItemProperty.PackIP(bonusIP);
                        NWItem item = _.CreateItemOnObject(resref, _player);
                        item.RecommendedLevel = levelIncrease;
                        BiowareXP2.IPSafeAddItemProperty(item, bonusIPPacked, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, false);

                        xp += (150 * maxBonuses + RandomService.Random(0, 5));
                    }
                    else
                    {
                        _player.SendMessage(ColorTokenService.Red("You failed to create a component. (+" + maxBonuses + ")"));
                        xp += (50 + RandomService.Random(0, 5));
                    }
                    // Penalty to chance increases regardless if item was created or not.
                    penalty += (maxBonuses * 5);
                    amount -= maxBonuses;
                }
                else
                {
                    if (success)
                    {
                        int levelIncrease = (int)(amount * levelsPerBonus);
                        bonusIP.CostTableValue = amount;
                        ItemProperty bonusIPPacked = NWNXItemProperty.PackIP(bonusIP);
                        NWItem item = _.CreateItemOnObject(resref, _player);
                        item.RecommendedLevel = levelIncrease;
                        BiowareXP2.IPSafeAddItemProperty(item, bonusIPPacked, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, false);

                        xp += (150 * amount + RandomService.Random(0, 5));
                    }
                    else
                    {
                        _player.SendMessage(ColorTokenService.Red("You failed to create a component. (+" + amount + ")"));
                        xp += (50 + RandomService.Random(0, 5));
                    }
                    break;
                }
            }

            return xp;
        }
    }
}
