using System.Collections.Generic;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.ValueObject
{
    public class DatabaseAction
    {
        public IList<IEntity> Data { get; set; }
        public DatabaseActionType Action { get; set; }

        public DatabaseAction(IEntity data, DatabaseActionType action)
        {
            Data = new List<IEntity>();
            Data.Add(data);
            Action = action;
        }

        public DatabaseAction(IEnumerable<IEntity> data, DatabaseActionType action)
        {
            Data = new List<IEntity>();
            foreach (var item in data)
            {
                Data.Add(item);
            }
            Action = action;
        }
    }
}
