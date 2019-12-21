using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    public class Error: IEntity
    {
        public Error()
        {
            ID = Guid.NewGuid();
        }

        [Key]
        public Guid ID { get; set; }
        public DateTime DateCreated { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public string Caller { get; set; }

        public IEntity Clone()
        {
            return new Error
            {
                ID = ID,
                DateCreated = DateCreated,
                Message = Message,
                StackTrace = StackTrace,
                Caller = Caller
            };
        }
    }
}
