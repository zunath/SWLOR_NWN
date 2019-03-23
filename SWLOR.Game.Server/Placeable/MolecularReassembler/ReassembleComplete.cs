using NWN;
using SWLOR.Game.Server.Bioware;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using ComponentType = SWLOR.Game.Server.Data.Entity.ComponentType;

namespace SWLOR.Game.Server.Placeable.MolecularReassembler
{
    public class ReassembleComplete: IRegisteredEvent
    {
        
        private readonly ISerializationService _serialization;
        
        
        private readonly ICraftService _craft;
        
        private readonly IColorTokenService _color;
        private readonly IPerkService _perk;
        private readonly IPlayerStatService _playerStat;
        private readonly ISkillService _skill;

        public ReassembleComplete(
            
            ISerializationService serialization,
             
            ICraftService craft,
            
            IColorTokenService color,
            IPerkService perk,
            IPlayerStatService playerStat,
            ISkillService skill)
        {
            _serialization = serialization;
            
            _craft = craft;
            
            _color = color;
            _perk = perk;
            _playerStat = playerStat;
            _skill = skill;
        }

        private ComponentType _componentType;
        private NWPlayer _player;
        private EffectiveItemStats _playerItemStats;

        // New component items will be spawned and the appropriate properties from the base item will be transferred.
        public bool Run(params object[] args)
        {
            _player = (NWPlayer) args[0];
            _playerItemStats = _playerStat.GetPlayerItemEffectiveStats(_player);
            int xp = 100; // Always grant at least this much XP to player.

            // Remove the immobilization effect
            foreach (var effect in _player.Effects)
            {
                if (_.GetEffectTag(effect) == "CRAFTING_IMMOBILIZATION")
                {
                    _.RemoveEffect(_player, effect);
                }
            }

            string serializedSalvageItem = (string)args[1];
            NWPlaceable tempStorage = _.GetObjectByTag("TEMP_ITEM_STORAGE");
            NWItem item = _serialization.DeserializeItem(serializedSalvageItem, tempStorage);
            int salvageComponentTypeID = (int) args[2];
            _componentType = DataService.Get<ComponentType>(salvageComponentTypeID);
            
            // Create an item with no bonuses every time.
            _.CreateItemOnObject(_componentType.ReassembledResref, _player);

            // First check is for attack bonuses
            foreach (var prop in item.ItemProperties)
            {
                int propTypeID = _.GetItemPropertyType(prop);
                if (propTypeID == _.ITEM_PROPERTY_ATTACK_BONUS)
                {
                    // Get the amount of Attack Bonus
                    int amount = _.GetItemPropertyCostTableValue(prop);

                    xp += ProcessProperty(amount, 3, ComponentBonusType.AttackBonusUp);
                }
            }

            // Now check specific custom properties which are stored as local variables on the item.
            xp += ProcessProperty(item.CustomAC, 3, ComponentBonusType.ACUp);
            xp += ProcessProperty(item.HarvestingBonus, 3, ComponentBonusType.HarvestingUp);
            xp += ProcessProperty(item.PilotingBonus, 3, ComponentBonusType.PilotingUp);
            xp += ProcessProperty(item.ScanningBonus, 3, ComponentBonusType.ScanningUp);
            xp += ProcessProperty(item.ScavengingBonus, 3, ComponentBonusType.ScavengingUp);
            xp += ProcessProperty(item.CastingSpeed, 3, ComponentBonusType.CastingSpeedUp);
            xp += ProcessProperty(item.CraftBonusArmorsmith, 3, ComponentBonusType.ArmorsmithUp);
            xp += ProcessProperty(item.CraftBonusWeaponsmith, 3, ComponentBonusType.WeaponsmithUp);
            xp += ProcessProperty(item.CraftBonusCooking, 3, ComponentBonusType.CookingUp);
            xp += ProcessProperty(item.CraftBonusEngineering, 3, ComponentBonusType.EngineeringUp);
            xp += ProcessProperty(item.CraftBonusFabrication, 3, ComponentBonusType.FabricationUp);
            xp += ProcessProperty(item.HPBonus, 5, ComponentBonusType.HPUp, 0.5f);
            xp += ProcessProperty(item.FPBonus, 5, ComponentBonusType.FPUp, 0.5f);
            xp += ProcessProperty(item.EnmityRate, 3, ComponentBonusType.EnmityUp);
            xp += ProcessProperty(item.ForcePotencyBonus, 3, ComponentBonusType.ForcePotencyUp);
            xp += ProcessProperty(item.ForceAccuracyBonus, 3, ComponentBonusType.ForceAccuracyUp);
            xp += ProcessProperty(item.ForceDefenseBonus, 3, ComponentBonusType.ForceDefenseUp);
            xp += ProcessProperty(item.ElectricalPotencyBonus, 3, ComponentBonusType.ElectricalPotencyUp);
            xp += ProcessProperty(item.MindPotencyBonus, 3, ComponentBonusType.MindPotencyUp);
            xp += ProcessProperty(item.LightPotencyBonus, 3, ComponentBonusType.LightPotencyUp);
            xp += ProcessProperty(item.DarkPotencyBonus, 3, ComponentBonusType.DarkPotencyUp);
            xp += ProcessProperty(item.ElectricalDefenseBonus, 3, ComponentBonusType.ElectricalDefenseUp);
            xp += ProcessProperty(item.MindDefenseBonus, 3, ComponentBonusType.MindDefenseUp);
            xp += ProcessProperty(item.LightDefenseBonus, 3, ComponentBonusType.LightDefenseUp);
            xp += ProcessProperty(item.DarkDefenseBonus, 3, ComponentBonusType.DarkDefenseUp);

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

            _skill.GiveSkillXP(_player, SkillType.Harvesting, xp);
            return true;
        }

        private int ProcessProperty(int amount, int maxBonuses, ComponentBonusType bonus, float levelsPerBonus = 1.0f)
        {
            string resref = _componentType.ReassembledResref;
            int penalty = 0;
            int luck = _perk.GetPCPerkLevel(_player, PerkType.Lucky) + (_playerItemStats.Luck / 3);
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
                int chanceToTransfer = _craft.CalculateReassemblyChance(_player, penalty);
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
                        _player.SendMessage(_color.Red("You failed to create a component. (+" + maxBonuses + ")"));
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
                        _player.SendMessage(_color.Red("You failed to create a component. (+" + amount + ")"));
                        xp += (50 + RandomService.Random(0, 5));
                    }
                    break;
                }
            }

            return xp;
        }
    }
}
