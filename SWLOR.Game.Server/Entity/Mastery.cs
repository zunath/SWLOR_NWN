using SWLOR.Game.Server.Service.MasteryService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// A catalog entry for the Masteries system. Represents a rank-1-through-5 roleplay
    /// specialization a character can request and train into over real-world time.
    /// No combat/mechanical effects are associated with a Mastery - see MASTERY_SPEC.md.
    /// </summary>
    public class Mastery: EntityBase
    {
        public Mastery()
        {
            Name = string.Empty;
            Description = string.Empty;
            IsActive = true;
            IsSeeded = false;
            SeedKey = string.Empty;
        }

        [Indexed]
        public string Name { get; set; }
        [Indexed]
        public MasteryCategoryType Category { get; set; }
        public string Description { get; set; }
        [Indexed]
        public MasteryRarityType Rarity { get; set; }

        /// <summary>
        /// The skill which must be rank 50 before a character may request this mastery.
        /// Null indicates there is no skill rank requirement.
        /// </summary>
        public SkillType? AssociatedSkill { get; set; }

        /// <summary>
        /// Soft-delete/retire flag. Retired masteries are hidden from the catalog but
        /// left in the database so existing player records referencing them stay valid.
        /// </summary>
        [Indexed]
        public bool IsActive { get; set; }

        /// <summary>
        /// True if this entry came from the initial mastery-catalog.json seed. False if
        /// it was created by staff after launch. Purely informational.
        /// </summary>
        public bool IsSeeded { get; set; }

        /// <summary>
        /// Immutable key set once at creation to the seed entry's Name, used to match this
        /// row against <see cref="MasteryService.MasteryCatalogSeed"/> on startup. Never
        /// changes even if staff later rename <see cref="Name"/> via the catalog
        /// management screen - see <see cref="MasteryService.MasteryRules.BuildMissingCatalogEntries"/>,
        /// which matches on this first (falling back to Name for older rows created before
        /// this field existed) so a renamed seeded row is never mistaken for "missing" and
        /// re-seeded as a duplicate. Empty for staff-created rows, which are never subject
        /// to seed matching at all.
        /// </summary>
        public string SeedKey { get; set; }
    }
}
