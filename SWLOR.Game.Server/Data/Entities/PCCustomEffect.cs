using System.ComponentModel.DataAnnotations;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class PCCustomEffect
    {
        public PCCustomEffect()
        {
            Data = string.Empty;
        }
        public long PCCustomEffectID { get; set; }

        [Required]
        [StringLength(60)]
        public string PlayerID { get; set; }

        public long CustomEffectID { get; set; }

        public int Ticks { get; set; }

        public int EffectiveLevel { get; set; }

        public string CasterNWNObjectID { get; set; }

        [StringLength(32)]
        public string Data { get; set; }

        public int? StancePerkID { get; set; }

        public virtual CustomEffect CustomEffect { get; set; }

        public virtual PlayerCharacter PlayerCharacter { get; set; }
    }
}
