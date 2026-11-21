using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.TravelerService
{
    /// <summary>One traveler: who they are, how they look, and the pools their weekly stock is drawn from.</summary>
    public sealed class TravelerDetail
    {
        public TravelerDetail(TravelerType type)
        {
            Type = type;
        }

        public TravelerType Type { get; }

        /// <summary>The key of the traveler's records: the name of its <see cref="TravelerType"/> member.</summary>
        public string Key => Type.ToString();

        /// <summary>The name players see.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>What they do, such as "exotic beast trader". Used in the Gazette and the login notice.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>A short story, read from the conversation and on the creature's description.</summary>
        public string Story { get; set; } = string.Empty;

        /// <summary>What they say when spoken to.</summary>
        public string Greeting { get; set; } = string.Empty;

        /// <summary>
        /// The existing module creature template they are made from: a script-free Veles colonist of a playable species.
        /// Spawning clears the template's other event scripts, makes them plot and Neutral, and renames them.
        /// </summary>
        public string LookResref { get; set; } = string.Empty;

        /// <summary>The pools the weekly stock is drawn from.</summary>
        public List<TravelerStockGroup> Groups { get; } = new();

        /// <summary>Lines the traveler may let slip, for the ones who deal in rumors. Empty for the others.</summary>
        public List<string> Rumors { get; } = new();
    }
}
