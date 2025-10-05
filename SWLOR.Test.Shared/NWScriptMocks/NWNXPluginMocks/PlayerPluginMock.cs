using System.Numerics;
using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX.Model;
using QuickBarSlot = SWLOR.NWN.API.NWNX.Model.QuickBarSlot;

namespace SWLOR.Test.Shared.NWScriptMocks.NWNXPluginMocks
{
    /// <summary>
    /// Mock implementation of the PlayerPlugin for testing purposes.
    /// Provides comprehensive player interface and interaction functionality including GUI controls,
    /// timing bars, quickbar management, and advanced player-specific features.
    /// </summary>
    public class PlayerPluginMock: IPlayerPluginService
    {
        // Mock data storage
        private readonly Dictionary<uint, PlayerData> _playerData = new();
        private readonly Dictionary<uint, List<QuickBarSlot>> _quickBarSlots = new();
        private readonly Dictionary<uint, TimingBarData> _timingBars = new();

        /// <summary>
        /// Forces the display of a placeable's examine window for the specified player.
        /// </summary>
        /// <param name="player">The player object to show the examine window to. Must be a valid player character.</param>
        /// <param name="placeable">The placeable object to examine. Must be a valid placeable object.</param>
        public void ForcePlaceableExamineWindow(uint player, uint placeable)
        {
            // Mock implementation - in real tests, this would open the examine window
        }

        /// <summary>
        /// Forces the opening of a placeable's inventory window for the specified player.
        /// </summary>
        /// <param name="player">The player object to open the inventory for. Must be a valid player character.</param>
        /// <param name="placeable">The placeable object whose inventory to open. Must be a valid placeable object.</param>
        public void ForcePlaceableInventoryWindow(uint player, uint placeable)
        {
            // Mock implementation - in real tests, this would open the inventory window
        }

        /// <summary>
        /// Starts displaying a timing bar for the specified player.
        /// </summary>
        /// <param name="player">The player object to show the timing bar to. Must be a valid player character.</param>
        /// <param name="seconds">The duration in seconds for the timing bar to complete. Must be a positive value.</param>
        /// <param name="script">Optional script to execute when the timing bar completes. Can be empty string for no script.</param>
        /// <param name="type">The type of timing bar to display. See TimingBarType enum for available options.</param>
        public void StartGuiTimingBar(uint player, float seconds, string script = "", TimingBarType type = TimingBarType.Custom)
        {
            var timingBar = new TimingBarData
            {
                Player = player,
                Duration = Math.Max(0.1f, seconds),
                Script = script ?? "",
                Type = type,
                StartTime = DateTime.UtcNow,
                IsActive = true
            };

            _timingBars[player] = timingBar;
        }

        /// <summary>
        /// Stops displaying a timing bar.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="script">The script to run when stopped.</param>
        public void StopGuiTimingBar(uint player, string script = "")
        {
            if (_timingBars.TryGetValue(player, out var timingBar))
            {
                timingBar.IsActive = false;
                
                if (!string.IsNullOrEmpty(script))
                {
                    // Mock implementation - in real tests, this would execute the script
                }
            }
        }

        /// <summary>
        /// Sets whether the player should always walk when given movement commands.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="walk">True to set the player to always walk.</param>
        public void SetAlwaysWalk(uint player, bool walk)
        {
            GetPlayerData(player).AlwaysWalk = walk;
        }

        /// <summary>
        /// Gets the player's quickbar slot info.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="slot">Slot ID 0-35.</param>
        /// <returns>An QuickBarSlot struct.</returns>
        public QuickBarSlot GetQuickBarSlot(uint player, int slot)
        {
            if (slot < 0 || slot > 35)
                return new QuickBarSlot();

            var playerData = GetPlayerData(player);
            if (playerData.QuickBarSlots.TryGetValue(slot, out var quickBarSlot))
            {
                return quickBarSlot;
            }

            return new QuickBarSlot();
        }

        /// <summary>
        /// Sets the player's quickbar slot.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="slot">Slot ID 0-35.</param>
        /// <param name="quickBarSlot">The QuickBarSlot struct to set.</param>
        public void SetQuickBarSlot(uint player, int slot, QuickBarSlot quickBarSlot)
        {
            if (slot < 0 || slot > 35)
                return;

            GetPlayerData(player).QuickBarSlots[slot] = quickBarSlot;
        }

        /// <summary>
        /// Gets the player's name.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's name.</returns>
        public string GetPlayerName(uint player)
        {
            return GetPlayerData(player).Name;
        }

        /// <summary>
        /// Sets the player's name.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="name">The new name for the player.</param>
        public void SetPlayerName(uint player, string name)
        {
            GetPlayerData(player).Name = name ?? "";
        }

