using System;
using System.Linq;
using SWLOR.Game.Server.Bioware.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;

using NWN;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service
{
    public class ItemService : IItemService
    {
        private readonly INWScript _;
        private readonly IBiowareXP2 _xp2;
        private readonly ISkillService _skill;
        private readonly IColorTokenService _color;
        private readonly INWNXPlayer _nwnxPlayer;

        public ItemService(
            INWScript script,
            IBiowareXP2 xp2,
            ISkillService skill,
            IColorTokenService color,
            INWNXPlayer nwnxPlayer)
        {
            _ = script;
            _xp2 = xp2;
            _skill = skill;
            _color = color;
            _nwnxPlayer = nwnxPlayer;
        }

        public string GetNameByResref(string resref)
        {
            NWPlaceable tempStorage = NWPlaceable.Wrap(_.GetObjectByTag("TEMP_ITEM_STORAGE"));
            if (!tempStorage.IsValid)
            {
                Console.WriteLine("Could not locate temp item storage object. Create a placeable container in a non-accessible area with the tag TEMP_ITEM_STORAGE.");
                return null;
            }
            NWItem item = NWItem.Wrap(_.CreateItemOnObject(resref, tempStorage.Object));
            string name = item.Name;
            item.Destroy();
            return name;
        }
        
        public void OnModuleActivatedItem()
        {
            NWPlayer oPC = NWPlayer.Wrap(_.GetItemActivator());
            NWItem oItem = NWItem.Wrap(_.GetItemActivated());
            NWObject oTarget = NWObject.Wrap(_.GetItemActivatedTarget());
            Location targetLocation = _.GetItemActivatedTargetLocation();
            
            string className = oItem.GetLocalString("JAVA_SCRIPT");
            if (string.IsNullOrWhiteSpace(className)) className = oItem.GetLocalString("ACTIVATE_JAVA_SCRIPT");
            if (string.IsNullOrWhiteSpace(className)) className = oItem.GetLocalString("JAVA_ACTION_SCRIPT");
            if (string.IsNullOrWhiteSpace(className)) className = oItem.GetLocalString("SCRIPT");
            if (string.IsNullOrWhiteSpace(className)) return;

            oPC.ClearAllActions();

            // Remove "Item." prefix if it exists.
            if (className.StartsWith("Item."))
                className = className.Substring(5);

            IActionItem item = App.ResolveByInterface<IActionItem>("Item." + className);

            if (oPC.IsBusy)
            {
                oPC.SendMessage("You are busy.");
                return;
            }

            string invalidTargetMessage = item.IsValidTarget(oPC, oItem, oTarget, targetLocation);
            if (!string.IsNullOrWhiteSpace(invalidTargetMessage))
            {
                oPC.SendMessage(invalidTargetMessage);
                return;
            }

            if (item.MaxDistance() > 0.0f)
            {
                if (_.GetDistanceBetween(oPC.Object, oTarget.Object) > item.MaxDistance() ||
                    oPC.Area.Resref != oTarget.Area.Resref)
                {
                    oPC.SendMessage("Your target is too far away.");
                    return;
                }
            }

            CustomData customData = item.StartUseItem(oPC, oItem, oTarget, targetLocation);
            float delay = item.Seconds(oPC, oItem, oTarget, targetLocation, customData);
            int animationID = item.AnimationID();
            bool faceTarget = item.FaceTarget();
            Vector userPosition = oPC.Position;

            oPC.AssignCommand(() =>
            {
                oPC.IsBusy = true;
                if (faceTarget)
                    _.SetFacingPoint(oTarget.Position);
                if (animationID > 0)
                    _.ActionPlayAnimation(animationID, 1.0f, delay);
            });
            
            _nwnxPlayer.StartGuiTimingBar(oPC, delay, string.Empty);
            oPC.DelayCommand(() =>
            {
                FinishActionItem(item, oPC, oItem, oTarget, targetLocation, userPosition, customData);
            }, delay);
        }

        public void OnModuleHeartbeat()
        {
            NWModule module = NWModule.Get();

            foreach (NWPlayer pc in module.Players)
            {
                NWItem offHand = pc.LeftHand;
                if (offHand.BaseItemType == NWScript.BASE_ITEM_TORCH)
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
            if (examinedObject.ObjectType != NWScript.OBJECT_TYPE_ITEM) return existingDescription;

            NWItem examinedItem = NWItem.Wrap(examinedObject.Object);
            string description = "";

            if (examinedItem.RecommendedLevel > 0)
            {
                description += _color.Orange("Recommended Level: ") + examinedItem.RecommendedLevel + "\n";
            }
            if (examinedItem.AssociatedSkillType > 0)
            {
                PCSkill pcSkill = _skill.GetPCSkillByID(examiner.GlobalID, (int)examinedItem.AssociatedSkillType);
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
            if (examinedItem.CastingSpeed > 0)
            {
                description += _color.Orange("Casting Speed: +") + examinedItem.CastingSpeed + "%\n";
            }
            else if (examinedItem.CastingSpeed < 0)
            {
                description += _color.Orange("Casting Penalty: -") + examinedItem.CastingSpeed + "%\n";
            }

            if (examinedItem.LoggingBonus > 0)
            {
                description += _color.Orange("Logging Bonus: ") + examinedItem.LoggingBonus + "\n";
            }
            if (examinedItem.MiningBonus > 0)
            {
                description += _color.Orange("Mining Bonus: ") + examinedItem.MiningBonus + "\n";
            }
            if (examinedItem.CraftBonusArmorsmith > 0)
            {
                description += _color.Orange("Armorsmith Bonus: ") + examinedItem.CraftBonusArmorsmith + "\n";
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
            if (examinedItem.EvocationBonus > 0)
            {
                description += _color.Orange("Evocation Bonus: ") + examinedItem.EvocationBonus + "\n";
            }
            if (examinedItem.AlterationBonus > 0)
            {
                description += _color.Orange("Alteration Bonus: ") + examinedItem.AlterationBonus + "\n";
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
            if (examinedItem.FirstAidBonus > 0)
            {
                description += _color.Orange("First Aid Bonus: ") + examinedItem.FirstAidBonus + "\n";
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
                    NWScript.BASE_ITEM_AMULET,
                    NWScript.BASE_ITEM_ARMOR,
                    NWScript.BASE_ITEM_BELT,
                    NWScript.BASE_ITEM_CLOAK,
                    NWScript.BASE_ITEM_HELMET,
                    NWScript.BASE_ITEM_LARGESHIELD,
                    NWScript.BASE_ITEM_SMALLSHIELD,
                    NWScript.BASE_ITEM_TOWERSHIELD
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
                    NWScript.BASE_ITEM_ARROW,
                    NWScript.BASE_ITEM_BASTARDSWORD,
                    NWScript.BASE_ITEM_BATTLEAXE,
                    NWScript.BASE_ITEM_BOLT,
                    NWScript.BASE_ITEM_BRACER,
                    NWScript.BASE_ITEM_BULLET,
                    NWScript.BASE_ITEM_CLUB,
                    NWScript.BASE_ITEM_DAGGER,
                    NWScript.BASE_ITEM_DART,
                    NWScript.BASE_ITEM_DIREMACE,
                    NWScript.BASE_ITEM_DOUBLEAXE,
                    NWScript.BASE_ITEM_DWARVENWARAXE,
                    NWScript.BASE_ITEM_GLOVES,
                    NWScript.BASE_ITEM_GREATAXE,
                    NWScript.BASE_ITEM_GREATSWORD,
                    NWScript.BASE_ITEM_GRENADE,
                    NWScript.BASE_ITEM_HALBERD,
                    NWScript.BASE_ITEM_HANDAXE,
                    NWScript.BASE_ITEM_HEAVYCROSSBOW,
                    NWScript.BASE_ITEM_HEAVYFLAIL,
                    NWScript.BASE_ITEM_KAMA,
                    NWScript.BASE_ITEM_KATANA,
                    NWScript.BASE_ITEM_KUKRI,
                    NWScript.BASE_ITEM_LIGHTCROSSBOW,
                    NWScript.BASE_ITEM_LIGHTFLAIL,
                    NWScript.BASE_ITEM_LIGHTHAMMER,
                    NWScript.BASE_ITEM_LIGHTMACE,
                    NWScript.BASE_ITEM_LONGBOW,
                    NWScript.BASE_ITEM_LONGSWORD,
                    NWScript.BASE_ITEM_MORNINGSTAR,
                    NWScript.BASE_ITEM_QUARTERSTAFF,
                    NWScript.BASE_ITEM_RAPIER,
                    NWScript.BASE_ITEM_SCIMITAR,
                    NWScript.BASE_ITEM_SCYTHE,
                    NWScript.BASE_ITEM_SHORTBOW,
                    NWScript.BASE_ITEM_SHORTSPEAR,
                    NWScript.BASE_ITEM_SHORTSWORD,
                    NWScript.BASE_ITEM_SHURIKEN,
                    NWScript.BASE_ITEM_SICKLE,
                    NWScript.BASE_ITEM_SLING,
                    NWScript.BASE_ITEM_THROWINGAXE,
                    NWScript.BASE_ITEM_TRIDENT,
                    NWScript.BASE_ITEM_TWOBLADEDSWORD,
                    NWScript.BASE_ITEM_WARHAMMER,
                    NWScript.BASE_ITEM_WHIP
                };

                return weaponTypes;
            }
        }

        public void OnModuleEquipItem()
        {
            int[] validItemTypes = {
                NWScript.BASE_ITEM_ARMOR,
                NWScript.BASE_ITEM_ARROW,
                NWScript.BASE_ITEM_BASTARDSWORD,
                NWScript.BASE_ITEM_BATTLEAXE,
                NWScript.BASE_ITEM_BELT,
                NWScript.BASE_ITEM_BOLT,
                NWScript.BASE_ITEM_BOOTS,
                NWScript.BASE_ITEM_BRACER,
                NWScript.BASE_ITEM_BULLET,
                NWScript.BASE_ITEM_CLOAK,
                NWScript.BASE_ITEM_CLUB,
                NWScript.BASE_ITEM_DAGGER,
                NWScript.BASE_ITEM_DART,
                NWScript.BASE_ITEM_DIREMACE,
                NWScript.BASE_ITEM_DOUBLEAXE,
                NWScript.BASE_ITEM_DWARVENWARAXE,
                NWScript.BASE_ITEM_GLOVES,
                NWScript.BASE_ITEM_GREATAXE,
                NWScript.BASE_ITEM_GREATSWORD,
                NWScript.BASE_ITEM_GRENADE,
                NWScript.BASE_ITEM_HALBERD,
                NWScript.BASE_ITEM_HANDAXE,
                NWScript.BASE_ITEM_HEAVYCROSSBOW,
                NWScript.BASE_ITEM_HEAVYFLAIL,
                NWScript.BASE_ITEM_HELMET,
                NWScript.BASE_ITEM_KAMA,
                NWScript.BASE_ITEM_KATANA,
                NWScript.BASE_ITEM_KUKRI,
                NWScript.BASE_ITEM_LARGESHIELD,
                NWScript.BASE_ITEM_LIGHTCROSSBOW,
                NWScript.BASE_ITEM_LIGHTFLAIL,
                NWScript.BASE_ITEM_LIGHTHAMMER,
                NWScript.BASE_ITEM_LIGHTMACE,
                NWScript.BASE_ITEM_LONGBOW,
                NWScript.BASE_ITEM_LONGSWORD,
                NWScript.BASE_ITEM_MORNINGSTAR,
                NWScript.BASE_ITEM_QUARTERSTAFF,
                NWScript.BASE_ITEM_RAPIER,
                NWScript.BASE_ITEM_SCIMITAR,
                NWScript.BASE_ITEM_SCYTHE,
                NWScript.BASE_ITEM_SHORTBOW,
                NWScript.BASE_ITEM_SHORTSPEAR,
                NWScript.BASE_ITEM_SHORTSWORD,
                NWScript.BASE_ITEM_SHURIKEN,
                NWScript.BASE_ITEM_SICKLE,
                NWScript.BASE_ITEM_SLING,
                NWScript.BASE_ITEM_SMALLSHIELD,
                NWScript.BASE_ITEM_THROWINGAXE,
                NWScript.BASE_ITEM_TOWERSHIELD,
                NWScript.BASE_ITEM_TRIDENT,
                NWScript.BASE_ITEM_TWOBLADEDSWORD,
                NWScript.BASE_ITEM_WARHAMMER,
                NWScript.BASE_ITEM_WHIP
        };

            NWItem oItem = NWItem.Wrap(_.GetPCItemLastEquipped());
            int baseItemType = oItem.BaseItemType;

            if (!validItemTypes.Contains(baseItemType)) return;

            foreach (ItemProperty ip in oItem.ItemProperties)
            {
                if (_.GetItemPropertyType(ip) == NWScript.ITEM_PROPERTY_ONHITCASTSPELL)
                {
                    if (_.GetItemPropertySubType(ip) == NWScript.IP_CONST_ONHIT_CASTSPELL_ONHIT_UNIQUEPOWER)
                    {
                        return;
                    }
                }
            }

            // No item property found. Add it to the item.
            _xp2.IPSafeAddItemProperty(oItem, _.ItemPropertyOnHitCastSpell(NWScript.IP_CONST_ONHIT_CASTSPELL_ONHIT_UNIQUEPOWER, 40), 0.0f, AddItemPropertyPolicy.ReplaceExisting, false, false);

            if (baseItemType == NWScript.BASE_ITEM_TORCH)
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
            _.CopyItem(item.Object, target.Object, NWScript.TRUE);
            item.Destroy();
        }

        public void StripAllItemProperties(NWItem item)
        {
            foreach (var ip in item.ItemProperties)
            {
                _.RemoveItemProperty(item.Object, ip);
            }
        }
        
        private void FinishActionItem(IActionItem actionItem, NWPlayer user, NWItem item, NWObject target, Location targetLocation, Vector userStartPosition, CustomData customData)
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

            if (actionItem.MaxDistance() > 0.0f)
            {
                if (_.GetDistanceBetween(user.Object, target.Object) > actionItem.MaxDistance() ||
                    user.Area.Resref != target.Area.Resref)
                {
                    user.SendMessage("Your target is too far away.");
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

        public CustomItemType GetCustomItemType(NWItem item)
        {
            int[] blades =
            {
                NWScript.BASE_ITEM_BASTARDSWORD,
                NWScript.BASE_ITEM_LONGSWORD,
                NWScript.BASE_ITEM_KATANA,
                NWScript.BASE_ITEM_SCIMITAR,
                NWScript.BASE_ITEM_BATTLEAXE
            };

            int[] finesseBlades =
            {
                NWScript.BASE_ITEM_DAGGER,
                NWScript.BASE_ITEM_RAPIER,
                NWScript.BASE_ITEM_SHORTSWORD,
                NWScript.BASE_ITEM_KUKRI,
                NWScript.BASE_ITEM_SICKLE,
                NWScript.BASE_ITEM_WHIP,
                NWScript.BASE_ITEM_HANDAXE
            };

            int[] blunts =
            {
                NWScript.BASE_ITEM_CLUB,
                NWScript.BASE_ITEM_LIGHTFLAIL,
                NWScript.BASE_ITEM_LIGHTHAMMER,
                NWScript.BASE_ITEM_LIGHTMACE,
                NWScript.BASE_ITEM_MORNINGSTAR
            };

            int[] heavyBlades =
            {
                NWScript.BASE_ITEM_GREATAXE,
                NWScript.BASE_ITEM_GREATSWORD,
                NWScript.BASE_ITEM_DWARVENWARAXE
            };

            int[] heavyBlunts =
            {
                NWScript.BASE_ITEM_HEAVYFLAIL,
                NWScript.BASE_ITEM_WARHAMMER,
                NWScript.BASE_ITEM_DIREMACE,
                NWScript.BASE_ITEM_QUARTERSTAFF
            };

            int[] polearms =
            {
                NWScript.BASE_ITEM_HALBERD,
                NWScript.BASE_ITEM_SCYTHE,
                NWScript.BASE_ITEM_SHORTSPEAR,
                NWScript.BASE_ITEM_TRIDENT
            };

            int[] twinBlades =
            {
                NWScript.BASE_ITEM_DOUBLEAXE,
                NWScript.BASE_ITEM_TWOBLADEDSWORD
            };

            int[] martialArts =
            {
                NWScript.BASE_ITEM_GLOVES,
                NWScript.BASE_ITEM_BRACER,
                NWScript.BASE_ITEM_KAMA
            };

            int[] rifles =
            {
                NWScript.BASE_ITEM_LIGHTCROSSBOW,
                NWScript.BASE_ITEM_HEAVYCROSSBOW
            };

            int[] blasters =
            {
                NWScript.BASE_ITEM_SHORTBOW,
                NWScript.BASE_ITEM_LONGBOW,
            };

            int[] throwing =
            {
                NWScript.BASE_ITEM_SLING,
                NWScript.BASE_ITEM_DART,
                NWScript.BASE_ITEM_SHURIKEN,
                NWScript.BASE_ITEM_THROWINGAXE
            };


            if (blades.Contains(item.BaseItemType)) return CustomItemType.Blade;
            if (finesseBlades.Contains(item.BaseItemType)) return CustomItemType.FinesseBlade;
            if (blunts.Contains(item.BaseItemType)) return CustomItemType.Blunt;
            if (heavyBlades.Contains(item.BaseItemType)) return CustomItemType.HeavyBlade;
            if (heavyBlunts.Contains(item.BaseItemType)) return CustomItemType.HeavyBlunt;
            if (polearms.Contains(item.BaseItemType)) return CustomItemType.Polearm;
            if (twinBlades.Contains(item.BaseItemType)) return CustomItemType.TwinBlade;
            if (martialArts.Contains(item.BaseItemType)) return CustomItemType.MartialArtWeapon;
            if (rifles.Contains(item.BaseItemType)) return CustomItemType.Rifle;
            if (blasters.Contains(item.BaseItemType)) return CustomItemType.Blaster;
            if (throwing.Contains(item.BaseItemType)) return CustomItemType.Throwing;
            // Armor is deliberately left out here because we don't have a way to determine the type of armor it should be
            // based on base item type.

            return CustomItemType.None;
        }

        public ItemProperty GetCustomItemPropertyByItemTag(string tag)
        {
            NWPlaceable container = NWPlaceable.Wrap(_.GetObjectByTag("item_props"));
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
    }
}
