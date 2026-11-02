using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.HouseSpecializationService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// House specializations (Player City Revamp, "House specializations"): reads the type, chosen skill and last change from the
    /// house's property record, changes them for its owner, and answers the Workshop bonus a craft carries. The rules are
    /// <see cref="HouseSpecializationRules"/>. The record belongs to the house, so a change of owner keeps the type and the timer.
    /// </summary>
    public static class HouseSpecializations
    {
        /// <summary>Whether the property is a house with the Greenhouse specialization, which holds 10 garden plots and adds 1 to a harvest.</summary>
        public static bool IsGreenhouse(WorldProperty property)
        {
            return property is { PropertyType: PropertyType.House, HouseSpecialization: HouseSpecializationType.Greenhouse };
        }

        /// <summary>Whether the house stands in a city or station.</summary>
        public static bool IsInCity(WorldProperty house)
        {
            return house != null && !string.IsNullOrWhiteSpace(Property.GetCityIdForProperty(house.Id));
        }

        /// <summary>Why the player cannot give the house this specialization right now, or <see cref="HouseSpecializationRefusal.None"/>.</summary>
        public static HouseSpecializationRefusal CheckChange(
            WorldProperty house,
            string playerId,
            HouseSpecializationType newType,
            SkillType workshopSkill,
            DateTime? nowUtc = null)
        {
            return HouseSpecializationRules.CheckChange(house, IsInCity(house), playerId, newType, workshopSkill, nowUtc ?? DateTime.UtcNow);
        }

        /// <summary>
        /// Gives the house the specialization. The house is read again, so a second owner's window cannot apply a stale choice, and one
        /// save of the record holds the type, the timer and the storage together.
        /// </summary>
        /// <returns>The refusal, or <see cref="HouseSpecializationRefusal.None"/> when it changed.</returns>
        public static HouseSpecializationRefusal Change(string houseId, string playerId, HouseSpecializationType newType, SkillType workshopSkill)
        {
            var house = string.IsNullOrWhiteSpace(houseId) ? null : DB.Get<WorldProperty>(houseId);
            var now = DateTime.UtcNow;
            var refusal = CheckChange(house, playerId, newType, workshopSkill, now);
            if (refusal != HouseSpecializationRefusal.None)
                return refusal;

            var previous = HouseSpecializationRules.Describe(house.HouseSpecialization, house.WorkshopSkill);
            HouseSpecializationRules.Apply(house, newType, workshopSkill, GetLayoutStorage(house), now);
            DB.Set(house);

            Log.Write(
                LogGroup.Property,
                $"House '{house.CustomName}' ({house.Id}) changed from {previous} to {HouseSpecializationRules.Describe(newType, workshopSkill)}; item storage is now {house.ItemStorageCount}.");
            return HouseSpecializationRefusal.None;
        }

        /// <summary>The house layout's own storage limit, without furniture.</summary>
        private static int GetLayoutStorage(WorldProperty house)
        {
            return Property.GetLayoutByType(house.Layout)?.ItemStorageLimit ?? 0;
        }

        /// <summary>
        /// The houses' plots in the order they were placed, oldest first. A Greenhouse that went back to a house plants only the first 5.
        /// </summary>
        public static IReadOnlyList<string> GetPlotIdsInPlacementOrder(WorldProperty house)
        {
            if (house == null || !house.ChildPropertyIds.TryGetValue(PropertyChildType.Structure, out var children))
                return new List<string>();

            return children
                .Where(id => DB.Get<WorldProperty>(id) is { StructureType: StructureType.GardenPlot })
                .ToList();
        }

        /// <summary>
        /// Whether the garden plot is one its house can no longer plant: the house has lost its Greenhouse and the plot is past the first
        /// 5. A crop already growing there finishes and can be harvested.
        /// </summary>
        public static bool IsPlotAboveLimit(WorldProperty plot)
        {
            var house = plot == null || string.IsNullOrWhiteSpace(plot.ParentPropertyId) ? null : DB.Get<WorldProperty>(plot.ParentPropertyId);
            if (house is not { PropertyType: PropertyType.House })
                return false;

            return HouseSpecializationRules.IsPlotAboveLimit(GetPlotIdsInPlacementOrder(house), plot.Id, IsGreenhouse(house));
        }

        #region Workshop

        /// <summary>The Workshop bonus of the house the player stands in for a craft of the skill, or 0.</summary>
        public static int GetLiveWorkshopBonus(uint player, SkillType craftSkill)
        {
            var propertyId = Property.GetPropertyId(GetArea(player));
            var house = string.IsNullOrWhiteSpace(propertyId) ? null : DB.Get<WorldProperty>(propertyId);
            if (house is not { PropertyType: PropertyType.House })
                return 0;

            return HouseSpecializationRules.GetWorkshopBonus(house.HouseSpecialization, house.WorkshopSkill, craftSkill);
        }

        /// <summary>
        /// The Craftsmanship and Control a Workshop adds to the player's calculation for the skill: the bonus recorded when their open
        /// craft started, or that of the house they stand in when none is open.
        /// </summary>
        public static int GetWorkshopBonus(uint player, Player dbPlayer, SkillType craftSkill)
        {
            // The house is only looked up when no craft of the skill is open, since an open craft ignores it.
            var session = dbPlayer?.ActiveCraftSession;
            var hasOpenCraft = session != null && session.Skill == craftSkill;
            return HouseSpecializationRules.ResolveWorkshopBonus(session, craftSkill, hasOpenCraft ? 0 : GetLiveWorkshopBonus(player, craftSkill));
        }

        #endregion
    }
}
