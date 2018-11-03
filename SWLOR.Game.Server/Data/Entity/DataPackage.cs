
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("DataPackages")]
    public partial class DataPackage: IEntity
    {
        [Key]
        public int DataPackageID { get; set; }
        public System.DateTime DateFound { get; set; }
        public System.DateTime DateExported { get; set; }
        public string FileName { get; set; }
        public string PackageName { get; set; }
        public string Checksum { get; set; }
        public string Content { get; set; }
        public bool ImportedSuccessfully { get; set; }
        public string ErrorMessage { get; set; }
    }
}