        /// <summary>
        /// Gets the player's CD key.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's CD key.</returns>
        public string GetPlayerCDKey(uint player)
        {
            return GetPlayerData(player).CDKey;
        }

        /// <summary>
        /// Sets the player's CD key.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="cdKey">The new CD key for the player.</param>
        public void SetPlayerCDKey(uint player, string cdKey)
        {
            GetPlayerData(player).CDKey = cdKey ?? "";
        }

        /// <summary>
        /// Gets the player's IP address.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's IP address.</returns>
        public string GetPlayerIPAddress(uint player)
        {
            return GetPlayerData(player).IPAddress;
        }

        /// <summary>
        /// Sets the player's IP address.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="ipAddress">The new IP address for the player.</param>
        public void SetPlayerIPAddress(uint player, string ipAddress)
        {
            GetPlayerData(player).IPAddress = ipAddress ?? "";
        }

        /// <summary>
        /// Gets the player's login time.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's login time.</returns>
        public DateTime GetPlayerLoginTime(uint player)
        {
            return GetPlayerData(player).LoginTime;
        }

        /// <summary>
        /// Sets the player's login time.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="loginTime">The new login time for the player.</param>
        public void SetPlayerLoginTime(uint player, DateTime loginTime)
        {
            GetPlayerData(player).LoginTime = loginTime;
        }

        /// <summary>
        /// Gets the player's current area.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's current area.</returns>
        public uint GetPlayerArea(uint player)
        {
            return GetPlayerData(player).CurrentArea;
        }

        /// <summary>
        /// Sets the player's current area.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="area">The new area for the player.</param>
        public void SetPlayerArea(uint player, uint area)
        {
            GetPlayerData(player).CurrentArea = area;
        }

        /// <summary>
        /// Gets the player's position.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's position.</returns>
        public Vector3 GetPlayerPosition(uint player)
        {
            return GetPlayerData(player).Position;
        }

        /// <summary>
        /// Sets the player's position.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="position">The new position for the player.</param>
        public void SetPlayerPosition(uint player, Vector3 position)
        {
            GetPlayerData(player).Position = position;
        }

        /// <summary>
        /// Gets the player's facing.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's facing.</returns>
        public float GetPlayerFacing(uint player)
        {
            return GetPlayerData(player).Facing;
        }

        /// <summary>
        /// Sets the player's facing.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="facing">The new facing for the player.</param>
        public void SetPlayerFacing(uint player, float facing)
        {
            GetPlayerData(player).Facing = facing;
        }

        /// <summary>
        /// Gets the player's level.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's level.</returns>
        public int GetPlayerLevel(uint player)
        {
            return GetPlayerData(player).Level;
        }

        /// <summary>
        /// Sets the player's level.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="level">The new level for the player.</param>
        public void SetPlayerLevel(uint player, int level)
        {
            GetPlayerData(player).Level = Math.Max(1, level);
        }

        /// <summary>
        /// Gets the player's class.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's class.</returns>
        public ClassType GetPlayerClass(uint player)
        {
            return GetPlayerData(player).Class;
        }

        /// <summary>
        /// Sets the player's class.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="classType">The new class for the player.</param>
        public void SetPlayerClass(uint player, ClassType classType)
        {
            GetPlayerData(player).Class = classType;
        }

        /// <summary>
        /// Gets the player's race.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's race.</returns>
        public RacialType GetPlayerRace(uint player)
        {
            return GetPlayerData(player).Race;
        }

        /// <summary>
        /// Sets the player's race.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="race">The new race for the player.</param>
        public void SetPlayerRace(uint player, RacialType race)
        {
            GetPlayerData(player).Race = race;
        }

        /// <summary>
        /// Gets the player's gender.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's gender.</returns>
        public GenderType GetPlayerGender(uint player)
        {
            return GetPlayerData(player).Gender;
        }

        /// <summary>
        /// Sets the player's gender.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="gender">The new gender for the player.</param>
        public void SetPlayerGender(uint player, GenderType gender)
        {
            GetPlayerData(player).Gender = gender;
        }

        /// <summary>
        /// Gets the player's hit points.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's hit points.</returns>
        public int GetPlayerHitPoints(uint player)
        {
            return GetPlayerData(player).HitPoints;
        }

        /// <summary>
        /// Sets the player's hit points.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="hitPoints">The new hit points for the player.</param>
        public void SetPlayerHitPoints(uint player, int hitPoints)
        {
            GetPlayerData(player).HitPoints = Math.Max(0, hitPoints);
        }

