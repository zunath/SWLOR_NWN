using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    [Table("ServerConfiguration")]
    public partial class ServerConfiguration
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ServerConfigurationID { get; set; }

        [Required]
        [StringLength(50)]
        public string ServerName { get; set; }

        [Required]
        [StringLength(1024)]
        public string MessageOfTheDay { get; set; }

        public int AreaBakeStep { get; set; }
    }
}
