using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.FarmingService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.HouseSpecializationService
{
    /// <summary>
    /// House specializations (Player City Revamp, "Homes, gardens, and projects"): who may change a house's job and when, how
    /// a Storehouse adds and removes its storage without touching what furniture adds, which Greenhouse plots can be planted,
    /// and which Workshop bonus a craft carries. No database or engine calls: <see cref="HouseSpecializations"/> reads and writes
    /// the records and asks these rules.
    /// </summary>
    public static class HouseSpecializationRules
    {
        /// <summary>A house's specialization changes once every 30 days. The timer belongs to the house, not its owner.</summary>
        public const int ChangeIntervalDays = 30;

        /// <summary>
        /// The Craftsmanship and Control a Workshop adds for its chosen skill. PROPOSED, NEEDS THE OWNER'S APPROVAL: the plan sizes it
        /// "against crafting gear in the balance review". A full tier 5 crafting set gives 28 Control and 29 Craftsmanship (one piece
        /// gives 1 to 10), so 5 is about a sixth of it: worth having, never a replacement for gear. Change this one constant to resize it.
        /// </summary>
        public const int WorkshopBonus = 5;

        /// <summary>A Storehouse adds half of the layout's own storage.</summary>
        public const double StorehouseShare = 0.5;

        #region Changing the specialization

        /// <summary>When the house's specialization can next change, or null when it never changed and can change now.</summary>
        public static DateTime? GetNextChangeUtc(DateTime? lastChangedUtc)
        {
            return lastChangedUtc?.AddDays(ChangeIntervalDays);
        }

        /// <summary>Whether the skill is one a Workshop can be set to: an active crafting skill.</summary>
        public static bool IsCraftingSkill(SkillType skill)
        {
            if (skill == SkillType.Invalid)
                return false;

            var detail = skill.GetAttribute<SkillType, SkillAttribute>();
            return detail.IsActive && detail.IsShownInCraftMenu;
        }

        /// <summary>
        /// Why the player cannot give the house this specialization, or <see cref="HouseSpecializationRefusal.None"/>. Checked in this
        /// order. Only the owner changes it, the first choice is free, and then it waits 30 days from the last change, whoever owned
        /// the house when that was.
        /// </summary>
        /// <param name="house">The house.</param>
        /// <param name="isInCity">Whether the house stands in a city or station.</param>
        /// <param name="playerId">The player asking.</param>
        /// <param name="newType">The specialization asked for.</param>
        /// <param name="workshopSkill">The crafting skill a Workshop is asked for.</param>
        /// <param name="nowUtc">The time now.</param>
        public static HouseSpecializationRefusal CheckChange(
            WorldProperty house,
            bool isInCity,
            string playerId,
            HouseSpecializationType newType,
            SkillType workshopSkill,
            DateTime nowUtc)
        {
            if (house == null || house.PropertyType != PropertyType.House)
                return HouseSpecializationRefusal.NotAHouse;
            if (!isInCity)
                return HouseSpecializationRefusal.NotInCity;
            if (string.IsNullOrWhiteSpace(playerId) || house.OwnerPlayerId != playerId)
                return HouseSpecializationRefusal.NotTheOwner;
            if (newType is not (HouseSpecializationType.Workshop or HouseSpecializationType.Storehouse or HouseSpecializationType.Greenhouse))
                return HouseSpecializationRefusal.NotASpecialization;
            if (newType == HouseSpecializationType.Workshop && !IsCraftingSkill(workshopSkill))
                return HouseSpecializationRefusal.NeedsCraftingSkill;
            if (newType == house.HouseSpecialization &&
                (newType != HouseSpecializationType.Workshop || workshopSkill == house.WorkshopSkill))
                return HouseSpecializationRefusal.Unchanged;

            var next = GetNextChangeUtc(house.HouseSpecializationChangedUtc);
            return next.HasValue && nowUtc < next.Value
                ? HouseSpecializationRefusal.TooSoon
                : HouseSpecializationRefusal.None;
        }

        /// <summary>
        /// Gives the house its new specialization and starts the 30 days. Leaving a Storehouse subtracts the storage it recorded;
        /// becoming one adds half the layout's own storage and records it. The storage furniture adds is never touched, so it
        /// survives every switch. The caller has checked <see cref="CheckChange"/> and saves the house once, so a restart cannot
        /// split the type from the storage.
        /// </summary>
        /// <param name="house">The house, changed in place.</param>
        /// <param name="newType">The new specialization.</param>
        /// <param name="workshopSkill">The Workshop's skill; ignored for any other type.</param>
        /// <param name="layoutStorageLimit">The house layout's own <c>ItemStorageLimit</c>, without furniture.</param>
        /// <param name="nowUtc">The time now.</param>
        public static void Apply(
            WorldProperty house,
            HouseSpecializationType newType,
            SkillType workshopSkill,
            int layoutStorageLimit,
            DateTime nowUtc)
        {
            if (house.HouseSpecialization == HouseSpecializationType.Storehouse)
            {
                house.ItemStorageCount = Math.Max(0, house.ItemStorageCount - house.StorehouseBonus);
                house.StorehouseBonus = 0;
            }

            if (newType == HouseSpecializationType.Storehouse)
            {
                var bonus = GetStorehouseBonus(layoutStorageLimit);
                house.ItemStorageCount += bonus;
                house.StorehouseBonus = bonus;
            }

            house.HouseSpecialization = newType;
            house.WorkshopSkill = newType == HouseSpecializationType.Workshop ? workshopSkill : SkillType.Invalid;
            house.HouseSpecializationChangedUtc = nowUtc;
        }

        #endregion

        #region Storehouse

        /// <summary>The storage a Storehouse adds: half the layout's own storage, so 20, 30 or 40 for a small, medium or large house.</summary>
        public static int GetStorehouseBonus(int layoutStorageLimit)
        {
            return (int)Math.Floor(Math.Max(0, layoutStorageLimit) * StorehouseShare);
        }

        /// <summary>Whether another item can go into storage. Over the limit after leaving a Storehouse, only withdrawals work.</summary>
        public static bool CanDeposit(int itemCount, int storageLimit)
        {
            return itemCount < storageLimit;
        }

        /// <summary>Withdrawals always work, however full the storage is.</summary>
        public static bool CanWithdraw()
        {
            return true;
        }

        #endregion

        #region Greenhouse

        /// <summary>
        /// Whether a garden plot is one a house can no longer plant: the house is not a Greenhouse and the plot is past the first 5, in
        /// the order they were placed. Crops already growing in it finish and can be harvested.
        /// </summary>
        /// <param name="plotIdsInPlacementOrder">The house's garden plots, oldest first.</param>
        /// <param name="plotId">The plot being planted.</param>
        /// <param name="isGreenhouse">Whether the house is a Greenhouse.</param>
        public static bool IsPlotAboveLimit(IReadOnlyList<string> plotIdsInPlacementOrder, string plotId, bool isGreenhouse)
        {
            if (isGreenhouse || plotIdsInPlacementOrder == null)
                return false;

            for (var index = 0; index < plotIdsInPlacementOrder.Count; index++)
            {
                if (plotIdsInPlacementOrder[index] == plotId)
                    return index >= FarmingRules.HousePlotLimit;
            }

            return false;
        }

        #endregion

        #region Workshop

        /// <summary>The Craftsmanship and Control a Workshop adds to a craft of <paramref name="craftSkill"/>: its bonus for its chosen skill, otherwise 0.</summary>
        public static int GetWorkshopBonus(HouseSpecializationType type, SkillType workshopSkill, SkillType craftSkill)
        {
            return type == HouseSpecializationType.Workshop && workshopSkill != SkillType.Invalid && workshopSkill == craftSkill
                ? WorkshopBonus
                : 0;
        }

        /// <summary>
        /// The Workshop bonus a Craftsmanship or Control calculation adds. While a craft of that skill is open it is the bonus recorded
        /// when the craft opened (none for a craft saved before Workshops existed), so a type change mid-craft changes nothing;
        /// otherwise it is the bonus of the house the player stands in.
        /// </summary>
        /// <param name="activeSession">The player's open craft, or null.</param>
        /// <param name="craftSkill">The skill being calculated.</param>
        /// <param name="liveBonus">The bonus of the house the player stands in now.</param>
        public static int ResolveWorkshopBonus(CraftSessionRecord activeSession, SkillType craftSkill, int liveBonus)
        {
            return activeSession != null && activeSession.Skill == craftSkill
                ? activeSession.WorkshopBonus ?? 0
                : liveBonus;
        }

        #endregion

        #region Text

        /// <summary>The specialization as a player reads it: "Workshop (Fabrication)", "Storehouse", "Greenhouse" or "None".</summary>
        public static string Describe(HouseSpecializationType type, SkillType workshopSkill)
        {
            switch (type)
            {
                case HouseSpecializationType.Workshop:
                    return workshopSkill == SkillType.Invalid
                        ? "Workshop"
                        : $"Workshop ({workshopSkill.GetAttribute<SkillType, SkillAttribute>().Name})";
                case HouseSpecializationType.Storehouse:
                    return "Storehouse";
                case HouseSpecializationType.Greenhouse:
                    return "Greenhouse";
                default:
                    return "None";
            }
        }

        /// <summary>What a specialization does, in a line.</summary>
        public static string DescribeEffect(HouseSpecializationType type)
        {
            switch (type)
            {
                case HouseSpecializationType.Workshop:
                    return $"Anyone crafting the chosen skill here gets +{WorkshopBonus} Craftsmanship and +{WorkshopBonus} Control.";
                case HouseSpecializationType.Storehouse:
                    return "The house's own storage grows by half. Storage from furniture is unchanged.";
                case HouseSpecializationType.Greenhouse:
                    return $"The house holds {FarmingRules.GreenhousePlotLimit} garden plots instead of {FarmingRules.HousePlotLimit}, and crops harvested here yield 1 more item.";
                default:
                    return "This house has no specialization.";
            }
        }

        /// <summary>The message a player reads when the change is refused.</summary>
        public static string Describe(HouseSpecializationRefusal refusal, DateTime? nextChangeUtc = null)
        {
            switch (refusal)
            {
                case HouseSpecializationRefusal.NotAHouse:
                    return "Only houses can have a specialization.";
                case HouseSpecializationRefusal.NotInCity:
                    return "Only a house in a city can have a specialization.";
                case HouseSpecializationRefusal.NotTheOwner:
                    return "Only the owner of this house can change its specialization.";
                case HouseSpecializationRefusal.NotASpecialization:
                    return "Choose a Workshop, a Storehouse or a Greenhouse.";
                case HouseSpecializationRefusal.NeedsCraftingSkill:
                    return "Choose the crafting skill the Workshop supports.";
                case HouseSpecializationRefusal.Unchanged:
                    return "The house already has that specialization.";
                case HouseSpecializationRefusal.TooSoon:
                    return nextChangeUtc.HasValue
                        ? $"The specialization can change again on {nextChangeUtc.Value:yyyy-MM-dd} UTC."
                        : $"The specialization can change once every {ChangeIntervalDays} days.";
                default:
                    return string.Empty;
            }
        }

        #endregion
    }
}
