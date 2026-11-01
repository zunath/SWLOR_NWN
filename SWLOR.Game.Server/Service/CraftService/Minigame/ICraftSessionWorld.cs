using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// Everything a craft session needs from the game engine. <see cref="CraftSessionLifecycle"/> holds the rules and calls only this,
/// so the rules run without the engine; <c>NwnCraftSessionWorld</c> is the engine implementation.
/// </summary>
public interface ICraftSessionWorld
{
    /// <summary>The current time in UTC. A session's start and the time it took are read from this clock, so a test can move it.</summary>
    DateTime UtcNow { get; }

    RecipeDetail GetRecipe(RecipeType recipe);

    /// <summary>True when the player meets the recipe's skill and other requirements. It does not check materials.</summary>
    bool CanCraft(uint player, RecipeType recipe);

    /// <summary>Returns the reason the blueprint cannot be used for the recipe now, or an empty string when it can.</summary>
    string ValidateBlueprint(uint player, uint blueprintItem, RecipeType recipe);

    /// <summary>Takes the blueprint's credit cost and one licensed run, and returns its details after the charge.</summary>
    BlueprintDetail ChargeBlueprint(uint player, uint blueprintItem);

    /// <summary>
    /// Returns the reason the station's fee for the recipe cannot be paid beside the blueprint's credit cost, or an empty string when it
    /// can or there is none. A station Shipyard charges one for each Capital hull (space revamp).
    /// </summary>
    string ValidateStationFee(uint player, RecipeDetail recipe, uint blueprintItem);

    /// <summary>
    /// Takes the station's fee for the recipe from the player, when it has one, and returns it for the station's treasury; null when
    /// there is none. It is not refunded when the craft fails. The treasury is credited by <see cref="CreditStationFee"/> only when the
    /// craft ends and a character file holds the payment.
    /// </summary>
    StationFeeCharge ChargeStationFee(uint player, RecipeDetail recipe);

    /// <summary>
    /// Credits a fee the player paid to its station's treasury, exactly once: a fee already credited (the same id) is not credited again,
    /// so a retry after a stop between the credit and the player's save does nothing.
    /// </summary>
    void CreditStationFee(uint player, PendingStationFee fee);

    /// <summary>
    /// Takes exactly the recipe's components from the player's inventory, one serialized item per stack taken. When the
    /// inventory lacks any of them, it takes nothing and returns false.
    /// </summary>
    bool TryTakeComponents(uint player, RecipeDetail recipe, List<string> taken);

    /// <summary>True when the player carries the enhancement item.</summary>
    bool HasEnhancement(uint player, uint item);

    /// <summary>Serializes and removes an enhancement the player carries (checked with <see cref="HasEnhancement"/> first).</summary>
    string TakeEnhancement(uint player, uint item);

    /// <summary>Gives a serialized item back to the player. Returns false, leaving the item serialized, when the inventory is full.</summary>
    bool TryReturnItem(uint player, string serialized);

    /// <summary>Keeps the player at the station and walkable-through: the immobilize and collision ghost of a craft in progress.</summary>
    void BeginStationWork(uint player);

    /// <summary>
    /// Starts the looping work clip of the skill's station for as long as the station work lasts, where one can play (not for a
    /// rifle or cannon holder, who keeps the native pose). <see cref="EndStationWork"/> stops it.
    /// </summary>
    void PlayStationWork(uint player, SkillType skill);

    /// <summary>Releases the player from the station and stops the work clip.</summary>
    void EndStationWork(uint player);

    /// <summary>
    /// Creates the finished item outside the player's inventory (one stack of the recipe's quantity, or a single item for a template
    /// that does not stack), so its bonuses are applied to it before it can merge into a stack the player already carries.
    /// </summary>
    uint CreateFinishedItem(RecipeDetail recipe);

    /// <summary>
    /// The units of the finished <paramref name="item"/>, bonuses applied and serialized, still outside the player's inventory. A template
    /// that does not stack made one item, so the rest of the recipe's quantity are copies of it; a copy that could not be made is the
    /// finished item's serialized form with an invalid item.
    /// </summary>
    List<CraftedUnit> PrepareUnits(uint player, uint item, RecipeDetail recipe);

    /// <summary>
    /// Puts a prepared unit in the player's inventory. Returns false, and destroys the unit, when it does not fit or could not be made;
    /// the caller then holds its serialized form.
    /// </summary>
    bool DeliverUnit(uint player, CraftedUnit unit);

    void ApplyVendorValue(uint item, int addGoldPiece);

    /// <summary>Moves the properties of a serialized enhancement onto the crafted item.</summary>
    void ApplyEnhancement(uint item, string serializedEnhancement, RecipeDetail recipe);

    void ApplyFoodBonus(uint item, int durationSeconds, int bonusCharges);

    /// <summary>Applies the blueprint's random and guaranteed bonuses and marks the item with the blueprint level.</summary>
    void ApplyBlueprintBonuses(uint player, uint item, RecipeDetail recipe, BlueprintDetail blueprint);

    /// <summary>
    /// Gives the crafted item what its consumed components pass on: a ship component variant stamps its trait, and a ship module, deed or
    /// capital part keeps the traits of the components it consumed (space revamp). Other recipes are unchanged.
    /// </summary>
    void ApplyComponentTraits(uint item, RecipeType recipeType, RecipeDetail recipe, IReadOnlyList<string> components);

    /// <summary>
    /// Gives one skill's waiting craft XP and takes it off <see cref="Player.PendingCraftXp"/> in the same player save, so a stop can
    /// neither lose it nor give it twice. Returns false, keeping the entitlement, when the award cannot run now.
    /// </summary>
    bool AwardPendingXp(uint player, Player dbPlayer, SkillType skill, int xp);

    /// <summary>True when the player can be given skill XP now. A dead player cannot, so XP earned then waits on the record.</summary>
    bool CanReceiveXp(uint player);

    void SaveRecord(Player dbPlayer);

    /// <summary>
    /// Writes the character file so it matches the record just saved. The record is saved at once, but the inventory lives in the
    /// character file, which is only written when the character is exported; a crash before the next export would leave a record that
    /// says the craft holds the materials beside a file that still has them (or a finished item the file lacks). Call it after a
    /// record save that moved items between the inventory and the record. The engine implementation exports once the item changes
    /// have landed and does nothing for a DM or a player who is gone.
    /// </summary>
    void PersistCharacter(uint player);

    /// <summary>Serializes an item the player carries, such as the finished craft, so a transfer can be given again after a crash.</summary>
    string SerializeItem(uint item);

    /// <summary>
    /// The craft file sequence of the player: the value the next character file will carry. Right after a login it is the value the
    /// loaded file carried, 0 for a file written before any craft.
    /// </summary>
    int FileSequence(uint player);

    /// <summary>The craft file sequence the character file carried when it loaded; it does not move until the next load.</summary>
    int LoadedSequence(uint player);

    /// <summary>Raises the craft file sequence by one and returns the new value. Call it once the inventory changes of a transfer are made.</summary>
    int AdvanceFileSequence(uint player);

    /// <summary>The highest craft file sequence known to be in the character file: the value it was loaded with, or the value of this session's last export.</summary>
    int ExportedSequence(uint player);

    /// <summary>
    /// Writes the character file now and records its sequence as exported. For a later script run than the inventory change it
    /// covers, because a destroyed item is only gone once the script that destroyed it ends. Does nothing for a DM or a player who is gone.
    /// </summary>
    void FlushCharacter(uint player);

    void Message(uint player, string text);

    void LogCraft(uint player, string text);
}
