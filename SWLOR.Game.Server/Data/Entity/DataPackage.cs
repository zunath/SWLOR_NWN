

using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[DataPackage]")]
    public class DataPackage: IEntity
    {
        public DataPackage()
        {
            ID = Guid.NewGuid();
        }
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

        public DataPackage Clone()
        {
            return new DataPackage
            {
                ID = ID,
                DateFound = DateFound,
                DateExported = DateExported,
                FileName = FileName,
                PackageName = PackageName,
                Checksum = Checksum,
                Content = Content,
                ImportedSuccessfully = ImportedSuccessfully,
                ErrorMessage = ErrorMessage
            };
        }
    }
}
