using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.ValueObject
{
    public class DatabaseAction
    {
        public IEntity Data { get; set; }
        public DatabaseActionType Action { get; set; }

        public DatabaseAction(IEntity data, DatabaseActionType action)
        {
            Data = data;
            Action = action;
        }
    }
}
