using System;
using NWN;
using SWLOR.Game.Server.Bioware.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;
using ComponentType = SWLOR.Game.Server.Data.Entity.ComponentType;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.AtomicReassembler
{
    public class ReassembleComplete: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly ISerializationService _serialization;
        private readonly IDataService _data;
        private readonly INWNXItemProperty _nwnxItemProperty;
        private readonly IBiowareXP2 _biowareXP2;

        public ReassembleComplete(
            INWScript script,
            ISerializationService serialization,
            IDataService data, 
            INWNXItemProperty nwnxItemProperty,
            IBiowareXP2 biowareXP2)
        {
            _ = script;
            _serialization = serialization;
            _data = data;
            _nwnxItemProperty = nwnxItemProperty;
            _biowareXP2 = biowareXP2;
        }

        private ComponentType _componentType;
        private NWPlayer _player;

        // New component items will be spawned and the appropriate properties from the base item will be transferred.
        public bool Run(params object[] args)
        {
            _player = (NWPlayer) args[0];

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
            _componentType = _data.Get<ComponentType>(salvageComponentTypeID);

            // First check is for attack bonuses
            foreach (var prop in item.ItemProperties)
            {
                int propTypeID = _.GetItemPropertyType(prop);
                if (propTypeID == ITEM_PROPERTY_ATTACK_BONUS)
                {
                    // Get the amount of Attack Bonus
                    int amount = _.GetItemPropertyCostTableValue(prop);

                    ProcessProperty(amount, 3, ComponentBonusType.AttackBonusUp);
                }
            }

            // Now check specific custom properties which are stored as local variables on the item.
            ProcessProperty(item.CustomAC, 3, ComponentBonusType.ACUp);
            ProcessProperty(item.HarvestingBonus, 3, ComponentBonusType.HarvestingUp);
            ProcessProperty(item.PilotingBonus, 3, ComponentBonusType.PilotingUp);
            ProcessProperty(item.ScanningBonus, 3, ComponentBonusType.ScanningUp);
            ProcessProperty(item.ScavengingBonus, 3, ComponentBonusType.ScavengingUp);
            ProcessProperty(item.CastingSpeed, 3, ComponentBonusType.CastingSpeedUp);
            ProcessProperty(item.CraftBonusArmorsmith, 3, ComponentBonusType.ArmorsmithUp);
            ProcessProperty(item.CraftBonusWeaponsmith, 3, ComponentBonusType.WeaponsmithUp);
            ProcessProperty(item.CraftBonusCooking, 3, ComponentBonusType.CookingUp);
            ProcessProperty(item.CraftBonusEngineering, 3, ComponentBonusType.EngineeringUp);
            ProcessProperty(item.CraftBonusFabrication, 3, ComponentBonusType.FabricationUp);
            ProcessProperty(item.HPBonus, 5, ComponentBonusType.HPUp, 0.5f);
            ProcessProperty(item.FPBonus, 5, ComponentBonusType.FPUp, 0.5f);
            ProcessProperty(item.EnmityRate, 3, ComponentBonusType.EnmityUp);
            ProcessProperty(item.ForcePotencyBonus, 3, ComponentBonusType.ForcePotencyUp);
            ProcessProperty(item.ForceAccuracyBonus, 3, ComponentBonusType.ForceAccuracyUp);
            ProcessProperty(item.ForceDefenseBonus, 3, ComponentBonusType.ForceDefenseUp);
            ProcessProperty(item.ElectricalPotencyBonus, 3, ComponentBonusType.ElectricalPotencyUp);
            ProcessProperty(item.MindPotencyBonus, 3, ComponentBonusType.MindPotencyUp);
            ProcessProperty(item.LightPotencyBonus, 3, ComponentBonusType.LightPotencyUp);
            ProcessProperty(item.DarkPotencyBonus, 3, ComponentBonusType.DarkPotencyUp);
            ProcessProperty(item.ElectricalDefenseBonus, 3, ComponentBonusType.ElectricalDefenseUp);
            ProcessProperty(item.MindDefenseBonus, 3, ComponentBonusType.MindDefenseUp);
            ProcessProperty(item.LightDefenseBonus, 3, ComponentBonusType.LightDefenseUp);
            ProcessProperty(item.DarkDefenseBonus, 3, ComponentBonusType.DarkDefenseUp);

            ProcessProperty(item.LuckBonus, 3, ComponentBonusType.LuckUp);
            ProcessProperty(item.MeditateBonus, 3, ComponentBonusType.MeditateUp);
            ProcessProperty(item.RestBonus, 3, ComponentBonusType.RestUp);
            ProcessProperty(item.MedicineBonus, 3, ComponentBonusType.MedicineUp);
            ProcessProperty(item.HPRegenBonus, 3, ComponentBonusType.HPRegenUp);
            ProcessProperty(item.FPRegenBonus, 3, ComponentBonusType.FPRegenUp);
            ProcessProperty(item.BaseAttackBonus, 3, ComponentBonusType.BaseAttackBonusUp);
            ProcessProperty(item.StructureBonus, 3, ComponentBonusType.StructureBonusUp);
            ProcessProperty(item.SneakAttackBonus, 3, ComponentBonusType.SneakAttackUp);
            ProcessProperty(item.DamageBonus, 3, ComponentBonusType.DamageUp);
            ProcessProperty(item.StrengthBonus, 3, ComponentBonusType.StrengthUp);
            ProcessProperty(item.DexterityBonus, 3, ComponentBonusType.DexterityUp);
            ProcessProperty(item.ConstitutionBonus, 3, ComponentBonusType.ConstitutionUp);
            ProcessProperty(item.WisdomBonus, 3, ComponentBonusType.WisdomUp);
            ProcessProperty(item.IntelligenceBonus, 3, ComponentBonusType.IntelligenceUp);
            ProcessProperty(item.CharismaBonus, 3, ComponentBonusType.CharismaUp);
            ProcessProperty(item.DurationBonus, 3, ComponentBonusType.DurationUp);

            item.Destroy();

            return true;
        }

        private void ProcessProperty(int amount, int maxBonuses, ComponentBonusType bonus, float levelsPerBonus = 1.0f)
        {
            string resref = _componentType.ReassembledResref;

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
                if (amount >= maxBonuses)
                {
                    int levelIncrease = (int)(maxBonuses / levelsPerBonus);
                    bonusIP.CostTableValue = maxBonuses;
                    ItemProperty bonusIPPacked = _nwnxItemProperty.NWNX_ItemProperty_PackIP(bonusIP);
                    NWItem item = _.CreateItemOnObject(resref, _player);
                    item.RecommendedLevel = levelIncrease;
                    _biowareXP2.IPSafeAddItemProperty(item, bonusIPPacked, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, false);

                    amount -= maxBonuses;
                }
                else
                {
                    int levelIncrease = (int)(amount / levelsPerBonus);
                    bonusIP.CostTableValue = amount;
                    ItemProperty bonusIPPacked = _nwnxItemProperty.NWNX_ItemProperty_PackIP(bonusIP);
                    NWItem item = _.CreateItemOnObject(resref, _player);
                    item.RecommendedLevel = levelIncrease;
                    _biowareXP2.IPSafeAddItemProperty(item, bonusIPPacked, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, false);

                    break;
                }
            }

        }
    }
}
