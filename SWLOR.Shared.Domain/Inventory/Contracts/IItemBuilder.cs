using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Inventory.Delegates;
using SWLOR.Shared.Domain.Inventory.ValueObjects;

namespace SWLOR.Shared.Domain.Inventory.Contracts;

public interface IItemBuilder
{
    /// <summary>
    /// Creates a new item.
    /// </summary>
    /// <param name="itemTag">The tag of the item which will use these rules.</param>
    /// <param name="itemTags">The additional item tags which will also use these rules.</param>
    /// <returns>An item builder with the configured options.</returns>
    IItemBuilder Create(string itemTag, params string[] itemTags);

    /// <summary>
    /// Sets the message which will be sent to the user when the item is used.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <returns>An item builder with the configured options.</returns>
    IItemBuilder InitializationMessage(string message);

    /// <summary>
    /// Runs custom logic to determine the message which will be played at the start of using an item.
    /// </summary>
    /// <param name="action">The action to run.</param>
    /// <returns>An item builder with the configured options.</returns>
    IItemBuilder InitializationMessage(InitializationMessageDelegate action);

    /// <summary>
    /// Sets the amount of time it takes before the item will be used.
    /// Moving will interrupt the usage.
    /// </summary>
    /// <param name="seconds">The number of seconds.</param>
    /// <returns>An item builder with the configured options.</returns>
    IItemBuilder Delay(float seconds);

    /// <summary>
    /// Runs custom logic to determine the number of seconds it takes to use an item.
    /// </summary>
    /// <param name="action">The action to run.</param>
    /// <returns>An item builder with the configured options.</returns>
    IItemBuilder Delay(CalculateDelayDelegate action);

    /// <summary>
    /// Forces the user to turn and face the target.
    /// </summary>
    /// <returns>An item builder with the configured options.</returns>
    IItemBuilder UserFacesTarget();

    /// <summary>
    /// Sets the animation which will play while the item is executing.
    /// </summary>
    /// <param name="animation">The animation to play</param>
    /// <returns>An item builder with the configured options.</returns>
    IItemBuilder PlaysAnimation(AnimationType animation);

    /// <summary>
    /// The max distance the user may be from their target when using this item.
    /// Default is 3.5f meters.
    /// </summary>
    /// <param name="maxDistance">The max distance in meters.</param>
    /// <returns>An item builder with the configured options.</returns>
    IItemBuilder MaxDistance(float maxDistance);

    /// <summary>
    /// Runs custom logic to determine the max distance an item may be used on a target.
    /// </summary>
    /// <param name="action">The action to run.</param>
    /// <returns>An item builder with the configured options.</returns>
    IItemBuilder MaxDistance(CalculateDistanceDelegate action);

    /// <summary>
    /// Indicates that the item will lose a charge when successfully used.
    /// </summary>
    /// <returns>An item builder with the configured options.</returns>
    IItemBuilder ReducesItemCharge();

    /// <summary>
    /// Runs custom logic to determine if the item will lose charges.
    /// </summary>
    /// <param name="action">The action to run.</param>
    /// <returns>An item builder with the configured options.</returns>
    IItemBuilder ReducesItemCharge(ReducesItemChargeDelegate action);

    /// <summary>
    /// Enables targeting locations in addition to other objects.
    /// </summary>
    /// <returns>An item builder with the configured options.</returns>
    IItemBuilder TargetsLocation();

    /// <summary>
    /// Sets the action which will run during validation.
    /// </summary>
    /// <param name="action">The action to run.</param>
    /// <returns>An item builder with the configured options.</returns>
    IItemBuilder ValidationAction(ValidateItemDelegate action);

    /// <summary>
    /// Sets the action which will run when the item is successfully used.
    /// </summary>
    /// <param name="action">The action to run.</param>
    /// <returns>An item builder with the configured options.</returns>
    IItemBuilder ApplyAction(ApplyItemEffectsDelegate action);

    /// <summary>
    /// Indicates this item has a recast timer when used.
    /// </summary>
    /// <param name="type">The recast group type</param>
    /// <param name="delaySeconds">The delay in seconds</param>
    /// <returns>An item builder with the configured options.</returns>
    IItemBuilder HasRecastDelay(RecastGroupType type, float delaySeconds);

    /// <summary>
    /// Returns a built dictionary of item details.
    /// </summary>
    /// <returns>A dictionary of item details.</returns>
    Dictionary<string, ItemDetail> Build();
}