        /// <summary>
        /// Gets the player's maximum hit points.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's maximum hit points.</returns>
        public int GetPlayerMaxHitPoints(uint player)
        {
            return GetPlayerData(player).MaxHitPoints;
        }

        /// <summary>
        /// Sets the player's maximum hit points.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="maxHitPoints">The new maximum hit points for the player.</param>
        public void SetPlayerMaxHitPoints(uint player, int maxHitPoints)
        {
            GetPlayerData(player).MaxHitPoints = Math.Max(1, maxHitPoints);
        }

        /// <summary>
        /// Gets the player's armor class.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's armor class.</returns>
        public int GetPlayerArmorClass(uint player)
        {
            return GetPlayerData(player).ArmorClass;
        }

        /// <summary>
        /// Sets the player's armor class.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="armorClass">The new armor class for the player.</param>
        public void SetPlayerArmorClass(uint player, int armorClass)
        {
            GetPlayerData(player).ArmorClass = armorClass;
        }

        /// <summary>
        /// Gets the player's experience points.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's experience points.</returns>
        public int GetPlayerExperience(uint player)
        {
            return GetPlayerData(player).Experience;
        }

        /// <summary>
        /// Sets the player's experience points.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="experience">The new experience points for the player.</param>
        public void SetPlayerExperience(uint player, int experience)
        {
            GetPlayerData(player).Experience = Math.Max(0, experience);
        }

        /// <summary>
        /// Gets the player's gold.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's gold.</returns>
        public int GetPlayerGold(uint player)
        {
            return GetPlayerData(player).Gold;
        }

        /// <summary>
        /// Sets the player's gold.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="gold">The new gold amount for the player.</param>
        public void SetPlayerGold(uint player, int gold)
        {
            GetPlayerData(player).Gold = Math.Max(0, gold);
        }

        /// <summary>
        /// Gets the player's alignment.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's alignment.</returns>
        public AlignmentType GetPlayerAlignment(uint player)
        {
            return GetPlayerData(player).Alignment;
        }

        /// <summary>
        /// Sets the player's alignment.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="alignment">The new alignment for the player.</param>
        public void SetPlayerAlignment(uint player, AlignmentType alignment)
        {
            GetPlayerData(player).Alignment = alignment;
        }

        /// <summary>
        /// Gets the player's deity.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's deity.</returns>
        public int GetPlayerDeity(uint player)
        {
            return GetPlayerData(player).Deity;
        }

        /// <summary>
        /// Sets the player's deity.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="deity">The new deity for the player.</param>
        public void SetPlayerDeity(uint player, int deity)
        {
            GetPlayerData(player).Deity = deity;
        }

        /// <summary>
        /// Gets the player's portrait.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's portrait.</returns>
        public int GetPlayerPortrait(uint player)
        {
            return GetPlayerData(player).Portrait;
        }

        /// <summary>
        /// Sets the player's portrait.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="portrait">The new portrait for the player.</param>
        public void SetPlayerPortrait(uint player, int portrait)
        {
            GetPlayerData(player).Portrait = portrait;
        }

        /// <summary>
        /// Gets the player's description.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's description.</returns>
        public string GetPlayerDescription(uint player)
        {
            return GetPlayerData(player).Description;
        }

        /// <summary>
        /// Sets the player's description.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="description">The new description for the player.</param>
        public void SetPlayerDescription(uint player, string description)
        {
            GetPlayerData(player).Description = description ?? "";
        }

        /// <summary>
        /// Gets the player's tag.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's tag.</returns>
        public string GetPlayerTag(uint player)
        {
            return GetPlayerData(player).Tag;
        }

        /// <summary>
        /// Sets the player's tag.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="tag">The new tag for the player.</param>
        public void SetPlayerTag(uint player, string tag)
        {
            GetPlayerData(player).Tag = tag ?? "";
        }

        /// <summary>
        /// Gets the player's resref.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's resref.</returns>
        public string GetPlayerResRef(uint player)
        {
            return GetPlayerData(player).ResRef;
        }

        /// <summary>
        /// Sets the player's resref.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="resRef">The new resref for the player.</param>
        public void SetPlayerResRef(uint player, string resRef)
        {
            GetPlayerData(player).ResRef = resRef ?? "";
        }

        /// <summary>
        /// Gets the player's appearance.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's appearance.</returns>
        public AppearanceType GetPlayerAppearance(uint player)
        {
            return GetPlayerData(player).Appearance;
        }

        /// <summary>
        /// Sets the player's appearance.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="appearance">The new appearance for the player.</param>
        public void SetPlayerAppearance(uint player, AppearanceType appearance)
        {
            GetPlayerData(player).Appearance = appearance;
        }

