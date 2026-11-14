using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.ClerkRecoveryService
{
    /// <summary>One crop or animal the clerk can act on for a player, with the actions it allows right now.</summary>
    public sealed class ClerkRecoveryOffer
    {
        public ClerkRecoveryOffer(
            ClerkRecoverySourceKind kind,
            string sourceId,
            string name,
            string title,
            string description,
            IReadOnlyList<ClerkRecoveryAction> actions)
        {
            Kind = kind;
            SourceId = sourceId;
            Name = name;
            Title = title;
            Description = description;
            Actions = actions;
        }

        public ClerkRecoverySourceKind Kind { get; }

        /// <summary>The garden plot's property id for a crop, or the animal's record id.</summary>
        public string SourceId { get; }

        /// <summary>The crop's or animal's name.</summary>
        public string Name { get; }

        /// <summary>One line for the list: the name and the stage.</summary>
        public string Title { get; }

        /// <summary>The state of the crop or animal, for its page.</summary>
        public string Description { get; }

        public IReadOnlyList<ClerkRecoveryAction> Actions { get; }
    }
}
