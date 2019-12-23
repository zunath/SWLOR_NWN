using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class MessageCache: CacheBase<Message>
    {
        public MessageCache() 
            : base("Message")
        {
        }

        private const string ByBoardIDIndex = "ByBoardID";

        protected override void OnCacheObjectSet(Message entity)
        {
            SetIntoListIndex(ByBoardIDIndex, entity.BoardID.ToString(), entity);
        }

        protected override void OnCacheObjectRemoved(Message entity)
        {
            RemoveFromListIndex(ByBoardIDIndex, entity.BoardID.ToString(), entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public Message GetByID(Guid id)
        {
            return ByID(id);
        }

        public IEnumerable<Message> GetAllByBoardID(Guid boardID)
        {
            if(!ExistsByListIndex(ByBoardIDIndex, boardID.ToString()))
                return new List<Message>();

            return GetFromListIndex(ByBoardIDIndex, boardID.ToString());
        }
    }
}
