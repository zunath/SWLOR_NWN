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
    /// The extra hotbars a player has saved and the windows they open in: the bars of a record with any missing bars and slots added,
    /// which bars are open, which are unlocked for moving, and where they sit after the screen changes. A part of the ExtraHotbars service.
    /// </summary>
    public class ExtraHotbarsWindowService : IExtraHotbarsWindowService
    {
        public const int BarCount = 3;
        public const int MaxSlots = 12;

        private readonly GuiWindowType[] _windowTypes =
        {
            GuiWindowType.ExtraHotbar1,
            GuiWindowType.ExtraHotbar2,
            GuiWindowType.ExtraHotbar3
        };

        // Bars can only be moved while the Hotbars window is open, so a bar never stays unlocked by accident.
        private readonly HashSet<uint> _unlockedPlayers = new();

        private readonly IDBService _db;
        private readonly IGuiService _gui;

        public ExtraHotbarsWindowService(
            IDBService db,
            IGuiService gui)
        {
            _db = db;
            _gui = gui;
        }

        public GuiWindowType GetWindowType(int barIndex) => _windowTypes[barIndex];

        public int GetBarIndex(GuiWindowType windowType) => System.Array.IndexOf(_windowTypes, windowType);

        /// <summary>
        /// Returns the player's bars, adding any that are missing so every bar has a full set of slots.
        /// </summary>
        public List<ExtraHotbar> GetBars(Player dbPlayer)
        {
            dbPlayer.ExtraHotbars ??= new List<ExtraHotbar>();
            while (dbPlayer.ExtraHotbars.Count < BarCount)
                dbPlayer.ExtraHotbars.Add(new ExtraHotbar());

            foreach (var bar in dbPlayer.ExtraHotbars)
            {
                bar.Slots ??= new List<ExtraHotbarSlot>();
                while (bar.Slots.Count < MaxSlots)
                    bar.Slots.Add(new ExtraHotbarSlot());
                bar.SlotCount = System.Math.Clamp(bar.SlotCount, 1, MaxSlots);
            }

            return dbPlayer.ExtraHotbars;
        }

        public ExtraHotbar GetBar(uint player, int barIndex)
        {
            var dbPlayer = GetDbPlayer(player);
            return dbPlayer == null ? null : GetBars(dbPlayer)[barIndex];
        }

        public Player GetDbPlayer(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return null;

            var playerId = GetObjectUUID(player);
            return string.IsNullOrWhiteSpace(playerId) ? null : _db.Get<Player>(playerId);
        }

        public bool IsUnlocked(uint player) => _unlockedPlayers.Contains(player);

        // Area entry follows every login, after the player's windows exist.
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public void OpenBarsOnEnter()
        {
            ApplyVisibility(GetEnteringObject());
        }

        [PlayerSession(PlayerReload.LoginOnly,
            "Clears the unlocked flag of a fresh login; the quiescence step locked the bars and the new build starts with none unlocked.")]
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public void LockBarsOnEnter()
        {
            _unlockedPlayers.Remove(GetEnteringObject());
        }

        [PlayerSession(PlayerReload.Quiesce,
            "Locks bars the player unlocked and saves their geometry, which only the old build's view models hold, before the windows close.",
            Step = nameof(LockBarsFor))]
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public void LockBarsOnExit()
        {
            LockBarsFor(GetExitingObject());
        }

        /// <summary>
        /// The work of <see cref="LockBarsOnExit"/> for <paramref name="player"/>: the logout's handler, and a hot reload's quiescence step for a player who stays online.
        /// </summary>
        public void LockBarsFor(uint player)
        {
            if (!_unlockedPlayers.Remove(player))
                return;

            // The window positions are saved on exit, so bars are put back in their locked spots first.
            SaveLockedPositions(player);
        }

        /// <summary>
        /// Opens the bars the player has turned on and closes the rest.
        /// </summary>
        public void ApplyVisibility(uint player)
        {
            var dbPlayer = GetDbPlayer(player);
            if (dbPlayer == null)
                return;

            var bars = GetBars(dbPlayer);
            for (var index = 0; index < BarCount; index++)
            {
                var windowType = _windowTypes[index];
                var isOpen = _gui.IsWindowOpen(player, windowType);
                if (bars[index].IsVisible && !isOpen)
                    _gui.TogglePlayerWindow(player, windowType);
                else if (!bars[index].IsVisible && isOpen)
                {
                    // Closing saves the window's position, which must not include an unlocked bar's title bar.
                    ((ExtraHotbarViewModel)_gui.GetPlayerWindow(player, windowType).ViewModel).ApplyLockedGeometry();
                    _gui.ClosePlayerWindow(player, windowType);
                }
            }
        }

        public void SetUnlocked(uint player, bool isUnlocked)
        {
            if (isUnlocked)
                _unlockedPlayers.Add(player);
            else
                _unlockedPlayers.Remove(player);

            _gui.PublishRefreshEvent(player, new ExtraHotbarRefreshEvent());
            if (isUnlocked)
                KeepTitleBarsClear(player);
            else
                SaveLockedPositions(player);
        }

        /// <summary>
        /// Unlocked bars grow a title bar upward, which covers a bar stacked right above. A covered bar ignores the
        /// mouse, so each bar moves off the bars numbered below it, bar 1 staying put.
        /// </summary>
        private void KeepTitleBarsClear(uint player)
        {
            foreach (var windowType in _windowTypes)
            {
                if (_gui.IsWindowOpen(player, windowType))
                    ((ExtraHotbarViewModel)_gui.GetPlayerWindow(player, windowType).ViewModel).KeepClearOfLowerBars();
            }
        }

        /// <summary>
        /// Moves every open bar with the screen edges it sits nearest after the game window or UI scale changes.
        /// Bars go in order, each kept off the bars below it, so a smaller screen cannot stack them on each other.
        /// </summary>
        public void FollowScreen(uint player)
        {
            var viewModels = new List<ExtraHotbarViewModel>();
            foreach (var windowType in _windowTypes)
            {
                if (_gui.IsWindowOpen(player, windowType))
                    viewModels.Add((ExtraHotbarViewModel)_gui.GetPlayerWindow(player, windowType).ViewModel);
            }

            var moved = new List<ExtraHotbarViewModel>();
            foreach (var viewModel in viewModels)
            {
                if (!viewModel.HasScreenChanged())
                    continue;

                viewModel.FollowScreen();
                moved.Add(viewModel);
            }

            foreach (var viewModel in moved)
                viewModel.KeepClearOfLowerBarsAfterScreenChange();
        }

        /// <summary>
        /// Saves where each open bar's slots sit, without the title bar shown while bars are unlocked.
        /// </summary>
        private void SaveLockedPositions(uint player)
        {
            var dbPlayer = GetDbPlayer(player);
            if (dbPlayer == null)
                return;

            foreach (var windowType in _windowTypes)
            {
                if (!_gui.IsWindowOpen(player, windowType))
                    continue;

                var viewModel = (ExtraHotbarViewModel)_gui.GetPlayerWindow(player, windowType).ViewModel;
                viewModel.ApplyLockedGeometry();
                dbPlayer.WindowGeometries[windowType] = viewModel.Geometry;
            }

            _db.Set(dbPlayer);
        }
    }
}
