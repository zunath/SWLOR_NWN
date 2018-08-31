using System.ComponentModel.DataAnnotations;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class AuthorizedDM
    {
        public int AuthorizedDMID { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; }

        [Required]
        [StringLength(20)]
        public string CDKey { get; set; }

        public int DMRole { get; set; }

        public bool IsActive { get; set; }
    }
}
