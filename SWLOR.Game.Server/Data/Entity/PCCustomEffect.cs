
using System;

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCCustomEffect]")]
    public class PCCustomEffect: IEntity
    {
        public PCCustomEffect()
        {
            ID = Guid.NewGuid();
            Data = "";
            CasterNWNObjectID = "";
        }

        [ExplicitKey]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public int CustomEffectID { get; set; }
        public int Ticks { get; set; }
        public int EffectiveLevel { get; set; }
        public string Data { get; set; }
        public string CasterNWNObjectID { get; set; }
        public int? StancePerkID { get; set; }

        public IEntity Clone()
        {
            return new PCCustomEffect
            {
                ID = ID,
                PlayerID = PlayerID,
                CustomEffectID = CustomEffectID,
                Ticks = Ticks,
                EffectiveLevel = EffectiveLevel,
                Data = Data,
                CasterNWNObjectID = CasterNWNObjectID,
                StancePerkID = StancePerkID
            };
        }
    }
}
