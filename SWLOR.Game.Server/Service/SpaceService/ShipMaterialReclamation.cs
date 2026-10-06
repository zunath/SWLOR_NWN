using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
namespace SWLOR.Game.Server.Service.SpaceService
{
    public static class ShipMaterialReclamation
    {
        public static bool Apply(SpaceEconomyLedger ledger,string identity,string oldResref,ShipItemProvenance verified)
        {
            if(verified==null||verified.Resref!=oldResref)return false;
            var recipe=SpaceEconomyCatalog.Default.LegacyRefunds.GetValueOrDefault(oldResref);
            if(recipe==null)return false;
            if(!ledger.Receipts.Add("reclaim/"+identity))return true;
            foreach(var component in verified.RecipeMaterials)
                Add(component.Key,Math.Min(Math.Max(0,component.Value),recipe.Components.GetValueOrDefault(component.Key))*recipe.Fraction);
            var fraction=verified.EnhancementGrade>50?.8*(verified.EnhancementGrade-50)/verified.EnhancementGrade:0;
            foreach(var component in verified.EnhancementMaterials)Add(component.Key,Math.Max(0,component.Value)*fraction);
            return true;
            void Add(string resref,double amount)
            {
                if(amount>0)ledger.MaterialRecovery[resref]=ledger.MaterialRecovery.GetValueOrDefault(resref)+amount;
            }
        }
    }
}
