using System.Collections.Generic;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// What one candidate stands for: the Edicts they promise to enact if they win outright, and a statement. A
    /// candidate with no entry on <see cref="Election.Platforms"/> made no platform at all (an election that was already
    /// open when platforms arrived), which differs from a platform with no Edicts: the first leaves the city's Edicts as
    /// they are when it wins, the second replaces them with none.
    /// </summary>
    public class ElectionPlatform
    {
        public ElectionPlatform()
        {
            Statement = string.Empty;
            Edicts = new List<ElectionPlatformEdict>();
        }

        /// <summary>The candidate's statement, at most <c>ElectionRules.StatementMaxLength</c> characters.</summary>
        public string Statement { get; set; }

        /// <summary>
        /// The Edicts promised, in Edict order, at most one of each type and no more than the city's slot count when the
        /// platform was saved.
        /// </summary>
        public List<ElectionPlatformEdict> Edicts { get; set; }
    }
}
