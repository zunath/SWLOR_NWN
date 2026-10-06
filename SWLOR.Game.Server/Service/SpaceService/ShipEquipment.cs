using System;
using System.Globalization;
using System.Linq;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public static class ShipEquipment
    {
        public const string IdentityVariable = "SHIP_ITEM_ID";
        private const string RecastVariable = "SHIP_RECAST_AT";

        public static ShipStatus.ShipStatusModule Read(uint item)
        {
            var catalog = ShipFittingCatalog.Default;
            var tag = GetTag(item);
            var design = catalog.DesignByItemTag(tag);
            if (design == null) throw new InvalidOperationException("Choose recognized ship equipment from your inventory.");
            var legacy = catalog.LegacyModules.ContainsKey(tag);
            if (!legacy && (GetLocalInt(item, "SHIP_FITTING") != 1 || GetLocalString(item, "SHIP_DESIGN") != design))
                throw new InvalidOperationException("Ship equipment identity does not match its blueprint.");
            var identity = GetLocalString(item, IdentityVariable);
            if (string.IsNullOrEmpty(identity)) identity = GetObjectUUID(item);
            SetLocalString(item, IdentityVariable, identity);
            var configuration = catalog.Configurations.TryGetValue(design, out var config);
            var calibration = legacy || configuration ? "Standard" : GetLocalString(item, "SHIP_CALIBRATION");
            var condition = legacy ? 100 : GetLocalInt(item, "SHIP_CONDITION");
            if (condition < 0 || condition > 100) throw new InvalidOperationException("Invalid equipment condition.");
            var dimension = ShipQualityDimension.None;
            var quality = 0;
            var oldGrade = 0;
            if (legacy)
            {
                oldGrade = Space.GetModuleBonus(item);
                quality = (int)Math.Min(100L, (long)Math.Max(0, oldGrade) * 2);
                dimension = configuration ? ShipQualityDimension.Output : PrimaryQuality(catalog.Modules[design].QualityDimensions);
            }
            else
            {
                var properties = 0;
                for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetNextItemProperty(item))
                {
                    if (GetItemPropertyType(property) != ItemPropertyType.ModuleBonus) continue;
                    if (++properties > 1) throw new InvalidOperationException("Ship equipment permits one refinement property.");
                    dimension = ShipRefinement.Dimension(GetItemPropertySubType(property));
                    quality = GetItemPropertyCostTableValue(property);
                    if (quality < 1 || quality > 100) throw new InvalidOperationException("Invalid ship refinement magnitude.");
                }
                if (!configuration) ShipModuleTuning.Refine(catalog.Modules[design], catalog.GetVariant(design, calibration), dimension, quality);
                else if (dimension != ShipQualityDimension.None && dimension != ShipQualityDimension.Output)
                    throw new InvalidOperationException("Configurations permit one output refinement.");
            }
            var recast = DateTime.TryParse(GetLocalString(item, RecastVariable), CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var parsed) ? parsed : DateTime.MinValue;
            var serialized = ObjectPlugin.Serialize(item);
            return new() { ItemInstanceId = identity, ItemTag = configuration ? config.ItemTag : catalog.Modules[design].ItemTag,
                SerializedItem = serialized, OriginalSerializedItem = legacy ? serialized : null,
                Design = design, Calibration = calibration, QualityDimension = dimension, Quality = quality,
                ModuleBonus = oldGrade, Condition = condition, RecastTime = recast };
        }

        public static uint CreateForWithdrawal(ShipStatus.ShipStatusModule equipment, uint player)
        {
            var catalog = ShipFittingCatalog.Default;
            uint item;
            if (catalog.Modules.TryGetValue(equipment.Design ?? "", out var profile))
                item = CreateItemOnObject(catalog.GetVariant(profile.Id, equipment.Calibration).ItemResref, player);
            else if (catalog.Configurations.TryGetValue(equipment.Design ?? "", out var configuration))
                item = CreateItemOnObject(configuration.ItemResref, player);
            else
            {
                item = ObjectPlugin.Deserialize(equipment.SerializedItem);
                ObjectPlugin.AcquireItem(player, item);
            }
            if (!GetIsObjectValid(item) || GetItemPossessor(item) != player)
            {
                if (GetIsObjectValid(item)) DestroyObject(item);
                throw new InvalidOperationException("Unable to deliver equipment to your inventory.");
            }
            SetLocalString(item, IdentityVariable, equipment.ItemInstanceId);
            ObjectPlugin.ForceAssignUUID(item, equipment.ItemInstanceId);
            SetLocalString(item, RecastVariable, equipment.RecastTime.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
            if (!string.IsNullOrEmpty(equipment.Design))
            {
                SetLocalInt(item, "SHIP_CONDITION", equipment.Condition);
                SetLocalInt(item, "SHIP_QUALITY_DIM", (int)equipment.QualityDimension);
                SetLocalInt(item, "SHIP_QUALITY", equipment.Quality);
                if (equipment.Quality > 0 && equipment.QualityDimension != ShipQualityDimension.None)
                    AddItemProperty(DurationType.Permanent,
                        ItemPropertyCustom(ItemPropertyType.ModuleBonus, ShipRefinement.Subtype(equipment.QualityDimension), equipment.Quality), item);
            }
            return item;
        }

        public static ShipQualityDimension PrimaryQuality(ShipQualityDimension eligible) =>
            new[] { ShipQualityDimension.Output, ShipQualityDimension.RecoveryFraction, ShipQualityDimension.Tracking, ShipQualityDimension.Range }
                .FirstOrDefault(dimension => eligible.HasFlag(dimension));
    }
}
