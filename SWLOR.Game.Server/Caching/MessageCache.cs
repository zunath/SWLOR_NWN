using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class MessageCache: CacheBase<Message>
    {
        private Dictionary<Guid, Dictionary<Guid, Message>> ByBoardID { get; } = new Dictionary<Guid, Dictionary<Guid, Message>>();

        protected override void OnCacheObjectSet(Message entity)
        {
            SetEntityIntoDictionary(entity.BoardID, entity.ID, entity, ByBoardID);
        }

        protected override void OnCacheObjectRemoved(Message entity)
        {
            RemoveEntityFromDictionary(entity.BoardID, entity.ID, ByBoardID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public Message GetByID(Guid id)
        {
            return (Message)ByID[id].Clone();
        }

        public IEnumerable<Message> GetAllByBoardID(Guid boardID)
        {
            if(!ByBoardID.ContainsKey(boardID))
                return new List<Message>();

            var list = new List<Message>();
            foreach (var message in ByBoardID[boardID].Values)
            {
                list.Add((Message)message.Clone());
            }

            return list;
        }
    }
}