        /// <summary>
        /// Gets the player's size.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's size.</returns>
        public CreatureSizeType GetPlayerSize(uint player)
        {
            return GetPlayerData(player).Size;
        }

        /// <summary>
        /// Sets the player's size.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="size">The new size for the player.</param>
        public void SetPlayerSize(uint player, CreatureSizeType size)
        {
            GetPlayerData(player).Size = size;
        }

        /// <summary>
        /// Gets the player's walk rate.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's walk rate.</returns>
        public float GetPlayerWalkRate(uint player)
        {
            return GetPlayerData(player).WalkRate;
        }

        /// <summary>
        /// Sets the player's walk rate.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="walkRate">The new walk rate for the player.</param>
        public void SetPlayerWalkRate(uint player, float walkRate)
        {
            GetPlayerData(player).WalkRate = Math.Max(0.1f, walkRate);
        }

        /// <summary>
        /// Gets the player's run rate.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's run rate.</returns>
        public float GetPlayerRunRate(uint player)
        {
            return GetPlayerData(player).RunRate;
        }

        /// <summary>
        /// Sets the player's run rate.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="runRate">The new run rate for the player.</param>
        public void SetPlayerRunRate(uint player, float runRate)
        {
            GetPlayerData(player).RunRate = Math.Max(0.1f, runRate);
        }

        /// <summary>
        /// Gets the player's jump velocity.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's jump velocity.</returns>
        public float GetPlayerJumpVelocity(uint player)
        {
            return GetPlayerData(player).JumpVelocity;
        }

        /// <summary>
        /// Sets the player's jump velocity.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="jumpVelocity">The new jump velocity for the player.</param>
        public void SetPlayerJumpVelocity(uint player, float jumpVelocity)
        {
            GetPlayerData(player).JumpVelocity = Math.Max(0.0f, jumpVelocity);
        }

        /// <summary>
        /// Gets the player's jump height.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's jump height.</returns>
        public float GetPlayerJumpHeight(uint player)
        {
            return GetPlayerData(player).JumpHeight;
        }

        /// <summary>
        /// Sets the player's jump height.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="jumpHeight">The new jump height for the player.</param>
        public void SetPlayerJumpHeight(uint player, float jumpHeight)
        {
            GetPlayerData(player).JumpHeight = Math.Max(0.0f, jumpHeight);
        }

        /// <summary>
        /// Gets the player's jump distance.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's jump distance.</returns>
        public float GetPlayerJumpDistance(uint player)
        {
            return GetPlayerData(player).JumpDistance;
        }

        /// <summary>
        /// Sets the player's jump distance.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="jumpDistance">The new jump distance for the player.</param>
        public void SetPlayerJumpDistance(uint player, float jumpDistance)
        {
            GetPlayerData(player).JumpDistance = Math.Max(0.0f, jumpDistance);
        }

        /// <summary>
        /// Gets the player's jump speed.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's jump speed.</returns>
        public float GetPlayerJumpSpeed(uint player)
        {
            return GetPlayerData(player).JumpSpeed;
        }

        /// <summary>
        /// Sets the player's jump speed.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="jumpSpeed">The new jump speed for the player.</param>
        public void SetPlayerJumpSpeed(uint player, float jumpSpeed)
        {
            GetPlayerData(player).JumpSpeed = Math.Max(0.0f, jumpSpeed);
        }

        /// <summary>
        /// Gets the player's jump gravity.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's jump gravity.</returns>
        public float GetPlayerJumpGravity(uint player)
        {
            return GetPlayerData(player).JumpGravity;
        }

        /// <summary>
        /// Sets the player's jump gravity.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="jumpGravity">The new jump gravity for the player.</param>
        public void SetPlayerJumpGravity(uint player, float jumpGravity)
        {
            GetPlayerData(player).JumpGravity = Math.Max(0.0f, jumpGravity);
        }

        /// <summary>
        /// Gets the player's jump bounce.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's jump bounce.</returns>
        public float GetPlayerJumpBounce(uint player)
        {
            return GetPlayerData(player).JumpBounce;
        }

        /// <summary>
        /// Sets the player's jump bounce.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="jumpBounce">The new jump bounce for the player.</param>
        public void SetPlayerJumpBounce(uint player, float jumpBounce)
        {
            GetPlayerData(player).JumpBounce = Math.Max(0.0f, jumpBounce);
        }

        /// <summary>
        /// Gets the player's jump friction.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's jump friction.</returns>
        public float GetPlayerJumpFriction(uint player)
        {
            return GetPlayerData(player).JumpFriction;
        }

