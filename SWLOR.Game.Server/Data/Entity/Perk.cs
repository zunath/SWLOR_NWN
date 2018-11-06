
using System;
using System.Collections.Generic;

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[Perks]")]
    public class Perk: IEntity
    {
        public Perk()
        {
            Name = "";
            ScriptName = "";
            Description = "";
        }

        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }
        public int? FeatID { get; set; }
        public bool IsActive { get; set; }
        public string ScriptName { get; set; }
        public int BaseFPCost { get; set; }
        public double BaseCastingTime { get; set; }
        public string Description { get; set; }
        public int PerkCategoryID { get; set; }
        public int? CooldownCategoryID { get; set; }
        public int ExecutionTypeID { get; set; }
        public string ItemResref { get; set; }
        public bool IsTargetSelfOnly { get; set; }
        public int Enmity { get; set; }
        public int EnmityAdjustmentRuleID { get; set; }
        public int? CastAnimationID { get; set; }
    }
}
