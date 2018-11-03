
using System;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("PCCustomEffects")]
    public partial class PCCustomEffect: IEntity
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public PCCustomEffect()
        {
            this.Data = "";
            this.CasterNWNObjectID = "";
        }

        [Key]
        public long PCCustomEffectID { get; set; }
        public string PlayerID { get; set; }
        public long CustomEffectID { get; set; }
        public int Ticks { get; set; }
        public int EffectiveLevel { get; set; }
        public string Data { get; set; }
        public string CasterNWNObjectID { get; set; }
        public Nullable<int> StancePerkID { get; set; }
    
        public virtual PlayerCharacter PlayerCharacter { get; set; }
        public virtual CustomEffect CustomEffect { get; set; }
    }
}
