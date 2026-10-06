using System.Collections.Generic;
namespace SWLOR.Game.Server.Entity
{
    public sealed class ShipItemAudit : EntityBase
    {
        public string OriginalItem {get;set;}
        public string OriginalResref {get;set;}
        public string OwnerPlayerId {get;set;}
        public bool Converted {get;set;}
        public bool Reclaimed {get;set;}
        public string ReclamationNote {get;set;}
    }
    public sealed class ShipItemProvenance : EntityBase
    {
        public string Resref {get;set;}
        public Dictionary<string,int> RecipeMaterials {get;set;} = new();
        public int EnhancementGrade {get;set;}
        public Dictionary<string,int> EnhancementMaterials {get;set;} = new();
    }
}