        /// <summary>
        /// Sets the player's jump friction.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="jumpFriction">The new jump friction for the player.</param>
        public void SetPlayerJumpFriction(uint player, float jumpFriction)
        {
            GetPlayerData(player).JumpFriction = Math.Max(0.0f, jumpFriction);
        }

        /// <summary>
        /// Gets the player's jump air control.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's jump air control.</returns>
        public float GetPlayerJumpAirControl(uint player)
        {
            return GetPlayerData(player).JumpAirControl;
        }

        /// <summary>
        /// Sets the player's jump air control.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="jumpAirControl">The new jump air control for the player.</param>
        public void SetPlayerJumpAirControl(uint player, float jumpAirControl)
        {
            GetPlayerData(player).JumpAirControl = Math.Max(0.0f, jumpAirControl);
        }

        /// <summary>
        /// Gets the player's jump air speed.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's jump air speed.</returns>
        public float GetPlayerJumpAirSpeed(uint player)
        {
            return GetPlayerData(player).JumpAirSpeed;
        }

        /// <summary>
        /// Sets the player's jump air speed.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="jumpAirSpeed">The new jump air speed for the player.</param>
        public void SetPlayerJumpAirSpeed(uint player, float jumpAirSpeed)
        {
            GetPlayerData(player).JumpAirSpeed = Math.Max(0.0f, jumpAirSpeed);
        }

        /// <summary>
        /// Gets the player's jump air acceleration.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's jump air acceleration.</returns>
        public float GetPlayerJumpAirAcceleration(uint player)
        {
            return GetPlayerData(player).JumpAirAcceleration;
        }

        /// <summary>
        /// Sets the player's jump air acceleration.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="jumpAirAcceleration">The new jump air acceleration for the player.</param>
        public void SetPlayerJumpAirAcceleration(uint player, float jumpAirAcceleration)
        {
            GetPlayerData(player).JumpAirAcceleration = Math.Max(0.0f, jumpAirAcceleration);
        }

        /// <summary>
        /// Gets the player's jump air deceleration.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's jump air deceleration.</returns>
        public float GetPlayerJumpAirDeceleration(uint player)
        {
            return GetPlayerData(player).JumpAirDeceleration;
        }

        /// <summary>
        /// Sets the player's jump air deceleration.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="jumpAirDeceleration">The new jump air deceleration for the player.</param>
        public void SetPlayerJumpAirDeceleration(uint player, float jumpAirDeceleration)
        {
            GetPlayerData(player).JumpAirDeceleration = Math.Max(0.0f, jumpAirDeceleration);
        }

        /// <summary>
        /// Gets the player's jump air friction.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's jump air friction.</returns>
        public float GetPlayerJumpAirFriction(uint player)
        {
            return GetPlayerData(player).JumpAirFriction;
        }

        /// <summary>
        /// Sets the player's jump air friction.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="jumpAirFriction">The new jump air friction for the player.</param>
        public void SetPlayerJumpAirFriction(uint player, float jumpAirFriction)
        {
            GetPlayerData(player).JumpAirFriction = Math.Max(0.0f, jumpAirFriction);
        }

        /// <summary>
        /// Gets the player's jump air bounce.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's jump air bounce.</returns>
        public float GetPlayerJumpAirBounce(uint player)
        {
            return GetPlayerData(player).JumpAirBounce;
        }

        /// <summary>
        /// Sets the player's jump air bounce.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="jumpAirBounce">The new jump air bounce for the player.</param>
        public void SetPlayerJumpAirBounce(uint player, float jumpAirBounce)
        {
            GetPlayerData(player).JumpAirBounce = Math.Max(0.0f, jumpAirBounce);
        }

        /// <summary>
        /// Gets the player's jump air gravity.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's jump air gravity.</returns>
        public float GetPlayerJumpAirGravity(uint player)
        {
            return GetPlayerData(player).JumpAirGravity;
        }

        /// <summary>
        /// Sets the player's jump air gravity.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="jumpAirGravity">The new jump air gravity for the player.</param>
        public void SetPlayerJumpAirGravity(uint player, float jumpAirGravity)
        {
            GetPlayerData(player).JumpAirGravity = Math.Max(0.0f, jumpAirGravity);
        }

        /// <summary>
        /// Gets the player's jump air speed limit.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's jump air speed limit.</returns>
        public float GetPlayerJumpAirSpeedLimit(uint player)
        {
            return GetPlayerData(player).JumpAirSpeedLimit;
        }

        /// <summary>
        /// Sets the player's jump air speed limit.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="jumpAirSpeedLimit">The new jump air speed limit for the player.</param>
        public void SetPlayerJumpAirSpeedLimit(uint player, float jumpAirSpeedLimit)
        {
            GetPlayerData(player).JumpAirSpeedLimit = Math.Max(0.0f, jumpAirSpeedLimit);
        }

