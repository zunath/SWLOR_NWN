using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.TownsfolkService
{
    /// <summary>
    /// The looks street life is made from. Every look is an existing module creature template with no AI scripts, so a
    /// townsperson spawned from it does nothing until the city tells it to walk. Spawning clears the template's
    /// dialogue script, makes it plot in the Neutral faction, and renames it.
    /// </summary>
    public static class TownsfolkLooks
    {
        /// <summary>The name every wandering townsperson shows.</summary>
        public const string TownsfolkName = "Townsperson";

        /// <summary>The name every guard shows.</summary>
        public const string GuardName = "City Guard";

        /// <summary>
        /// The Veles colonists (the templates dress playable species in plain colonist clothes) and the cantina patrons.
        /// Both are unarmed civilians.
        /// </summary>
        public static readonly IReadOnlyList<TownsfolkLook> Wanderers = new List<TownsfolkLook>
        {
            new("colonisthuman1", TownsfolkName),
            new("colonisthuman2", TownsfolkName),
            new("colonistwq", TownsfolkName),
            new("colonistkeldor", TownsfolkName),
            new("colonistmoncala", TownsfolkName),
            new("colonistbith", TownsfolkName),
            new("colonistbothan", TownsfolkName),
            new("colonistbothan2", TownsfolkName),
            new("colonistduro", TownsfolkName),
            new("colonistgran", TownsfolkName),
            new("colonistrodian", TownsfolkName),
            new("colonistrodian2", TownsfolkName),
            new("colonisttrando", TownsfolkName),
            new("colonisttrando2", TownsfolkName),
            new("colonisttwilek", TownsfolkName),
            new("colonisttwilek3", TownsfolkName),
            new("malepatron1", TownsfolkName),
            new("malepatron2", TownsfolkName),
            new("malepatron3", TownsfolkName),
            new("femalepatron1", TownsfolkName),
            new("femalepatron2", TownsfolkName),
            new("femalepatron3", TownsfolkName),
            new("femalepatron4", TownsfolkName),
            new("femalepatron5", TownsfolkName),
        };

        /// <summary>The Czerka security uniform with a club, in both genders. Scenery only.</summary>
        public static readonly IReadOnlyList<TownsfolkLook> Guards = new List<TownsfolkLook>
        {
            new("maleczerkaguard", GuardName),
            new("femczerkaguard", GuardName),
        };

        /// <summary>The template every critter is made from: an animal with no scripts, no loot, and no beast type.</summary>
        public const string CritterTemplate = "night_bantha";

        /// <summary>
        /// Small harmless animals: the gizka (the SWLOR gizka model) and the womp rat (the capybara model the womp rat
        /// template uses). Each uses an appearance row an existing creature template already uses, on the script-free
        /// <see cref="CritterTemplate"/>, so no loot table, quest group, or beast type comes along with the look.
        /// </summary>
        public static readonly IReadOnlyList<TownsfolkLook> Critters = new List<TownsfolkLook>
        {
            new(CritterTemplate, "Gizka", 10040),
            new(CritterTemplate, "Womp Rat", 1438),
        };

        /// <summary>The looks for a group. Empty for anything that is not street life.</summary>
        public static IReadOnlyList<TownsfolkLook> Get(TownsfolkGroup group)
        {
            switch (group)
            {
                case TownsfolkGroup.Wanderer:
                    return Wanderers;
                case TownsfolkGroup.Guard:
                    return Guards;
                case TownsfolkGroup.Critter:
                    return Critters;
                default:
                    return new List<TownsfolkLook>();
            }
        }
    }
}
