using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service
{
    public static class HoloCom
    {
        // Local variable name constants
        private const string HolocomCallConnected = "HOLOCOM_CALL_CONNECTED";
        private const string HolocomCallConnectedWith = "HOLOCOM_CALL_CONNECTED_WITH";
        private const string HolocomCallSender = "HOLOCOM_CALL_SENDER";
        private const string HolocomCallReceiver = "HOLOCOM_CALL_RECEIVER";
        private const string HolocomCallSenderObject = "HOLOCOM_CALL_SENDER_OBJECT";
        private const string HolocomCallReceiverObject = "HOLOCOM_CALL_RECEIVER_OBJECT";
        private const string HolocomCallAttempt = "HOLOCOM_CALL_ATTEMPT";
        private const string HolocomHologram = "HOLOCOM_HOLOGRAM";
        private const string HologramOwner = "HOLOGRAM_OWNER";
        private const string HolocomCallImmobilize = "HOLOCOM_CALL_IMMOBILIZE";
        private const string HolocomPlaybackHologram = "HOLOCOM_PLAYBACK_HOLOGRAM";
        private const string HolocomCallLastSubmission = "HOLOCOM_CALL_LAST_SUBMISSION";

        private const int CallCooldownSeconds = 10;

        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void OnModuleDeath()
        {
            var player = GetLastPlayerDied();
            if (IsInCall(player))
                SetIsInCall(player, GetTargetForActiveCall(player), false);

        }

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void OnModuleEnter()
        {
            var player = GetEnteringObject();
            RemoveEffectByTag(player, HolocomCallImmobilize);
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void OnModuleLeave()
        {
            var player = GetExitingObject();

            CleanupAllHoloComState(player);
        }

        [NWNEventHandler(ScriptName.OnAreaExit)]
        public static void OnAreaExit()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player))
                return;

            CleanupMessagePlayback(player);
        }

        [NWNEventHandler(ScriptName.OnModuleChat)]
        public static void OnModuleChat()
        {
            var sender = GetPCChatSpeaker();
            var talkVolume = GetPCChatVolume();

            /*
            ChatChannelType channel = (ChatChannelType)NWNXChat.GetChannel();

            if (!IsInCall(sender)) return;
            if (channel != ChatChannelType.PlayerTalk) return;
            if (channel != ChatChannelType.PlayerWhisper) return;
            if (channel != ChatChannelType.PlayerParty) return;
            */

            if (talkVolume == TalkVolume.Shout) return;
            if (talkVolume == TalkVolume.Tell) return;
            if (talkVolume == TalkVolume.SilentShout) return;
            if (talkVolume == TalkVolume.SilentTalk) return;

            var receiver = GetHoloGram(sender);
            if (!GetIsObjectValid(receiver)) return;

            var text = GetPCChatMessage().Trim();

            if (text.StartsWith("/")) return;

            var animation = Animation.LoopingTalkNormal;
            if (text.Contains("!")) animation = Animation.LoopingTalkForceful;
            if (text.Contains("?")) animation = Animation.LoopingTalkPleading;

            SetCommandable(true, receiver);
            AssignCommand(receiver, () => ClearAllActions());

            AssignCommand(receiver, () =>
            {
                ActionPlayAnimation(animation);
            });

            AssignCommand(receiver, () => ActionSpeakString(text, talkVolume));
        }

        public static bool IsInCall(uint player)
        {
            if (GetLocalBool(player, HolocomCallConnected) == true) return true;
            else return false;
        }
        public static void SetIsInCall(uint sender, uint receiver, bool value = true)
        {
            if (value) // START CALL
            {
                SetLocalBool(sender, HolocomCallConnected, true);
                SetLocalBool(receiver, HolocomCallConnected, true);

                SetLocalObject(sender, HolocomCallConnectedWith, receiver);
                SetLocalObject(receiver, HolocomCallConnectedWith, sender);

                var message = "Call Connected. (Use the HoloCom or the chat command /endcall to terminate the call)";
                SendMessageToPC(sender, message);
                SendMessageToPC(receiver, message);
                var effectImmobilized = EffectCutsceneImmobilize();
                TagEffect(effectImmobilized, HolocomCallImmobilize);
                ApplyEffectToObject(DurationType.Permanent, effectImmobilized, sender);
                ApplyEffectToObject(DurationType.Permanent, effectImmobilized, receiver);

                var receiverLocation = GetLocation(receiver);
                var senderLocation = GetLocation(sender);
                var holoSender = CopyObject(sender, BiowareVector.MoveLocation(receiverLocation, GetFacing(receiver), 2.0f, 180));
                var holoReceiver = CopyObject(receiver, BiowareVector.MoveLocation(senderLocation, GetFacing(sender), 2.0f, 180));
                ConfigureHologram(holoSender);
                ConfigureHologram(holoReceiver);
                SetLocalObject(sender, HolocomHologram, holoSender);
                SetLocalObject(receiver, HolocomHologram, holoReceiver);

                SetLocalObject(holoSender, HologramOwner, sender);
                SetLocalObject(holoReceiver, HologramOwner, receiver);

                AssignCommand(sender, () =>
                {
                    PlaySound("hologram_on");
                });
                AssignCommand(receiver, () =>
                {
                    PlaySound("hologram_on");
                });
            }
            else // END CALL
            {
                RemoveEffectByTag(sender, HolocomCallImmobilize);
                RemoveEffectByTag(receiver, HolocomCallImmobilize);

                AssignCommand(sender, () =>
                {
                    PlaySound("hologram_off");
                });
                AssignCommand(receiver, () =>
                {
                    PlaySound("hologram_off");
                });

                // Destroy holograms if they are valid
                var senderHologram = GetHoloGram(sender);
                var receiverHologram = GetHoloGram(receiver);

                if (GetIsObjectValid(senderHologram))
                {
                    DestroyObject(senderHologram);
                }
                if (GetIsObjectValid(receiverHologram))
                {
                    DestroyObject(receiverHologram);
                }

                DeleteLocalInt(sender, HolocomCallConnected);
                DeleteLocalInt(receiver, HolocomCallConnected);

                DeleteLocalInt(sender, HolocomCallSender);
                DeleteLocalInt(receiver, HolocomCallSender);

                DeleteLocalInt(sender, HolocomCallReceiver);
                DeleteLocalInt(receiver, HolocomCallReceiver);

                DeleteLocalObject(sender, HolocomCallConnectedWith);
                DeleteLocalObject(receiver, HolocomCallConnectedWith);

                DeleteLocalObject(sender, HolocomHologram);
                DeleteLocalObject(receiver, HolocomHologram);

                DeleteLocalInt(sender, HolocomCallAttempt);
                DeleteLocalInt(receiver, HolocomCallAttempt);

                DeleteLocalObject(sender, HolocomCallReceiverObject);
                DeleteLocalObject(receiver, HolocomCallReceiverObject);

                DeleteLocalObject(sender, HolocomCallSenderObject);
                DeleteLocalObject(receiver, HolocomCallSenderObject);
            }

            NotifyCallStateChanged(sender, receiver);
        }

        /// <summary>
        /// Pushes a refresh to both participants' open HoloCom windows so call-state
        /// banners (incoming/outgoing/in-call) update without requiring a button click.
        /// Call state changes on engine timers (ring loop) and from the other participant's
        /// actions, so bind updates alone never reach the other player's window.
        /// </summary>
        private static void NotifyCallStateChanged(uint sender, uint receiver)
        {
            var refreshEvent = new HoloComCallStateChangedRefreshEvent();

            if (GetIsObjectValid(sender))
                Gui.PublishRefreshEvent(sender, refreshEvent);

            if (GetIsObjectValid(receiver) && receiver != sender)
                Gui.PublishRefreshEvent(receiver, refreshEvent);
        }
        /// <summary>
        /// Applies the shared hologram treatment used by both live calls and message
        /// playback: generic display-safe name, plot flag, stripped AI, full heal, and
        /// the ghostly hologram visual.
        /// </summary>
        public static void ConfigureHologram(uint hologram)
        {
            SetName(hologram, "Hologram");
            SetPlotFlag(hologram, true);
            DisableHologramAI(hologram);
            ApplyEffectToObject(DurationType.Instant, EffectHeal(GetMaxHitPoints(hologram)), hologram);
            ApplyEffectToObject(DurationType.Permanent, EffectVisualEffect(VisualEffect.Vfx_Dur_Ghostly_Visage_No_Sound, false), hologram);
        }

        /// <summary>
        /// Strips every creature event script from the hologram so no AI, perception,
        /// or combat handler ever runs on it. Holograms are props: their only job is to
        /// stand still and play queued speak/animation actions, and any AI handler
        /// firing on them could flush or contest that action queue.
        /// </summary>
        private static void DisableHologramAI(uint hologram)
        {
            SetEventScript(hologram, EventScript.Creature_OnBlockedByDoor, string.Empty);
            SetEventScript(hologram, EventScript.Creature_OnEndCombatRound, string.Empty);
            SetEventScript(hologram, EventScript.Creature_OnDialogue, string.Empty);
            SetEventScript(hologram, EventScript.Creature_OnDamaged, string.Empty);
            SetEventScript(hologram, EventScript.Creature_OnDeath, string.Empty);
            SetEventScript(hologram, EventScript.Creature_OnDisturbed, string.Empty);
            SetEventScript(hologram, EventScript.Creature_OnHeartbeat, string.Empty);
            SetEventScript(hologram, EventScript.Creature_OnNotice, string.Empty);
            SetEventScript(hologram, EventScript.Creature_OnMeleeAttacked, string.Empty);
            SetEventScript(hologram, EventScript.Creature_OnRested, string.Empty);
            SetEventScript(hologram, EventScript.Creature_OnSpawnIn, string.Empty);
            SetEventScript(hologram, EventScript.Creature_OnSpellCastAt, string.Empty);
            SetEventScript(hologram, EventScript.Creature_OnUserDefined, string.Empty);
        }

        /// <summary>
        /// The message-playback hologram currently active near this player, if any.
        /// Tracked so playbacks cannot overlap and so logouts clean up in-flight
        /// recordings.
        /// </summary>
        public static uint GetActivePlaybackHologram(uint player)
        {
            return GetLocalObject(player, HolocomPlaybackHologram);
        }

        public static void SetActivePlaybackHologram(uint player, uint hologram)
        {
            SetLocalObject(player, HolocomPlaybackHologram, hologram);
        }

        public static void ClearActivePlaybackHologram(uint player)
        {
            DeleteLocalObject(player, HolocomPlaybackHologram);
        }

        public static uint GetHoloGram(uint player)
        {
            return GetLocalObject(player, HolocomHologram);
        }
        public static uint GetHoloGramOwner(uint hologram)
        {
            return GetLocalObject(hologram, HologramOwner);
        }
        public static uint GetTargetForActiveCall(uint player)
        {
            return GetLocalObject(player, HolocomCallConnectedWith);
        }
        public static bool IsCallSender(uint player)
        {
            if (GetLocalBool(player, HolocomCallSender) == true) return true;
            else return false;
        }
        public static void SetIsCallSender(uint player, bool value = true)
        {
            if (value) SetLocalBool(player, HolocomCallSender, true);
            else SetLocalBool(player, HolocomCallSender, false);
        }
        public static uint GetCallSender(uint player)
        {
            return GetLocalObject(player, HolocomCallSenderObject);
        }
        public static void SetCallSender(uint player, uint sender)
        {
            SetLocalObject(player, HolocomCallSenderObject, sender);
        }

        public static bool IsCallReceiver(uint player)
        {
            if (GetLocalBool(player, HolocomCallReceiver) == true) return true;
            else return false;
        }
        public static void SetIsCallReceiver(uint player, bool value = true)
        {
            if (value) SetLocalBool(player, HolocomCallReceiver, true);
            else SetLocalBool(player, HolocomCallReceiver, false);
        }
        public static uint GetCallReceiver(uint player)
        {
            return GetLocalObject(player, HolocomCallReceiverObject);
        }
        public static void SetCallReceiver(uint player, uint receiver)
        {
            SetLocalObject(player, HolocomCallReceiverObject, receiver);
        }
        public static int GetCallAttempt(uint player)
        {
            return GetLocalInt(player, HolocomCallAttempt);
        }
        public static void SetCallAttempt(uint player, int value = 0)
        {
            SetLocalInt(player, HolocomCallAttempt, value);
        }

        /// <summary>
        /// Ends whatever call state the player currently has: disconnects an active call,
        /// declines an incoming call attempt, or cancels an outgoing one. Shared by the
        /// HoloCom window's Decline/End button and the /endcall chat command so every
        /// path notifies the other party and cleans up the same way. Safe to invoke with
        /// no call state at all.
        /// </summary>
        public static void EndOrDeclineCall(uint player)
        {
            if (IsInCall(player))
            {
                var target = GetTargetForActiveCall(player);
                if (GetIsObjectValid(target))
                    SetIsInCall(player, target, false);
                else
                    CleanupOrphanedActiveCall(player);

                SendMessageToPC(player, "You end your HoloCom call.");
            }
            else if (IsCallReceiver(player))
            {
                var callSender = GetCallSender(player);
                if (GetIsObjectValid(callSender))
                    SendMessageToPC(callSender, "Your HoloCom call was declined.");

                CleanupCallAttempt(callSender, player);
                SendMessageToPC(player, "You decline the HoloCom call.");
            }
            else if (IsCallSender(player))
            {
                var callReceiver = GetCallReceiver(player);
                if (GetIsObjectValid(callReceiver))
                    SendMessageToPC(callReceiver, "Your HoloCom stops buzzing.");

                CleanupCallAttempt(player, callReceiver);
                SendMessageToPC(player, "You cancel your HoloCom call.");
            }
            else
            {
                SendMessageToPC(player, "You don't have any active calls or outgoing call attempts to end.");
            }
        }

        /// <summary>
        /// Cleans up one side of an active call whose partner object is no longer valid
        /// (logout, crash, server transition). SetIsInCall's end path assumes both
        /// participants exist, so this covers the pieces that would otherwise leak:
        /// the immobilize effect, the hologram, and every call-state local.
        /// </summary>
        private static void CleanupOrphanedActiveCall(uint player)
        {
            RemoveEffectByTag(player, HolocomCallImmobilize);

            var hologram = GetHoloGram(player);
            if (GetIsObjectValid(hologram))
                DestroyObject(hologram);

            DeleteLocalInt(player, HolocomCallConnected);
            DeleteLocalInt(player, HolocomCallSender);
            DeleteLocalInt(player, HolocomCallReceiver);
            DeleteLocalInt(player, HolocomCallAttempt);
            DeleteLocalObject(player, HolocomCallConnectedWith);
            DeleteLocalObject(player, HolocomHologram);
            DeleteLocalObject(player, HolocomCallSenderObject);
            DeleteLocalObject(player, HolocomCallReceiverObject);

            AssignCommand(player, () => PlaySound("hologram_off"));
            NotifyCallStateChanged(player, OBJECT_INVALID);
        }

        /// <summary>
        /// Cleans up call attempt state for both sender and receiver
        /// </summary>
        /// <param name="sender">The player who initiated the call</param>
        /// <param name="receiver">The player who was being called</param>
        public static void CleanupCallAttempt(uint sender, uint receiver)
        {
            if (GetIsObjectValid(receiver))
            {
                // Clean up the receiver's call state
                SetIsCallReceiver(receiver, false);
                DeleteLocalObject(receiver, HolocomCallReceiverObject);
                DeleteLocalObject(receiver, HolocomCallSenderObject);
                DeleteLocalInt(receiver, HolocomCallAttempt);
            }

            // Clean up the sender's call state
            SetIsCallSender(sender, false);
            DeleteLocalObject(sender, HolocomCallSenderObject);
            DeleteLocalObject(sender, HolocomCallReceiverObject);
            DeleteLocalInt(sender, HolocomCallAttempt);

            NotifyCallStateChanged(sender, receiver);
        }

        /// <summary>
        /// Comprehensive cleanup of all HoloCom state for a player
        /// </summary>
        /// <param name="player">The player to clean up</param>
        public static void CleanupAllHoloComState(uint player)
        {
            // Active calls retain their original sender/receiver attempt flags, so
            // handle the connected state first and let its shared cleanup clear all
            // flags on both participants.
            if (IsInCall(player))
            {
                var callTarget = GetTargetForActiveCall(player);
                if (GetIsObjectValid(callTarget))
                {
                    SetIsInCall(player, callTarget, false);
                }
                else
                {
                    CleanupOrphanedActiveCall(player);
                }
            }
            else if (IsCallSender(player))
            {
                var receiver = GetCallReceiver(player);
                if (GetIsObjectValid(receiver))
                    SendMessageToPC(receiver, "Your HoloCom stops buzzing.");

                CleanupCallAttempt(player, receiver);
            }
            else if (IsCallReceiver(player))
            {
                var sender = GetCallSender(player);
                if (GetIsObjectValid(sender))
                    SendMessageToPC(sender, "Your HoloCom call went unanswered.");

                CleanupCallAttempt(sender, player);
            }

            CleanupMessagePlayback(player);
        }

        /// <summary>
        /// Stops an in-flight recorded message without touching live call state.
        /// Used for logout and area transitions so a hologram cannot remain behind
        /// in a persistent area or block playback after its owner has moved on.
        /// </summary>
        public static void CleanupMessagePlayback(uint player)
        {
            var playbackHologram = GetActivePlaybackHologram(player);
            if (GetIsObjectValid(playbackHologram))
                DestroyObject(playbackHologram);

            ClearActivePlaybackHologram(player);
        }

        public const int MaxFavorites = 50;

        public static List<HoloComFavoriteEntry> GetFavorites(uint observer)
        {
            var dbFavorites = FindFavorites(GetObjectUUID(observer));
            return dbFavorites?.Favorites ?? new List<HoloComFavoriteEntry>();
        }

        public static bool IsFavorite(uint observer, uint target)
        {
            if (!GetIsObjectValid(target))
                return false;

            var identityKey = Disguise.GetIdentityKey(target);
            return GetFavorites(observer).Any(entry => entry.IdentityKey == identityKey);
        }

        public static string AddFavorite(uint observer, uint target)
        {
            if (!GetIsObjectValid(target) || !GetIsPC(target))
                return "Unable to identify that player.";

            var observerId = GetObjectUUID(observer);
            if (target == observer)
                return "You cannot favorite yourself.";

            var dbFavorites = FindFavorites(observerId) ?? new HoloComFavorite(observerId);
            dbFavorites.Favorites ??= new List<HoloComFavoriteEntry>();
            var identityKey = Disguise.GetIdentityKey(target);

            if (dbFavorites.Favorites.Any(entry => entry.IdentityKey == identityKey))
                return string.Empty;

            if (dbFavorites.Favorites.Count >= MaxFavorites)
                return $"You may only have up to {MaxFavorites} favorites.";

            var descriptor = Disguise.GetDisplayDescriptor(target);
            dbFavorites.Favorites.Add(new HoloComFavoriteEntry
            {
                IdentityKey = identityKey,
                Descriptor = descriptor,
                // A disguise favorite must never retain the canonical character name.
                // Undisguised identity keys are the canonical player Id and may use it.
                FallbackName = Disguise.IsDisguiseIdentityKey(identityKey)
                    ? descriptor
                    : PlayerName.GetCanonicalName(target)
            });
            DB.Set(dbFavorites);
            return string.Empty;
        }

        public static void RemoveFavorite(uint observer, string identityKey)
        {
            var dbFavorites = FindFavorites(GetObjectUUID(observer));
            if (dbFavorites?.Favorites == null)
                return;

            var entry = dbFavorites.Favorites.FirstOrDefault(favorite => favorite.IdentityKey == identityKey);
            if (entry == null)
                return;

            dbFavorites.Favorites.Remove(entry);
            DB.Set(dbFavorites);
        }

        private static HoloComFavorite FindFavorites(string observerId)
        {
            return DB.Search(new DBQuery<HoloComFavorite>()
                    .AddFieldSearch(nameof(HoloComFavorite.ObserverPlayerId), observerId, false))
                .FirstOrDefault();
        }

        /// <summary>
        /// Enumerates online players who can be called/messaged/favorited: excludes DMs,
        /// DM-possessed characters, and players in space mode. Excludes the observer
        /// themselves.
        /// </summary>
        public static IEnumerable<uint> GetCallableOnlinePlayers(uint observer)
        {
            for (var pc = GetFirstPC(); GetIsObjectValid(pc); pc = GetNextPC())
            {
                if (!IsCallableOnlinePlayer(observer, pc))
                    continue;

                yield return pc;
            }
        }

        public static bool IsCallableOnlinePlayer(uint observer, uint target)
        {
            return GetIsObjectValid(target) &&
                   GetIsPC(target) &&
                   target != observer &&
                   !GetIsDM(target) &&
                   !GetIsDMPossessed(target) &&
                   !Space.IsPlayerInSpaceMode(target);
        }

        public static uint FindOnlinePlayerByPlayerId(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                return OBJECT_INVALID;

            for (var pc = GetFirstPC(); GetIsObjectValid(pc); pc = GetNextPC())
            {
                if (GetObjectUUID(pc) == playerId)
                    return pc;
            }

            return OBJECT_INVALID;
        }

        public static uint FindOnlinePlayerByIdentityKey(string identityKey)
        {
            if (string.IsNullOrWhiteSpace(identityKey))
                return OBJECT_INVALID;

            for (var pc = GetFirstPC(); GetIsObjectValid(pc); pc = GetNextPC())
            {
                if (Disguise.GetIdentityKey(pc) == identityKey)
                    return pc;
            }

            return OBJECT_INVALID;
        }

        public static uint FindCallableOnlinePlayerByIdentityKey(uint observer, string identityKey)
        {
            var target = FindOnlinePlayerByIdentityKey(identityKey);
            return IsCallableOnlinePlayer(observer, target)
                ? target
                : OBJECT_INVALID;
        }

        /// <summary>
        /// Starts a call attempt from sender to receiver. Ported from the old
        /// HoloComDialog's call button handler.
        /// </summary>
        public static void InitiateCall(uint sender, uint receiver)
        {
            if (!GetIsObjectValid(receiver))
                return;

            // The call state machine tracks sender and receiver flags on each participant.
            // A self-call would set and clear those flags on the same object, wedging the
            // player in a phantom call state.
            if (sender == receiver)
            {
                SendMessageToPC(sender, "You cannot call yourself.");
                return;
            }

            if (!IsCallableOnlinePlayer(sender, receiver))
            {
                SendMessageToPC(sender, "That player is not available for HoloCom calls.");
                return;
            }

            if (IsInCall(sender) || IsCallSender(sender) || IsCallReceiver(sender))
            {
                SendMessageToPC(sender, "You are already in a call.");
                return;
            }

            if (IsInCall(receiver) || IsCallSender(receiver) || IsCallReceiver(receiver))
            {
                SendMessageToPC(sender, "That contact is already handling another call.");
                return;
            }

            if (Space.IsPlayerInSpaceMode(sender))
            {
                SendMessageToPC(sender, "HoloCom calls cannot be made in space.");
                return;
            }

            if (IsOnCallCooldown(sender))
            {
                SendMessageToPC(sender, "You are placing calls too quickly. Try again in a few seconds.");
                return;
            }

            SetLocalString(sender, HolocomCallLastSubmission, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            SetIsCallSender(sender);
            DelayCommand(1.0f, () => CallPlayer(sender, receiver));
        }

        private static bool IsOnCallCooldown(uint sender)
        {
            var lastSubmission = GetLocalString(sender, HolocomCallLastSubmission);
            if (string.IsNullOrWhiteSpace(lastSubmission))
                return false;

            var lastCall = DateTime.ParseExact(lastSubmission, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            return DateTime.UtcNow <= lastCall.AddSeconds(CallCooldownSeconds);
        }

        /// <summary>
        /// Rings the receiver, retrying every 5 seconds for up to 15 attempts before giving
        /// up. Revalidates both sides on every retry so overlapping call attempts cannot
        /// overwrite another caller's state.
        /// </summary>
        private static void CallPlayer(uint sender, uint receiver)
        {
            if (!GetIsObjectValid(sender) || !GetIsObjectValid(receiver))
            {
                if (GetIsObjectValid(sender))
                {
                    CleanupCallAttempt(sender, receiver);
                    SendMessageToPC(sender, "Your HoloCom call went unanswered.");
                }
                return;
            }

            if (!IsCallableOnlinePlayer(sender, receiver) || Space.IsPlayerInSpaceMode(sender))
            {
                CleanupCallAttempt(sender, receiver);
                SendMessageToPC(sender, "That contact is no longer available for HoloCom calls.");
                return;
            }

            if (IsInCall(sender))
                return;

            var receiverHasAnotherAttempt = IsCallSender(receiver) ||
                                            (IsCallReceiver(receiver) && GetCallSender(receiver) != sender);
            if (IsInCall(receiver) || receiverHasAnotherAttempt)
            {
                // This sender never owned the receiver's current attempt, so only
                // clear the sender side. Cleaning the receiver here would cancel the
                // other caller's legitimate ring state.
                CleanupCallAttempt(sender, OBJECT_INVALID);
                SendMessageToPC(sender, "That contact is already handling another call.");
                return;
            }

            if (!IsCallSender(sender))
                return;

            var receiverName = PlayerName.GetDisplayName(sender, receiver);
            SendMessageToPC(sender, "You wait for " + receiverName + " to answer their HoloCom.");

            SetIsCallSender(sender);
            SetIsCallSender(receiver, false);
            SetCallSender(sender, sender);
            SetCallSender(receiver, sender);
            SetIsCallReceiver(sender, false);
            SetIsCallReceiver(receiver);
            SetCallReceiver(sender, receiver);
            SetCallReceiver(receiver, receiver);

            var message = "Your HoloCom buzzes as you are receiving a call.";
            if (Random(10) == 1)
            {
                message += " " + ColorToken.Green("Maybe you should answer it.");
            }
            SendMessageToPC(receiver, message);
            if (GetCallAttempt(sender) % 5 == 0)
            {
                FloatingTextStringOnCreature(message, receiver);
            }

            // The ring state was just (re)stamped on both parties on an engine timer -
            // push it to any open HoloCom windows so the receiver sees Answer/Decline
            // and the sender sees the outgoing-call banner without clicking anything.
            NotifyCallStateChanged(sender, receiver);

            if (GetCallAttempt(sender) <= 15)
            {
                SetCallAttempt(sender, GetCallAttempt(sender) + 1);
                DelayCommand(5.0f, () => { CallPlayer(sender, receiver); });
            }
            else
            {
                SendMessageToPC(sender, "Your HoloCom call went unanswered.");
                SendMessageToPC(receiver, "Your HoloCom stops buzzing.");

                // the following call cleans everything up even if a call isn't currently connected.
                SetIsInCall(sender, receiver, false);
            }
        }
    }
}