        /// <summary>
        /// Gets the player's jump air acceleration limit.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's jump air acceleration limit.</returns>
        public float GetPlayerJumpAirAccelerationLimit(uint player)
        {
            return GetPlayerData(player).JumpAirAccelerationLimit;
        }

        /// <summary>
        /// Sets the player's jump air acceleration limit.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="jumpAirAccelerationLimit">The new jump air acceleration limit for the player.</param>
        public void SetPlayerJumpAirAccelerationLimit(uint player, float jumpAirAccelerationLimit)
        {
            GetPlayerData(player).JumpAirAccelerationLimit = Math.Max(0.0f, jumpAirAccelerationLimit);
        }

        /// <summary>
        /// Gets the player's jump air deceleration limit.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's jump air deceleration limit.</returns>
        public float GetPlayerJumpAirDecelerationLimit(uint player)
        {
            return GetPlayerData(player).JumpAirDecelerationLimit;
        }

        /// <summary>
        /// Sets the player's jump air deceleration limit.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="jumpAirDecelerationLimit">The new jump air deceleration limit for the player.</param>
        public void SetPlayerJumpAirDecelerationLimit(uint player, float jumpAirDecelerationLimit)
        {
            GetPlayerData(player).JumpAirDecelerationLimit = Math.Max(0.0f, jumpAirDecelerationLimit);
        }

        /// <summary>
        /// Gets the player's jump air friction limit.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's jump air friction limit.</returns>
        public float GetPlayerJumpAirFrictionLimit(uint player)
        {
            return GetPlayerData(player).JumpAirFrictionLimit;
        }

        /// <summary>
        /// Sets the player's jump air friction limit.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="jumpAirFrictionLimit">The new jump air friction limit for the player.</param>
        public void SetPlayerJumpAirFrictionLimit(uint player, float jumpAirFrictionLimit)
        {
            GetPlayerData(player).JumpAirFrictionLimit = Math.Max(0.0f, jumpAirFrictionLimit);
        }

        /// <summary>
        /// Gets the player's jump air bounce limit.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's jump air bounce limit.</returns>
        public float GetPlayerJumpAirBounceLimit(uint player)
        {
            return GetPlayerData(player).JumpAirBounceLimit;
        }

        /// <summary>
        /// Sets the player's jump air bounce limit.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="jumpAirBounceLimit">The new jump air bounce limit for the player.</param>
        public void SetPlayerJumpAirBounceLimit(uint player, float jumpAirBounceLimit)
        {
            GetPlayerData(player).JumpAirBounceLimit = Math.Max(0.0f, jumpAirBounceLimit);
        }

        /// <summary>
        /// Gets the player's jump air gravity limit.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player's jump air gravity limit.</returns>
        public float GetPlayerJumpAirGravityLimit(uint player)
        {
            return GetPlayerData(player).JumpAirGravityLimit;
        }

        /// <summary>
        /// Sets the player's jump air gravity limit.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="jumpAirGravityLimit">The new jump air gravity limit for the player.</param>
        public void SetPlayerJumpAirGravityLimit(uint player, float jumpAirGravityLimit)
        {
            GetPlayerData(player).JumpAirGravityLimit = Math.Max(0.0f, jumpAirGravityLimit);
        }

        // Helper methods for testing
        /// <summary>
        /// Resets all mock data to default values for testing.
        /// </summary>
        public void Reset()
        {
            _playerData.Clear();
            _quickBarSlots.Clear();
            _timingBars.Clear();
        }

        /// <summary>
        /// Gets the player data for the specified player, creating it if it doesn't exist.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player data for the specified player.</returns>
        private PlayerData GetPlayerData(uint player)
        {
            if (!_playerData.TryGetValue(player, out var data))
            {
                data = new PlayerData();
                _playerData[player] = data;
            }
            return data;
        }

        /// <summary>
        /// Gets the player data for testing verification.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The player data for the specified player.</returns>
        public PlayerData GetPlayerDataForTesting(uint player)
        {
            return GetPlayerData(player);
        }

        /// <summary>
        /// Gets the timing bar data for testing verification.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The timing bar data for the specified player.</returns>
        public TimingBarData? GetTimingBarData(uint player)
        {
            return _timingBars.TryGetValue(player, out var data) ? data : null;
        }

        // Missing methods from interface
        public string GetBicFileName(uint player)
        {
            return GetPlayerData(player).BicFileName;
        }

        public void ShowVisualEffect(uint player, int effectId, float duration, Vector3 position, Vector3 direction, Vector3 scale)
        {
            // Mock implementation - in real tests, this would show visual effect
        }

