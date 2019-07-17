

using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[DatabaseVersion]")]
    public class DatabaseVersion: IEntity
    {
        public DatabaseVersion()
        {
            ID = Guid.NewGuid();
        }
        [ExplicitKey]
        public Guid ID { get; set; }
        public string ScriptName { get; set; }
        public DateTime DateApplied { get; set; }
        public DateTime VersionDate { get; set; }
        public int VersionNumber { get; set; }

        public IEntity Clone()
        {
            return new DatabaseVersion
            {
                ID = ID,
                ScriptName = ScriptName,
                DateApplied = DateApplied,
                VersionDate = VersionDate,
                VersionNumber = VersionNumber
            };
        }
    }
}
