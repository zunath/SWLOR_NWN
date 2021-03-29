using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ItemService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Skill = SWLOR.Game.Server.Service.Skill;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class RepairKitItemDefinition: IItemListDefinition
    {
        public Dictionary<string, ItemDetail> BuildItems()
        {
            var builder = new ItemBuilder();
            ArmorRepairKit(builder);
            WeaponRepairKit(builder);

            return builder.Build();
        }

        private string Validation(uint user, uint item, uint target, List<BaseItem> baseItemTypes)
        {
            if (GetObjectType(target) != ObjectType.Item)
                return "Only items may be targeted by repair kits.";

            var itemType = GetBaseItemType(target);

            if (!baseItemTypes.Contains(itemType))
                return "You cannot repair that item with this repair kit.";

            var maxDurability = Durability.GetMaxDurability(target);
            var durability = Durability.GetDurability(target);

            if (maxDurability <= 0.0f || durability >= maxDurability)
                return "That item does not need to be repaired.";

            if (maxDurability <= 0.1f)
                return "You cannot repair that item any more.";

            if (GetItemPossessor(target) != user)
                return "Items may only be repaired from your inventory.";

            return string.Empty;
        }

        private static void Application(
            uint user, 
            uint target, 
            float durabilityRepair, 
            float maxDurabilityLoss,
            SkillType skill,
            int tier)
        {
            var xp = tier * 50 + 200;
            Durability.RunItemRepair(user, target, durabilityRepair, maxDurabilityLoss);
            Skill.GiveSkillXP(user, skill, xp);
        }

        private void ArmorRepairKit(ItemBuilder builder)
        {
            void Repair(string tag, float durabilityRepair, float maxDurabilityLoss, int tier)
            {
                builder.Create(tag)
                    .Delay(12.0f)
                    .PlaysAnimation(Animation.LoopingGetMid)
                    .ReducesItemCharge()
                    .ValidationAction((user, item, target, location) =>
                    {
                        var baseItemTypes = new List<BaseItem>
                        {
                            BaseItem.Armor,
                            BaseItem.Helmet,
                            BaseItem.LargeShield,
                            BaseItem.SmallShield,
                            BaseItem.TowerShield,
                            BaseItem.Amulet,
                            BaseItem.Gloves,
                            BaseItem.Bracer,
                            BaseItem.Cloak,
                            BaseItem.Belt,
                            BaseItem.Ring
                        };

                        return Validation(user, item, target, baseItemTypes);
                    })
                    .ApplyAction((user, item, target, location) =>
                    {
                        Application(user, target, durabilityRepair, maxDurabilityLoss, SkillType.Smithery, tier);
                    });
            }

            Repair("arm_rep_b", 2.0f, 2.5f, 1);
            Repair("arm_rep_1", 4.0f, 2.0f, 2);
            Repair("arm_rep_2", 6.0f, 1.5f, 3);
            Repair("arm_rep_3", 8.0f, 1.0f, 4);
            Repair("arm_rep_4", 10.0f, 0.5f, 5);
        }

        private void WeaponRepairKit(ItemBuilder builder)
        {
            void Repair(string tag, float durabilityRepair, float maxDurabilityLoss, int tier)
            {
                builder.Create(tag)
                    .Delay(12.0f)
                    .PlaysAnimation(Animation.LoopingGetMid)
                    .ReducesItemCharge()
                    .ValidationAction((user, item, target, location) =>
                    {
                        var baseItemTypes = new List<BaseItem>
                        {
                            BaseItem.ShortSword,
                            BaseItem.Longsword,
                            BaseItem.BattleAxe,
                            BaseItem.BastardSword,
                            BaseItem.LightFlail,
                            BaseItem.WarHammer,
                            BaseItem.Cannon,
                            BaseItem.Rifle,
                            BaseItem.Longbow,
                            BaseItem.LightMace,
                            BaseItem.Halberd,
                            BaseItem.Pistol,
                            BaseItem.TwoBladedSword,
                            BaseItem.GreatSword,
                            BaseItem.GreatAxe,
                            BaseItem.Arrow,
                            BaseItem.Dagger,
                            BaseItem.Bolt,
                            BaseItem.Bullet,
                            BaseItem.Club,
                            BaseItem.Dart,
                            BaseItem.DireMace,
                            BaseItem.DoubleAxe,
                            BaseItem.HeavyFlail,
                            BaseItem.LightHammer,
                            BaseItem.HandAxe,
                            BaseItem.Kama,
                            BaseItem.Katana,
                            BaseItem.Kukri,
                            BaseItem.QuarterStaff,
                            BaseItem.Rapier,
                            BaseItem.Scimitar,
                            BaseItem.Scythe,
                            BaseItem.Shuriken,
                            BaseItem.Sickle,
                            BaseItem.Sling,
                            BaseItem.ThrowingAxe,
                            BaseItem.Grenade,
                            BaseItem.Lance,
                            BaseItem.Trident,
                            BaseItem.DwarvenWarAxe,
                            BaseItem.Whip,
                            BaseItem.OffHandPistol,
                            BaseItem.Knuckles
                        };

                        return Validation(user, item, target, baseItemTypes);
                    })
                    .ApplyAction((user, item, target, location) =>
                    {
                        Application(user, target, durabilityRepair, maxDurabilityLoss, SkillType.Smithery, tier);
                    });
            }

            Repair("wpn_rep_b", 2.0f, 2.5f, 1);
            Repair("wpn_rep_1", 4.0f, 2.0f, 2);
            Repair("wpn_rep_2", 6.0f, 1.5f, 3);
            Repair("wpn_rep_3", 8.0f, 1.0f, 4);
            Repair("wpn_rep_4", 10.0f, 0.5f, 5);
        }
    }
}
