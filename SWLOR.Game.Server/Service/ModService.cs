using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Mod.Contracts;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service
{
    public static class ModService
    {
        private static readonly Dictionary<int, IModHandler> _modHandlers;

        static ModService()
        {
            _modHandlers = new Dictionary<int, IModHandler>();
        }

        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleApplyDamage>(message => OnModuleApplyDamage());
            MessageHub.Instance.Subscribe<OnModuleLoad>(message => OnModuleLoad());
        }

        private static void OnModuleLoad()
        {
            RegisterModHandlers();
        }

        private static void RegisterModHandlers()
        {
            // Use reflection to get all of IModHandler implementations.
            var classes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(IModHandler).IsAssignableFrom(p) && p.IsClass && !p.IsAbstract).ToArray();
            foreach (var type in classes)
            {
                IModHandler instance = Activator.CreateInstance(type) as IModHandler;
                if (instance == null)
                {
                    throw new NullReferenceException("Unable to activate instance of type: " + type);
                }
                _modHandlers.Add(instance.ModTypeID, instance);
            }
        }

        public static bool IsModHandlerRegistered(int modTypeID)
        {
            return _modHandlers.ContainsKey(modTypeID);
        }

        public static IModHandler GetModHandler(int modTypeID)
        {
            if (!_modHandlers.ContainsKey(modTypeID))
            {
                throw new KeyNotFoundException("Mod type ID " + modTypeID + " is not registered. Did you add a script for it?");
            }

            return _modHandlers[modTypeID];
        }

        public static CustomItemPropertyType GetModType(NWItem item)
        {
            CustomItemPropertyType ipType = CustomItemPropertyType.Unknown;
            foreach (var ip in item.ItemProperties)
            {
                int type = _.GetItemPropertyType(ip);
                if (type == (int)CustomItemPropertyType.RedMod ||
                    type == (int)CustomItemPropertyType.BlueMod ||
                    type == (int)CustomItemPropertyType.GreenMod ||
                    type == (int)CustomItemPropertyType.YellowMod)
                {
                    ipType = (CustomItemPropertyType)type;
                    break;
                }
            }

            return ipType;
        }

        public static ModSlots GetModSlots(NWItem item)
        {
            ModSlots modSlots = new ModSlots();
            foreach (var ip in item.ItemProperties)
            {
                int type = _.GetItemPropertyType(ip);
                switch (type)
                {
                    case (int)CustomItemPropertyType.ModSlotRed:
                        modSlots.RedSlots++;
                        break;
                    case (int)CustomItemPropertyType.ModSlotBlue:
                        modSlots.BlueSlots++;
                        break;
                    case (int)CustomItemPropertyType.ModSlotGreen:
                        modSlots.GreenSlots++;
                        break;
                    case (int)CustomItemPropertyType.ModSlotYellow:
                        modSlots.YellowSlots++;
                        break;
                    case (int)CustomItemPropertyType.ModSlotPrismatic:
                        modSlots.PrismaticSlots++;
                        break;
                }
            }

            for (int red = 1; red <= modSlots.RedSlots; red++)
            {
                int modID = item.GetLocalInt("MOD_SLOT_RED_" + red);
                if (modID > 0)
                    modSlots.FilledRedSlots++;
            }
            for (int blue = 1; blue <= modSlots.BlueSlots; blue++)
            {
                int modID = item.GetLocalInt("MOD_SLOT_BLUE_" + blue);
                if (modID > 0)
                    modSlots.FilledBlueSlots++;
            }
            for (int green = 1; green <= modSlots.GreenSlots; green++)
            {
                int modID = item.GetLocalInt("MOD_SLOT_GREEN_" + green);
                if (modID > 0)
                    modSlots.FilledGreenSlots++;
            }
            for (int yellow = 1; yellow <= modSlots.YellowSlots; yellow++)
            {
                int modID = item.GetLocalInt("MOD_SLOT_YELLOW_" + yellow);
                if (modID > 0)
                    modSlots.FilledYellowSlots++;
            }
            for (int prismatic = 1; prismatic <= modSlots.PrismaticSlots; prismatic++)
            {
                int modID = item.GetLocalInt("MOD_SLOT_PRISMATIC_" + prismatic);
                if (modID > 0)
                    modSlots.FilledPrismaticSlots++;
            }

            return modSlots;
        }

        public static bool IsRune(NWItem item)
        {
            return GetModType(item) != CustomItemPropertyType.Unknown;
        }

        public static string PrismaticString()
        {
            return ColorTokenService.Red("p") + ColorTokenService.Orange("r") + ColorTokenService.Yellow("i") + ColorTokenService.Green("s") + ColorTokenService.Blue("m") +
                                   ColorTokenService.LightPurple("a") + ColorTokenService.Purple("t") + ColorTokenService.White("i") + ColorTokenService.Black("c");
        }

        public static string OnModuleExamine(string existingDescription, NWPlayer examiner, NWObject examinedObject)
        {
            if (examinedObject.ObjectType != _.OBJECT_TYPE_ITEM) return existingDescription;
            NWItem examinedItem = (examinedObject.Object);
            string description = string.Empty;
            ModSlots slot = GetModSlots(examinedItem);
            
            for (int red = 1; red <= slot.FilledRedSlots; red++)
            {
                description += ColorTokenService.Red("Red Slot #" + red + ": ") + examinedItem.GetLocalString("MOD_SLOT_RED_DESC_" + red) + "\n";
            }
            for (int blue = 1; blue <= slot.FilledBlueSlots; blue++)
            {
                description += ColorTokenService.Red("Blue Slot #" + blue + ": ") + examinedItem.GetLocalString("MOD_SLOT_BLUE_DESC_" + blue) + "\n";
            }
            for (int green = 1; green <= slot.FilledGreenSlots; green++)
            {
                description += ColorTokenService.Red("Green Slot #" + green + ": ") + examinedItem.GetLocalString("MOD_SLOT_GREEN_DESC_" + green) + "\n";
            }
            for (int yellow = 1; yellow <= slot.FilledYellowSlots; yellow++)
            {
                description += ColorTokenService.Red("Yellow Slot #" + yellow + ": ") + examinedItem.GetLocalString("MOD_SLOT_YELLOW_DESC_" + yellow) + "\n";
            }
            for (int prismatic = 1; prismatic <= slot.FilledPrismaticSlots; prismatic++)
            {
                description += PrismaticString() + " Slot #" + prismatic + ": " + examinedItem.GetLocalString("MOD_SLOT_PRISMATIC_DESC_" + prismatic) + "\n";
            }
            
            return existingDescription + "\n" + description;
        }

        private static void OnModuleApplyDamage()
        {
            var data = NWNXDamage.GetDamageEventData();
            if (data.Base <= 0) return;

            NWObject damager = data.Damager;
            if (!damager.IsPlayer) return;
            NWCreature target = NWGameObject.OBJECT_SELF;

            // Check that this was a normal attack, and not (say) a damage over time effect.
            if (target.GetLocalInt(AbilityService.LAST_ATTACK + damager.GlobalID) != AbilityService.ATTACK_PHYSICAL) return;

            NWItem weapon = (_.GetLastWeaponUsed(damager.Object));
            int damageBonus = weapon.DamageBonus;

            NWPlayer player = (damager.Object);
            int itemLevel = weapon.RecommendedLevel;
            SkillType skill = ItemService.GetSkillTypeForItem(weapon);
            if (skill == SkillType.Unknown) return;

            int rank = SkillService.GetPCSkillRank(player, skill);
            int delta = itemLevel - rank;
            if (delta >= 1) damageBonus--;
            damageBonus = damageBonus - delta / 5;

            if (damageBonus <= 0) damageBonus = 0;
            
            data.Base += damageBonus;
            NWNXDamage.SetDamageEventData(data);
        }
    }
}
