using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[SkillCategory]")]
    public class SkillCategory: IEntity
    {
        public SkillCategory()
        {
            Name = "";
        }

        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public int Sequence { get; set; }

        public IEntity Clone()
        {
            return new SkillCategory
            {
                ID = ID,
                Name = Name,
                IsActive = IsActive,
                Sequence = Sequence
            };
        }
    }
}
