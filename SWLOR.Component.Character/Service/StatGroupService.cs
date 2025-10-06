using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Repositories;

namespace SWLOR.Component.Character.Service
{
    internal class StatGroupService: IStatGroupService
    {
        private readonly IPlayerRepository _repo;

        public StatGroupService(IPlayerRepository repo)
        {
            _repo = repo;
        }

        public StatGroup LoadStats(uint creature)
        {
            return GetIsPC(creature) 
                ? LoadPlayerStats(creature) 
                : LoadNPCStats(creature);
        }

        private StatGroup LoadPlayerStats(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _repo.GetById(playerId);
            var statGroup = new StatGroup();

            foreach (var (type, value) in dbPlayer.Stats)
            {
                statGroup.SetStat(type, value);
            }

            statGroup.SetStat(StatType.Might, GetAbilityScore(player, AbilityType.Might));
            statGroup.SetStat(StatType.Perception, GetAbilityScore(player, AbilityType.Perception));
            statGroup.SetStat(StatType.Vitality, GetAbilityScore(player, AbilityType.Vitality));
            statGroup.SetStat(StatType.Agility, GetAbilityScore(player, AbilityType.Agility));
            statGroup.SetStat(StatType.Willpower, GetAbilityScore(player, AbilityType.Willpower));
            statGroup.SetStat(StatType.Social, GetAbilityScore(player, AbilityType.Social));

            statGroup.RightHandStat = LoadWeaponStat(InventorySlotType.RightHand, player);
            statGroup.LeftHandStat = LoadWeaponStat(InventorySlotType.LeftHand, player);

            return statGroup;
        }

        private StatGroup LoadNPCStats(uint npc)
        {
            var statGroup = new StatGroup();

            var skin = GetItemInSlot(InventorySlotType.CreatureArmor, npc);
            if (GetIsObjectValid(skin))
            {
                for (var ip = GetFirstItemProperty(skin); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(skin))
                {
                    var type = GetItemPropertyType(ip);
                    var value = GetItemPropertyCostTableValue(ip);
                    if (type == ItemPropertyType.NPCHP)
                    {
                        statGroup.SetStat(StatType.MaxHP, statGroup.GetStat(StatType.MaxHP) + value);
                    }
                    else if (type == ItemPropertyType.FP)
                    {
                        statGroup.SetStat(StatType.MaxFP, statGroup.GetStat(StatType.MaxFP) + value);
                    }
                    else if (type == ItemPropertyType.FPRegen)
                    {
                        statGroup.SetStat(StatType.FPRegen, statGroup.GetStat(StatType.FPRegen) + value);
                    }
                    else if (type == ItemPropertyType.STMRegen)
                    {
                        statGroup.SetStat(StatType.STMRegen, statGroup.GetStat(StatType.STMRegen) + value);
                    }
                    else if (type == ItemPropertyType.AbilityRecastReduction)
                    {
                        statGroup.SetStat(StatType.RecastReduction, statGroup.GetStat(StatType.RecastReduction) + value);
                    }
                    else if (type == ItemPropertyType.Defense)
                    {
                        var defenseType = (CombatDamageType)GetItemPropertySubType(ip);
                        if (defenseType == CombatDamageType.Physical)
                        {
                            statGroup.SetStat(StatType.Defense, statGroup.GetStat(StatType.Defense) + value);
                        }
                        else if (defenseType == CombatDamageType.Force)
                        {
                            statGroup.SetStat(StatType.ForceDefense, statGroup.GetStat(StatType.ForceDefense) + value);
                        }
                    }
                    else if (type == ItemPropertyType.Evasion)
                    {
                        statGroup.SetStat(StatType.Evasion, statGroup.GetStat(StatType.Evasion) + value);
                    }
                    else if (type == ItemPropertyType.AccuracyStat)
                    {
                        statGroup.SetStat(StatType.Accuracy, statGroup.GetStat(StatType.Accuracy) + value);
                    }
                    else if (type == ItemPropertyType.Attack)
                    {
                        statGroup.SetStat(StatType.Attack, statGroup.GetStat(StatType.Attack) + value);
                    }
                    else if (type == ItemPropertyType.ForceAttack)
                    {
                        statGroup.SetStat(StatType.ForceAttack, statGroup.GetStat(StatType.ForceAttack) + value);
                    }
                    else if (type == ItemPropertyType.NPCLevel)
                    {
                        statGroup.SetStat(StatType.Level, value);
                    }
                }
            }

            statGroup.SetStat(StatType.Might, GetAbilityScore(npc, AbilityType.Might));
            statGroup.SetStat(StatType.Perception, GetAbilityScore(npc, AbilityType.Perception));
            statGroup.SetStat(StatType.Vitality, GetAbilityScore(npc, AbilityType.Vitality));
            statGroup.SetStat(StatType.Agility, GetAbilityScore(npc, AbilityType.Agility));
            statGroup.SetStat(StatType.Willpower, GetAbilityScore(npc, AbilityType.Willpower));
            statGroup.SetStat(StatType.Social, GetAbilityScore(npc, AbilityType.Social));

            var rightSlot = InventorySlotType.RightHand;
            var rightHand = GetItemInSlot(InventorySlotType.RightHand, npc);
            if (!GetIsObjectValid(rightHand))
                rightSlot = InventorySlotType.CreatureRight;

            var leftSlot = InventorySlotType.LeftHand;
            var leftHand = GetItemInSlot(InventorySlotType.LeftHand, npc);
            if (!GetIsObjectValid(leftHand))
                leftSlot = InventorySlotType.CreatureLeft;

            statGroup.RightHandStat = LoadWeaponStat(rightSlot, npc);
            statGroup.LeftHandStat = LoadWeaponStat(leftSlot, npc);

            return statGroup;
        }

        private WeaponStat LoadWeaponStat(InventorySlotType slot, uint creature)
        {
            var stat = new WeaponStat();
            var weapon = GetItemInSlot(slot, creature);
            if (!GetIsObjectValid(weapon))
                return stat;

            stat.Item = weapon;
            for (var ip = GetFirstItemProperty(weapon); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(weapon))
            {
                var type = GetItemPropertyType(ip);
                var value = GetItemPropertyCostTableValue(ip);
                if (type == ItemPropertyType.DMG)
                {
                    stat.DMG = value;
                }
                else if (type == ItemPropertyType.Delay)
                {
                    stat.Delay = value * 10;
                }
            }

            return stat;
        }
    }
}
