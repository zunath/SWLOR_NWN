using System;

namespace SWLOR.Game.Server.Service.ProspectingService
{
    /// <summary>
    /// Everything a Prospecting session needs from the game engine. <see cref="ProspectingLifecycle"/> holds the rules and calls only
    /// this, so they run without the engine; <c>NwnProspectingWorld</c> is the engine implementation.
    /// </summary>
    public interface IProspectingWorld
    {
        /// <summary>The text a node was stored as (<c>ProspectingNode.Serialize</c>), or null for a node nobody has worked.</summary>
        string LoadNode(uint node);

        /// <summary>Stores the node's content, so the next session starts where this one left it.</summary>
        void SaveNode(uint node, string content);

        /// <summary>True when the node is still in the world.</summary>
        bool NodeExists(uint node);

        /// <summary>The share of a roll of the node's loot table that is a gem, which sets how often its seams are gem pockets. 0 when it has no gem.</summary>
        double GemChance(string lootTable);

        /// <summary>Removes a node that is spent or has collapsed, and the prop that stands for it.</summary>
        void DestroyNode(uint node);

        /// <summary>The charges the harvester has left.</summary>
        int GetCharges(uint harvester);

        /// <summary>Sets the harvester's charges.</summary>
        void SetCharges(uint harvester, int charges);

        /// <summary>Destroys a harvester that has no charges left, as it always has been.</summary>
        void DestroyHarvester(uint harvester);

        /// <summary>Puts a harvest in the player's inventory.</summary>
        void GiveHarvest(uint player, uint node, string lootTable, HarvestDelivery delivery);

        /// <summary>The player's Gathering rank now, which sets the XP of each unit.</summary>
        int GetGatheringRank(uint player);

        void GiveGatheringXp(uint player, int xp);

        void Message(uint player, string text);

        /// <summary>Marks the player busy prospecting, so they cannot start another activity until the session is over.</summary>
        void BeginSession(uint player);

        /// <summary>Clears the busy mark the session set.</summary>
        void EndSession(uint player);

        /// <summary>Plays the harvest animation and the timing bar for the seconds an action takes.</summary>
        void PlayWork(uint player, float seconds);

        /// <summary>Opens the Prospecting window. Returns false when it could not be opened.</summary>
        bool OpenWindow(uint player, ProspectingSession session);

        void CloseWindow(uint player);

        ProspectingPlayerState GetPlayerState(uint player, uint harvester);

        /// <summary>The current time, which tells how long a session has sat idle.</summary>
        DateTime UtcNow { get; }

        /// <summary>Runs <paramref name="action"/> after the given seconds, whether or not the player is still in the game.</summary>
        void Schedule(float seconds, Action action);
    }
}
