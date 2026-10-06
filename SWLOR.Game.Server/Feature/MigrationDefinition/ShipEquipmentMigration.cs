using System;
using System.Linq;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
namespace SWLOR.Game.Server.Feature.MigrationDefinition
{
    internal static class ShipEquipmentMigration
    {
        public static bool MigrateObject(uint obj)
        {
            var changed=false;
            if(GetObjectType(obj)==ObjectType.Item)changed=ConvertItem(obj);
            for(var item=GetFirstItemInInventory(obj);GetIsObjectValid(item);item=GetNextItemInInventory(obj))changed|=ConvertItem(item);
            return changed;
        }
        private static bool ConvertItem(uint item)
        {
            var catalog=ShipFittingCatalog.Default;
            if(GetLocalInt(item,"SHIP_FITTING")==1||!catalog.LegacyModules.TryGetValue(GetTag(item),out var legacy))return false;
            var identity=GetObjectUUID(item);var auditId="ship-item/"+identity;
            var audit=DB.Get<ShipItemAudit>(auditId)??new(){Id=auditId,OriginalItem=ObjectPlugin.Serialize(item),OriginalResref=GetResRef(item)};
            DB.Set(audit);
            var configuration=catalog.Configurations.GetValueOrDefault(legacy.Target);
            var profile=catalog.Modules.GetValueOrDefault(legacy.Target);
            var eligible=configuration!=null?ShipQualityDimension.Output:profile.QualityDimensions;
            var quality=(int)Math.Min(100L,Math.Max(0,Space.GetModuleBonus(item))*2L);
            var dimension=ShipEquipment.PrimaryQuality(eligible);
            var properties=new List<SWLOR.NWN.API.Engine.ItemProperty>();
            for(var ip=GetFirstItemProperty(item);GetIsItemPropertyValid(ip);ip=GetNextItemProperty(item))
                if(GetItemPropertyType(ip) is ItemPropertyType.ModuleBonus or ItemPropertyType.UseLimitationPerk or ItemPropertyType.RequiresSkill)properties.Add(ip);
            foreach(var ip in properties)RemoveItemProperty(item,ip);
            SetTag(item,configuration?.ItemTag??profile.ItemTag);SetName(item,configuration?.Name??profile.Name);
            SetLocalInt(item,"SHIP_FITTING",1);SetLocalString(item,"SHIP_DESIGN",legacy.Target);SetLocalString(item,"SHIP_CALIBRATION","Standard");
            SetLocalInt(item,"SHIP_CONDITION",100);SetLocalInt(item,"SHIP_QUALITY_DIM",(int)dimension);SetLocalInt(item,"SHIP_QUALITY",quality);SetLocalString(item,ShipEquipment.IdentityVariable,identity);
            if(quality>0&&dimension!=ShipQualityDimension.None)AddItemProperty(DurationType.Permanent,ItemPropertyCustom(ItemPropertyType.ModuleBonus,ShipRefinement.Subtype(dimension),quality),item);
            audit.Converted=true;DB.Set(audit);return true;
        }
        public static void ReclaimForOwner(uint player)
        {
            var record=DB.Get<Player>(GetObjectUUID(player));
            for(var item=GetFirstItemInInventory(player);GetIsObjectValid(item);item=GetNextItemInInventory(player))
            {
                var audit=DB.Get<ShipItemAudit>("ship-item/"+GetObjectUUID(item));if(audit==null||audit.Reclaimed)continue;
                var source=DB.Get<ShipItemProvenance>("ship-provenance/"+GetObjectUUID(item));
                var reclaimed=ShipMaterialReclamation.Apply(record.SpaceEconomy,GetObjectUUID(item),audit.OriginalResref,source);
                if(reclaimed)DB.Set(record);
                audit.Reclaimed=reclaimed;audit.OwnerPlayerId=record.Id;
                audit.ReclamationNote=reclaimed?"Verified materials credited with fractions retained.":"No verified recipe/enhancement material source; original serialized item retained for audit.";DB.Set(audit);
            }
        }
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void MigrateOnLogin()
        {var player=GetEnteringObject();if(GetIsPC(player)&&!GetIsDM(player)){MigrateObject(player);ReclaimForOwner(player);}}
        [NWNEventHandler(ScriptName.OnModuleAcquire)]
        public static void MigrateOnAcquire()
        {var player=GetModuleItemAcquiredBy();var item=GetModuleItemAcquired();if(GetIsPC(player)&&!GetIsDM(player)){ConvertItem(item);ReclaimForOwner(player);}}
    }
}
