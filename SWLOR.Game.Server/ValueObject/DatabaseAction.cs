using System;
using System.Collections;
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

        public DatabaseAction(IEnumerable<IEntity> data, DatabaseActionType action)
        {
            Data = new List<IEntity>();
            foreach (var item in data)
            {
                if (DataType == null)
                {
                    DataType = item.GetType();
                }
                else if (item.GetType() != DataType)
                {
                    throw new InvalidOperationException("All objects in a single database action must be of the same type.");
                }

                Data.Add(item);
            }
            Action = action;

            IsEnumerable = true;
        }
    }
}
