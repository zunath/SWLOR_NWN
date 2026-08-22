using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CurrencyService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Associate;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature
{
    public static class PlaceableScripts
    {
        private const float TeleportPartyMemberRange = 8.0f;

        /// <summary>
        /// When a teleport placeable is used, send the user to the configured waypoint.
        /// Checks are made for required key items, if specified as local variables on the placeable.
        /// </summary>
        [NWNEventHandler(ScriptName.OnPlaceableTeleport)]
        public static void UseTeleportDevice()
        {
            var user = GetLastUsedBy();

            if (IsInCombatOrHasEnmity(user))
            {
                SendMessageToPC(user, "You are in combat.");
                return;
            }

            var device = OBJECT_SELF;
            var destination = GetLocalString(device, "DESTINATION");
            var vfxId = GetLocalInt(device, "VISUAL_EFFECT");
            var vfx = vfxId > 0 ? (VisualEffect) vfxId : VisualEffect.None;
            var requiredKeyItemId = GetLocalInt(device, "KEY_ITEM_ID");
            var missingKeyItemMessage = GetLocalString(device, "MISSING_KEY_ITEM_MESSAGE");
            var teleportPartyMembers = GetLocalBool(device, "TELEPORT_PARTY_MEMBERS");
            if (string.IsNullOrWhiteSpace(missingKeyItemMessage))
                missingKeyItemMessage = "You don't have the necessary key item to access this object.";

            if (requiredKeyItemId > 0)
            {
                var keyItem = (KeyItemType) requiredKeyItemId;

                if (!KeyItem.HasKeyItem(user, keyItem))
                {
                    SendMessageToPC(user, missingKeyItemMessage);

                    return;
                }
            }

            if (vfx != VisualEffect.None)
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
            TeleportCreature(user, location, waypoint);

            if (!teleportPartyMembers)
            {
                return;
            }

            foreach (var partyMember in Party.GetAllPartyMembers(user))
            {
                if (partyMember == user ||
                    !GetIsObjectValid(partyMember) ||
                    !GetIsPC(partyMember) ||
                    GetIsDM(partyMember) ||
                    IsInCombatOrHasEnmity(partyMember) ||
                    GetArea(partyMember) != GetArea(device) ||
                    GetDistanceBetween(partyMember, device) > TeleportPartyMemberRange)
                {
                    continue;
                }

                TeleportCreature(partyMember, location, waypoint);

                var userName = PlayerName.GetDisplayName(partyMember, user);
                SendMessageToPC(partyMember, $"You ventured forth with {userName}.");
            }
        }

        private static bool IsInCombatOrHasEnmity(uint creature)
        {
            return GetIsInCombat(creature) || Enmity.HasEnmity(creature);
        }

        private static void TeleportCreature(uint creature, Location location, uint waypoint)
        {
            AssignCommand(creature, () => JumpToLocation(location));
            AssignCommand(creature, () => SetFacing(GetFacing(waypoint)));

            var henchman = GetAssociate(AssociateType.Henchman, creature);
            if (GetIsObjectValid(henchman))
            {
                AssignCommand(henchman, () => JumpToLocation(location));
            }
        }

        /// <summary>
        /// Applies a permanent VFX on a placeable or creature on heartbeat, then removes the heartbeat script.
        /// </summary>
        [NWNEventHandler(ScriptName.OnPlaceablePermanentVfx)]
        public static void ApplyPermanentVisualEffect()
        {
            var target = OBJECT_SELF;

            var vfxId = GetLocalInt(target, "PERMANENT_VFX_ID");
            var vfx = vfxId > 0 ? (VisualEffect) vfxId : VisualEffect.None;

            if (vfx != VisualEffect.None)
            {
                ApplyEffectToObject(DurationType.Permanent, EffectVisualEffect(vfx), target);
            }

            var type = GetObjectType(target);
            if(type == ObjectType.Placeable)
                SetEventScript(target, EventScript.Placeable_OnHeartbeat, string.Empty);
            else if (type == ObjectType.Creature)
                SetEventScript(target, EventScript.Creature_OnHeartbeat, string.Empty);
        }

        /// <summary>
        /// Handles starting a generic conversation when a placeable is clicked or used by a player or DM.
        /// </summary>
        [NWNEventHandler(ScriptName.OnPlaceableGenericConversation)]
        public static void GenericConversation()
        {
            var placeable = OBJECT_SELF;
            var user = GetObjectType(placeable) == ObjectType.Placeable ? GetLastUsedBy() : GetClickingObject();

            if (!GetIsPC(user) && !GetIsDM(user)) return;

            var conversation = GetLocalString(placeable, "CONVERSATION");
            var target = GetLocalBool(placeable, "TARGET_PC") ? user : placeable;

            if (!string.IsNullOrWhiteSpace(conversation))
            {
                if (Conversation.TryGetGraph(conversation, out _))
                    Conversation.Start(user, target, conversation);
                else if (!ConversationMenu.TryStart(user, target, conversation))
                    AssignCommand(user, () => ActionStartConversation(target, conversation, true, false));
            }
            else if (!Conversation.TryStartAssigned(user, target))
            {
                AssignCommand(user, () => ActionStartConversation(target, string.Empty, true, false));
            }
        }
        /// <summary>
        /// Handle sitting on an object.
        /// </summary>
        [NWNEventHandler(ScriptName.OnPlaceableSit)]
        public static void Sit()
        {
            var user = GetLastUsedBy();

            AssignCommand(user, () => ActionSit(OBJECT_SELF));
            if (GetObjectVisualTransform(user, ObjectVisualTransform.Scale) == 1.0) return;

            // Transformed creatures sit at the height of their transform. Normalise them to the height of the chair.
            // We want to take the negative/opposite of their differential from "standard" and divide by 2.  So a
            // creature at 1.6 scale (0.6 above standard) should be Z-transformed by -0.3.
            float fScale = GetObjectVisualTransform(user, ObjectVisualTransform.Scale) - 1.0f;
            SetObjectVisualTransform(user, ObjectVisualTransform.TranslateZ, (-fScale) / 2.0f);
        }

        /// <summary>
        /// Whenever a player purchases a rebuild from the training terminal,
        /// make them spend a rebuild token and send them to the rebuild area.
        /// </summary>
        [NWNEventHandler(ScriptName.OnPlaceableBuyRebuild)]
        public static void PurchaseRebuild()
        {
            PurchaseRebuild(GetPCSpeaker());
        }

        public static bool PurchaseRebuild(uint player)
        {

            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
            {
                SendMessageToPC(player, $"Only players may use this terminal.");
                return false;
            }

            if (Currency.GetCurrency(player, CurrencyType.RebuildToken) <= 0)
            {
                SendMessageToPC(player, ColorToken.Red($"You do not have any rebuild tokens."));
                return false;
            }

            Currency.TakeCurrency(player, CurrencyType.RebuildToken, 1);

            var waypoint = GetWaypointByTag("REBUILD_LANDING");
            var location = GetLocation(waypoint);
            AssignCommand(player, () => ClearAllActions());
            AssignCommand(player, () => JumpToLocation(location));

            SendMessageToPC(player, $"Remaining rebuild tokens: {Currency.GetCurrency(player, CurrencyType.RebuildToken)}");
            return true;
        }

        /// <summary>
        /// Opens the quest contract board for the player who used the placeable.
        /// </summary>
        [NWNEventHandler(ScriptName.OnQuestContractBoard)]
        public static void UseQuestContractBoard()
        {
            var player = GetLastUsedBy();

            if (!GetIsPC(player)) return;

            Gui.TogglePlayerWindow(player, GuiWindowType.QuestContractBoard, null, OBJECT_SELF);
        }
    }
}
