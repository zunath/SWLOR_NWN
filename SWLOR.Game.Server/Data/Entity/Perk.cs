using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[Perk]")]
    public class Perk: IEntity
    {
        public Perk()
        {
            Name = "";
            Description = "";
        }

        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public double BaseCastingTime { get; set; }
        public string Description { get; set; }
        public int PerkCategoryID { get; set; }
        public int? CooldownCategoryID { get; set; }
        public PerkExecutionType ExecutionTypeID { get; set; }
        public string ItemResref { get; set; }
        public bool IsTargetSelfOnly { get; set; }
        public int Enmity { get; set; }
        public int EnmityAdjustmentRuleID { get; set; }
        public int? CastAnimationID { get; set; }
        public SpecializationType Specialization { get; set; }
        public ForceBalanceType ForceBalance { get; set; }

        public IEntity Clone()
        {
            return new Perk
            {
                ID = ID,
                Name = Name,
                IsActive = IsActive,
                BaseCastingTime = BaseCastingTime,
                Description = Description,
                PerkCategoryID = PerkCategoryID,
                CooldownCategoryID = CooldownCategoryID,
                ExecutionTypeID = ExecutionTypeID,
                ItemResref = ItemResref,
                IsTargetSelfOnly = IsTargetSelfOnly,
                Enmity = Enmity,
                EnmityAdjustmentRuleID = EnmityAdjustmentRuleID,
                CastAnimationID = CastAnimationID,
                Specialization = Specialization,
                ForceBalance = ForceBalance
            };
        }
    }
}
