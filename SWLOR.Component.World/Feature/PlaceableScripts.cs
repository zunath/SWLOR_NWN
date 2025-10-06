using Microsoft.Extensions.DependencyInjection;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Dialog.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Inventory.Enums;
using SWLOR.Shared.Domain.World.Events;
using SWLOR.Shared.UI.Service;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.World.Feature
{
    public class PlaceableScripts
    {
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded services to break circular dependencies
        private readonly Lazy<IKeyItemService> _keyItemService;
        private readonly Lazy<IEnmityService> _enmityService;
        private readonly Lazy<ICurrencyService> _currencyService;
        private readonly Lazy<IDialogService> _dialogService;

        public PlaceableScripts(
            IServiceProvider serviceProvider,
            IEventAggregator eventAggregator)
        {
            _serviceProvider = serviceProvider;
            
            // Initialize lazy services
            _keyItemService = new Lazy<IKeyItemService>(() => _serviceProvider.GetRequiredService<IKeyItemService>());
            _enmityService = new Lazy<IEnmityService>(() => _serviceProvider.GetRequiredService<IEnmityService>());
            _currencyService = new Lazy<ICurrencyService>(() => _serviceProvider.GetRequiredService<ICurrencyService>());
            _dialogService = new Lazy<IDialogService>(() => _serviceProvider.GetRequiredService<IDialogService>());

            // Subscribe to events
            eventAggregator.Subscribe<OnPlaceableTeleport>(e => UseTeleportDevice());
            eventAggregator.Subscribe<OnPlaceablePermanentVfx>(e => ApplyPermanentVisualEffect());
            eventAggregator.Subscribe<OnPlaceableGenericConversation>(e => GenericConversation());
            eventAggregator.Subscribe<OnPlaceableSit>(e => Sit());
            eventAggregator.Subscribe<OnPlaceableBuyRebuild>(e => PurchaseRebuild());
        }
        
        // Lazy-loaded services to break circular dependencies
        private IKeyItemService KeyItemService => _keyItemService.Value;
        private IEnmityService EnmityService => _enmityService.Value;
        private ICurrencyService CurrencyService => _currencyService.Value;
        private IDialogService DialogService => _dialogService.Value;
        /// <summary>
        /// When a teleport placeable is used, send the user to the configured waypoint.
        /// Checks are made for required key items, if specified as local variables on the placeable.
        /// </summary>
        public void UseTeleportDevice()
        {
            var user = GetLastUsedBy();

            if (GetIsInCombat(user) || EnmityService.HasEnmity(user))
            {
                SendMessageToPC(user, "You are in combat.");
                return;
            }

            var device = OBJECT_SELF;
            var destination = GetLocalString(device, "DESTINATION");
            var vfxId = GetLocalInt(device, "VISUAL_EFFECT");
            var vfx = vfxId > 0 ? (VisualEffectType) vfxId : VisualEffectType.None;
            var requiredKeyItemId = GetLocalInt(device, "KEY_ITEM_ID");
            var missingKeyItemMessage = GetLocalString(device, "MISSING_KEY_ITEM_MESSAGE");
            if (string.IsNullOrWhiteSpace(missingKeyItemMessage))
                missingKeyItemMessage = "You don't have the necessary key item to access this object.";

            if (requiredKeyItemId > 0)
            {
                var keyItem = (KeyItemType) requiredKeyItemId;

                if (!KeyItemService.HasKeyItem(user, keyItem))
                {
                    SendMessageToPC(user, missingKeyItemMessage);

                    return;
                }
            }

            if (vfx != VisualEffectType.None)
            {
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(vfx), user);
            }

            var waypoint = GetWaypointByTag(destination);

            if (!GetIsObjectValid(waypoint))
            {
                waypoint = GetObjectByTag(destination);

                if (!GetIsObjectValid(waypoint))
                {
                    SendMessageToPC(user, "Cannot locate waypoint. Inform an admin this teleporter is broken.");
                    return;
                }
            }

            var location = GetLocation(waypoint);
            AssignCommand(user, () => JumpToLocation(location));
            AssignCommand(user, () => SetFacing(GetFacing(waypoint)));

            var henchman = GetAssociate(AssociateType.Henchman, user);
            if (GetIsObjectValid(henchman))
            {
                AssignCommand(henchman, () => JumpToLocation(location));
            }

        }

        /// <summary>
        /// Applies a permanent VFX on a placeable or creature on heartbeat, then removes the heartbeat script.
        /// </summary>
        public void ApplyPermanentVisualEffect()
        {
            var target = OBJECT_SELF;

            var vfxId = GetLocalInt(target, "PERMANENT_VFX_ID");
            var vfx = vfxId > 0 ? (VisualEffectType) vfxId : VisualEffectType.None;
            
            if (vfx != VisualEffectType.None)
            {
                ApplyEffectToObject(DurationType.Permanent, EffectVisualEffect(vfx), target);
            }

            var type = GetObjectType(target);
            if(type == ObjectType.Placeable)
                SetEventScript(target, EventScriptType.Placeable_OnHeartbeat, string.Empty);
            else if (type == ObjectType.Creature)
                SetEventScript(target, EventScriptType.Creature_OnHeartbeat, string.Empty);
        }

        /// <summary>
        /// Handles starting a generic conversation when a placeable is clicked or used by a player or DM.
        /// </summary>
        public void GenericConversation()
        {
            var placeable = OBJECT_SELF;
            var user = GetObjectType(placeable) == ObjectType.Placeable ? GetLastUsedBy() : GetClickingObject();

            if (!GetIsPC(user) && !GetIsDM(user)) return;

            var conversation = GetLocalString(placeable, "CONVERSATION");
            var target = GetLocalBool(placeable, "TARGET_PC") ? user : placeable;

            if (!string.IsNullOrWhiteSpace(conversation))
            {
                DialogService.StartConversation(user, target, conversation);
            }
            else
            {
                AssignCommand(user, () => ActionStartConversation(target, string.Empty, true, false));
            }
        }
        /// <summary>
        /// Handle sitting on an object.        
        /// </summary>
        public void Sit()
        {
            var user = GetLastUsedBy();

            AssignCommand(user, () => ActionSit(OBJECT_SELF));
            if (GetObjectVisualTransform(user, ObjectVisualTransformType.Scale) == 1.0) return;

            // Transformed creatures sit at the height of their transform. Normalise them to the height of the chair.
            // We want to take the negative/opposite of their differential from "standard" and divide by 2.  So a 
            // creature at 1.6 scale (0.6 above standard) should be Z-transformed by -0.3.
            float fScale = GetObjectVisualTransform(user, ObjectVisualTransformType.Scale) - 1.0f;
            SetObjectVisualTransform(user, ObjectVisualTransformType.TranslateZ, (-fScale) / 2.0f);           
        }

        /// <summary>
        /// Whenever a player purchases a rebuild from the training terminal,
        /// make them spend a rebuild token and send them to the rebuild area.
        /// </summary>
        public void PurchaseRebuild()
        {
            var player = GetPCSpeaker();

            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
            {
                SendMessageToPC(player, $"Only players may use this terminal.");
                return;
            }

            if (CurrencyService.GetCurrency(player, CurrencyType.RebuildToken) <= 0)
            {
                SendMessageToPC(player, ColorToken.Red($"You do not have any rebuild tokens."));
                return;
            }

            CurrencyService.TakeCurrency(player, CurrencyType.RebuildToken, 1);

            var waypoint = GetWaypointByTag("REBUILD_LANDING");
            var location = GetLocation(waypoint);
            AssignCommand(player, () => ClearAllActions());
            AssignCommand(player, () => JumpToLocation(location));

            SendMessageToPC(player, $"Remaining rebuild tokens: {CurrencyService.GetCurrency(player, CurrencyType.RebuildToken)}");
        }
    }
}
