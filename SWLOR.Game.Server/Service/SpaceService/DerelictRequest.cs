namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// What <see cref="DerelictRules.GetBoardRefusal"/> and <see cref="DerelictRules.GetSalvageRefusal"/> check when a pilot
    /// targets a derelict with <c>/board</c> or <c>/salvage</c>.
    /// </summary>
    public sealed class DerelictRequest
    {
        /// <summary>The target is a derelict site's wreck.</summary>
        public bool IsDerelict { get; init; }

        public bool Boardable { get; init; }

        /// <summary>Its strongbox was opened or a salvage team stripped it.</summary>
        public bool Looted { get; init; }

        /// <summary>The pilot's party holds 100% on the derelict's signature.</summary>
        public bool PartyLocated { get; init; }

        public ShipSize ShipSize { get; init; }

        /// <summary>The distance from the ship to the derelict in meters; <see cref="float.MaxValue"/> in another area.</summary>
        public float Distance { get; init; }

        public bool InCombat { get; init; }

        /// <summary>The derelict's turrets or defense platforms are still up.</summary>
        public bool DefensesLeft { get; init; }

        /// <summary>What already works the derelict: nothing, a boarding or a salvage team.</summary>
        public DerelictMode Underway { get; init; }

        /// <summary>The boarding underway belongs to the pilot's party.</summary>
        public bool SameParty { get; init; }

        /// <summary>The ships docked at the derelict already.</summary>
        public int DockedCount { get; init; }

        /// <summary>The droids posted aboard the ship (stations and droid bay).</summary>
        public int PostedDroids { get; init; }

        /// <summary>The ship's salvage team is out stripping a derelict, this one or another.</summary>
        public bool SalvageTeamOut { get; init; }
    }
}
