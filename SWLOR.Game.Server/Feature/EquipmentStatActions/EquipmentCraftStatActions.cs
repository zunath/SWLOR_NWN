using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using ItemProperty = SWLOR.NWN.API.Engine.ItemProperty;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature.EquipmentStatActions
{
    /// <summary>
    /// Equipment actions for the crafting stats: control, craftsmanship and CP bonus.
    /// </summary>
    public static class EquipmentCraftStatActions
    {
        /// <summary>
        /// Applies or removes control on a creature.
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding control, if false we're removing it.</param>
        public static void ApplyControl(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);

            if (GetIsPC(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = DB.Get<Player>(playerId);
                var subType = GetItemPropertySubType(ip);
                var skillType = SkillType.Invalid;

                // Types are defined in iprp_crafttype.2da
                switch (subType)
                {
                    case 1:
                        skillType = SkillType.Smithery;
                        break;
                    case 2:
                        skillType = SkillType.Engineering;
                        break;
                    case 3:
                        skillType = SkillType.Fabrication;
                        break;
                    case 4:
                        skillType = SkillType.Agriculture;
                        break;
                }

                if (skillType == SkillType.Invalid)
                {
                    throw new Exception($"Unable to determine skill type for {nameof(ApplyControl)}");
                }

                if (isAdding)
                {
                    StatRecord.AdjustControl(dbPlayer, skillType, amount);
                }
                else
                {
                    StatRecord.AdjustControl(dbPlayer, skillType, -amount);
                }

                DB.Set(dbPlayer);
            }
        }

        /// <summary>
        /// Applies or removes craftsmanship on a creature.
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding craftsmanship, if false we're removing it.</param>
        public static void ApplyCraftsmanship(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);

            if (GetIsPC(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = DB.Get<Player>(playerId);
                var subType = GetItemPropertySubType(ip);
                var skillType = SkillType.Invalid;

                // Types are defined in iprp_crafttype.2da
                switch (subType)
                {
                    case 1:
                        skillType = SkillType.Smithery;
                        break;
                    case 2:
                        skillType = SkillType.Engineering;
                        break;
                    case 3:
                        skillType = SkillType.Fabrication;
                        break;
                    case 4:
                        skillType = SkillType.Agriculture;
                        break;
                }
                if (isAdding)
                {
                    StatRecord.AdjustCraftsmanship(dbPlayer, skillType, amount);
                }
                else
                {
                    StatRecord.AdjustCraftsmanship(dbPlayer, skillType, -amount);
                }

                DB.Set(dbPlayer);
            }
        }

        /// <summary>
        /// Applies or removes CP bonuses on a player.
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the CP bonus, if false we're removing it.</param>
        public static void ApplyCPBonus(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);

            if (GetIsPC(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = DB.Get<Player>(playerId);
                var subType = GetItemPropertySubType(ip);
                var skillType = SkillType.Invalid;

                // Types are defined in iprp_crafttype.2da
                switch (subType)
                {
                    case 1:
                        skillType = SkillType.Smithery;
                        break;
                    case 2:
                        skillType = SkillType.Engineering;
                        break;
                    case 3:
                        skillType = SkillType.Fabrication;
                        break;
                    case 4:
                        skillType = SkillType.Agriculture;
                        break;
                }
                if (isAdding)
                {
                    StatRecord.AdjustCPBonus(dbPlayer, skillType, amount);
                }
                else
                {
                    StatRecord.AdjustCPBonus(dbPlayer, skillType, -amount);
                }

                DB.Set(dbPlayer);
            }
        }
    }
}
