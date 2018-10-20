using System;
using System.Linq;
using SWLOR.Game.Server.Bioware.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;

using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Event.Delayed;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Service
{
    public class ItemService : IItemService
    {
        private readonly INWScript _;
        private readonly IBiowareXP2 _xp2;
        private readonly IColorTokenService _color;
        private readonly INWNXPlayer _nwnxPlayer;
        private readonly IDataContext _db;

        public ItemService(
            INWScript script,
            IBiowareXP2 xp2,
            IColorTokenService color,
            INWNXPlayer nwnxPlayer,
            IDataContext db)
        {
            _ = script;
            _xp2 = xp2;
            _color = color;
            _nwnxPlayer = nwnxPlayer;
            _db = db;
        }

        public string GetNameByResref(string resref)
        {
            NWPlaceable tempStorage = (_.GetObjectByTag("TEMP_ITEM_STORAGE"));
            if (!tempStorage.IsValid)
            {
                Console.WriteLine("Could not locate temp item storage object. Create a placeable container in a non-accessible area with the tag TEMP_ITEM_STORAGE.");
                return null;
            }
            NWItem item = (_.CreateItemOnObject(resref, tempStorage.Object));
            string name = item.Name;
            item.Destroy();
            return name;
        }

        public CustomItemType GetCustomItemTypeByResref(string resref)
        {
            NWPlaceable tempStorage = (_.GetObjectByTag("TEMP_ITEM_STORAGE"));
            if (!tempStorage.IsValid)
            {
                Console.WriteLine("Could not locate temp item storage object. Create a placeable container in a non-accessible area with the tag TEMP_ITEM_STORAGE.");
                return CustomItemType.None;
            }
            NWItem item = (_.CreateItemOnObject(resref, tempStorage.Object));
            var itemType = item.CustomItemType;
            item.Destroy();
            return itemType;
        }
        
        public void OnModuleActivatedItem()
        {
            NWPlayer user = (_.GetItemActivator());
            NWItem oItem = (_.GetItemActivated());
            NWObject target = (_.GetItemActivatedTarget());
            Location targetLocation = _.GetItemActivatedTargetLocation();
            
            string className = oItem.GetLocalString("JAVA_SCRIPT");
            if (string.IsNullOrWhiteSpace(className)) className = oItem.GetLocalString("ACTIVATE_JAVA_SCRIPT");
            if (string.IsNullOrWhiteSpace(className)) className = oItem.GetLocalString("JAVA_ACTION_SCRIPT");
            if (string.IsNullOrWhiteSpace(className)) className = oItem.GetLocalString("SCRIPT");
            if (string.IsNullOrWhiteSpace(className)) return;

            user.ClearAllActions();

            if (user.IsBusy)
            {
                user.SendMessage("You are busy.");
                return;
            }

            // Remove "Item." prefix if it exists.
            if (className.StartsWith("Item."))
                className = className.Substring(5);

            App.ResolveByInterface<IActionItem>("Item." + className, (item) =>
            {
                string invalidTargetMessage = item.IsValidTarget(user, oItem, target, targetLocation);
                if (!string.IsNullOrWhiteSpace(invalidTargetMessage))
                {
                    user.SendMessage(invalidTargetMessage);
                    return;
                }

                float maxDistance = item.MaxDistance(user, oItem, target, targetLocation);
                if (maxDistance > 0.0f)
                {
                    if (target.IsValid &&
                        (_.GetDistanceBetween(user.Object, target.Object) > maxDistance ||
                        user.Area.Resref != target.Area.Resref))
                    {
                        user.SendMessage("Your target is too far away.");
                        return;
                    }
                    else if (!target.IsValid &&
                             (_.GetDistanceBetweenLocations(user.Location, targetLocation) > maxDistance ||
                             user.Area.Resref != ((NWArea)_.GetAreaFromLocation(targetLocation)).Resref))
                    {
                        user.SendMessage("That location is too far away.");
                        return;
                    }
                }

                CustomData customData = item.StartUseItem(user, oItem, target, targetLocation);
                float delay = item.Seconds(user, oItem, target, targetLocation, customData);
                int animationID = item.AnimationID();
                bool faceTarget = item.FaceTarget();
                Vector userPosition = user.Position;

                user.AssignCommand(() =>
                {
                    user.IsBusy = true;
                    if (faceTarget)
                        _.SetFacingPoint(!target.IsValid ? _.GetPositionFromLocation(targetLocation) : target.Position);
                    if (animationID > 0)
                        _.ActionPlayAnimation(animationID, 1.0f, delay);
                });

                _nwnxPlayer.StartGuiTimingBar(user, delay, string.Empty);
                user.DelayEvent<FinishActionItem>(
                    delay,
                    className,
                    user,
                    oItem,
                    target,
                    targetLocation,
                    userPosition,
                    customData);
            });

        }

        public void OnModuleHeartbeat()
        {
            NWModule module = NWModule.Get();

            foreach (NWPlayer pc in module.Players)
            {
                NWItem offHand = pc.LeftHand;
                if (offHand.BaseItemType == BASE_ITEM_TORCH)
                {
                    int charges = offHand.ReduceCharges();
                    if (charges <= 0)
                    {
                        offHand.Destroy();
                        pc.SendMessage("Your torch has burned out.");
                    }
                }
            }
        }
        
        public string OnModuleExamine(string existingDescription, NWPlayer examiner, NWObject examinedObject)
        {
            if (!examiner.IsPlayer) return existingDescription;
            if (examinedObject.ObjectType != OBJECT_TYPE_ITEM) return existingDescription;

            NWItem examinedItem = (examinedObject.Object);
            string description = "";

            if (examinedItem.RecommendedLevel > 0)
            {
                description += _color.Orange("Recommended Level: ") + examinedItem.RecommendedLevel + "\n";
            }
            if (examinedItem.AssociatedSkillType > 0)
            {
                PCSkill pcSkill = _db.PCSkills.Single(x => x.PlayerID == examiner.GlobalID && x.SkillID == (int) examinedItem.AssociatedSkillType);
                description += _color.Orange("Associated Skill: ") + pcSkill.Skill.Name + "\n";
            }
            if (examinedItem.CustomAC > 0)
            {
                description += _color.Orange("AC: ") + examinedItem.CustomAC + "\n";
            }
            if (examinedItem.HPBonus > 0)
            {
                description += _color.Orange("HP Bonus: ") + examinedItem.HPBonus + "\n";
            }
            if (examinedItem.FPBonus > 0)
            {
                description += _color.Orange("FP Bonus: ") + examinedItem.FPBonus + "\n";
            }
            if (examinedItem.StrengthBonus > 0)
            {
                description += _color.Orange("Strength Bonus: ") + examinedItem.StrengthBonus + "\n";
            }
            if (examinedItem.DexterityBonus > 0)
            {
                description += _color.Orange("Dexterity Bonus: ") + examinedItem.DexterityBonus + "\n";
            }
            if (examinedItem.ConstitutionBonus > 0)
            {
                description += _color.Orange("Constitution Bonus: ") + examinedItem.ConstitutionBonus + "\n";
            }
            if (examinedItem.WisdomBonus > 0)
            {
                description += _color.Orange("Wisdom Bonus: ") + examinedItem.WisdomBonus + "\n";
            }
            if (examinedItem.IntelligenceBonus > 0)
            {
                description += _color.Orange("Intelligence Bonus: ") + examinedItem.IntelligenceBonus + "\n";
            }
            if (examinedItem.CharismaBonus > 0)
            {
                description += _color.Orange("Charisma Bonus: ") + examinedItem.CharismaBonus + "\n";
            }
            if (examinedItem.CastingSpeed > 0)
            {
                description += _color.Orange("Casting Speed: +") + examinedItem.CastingSpeed + "%\n";
            }
            else if (examinedItem.CastingSpeed < 0)
            {
                description += _color.Orange("Casting Penalty: -") + examinedItem.CastingSpeed + "%\n";
            }
            if (examinedItem.HarvestingBonus > 0)
            {
                description += _color.Orange("Harvesting Bonus: ") + examinedItem.HarvestingBonus + "\n";
            }
            if (examinedItem.CraftBonusArmorsmith > 0)
            {
                description += _color.Orange("Armorsmith Bonus: ") + examinedItem.CraftBonusArmorsmith + "\n";
            }
            if (examinedItem.CraftBonusFabrication > 0)
            {
                description += _color.Orange("Fabrication Bonus: ") + examinedItem.CraftBonusFabrication + "\n";
            }
            if (examinedItem.CraftBonusWeaponsmith > 0)
            {
                description += _color.Orange("Weaponsmith Bonus: ") + examinedItem.CraftBonusWeaponsmith + "\n";
            }
            if (examinedItem.CraftBonusCooking > 0)
            {
                description += _color.Orange("Cooking Bonus: ") + examinedItem.CraftBonusCooking + "\n";
            }
            if (examinedItem.CraftTierLevel > 0)
            {
                description += _color.Orange("Tool Level: ") + examinedItem.CraftTierLevel + "\n";
            }
            if (examinedItem.EnmityRate != 0)
            {
                description += _color.Orange("Enmity: ") + examinedItem.EnmityRate +  "%\n";
            }
            if (examinedItem.DarkAbilityBonus > 0)
            {
                description += _color.Orange("Dark Ability Bonus: ") + examinedItem.DarkAbilityBonus + "\n";
            }
            if (examinedItem.LightAbilityBonus > 0)
            {
                description += _color.Orange("Light Ability Bonus: ") + examinedItem.LightAbilityBonus + "\n";
            }
            if (examinedItem.SummoningBonus > 0)
            {
                description += _color.Orange("Summoning Bonus: ") + examinedItem.SummoningBonus + "\n";
            }
            if (examinedItem.LuckBonus > 0)
            {
                description += _color.Orange("Luck Bonus: ") + examinedItem.LuckBonus + "\n";
            }
            if (examinedItem.MeditateBonus > 0)
            {
                description += _color.Orange("Meditate Bonus: ") + examinedItem.MeditateBonus + "\n";
            }
            if (examinedItem.RestBonus > 0)
            {
                description += _color.Orange("Rest Bonus: ") + examinedItem.RestBonus + "\n";
            }
            if (examinedItem.MedicineBonus > 0)
            {
                description += _color.Orange("Medicine Bonus: ") + examinedItem.MedicineBonus + "\n";
            }
            if (examinedItem.HPRegenBonus > 0)
            {
                description += _color.Orange("HP Regen Bonus: ") + examinedItem.HPRegenBonus + "\n";
            }
            if (examinedItem.FPRegenBonus > 0)
            {
                description += _color.Orange("FP Regen Bonus: ") + examinedItem.FPRegenBonus + "\n";
            }
            if (examinedItem.BaseAttackBonus > 0)
            {
                description += _color.Orange("Base Attack Bonus: ") + examinedItem.BaseAttackBonus + "\n";
            }
            if (examinedItem.DamageBonus > 0)
            {
                description += _color.Orange("Damage Bonus: ") + examinedItem.DamageBonus + "\n";
            }

            return existingDescription + "\n" + description;
        }

        public int[] ArmorBaseItemTypes
        {
            get
            {
                int[] armorTypes =
                {
                    BASE_ITEM_AMULET,
                    BASE_ITEM_ARMOR,
                    BASE_ITEM_BELT,
                    BASE_ITEM_CLOAK,
                    BASE_ITEM_HELMET,
                    BASE_ITEM_LARGESHIELD,
                    BASE_ITEM_SMALLSHIELD,
                    BASE_ITEM_TOWERSHIELD
                };

                return armorTypes;
            }
        }

        public int[] WeaponBaseItemTypes
        {
            get
            {
                int[] weaponTypes =
                {
                    BASE_ITEM_ARROW,
                    BASE_ITEM_BASTARDSWORD,
                    BASE_ITEM_BATTLEAXE,
                    BASE_ITEM_BOLT,
                    BASE_ITEM_BRACER,
                    BASE_ITEM_BULLET,
                    BASE_ITEM_CLUB,
                    BASE_ITEM_DAGGER,
                    BASE_ITEM_DART,
                    BASE_ITEM_DIREMACE,
                    BASE_ITEM_DOUBLEAXE,
                    BASE_ITEM_DWARVENWARAXE,
                    BASE_ITEM_GLOVES,
                    BASE_ITEM_GREATAXE,
                    BASE_ITEM_GREATSWORD,
                    BASE_ITEM_GRENADE,
                    BASE_ITEM_HALBERD,
                    BASE_ITEM_HANDAXE,
                    BASE_ITEM_HEAVYCROSSBOW,
                    BASE_ITEM_HEAVYFLAIL,
                    BASE_ITEM_KAMA,
                    BASE_ITEM_KATANA,
                    BASE_ITEM_KUKRI,
                    BASE_ITEM_LIGHTCROSSBOW,
                    BASE_ITEM_LIGHTFLAIL,
                    BASE_ITEM_LIGHTHAMMER,
                    BASE_ITEM_LIGHTMACE,
                    BASE_ITEM_LONGBOW,
                    BASE_ITEM_LONGSWORD,
                    BASE_ITEM_MORNINGSTAR,
                    BASE_ITEM_QUARTERSTAFF,
                    BASE_ITEM_RAPIER,
                    BASE_ITEM_SCIMITAR,
                    BASE_ITEM_SCYTHE,
                    BASE_ITEM_SHORTBOW,
                    BASE_ITEM_SHORTSPEAR,
                    BASE_ITEM_SHORTSWORD,
                    BASE_ITEM_SHURIKEN,
                    BASE_ITEM_SICKLE,
                    BASE_ITEM_SLING,
                    BASE_ITEM_THROWINGAXE,
                    BASE_ITEM_TRIDENT,
                    BASE_ITEM_TWOBLADEDSWORD,
                    BASE_ITEM_WARHAMMER,
                    BASE_ITEM_WHIP,
                    CustomBaseItemType.Saberstaff, 
                    CustomBaseItemType.Lightsaber
                };

                return weaponTypes;
            }
        }

        public void OnModuleUnequipItem()
        {
            NWPlayer player = _.GetPCItemLastUnequippedBy();
            NWItem oItem = _.GetPCItemLastUnequipped();

            // Handle lightsaber sounds
            if (oItem.CustomItemType == CustomItemType.Lightsaber ||
                oItem.CustomItemType == CustomItemType.Saberstaff)
            {
                player.AssignCommand(() =>
                {
                    _.PlaySound("saberoff");
                });
            }

        }

        public void OnModuleEquipItem()
        {
            int[] validItemTypes = {
                BASE_ITEM_ARMOR,
                BASE_ITEM_ARROW,
                BASE_ITEM_BASTARDSWORD,
                BASE_ITEM_BATTLEAXE,
                BASE_ITEM_BELT,
                BASE_ITEM_BOLT,
                BASE_ITEM_BOOTS,
                BASE_ITEM_BRACER,
                BASE_ITEM_BULLET,
                BASE_ITEM_CLOAK,
                BASE_ITEM_CLUB,
                BASE_ITEM_DAGGER,
                BASE_ITEM_DART,
                BASE_ITEM_DIREMACE,
                BASE_ITEM_DOUBLEAXE,
                BASE_ITEM_DWARVENWARAXE,
                BASE_ITEM_GLOVES,
                BASE_ITEM_GREATAXE,
                BASE_ITEM_GREATSWORD,
                BASE_ITEM_GRENADE,
                BASE_ITEM_HALBERD,
                BASE_ITEM_HANDAXE,
                BASE_ITEM_HEAVYCROSSBOW,
                BASE_ITEM_HEAVYFLAIL,
                BASE_ITEM_HELMET,
                BASE_ITEM_KAMA,
                BASE_ITEM_KATANA,
                BASE_ITEM_KUKRI,
                BASE_ITEM_LARGESHIELD,
                BASE_ITEM_LIGHTCROSSBOW,
                BASE_ITEM_LIGHTFLAIL,
                BASE_ITEM_LIGHTHAMMER,
                BASE_ITEM_LIGHTMACE,
                BASE_ITEM_LONGBOW,
                BASE_ITEM_LONGSWORD,
                BASE_ITEM_MORNINGSTAR,
                BASE_ITEM_QUARTERSTAFF,
                BASE_ITEM_RAPIER,
                BASE_ITEM_SCIMITAR,
                BASE_ITEM_SCYTHE,
                BASE_ITEM_SHORTBOW,
                BASE_ITEM_SHORTSPEAR,
                BASE_ITEM_SHORTSWORD,
                BASE_ITEM_SHURIKEN,
                BASE_ITEM_SICKLE,
                BASE_ITEM_SLING,
                BASE_ITEM_SMALLSHIELD,
                BASE_ITEM_THROWINGAXE,
                BASE_ITEM_TOWERSHIELD,
                BASE_ITEM_TRIDENT,
                BASE_ITEM_TWOBLADEDSWORD,
                BASE_ITEM_WARHAMMER,
                BASE_ITEM_WHIP,
                CustomBaseItemType.Saberstaff,
                CustomBaseItemType.Lightsaber

        };

            NWPlayer player = _.GetPCItemLastEquippedBy();
            NWItem oItem = (_.GetPCItemLastEquipped());
            int baseItemType = oItem.BaseItemType;

            // Handle lightsaber sounds
            if (oItem.CustomItemType == CustomItemType.Lightsaber ||
                oItem.CustomItemType == CustomItemType.Saberstaff)
            {
                player.AssignCommand(() =>
                {
                    _.PlaySound("saberon");
                });
            }

            if (!validItemTypes.Contains(baseItemType)) return;

            foreach (ItemProperty ip in oItem.ItemProperties)
            {
                if (_.GetItemPropertyType(ip) == ITEM_PROPERTY_ONHITCASTSPELL)
                {
                    if (_.GetItemPropertySubType(ip) == IP_CONST_ONHIT_CASTSPELL_ONHIT_UNIQUEPOWER)
                    {
                        return;
                    }
                }
            }

            // No item property found. Add it to the item.
            _xp2.IPSafeAddItemProperty(oItem, _.ItemPropertyOnHitCastSpell(IP_CONST_ONHIT_CASTSPELL_ONHIT_UNIQUEPOWER, 40), 0.0f, AddItemPropertyPolicy.ReplaceExisting, false, false);

            if (baseItemType == BASE_ITEM_TORCH)
            {
                int charges = oItem.ReduceCharges();
                if (charges <= 0)
                {
                    oItem.Destroy();
                }
            }
        }

        public void ReturnItem(NWObject target, NWItem item)
        {
            _.CopyItem(item.Object, target.Object, TRUE);
            item.Destroy();
        }

        public void StripAllItemProperties(NWItem item)
        {
            foreach (var ip in item.ItemProperties)
            {
                _.RemoveItemProperty(item.Object, ip);
            }
        }
        
        public void FinishActionItem(IActionItem actionItem, NWPlayer user, NWItem item, NWObject target, Location targetLocation, Vector userStartPosition, CustomData customData)
        {
            user.IsBusy = false;

            Vector userPosition = user.Position;
            if (userPosition.m_X != userStartPosition.m_X ||
                userPosition.m_Y != userStartPosition.m_Y ||
                userPosition.m_Z != userStartPosition.m_Z)
            {
                user.SendMessage("You move and interrupt your action.");
                return;
            }

            float maxDistance = actionItem.MaxDistance(user, item, target, targetLocation);
            if (maxDistance > 0.0f)
            {
                if (target.IsValid &&
                    (_.GetDistanceBetween(user.Object, target.Object) > maxDistance ||
                    user.Area.Resref != target.Area.Resref))
                {
                    user.SendMessage("Your target is too far away.");
                    return;
                }
                else if (!target.IsValid &&
                         (_.GetDistanceBetweenLocations(user.Location, targetLocation) > maxDistance ||
                         user.Area.Resref != ((NWArea)_.GetAreaFromLocation(targetLocation)).Resref))
                {
                    user.SendMessage("That location is too far away.");
                    return;
                }
            }

            if (!target.IsValid && !actionItem.AllowLocationTarget())
            {
                user.SendMessage("Unable to locate target.");
                return;
            }

            string invalidTargetMessage = actionItem.IsValidTarget(user, item, target, targetLocation);
            if (!string.IsNullOrWhiteSpace(invalidTargetMessage))
            {
                user.SendMessage(invalidTargetMessage);
                return;
            }

            actionItem.ApplyEffects(user, item, target, targetLocation, customData);

            if (actionItem.ReducesItemCharge(user, item, target, targetLocation, customData))
            {
                if (item.Charges > 0) item.ReduceCharges();
                else item.Destroy();
            }
        }
        
        public ItemProperty GetCustomItemPropertyByItemTag(string tag)
        {
            NWPlaceable container = (_.GetObjectByTag("item_props"));
            NWItem item = container.InventoryItems.SingleOrDefault(x => x.Tag == tag);
            if (item == null)
            {
                throw new Exception("Unable to find an item tagged '" + tag + "' in the item props container.");
            }

            ItemProperty prop = item.ItemProperties.FirstOrDefault();
            if (prop == null)
            {
                throw new Exception("Unable to find an item property on item tagged '" + tag + "' in the item props container.");
            }

            return prop;
        }

        public int[] MeleeWeaponTypes
        {
            get
            {
                int[] meleeTypes =
                {
                    BASE_ITEM_BASTARDSWORD,
                    BASE_ITEM_BATTLEAXE,
                    BASE_ITEM_CLUB,
                    BASE_ITEM_DAGGER,
                    BASE_ITEM_HANDAXE,
                    BASE_ITEM_KAMA,
                    BASE_ITEM_KATANA,
                    BASE_ITEM_KUKRI,
                    BASE_ITEM_LIGHTFLAIL,
                    BASE_ITEM_LIGHTHAMMER,
                    BASE_ITEM_LIGHTMACE,
                    BASE_ITEM_LONGSWORD,
                    BASE_ITEM_RAPIER,
                    BASE_ITEM_SCIMITAR,
                    BASE_ITEM_SHORTSPEAR,
                    BASE_ITEM_SHORTSWORD,
                    BASE_ITEM_SICKLE,
                    BASE_ITEM_WHIP,
                    CustomBaseItemType.Lightsaber,
                    BASE_ITEM_DIREMACE,
                    BASE_ITEM_DWARVENWARAXE,
                    BASE_ITEM_GREATAXE,
                    BASE_ITEM_GREATSWORD,
                    BASE_ITEM_HALBERD,
                    BASE_ITEM_HEAVYFLAIL,
                    BASE_ITEM_MORNINGSTAR,
                    BASE_ITEM_QUARTERSTAFF,
                    BASE_ITEM_SCYTHE,
                    BASE_ITEM_TRIDENT,
                    BASE_ITEM_WARHAMMER,
                    BASE_ITEM_DOUBLEAXE,
                    BASE_ITEM_TWOBLADEDSWORD,
                    CustomBaseItemType.Saberstaff,
                    BASE_ITEM_BRACER,
                    BASE_ITEM_GLOVES

                };

                return meleeTypes;
            }
        }

        public int[] RangedWeaponTypes
        {
            get
            {
                int[] rangedTypes =
                {
                    BASE_ITEM_HEAVYCROSSBOW,
                    BASE_ITEM_LIGHTCROSSBOW,
                    BASE_ITEM_LONGBOW,
                    BASE_ITEM_SHORTBOW,
                    BASE_ITEM_ARROW,
                    BASE_ITEM_BOLT,
                    BASE_ITEM_GRENADE,
                    BASE_ITEM_SHURIKEN,
                    BASE_ITEM_SLING,
                    BASE_ITEM_THROWINGAXE,
                    BASE_ITEM_BULLET,
                    BASE_ITEM_DART
                };

                return rangedTypes;
            }
        }


        public SkillType GetSkillTypeForItem(NWItem item)
        {
            SkillType skillType = SkillType.Unknown;
            int type = item.BaseItemType;
            int[] oneHandedTypes =
            {
                BASE_ITEM_BASTARDSWORD,
                BASE_ITEM_BATTLEAXE,
                BASE_ITEM_CLUB,
                BASE_ITEM_DAGGER,
                BASE_ITEM_HANDAXE,
                BASE_ITEM_KAMA,
                BASE_ITEM_KATANA,
                BASE_ITEM_KUKRI,
                BASE_ITEM_LIGHTFLAIL,
                BASE_ITEM_LIGHTHAMMER,
                BASE_ITEM_LIGHTMACE,
                BASE_ITEM_LONGSWORD,
                BASE_ITEM_MORNINGSTAR,
                BASE_ITEM_RAPIER,
                BASE_ITEM_SCIMITAR,
                BASE_ITEM_SHORTSPEAR,
                BASE_ITEM_SHORTSWORD,
                BASE_ITEM_SICKLE,
                BASE_ITEM_WHIP,
                CustomBaseItemType.Lightsaber
            };

            int[] twoHandedTypes =
            {
                BASE_ITEM_DIREMACE,
                BASE_ITEM_DWARVENWARAXE,
                BASE_ITEM_GREATAXE,
                BASE_ITEM_GREATSWORD,
                BASE_ITEM_HALBERD,
                BASE_ITEM_HEAVYFLAIL,
                BASE_ITEM_QUARTERSTAFF,
                BASE_ITEM_SCYTHE,
                BASE_ITEM_TRIDENT,
                BASE_ITEM_WARHAMMER
            };

            int[] twinBladeTypes =
            {
                BASE_ITEM_DOUBLEAXE,
                BASE_ITEM_TWOBLADEDSWORD,
                CustomBaseItemType.Saberstaff
            };

            int[] martialArtsTypes =
            {
                BASE_ITEM_BRACER,
                BASE_ITEM_GLOVES
            };

            int[] firearmTypes =
            {
                BASE_ITEM_HEAVYCROSSBOW,
                BASE_ITEM_LIGHTCROSSBOW,
                BASE_ITEM_LONGBOW,
                BASE_ITEM_SHORTBOW,
                BASE_ITEM_ARROW,
                BASE_ITEM_BOLT
            };

            int[] throwingTypes =
            {
                BASE_ITEM_GRENADE,
                BASE_ITEM_SHURIKEN,
                BASE_ITEM_SLING,
                BASE_ITEM_THROWINGAXE,
                BASE_ITEM_BULLET,
                BASE_ITEM_DART
            };

            int[] shieldTypes =
            {
                BASE_ITEM_SMALLSHIELD,
                BASE_ITEM_LARGESHIELD,
                BASE_ITEM_TOWERSHIELD
            };

            if (oneHandedTypes.Contains(type)) skillType = SkillType.OneHanded;
            else if (twoHandedTypes.Contains(type)) skillType = SkillType.TwoHanded;
            else if (twinBladeTypes.Contains(type)) skillType = SkillType.TwinBlades;
            else if (martialArtsTypes.Contains(type)) skillType = SkillType.MartialArts;
            else if (firearmTypes.Contains(type)) skillType = SkillType.Firearms;
            else if (throwingTypes.Contains(type)) skillType = SkillType.Throwing;
            else if (item.CustomItemType == CustomItemType.HeavyArmor) skillType = SkillType.HeavyArmor;
            else if (item.CustomItemType == CustomItemType.LightArmor) skillType = SkillType.LightArmor;
            else if (item.CustomItemType == CustomItemType.ForceArmor) skillType = SkillType.ForceArmor;
            else if (shieldTypes.Contains(type)) skillType = SkillType.Shields;

            return skillType;
        }


    }
}