        public void MusicBackgroundChangeTimeToggle(uint player, int time, bool enable)
        {
            // Mock implementation - in real tests, this would toggle music background change time
        }

        public void MusicBackgroundToggle(uint player, bool enable)
        {
            // Mock implementation - in real tests, this would toggle music background
        }

        public void MusicBattleChange(uint player, int musicId)
        {
            // Mock implementation - in real tests, this would change battle music
        }

        public void MusicBattleToggle(uint player, bool enable)
        {
            // Mock implementation - in real tests, this would toggle battle music
        }

        public void PlaySound(uint player, string soundResRef, uint objectId)
        {
            // Mock implementation - in real tests, this would play sound
        }

        public void SetPlaceableUseable(uint player, uint placeable, bool useable)
        {
            // Mock implementation - in real tests, this would set placeable useable
        }

        public void SetRestDuration(uint player, int duration)
        {
            // Mock implementation - in real tests, this would set rest duration
        }

        public void ApplyInstantVisualEffectToObject(uint player, uint target, VisualEffectType effectType)
        {
            // Mock implementation - in real tests, this would apply instant visual effect
        }

        public void UpdateCharacterSheet(uint player)
        {
            // Mock implementation - in real tests, this would update character sheet
        }

        public void OpenInventory(uint player, uint target, bool open)
        {
            // Mock implementation - in real tests, this would open inventory
        }

        public string GetAreaExplorationState(uint player, uint area)
        {
            return GetPlayerData(player).AreaExplorationStates.TryGetValue(area, out var state) ? state : "";
        }

        public void SetAreaExplorationState(uint player, uint area, string state)
        {
            GetPlayerData(player).AreaExplorationStates[area] = state ?? "";
        }

        public void SetRestAnimation(uint player, int animationId)
        {
            // Mock implementation - in real tests, this would set rest animation
        }

        public void SetObjectVisualTransformOverride(uint player, uint target, int transformType, float value)
        {
            // Mock implementation - in real tests, this would set object visual transform override
        }

        public void ApplyLoopingVisualEffectToObject(uint player, uint target, VisualEffectType effectType)
        {
            // Mock implementation - in real tests, this would apply looping visual effect
        }

        public void SetPlaceableNameOverride(uint player, uint placeable, string name)
        {
            // Mock implementation - in real tests, this would set placeable name override
        }

        public int GetQuestCompleted(uint player, string questId)
        {
            return GetPlayerData(player).CompletedQuests.Contains(questId) ? 1 : 0;
        }

        public void SetPersistentLocation(string playerName, string areaTag, uint area, bool isPersistent)
        {
            // Mock implementation - in real tests, this would set persistent location
        }

        public void UpdateItemName(uint player, uint item)
        {
            // Mock implementation - in real tests, this would update item name
        }

        public bool PossessCreature(uint player, uint creature, bool possess, bool displayFeedback)
        {
            // Mock implementation - in real tests, this would possess creature
            return true;
        }

        public int GetPlatformId(uint player)
        {
            return GetPlayerData(player).PlatformId;
        }

        public int GetLanguage(uint player)
        {
            return GetPlayerData(player).Language;
        }

        public void SetResManOverride(uint player, int resType, string resRef, string newResRef)
        {
            // Mock implementation - in real tests, this would set resource manager override
        }

        public void ToggleDM(uint player, bool isDM)
        {
            GetPlayerData(player).IsDM = isDM;
        }

        public void SetObjectMouseCursorOverride(uint player, uint target, MouseCursorType cursorType)
        {
            // Mock implementation - in real tests, this would set object mouse cursor override
        }

        public void SetObjectHiliteColorOverride(uint player, uint target, int color)
        {
            // Mock implementation - in real tests, this would set object hilite color override
        }

        public void RemoveEffectFromTURD(uint player, string effectTag)
        {
            // Mock implementation - in real tests, this would remove effect from TURD
        }

        public void SetSpawnLocation(uint player, Location location)
        {
            GetPlayerData(player).SpawnLocation = location;
        }

        public void SetCustomToken(uint player, int tokenId, string value)
        {
            GetPlayerData(player).CustomTokens[tokenId] = value ?? "";
        }

        public void SetCreatureNameOverride(uint player, uint creature, string name)
        {
            // Mock implementation - in real tests, this would set creature name override
        }

        public void FloatingTextStringOnCreature(uint player, uint creature, string text, bool displayToAll)
        {
            // Mock implementation - in real tests, this would show floating text
        }

        public int AddCustomJournalEntry(uint player, JournalEntry entry, bool displayToAll)
        {
            GetPlayerData(player).JournalEntries.Add(entry);
            return 1;
        }

