using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// The saved bars and their windows. A part of the ExtraHotbars service.
    /// </summary>
    public interface IExtraHotbarsWindowService
    {
        GuiWindowType GetWindowType(int barIndex);

        int GetBarIndex(GuiWindowType windowType);

        /// <summary>
        /// Returns the player's bars, adding any that are missing so every bar has a full set of slots.
        /// </summary>
        List<ExtraHotbar> GetBars(Player dbPlayer);

        ExtraHotbar GetBar(uint player, int barIndex);

        /// <summary>The player's saved record, or null for a creature that is not a player or has no id.</summary>
        Player GetDbPlayer(uint player);

        bool IsUnlocked(uint player);

        void OpenBarsOnEnter();

        void LockBarsOnEnter();

        void LockBarsOnExit();

        /// <summary>
        /// Moves every open bar with the screen edges it sits nearest after the game window or UI scale changes.
        /// Bars go in order, each kept off the bars below it, so a smaller screen cannot stack them on each other.
        /// </summary>
        void FollowScreen(uint player);

        /// <summary>The work of <see cref="LockBarsOnExit"/> for one player (a hot reload's quiescence step).</summary>
        void LockBarsFor(uint player);

        /// <summary>
        /// Opens the bars the player has turned on and closes the rest.
        /// </summary>
        void ApplyVisibility(uint player);

        void SetUnlocked(uint player, bool isUnlocked);
    }
}
