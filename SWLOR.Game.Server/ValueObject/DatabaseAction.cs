using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.ValueObject
{
    public class DatabaseAction
    {
        public IList<IEntity> Data { get; set; }
        public DatabaseActionType Action { get; set; }
        public Type DataType { get; }
        public bool IsEnumerable { get; }

        public DatabaseAction(IEntity data, DatabaseActionType action)
        {
            Data = new List<IEntity>();
            Data.Add(data);
            DataType = data.GetType();
            Action = action;
            IsEnumerable = false;
        }

    }
}