        public JournalEntry GetJournalEntry(uint player, string questId)
        {
            return GetPlayerData(player).JournalEntries.FirstOrDefault(e => e.Tag == questId) ?? new JournalEntry();
        }

        public void CloseStore(uint player)
        {
            // Mock implementation - in real tests, this would close store
        }

        public void SetTlkOverride(uint player, int strRef, string value, bool displayToAll)
        {
            GetPlayerData(player).TlkOverrides[strRef] = value ?? "";
        }

        public uint GetOpenStore(uint player)
        {
            return GetPlayerData(player).OpenStore;
        }

        // Constants
        private const uint OBJECT_INVALID = 0x7F000000;

        // Helper classes
        public class PlayerData
        {
            public string Name { get; set; } = "";
            public string CDKey { get; set; } = "";
            public string IPAddress { get; set; } = "";
            public DateTime LoginTime { get; set; } = DateTime.UtcNow;
            public uint CurrentArea { get; set; } = OBJECT_INVALID;
            public Vector3 Position { get; set; } = Vector3.Zero;
            public float Facing { get; set; } = 0.0f;
            public int Level { get; set; } = 1;
            public ClassType Class { get; set; } = ClassType.Barbarian;
            public RacialType Race { get; set; } = RacialType.Human;
            public GenderType Gender { get; set; } = GenderType.Male;
            public int HitPoints { get; set; } = 100;
            public int MaxHitPoints { get; set; } = 100;
            public int ArmorClass { get; set; } = 10;
            public int Experience { get; set; } = 0;
            public int Gold { get; set; } = 0;
            public AlignmentType Alignment { get; set; } = AlignmentType.Neutral;
            public int Deity { get; set; } = 0;
            public int Portrait { get; set; } = 0;
            public string Description { get; set; } = "";
            public string Tag { get; set; } = "";
            public string ResRef { get; set; } = "";
            public AppearanceType Appearance { get; set; } = AppearanceType.Human;
            public CreatureSizeType Size { get; set; } = CreatureSizeType.Medium;
            public float WalkRate { get; set; } = 1.0f;
            public float RunRate { get; set; } = 2.0f;
            public float JumpVelocity { get; set; } = 0.0f;
            public float JumpHeight { get; set; } = 0.0f;
            public float JumpDistance { get; set; } = 0.0f;
            public float JumpSpeed { get; set; } = 0.0f;
            public float JumpGravity { get; set; } = 0.0f;
            public float JumpBounce { get; set; } = 0.0f;
            public float JumpFriction { get; set; } = 0.0f;
            public float JumpAirControl { get; set; } = 0.0f;
            public float JumpAirSpeed { get; set; } = 0.0f;
            public float JumpAirAcceleration { get; set; } = 0.0f;
            public float JumpAirDeceleration { get; set; } = 0.0f;
            public float JumpAirFriction { get; set; } = 0.0f;
            public float JumpAirBounce { get; set; } = 0.0f;
            public float JumpAirGravity { get; set; } = 0.0f;
            public float JumpAirSpeedLimit { get; set; } = 0.0f;
            public float JumpAirAccelerationLimit { get; set; } = 0.0f;
            public float JumpAirDecelerationLimit { get; set; } = 0.0f;
            public float JumpAirFrictionLimit { get; set; } = 0.0f;
            public float JumpAirBounceLimit { get; set; } = 0.0f;
            public float JumpAirGravityLimit { get; set; } = 0.0f;
            public bool AlwaysWalk { get; set; } = false;
            public Dictionary<int, QuickBarSlot> QuickBarSlots { get; set; } = new();
            
            // Additional properties for new methods
            public string BicFileName { get; set; } = "";
            public Dictionary<uint, string> AreaExplorationStates { get; set; } = new();
            public HashSet<string> CompletedQuests { get; set; } = new();
            public int PlatformId { get; set; } = 0;
            public int Language { get; set; } = 0;
            public bool IsDM { get; set; } = false;
            public Location SpawnLocation { get; set; } = new Location(0);
            public Dictionary<int, string> CustomTokens { get; set; } = new();
            public List<JournalEntry> JournalEntries { get; set; } = new();
            public Dictionary<int, string> TlkOverrides { get; set; } = new();
            public uint OpenStore { get; set; } = OBJECT_INVALID;
        }

        public class TimingBarData
        {
            public uint Player { get; set; }
            public float Duration { get; set; }
            public string Script { get; set; } = "";
            public TimingBarType Type { get; set; }
            public DateTime StartTime { get; set; }
            public bool IsActive { get; set; }
        }

    }
}
