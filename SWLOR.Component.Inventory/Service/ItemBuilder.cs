using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Inventory.Delegates;
using SWLOR.Shared.Domain.Inventory.ValueObjects;

namespace SWLOR.Component.Inventory.Service
{
    public class ItemBuilder : IItemBuilder
    {
        private readonly Dictionary<string, ItemDetail> _items = new();
        private readonly List<ItemDetail> _activeItems = new();

        /// <summary>
        /// Creates a new item.
        /// </summary>
        /// <param name="itemTag">The tag of the item which will use these rules.</param>
        /// <param name="itemTags">The additional item tags which will also use these rules.</param>
        /// <returns>An item builder with the configured options.</returns>
        public IItemBuilder Create(string itemTag, params string[] itemTags)
        {
            _activeItems.Clear();

            var detail = new ItemDetail();
            _activeItems.Add(detail);
            _items[itemTag] = detail;

            foreach (var tag in itemTags)
            {
                detail = new ItemDetail();
                _activeItems.Add(detail);
                _items[tag] = detail;
            }
            
            return this;
        }

        /// <summary>
        /// Sets the message which will be sent to the user when the item is used.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <returns>An item builder with the configured options.</returns>
        public IItemBuilder InitializationMessage(string message)
        {
            foreach (var item in _activeItems)
            {
                item.InitializationMessageAction = (user, u, target, location, itemPropertyIndex) => message;
            }

            return this;
        }

        /// <summary>
        /// Runs custom logic to determine the message which will be played at the start of using an item.
        /// </summary>
        /// <param name="action">The action to run.</param>
        /// <returns>An item builder with the configured options.</returns>
        public IItemBuilder InitializationMessage(InitializationMessageDelegate action)
        {
            foreach (var item in _activeItems)
            {
                item.InitializationMessageAction = action;
            }

            return this;
        }

        /// <summary>
        /// Sets the amount of time it takes before the item will be used.
        /// Moving will interrupt the usage.
        /// </summary>
        /// <param name="seconds">The number of seconds.</param>
        /// <returns>An item builder with the configured options.</returns>
        public IItemBuilder Delay(float seconds)
        {
            foreach (var item in _activeItems)
            {
                item.DelayAction = (user, u, target, location, itemPropertyIndex) => seconds;
            }

            return this;
        }

        /// <summary>
        /// Runs custom logic to determine the number of seconds it takes to use an item.
        /// </summary>
        /// <param name="action">The action to run.</param>
        /// <returns>An item builder with the configured options.</returns>
        public IItemBuilder Delay(CalculateDelayDelegate action)
        {
            foreach (var item in _activeItems)
            {
                item.DelayAction = action;
            }

            return this;
        }

        /// <summary>
        /// Forces the user to turn and face the target.
        /// </summary>
        /// <returns>An item builder with the configured options.</returns>
        public IItemBuilder UserFacesTarget()
        {
            foreach (var item in _activeItems)
            {
                item.UserFacesTarget = true;
            }

            return this;
        }

        /// <summary>
        /// Sets the animation which will play while the item is executing.
        /// </summary>
        /// <param name="animation">The animation to play</param>
        /// <returns>An item builder with the configured options.</returns>
        public IItemBuilder PlaysAnimation(AnimationType animation)
        {
            foreach (var item in _activeItems)
            {
                item.ActivationAnimation = animation;
            }

            return this;
        }

        /// <summary>
        /// The max distance the user may be from their target when using this item.
        /// Default is 3.5f meters.
        /// </summary>
        /// <param name="maxDistance">The max distance in meters.</param>
        /// <returns>An item builder with the configured options.</returns>
        public IItemBuilder MaxDistance(float maxDistance)
        {
            foreach (var item in _activeItems)
            {
                item.CalculateDistanceAction = (user, u, target, location, itemPropertyIndex) => maxDistance;
            }

            return this;
        }

        /// <summary>
        /// Runs custom logic to determine the max distance an item may be used on a target.
        /// </summary>
        /// <param name="action">The action to run.</param>
        /// <returns>An item builder with the configured options.</returns>
        public IItemBuilder MaxDistance(CalculateDistanceDelegate action)
        {
            foreach (var item in _activeItems)
            {
                item.CalculateDistanceAction = action;
            }

            return this;
        }
        
        /// <summary>
        /// Indicates that the item will lose a charge when successfully used.
        /// </summary>
        /// <returns>An item builder with the configured options.</returns>
        public IItemBuilder ReducesItemCharge()
        {
            foreach (var item in _activeItems)
            {
                item.ReducesItemChargeAction = (user, u, target, location, itemPropertyIndex) => true;
            }

            return this;
        }

        /// <summary>
        /// Runs custom logic to determine if the item will lose charges.
        /// </summary>
        /// <param name="action">The action to run.</param>
        /// <returns>An item builder with the configured options.</returns>
        public IItemBuilder ReducesItemCharge(ReducesItemChargeDelegate action)
        {
            foreach (var item in _activeItems)
            {
                item.ReducesItemChargeAction = action;
            }

            return this;
        }
        
        /// <summary>
        /// Enables targeting locations in addition to other objects.
        /// </summary>
        /// <returns>An item builder with the configured options.</returns>
        public IItemBuilder TargetsLocation()
        {
            foreach (var item in _activeItems)
            {
                item.CanTargetLocation = true;
            }

            return this;
        }

        /// <summary>
        /// Sets the action which will run during validation.
        /// </summary>
        /// <param name="action">The action to run.</param>
        /// <returns>An item builder with the configured options.</returns>
        public IItemBuilder ValidationAction(ValidateItemDelegate action)
        {
            foreach (var item in _activeItems)
            {
                item.ValidateAction = action;
            }

            return this;
        }

        /// <summary>
        /// Sets the action which will run when the item is successfully used.
        /// </summary>
        /// <param name="action">The action to run.</param>
        /// <returns>An item builder with the configured options.</returns>
        public IItemBuilder ApplyAction(ApplyItemEffectsDelegate action)
        {
            foreach (var item in _activeItems)
            {
                item.ApplyAction = action;
            }

            return this;
        }

        /// <summary>
        /// Indicates this item has a recast timer when used.
        /// </summary>
        /// <param name="type">The recast group type</param>
        /// <param name="delaySeconds">The delay in seconds</param>
        /// <returns>An item builder with the configured options.</returns>
        public IItemBuilder HasRecastDelay(RecastGroupType type, float delaySeconds)
        {
            foreach (var item in _activeItems)
            {
                item.RecastGroup = type;
                item.RecastCooldown = delaySeconds;
            }

            return this;
        }

        /// <summary>
        /// Returns a built dictionary of item details.
        /// </summary>
        /// <returns>A dictionary of item details.</returns>
        public Dictionary<string, ItemDetail> Build()
        {
            return _items;
        }
        
    }
}
