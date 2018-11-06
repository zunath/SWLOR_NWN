

using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[DataPackage]")]
    public class DataPackage: IEntity
    {
        [ExplicitKey]
        public Guid ID { get; set; }
        public DateTime DateFound { get; set; }
        public DateTime DateExported { get; set; }
        public string FileName { get; set; }
        public string PackageName { get; set; }
        public string Checksum { get; set; }
        public string Content { get; set; }
        public bool ImportedSuccessfully { get; set; }
        public string ErrorMessage { get; set; }
    }
}
