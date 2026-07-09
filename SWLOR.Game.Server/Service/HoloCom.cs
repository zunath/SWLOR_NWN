using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Entity;
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
                SetName(holoSender, "HoloCom Hologram");
                SetName(holoReceiver, "HoloCom Hologram");

                ApplyEffectToObject(DurationType.Instant, EffectHeal(GetMaxHitPoints(holoSender)), holoSender);
                ApplyEffectToObject(DurationType.Instant, EffectHeal(GetMaxHitPoints(holoReceiver)), holoReceiver);

                ApplyEffectToObject(DurationType.Permanent, EffectVisualEffect(VisualEffect.Vfx_Dur_Ghostly_Visage_No_Sound, false), holoSender);
                ApplyEffectToObject(DurationType.Permanent, EffectVisualEffect(VisualEffect.Vfx_Dur_Ghostly_Visage_No_Sound, false), holoReceiver);
                SetPlotFlag(holoReceiver, true);
                SetPlotFlag(holoSender, true);
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
                for(var effect = GetFirstEffect(sender); GetIsEffectValid(effect); effect = GetNextEffect(sender))
                {
                    if (GetIsEffectValid(effect))
                    {
                        var effectType = GetEffectType(effect);
                        if (effectType == EffectTypeScript.CutsceneImmobilize)
                        {
                            RemoveEffect(sender, effect);
                        }
                    }
                }

                for (var effect = GetFirstEffect(receiver); GetIsEffectValid(effect); effect = GetNextEffect(receiver))
                {
                    if (GetIsEffectValid(effect))
                    {
                        var effectType = GetEffectType(effect);
                        if (effectType == EffectTypeScript.CutsceneImmobilize)
                        {
                            RemoveEffect(receiver, effect);
                        }
                    }
                }

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
        }

        /// <summary>
        /// Comprehensive cleanup of all HoloCom state for a player
        /// </summary>
        /// <param name="player">The player to clean up</param>
        public static void CleanupAllHoloComState(uint player)
        {
            // Clean up call sender state
            if (IsCallSender(player))
            {
                var receiver = GetCallReceiver(player);
                if (GetIsObjectValid(receiver))
                {
                    // Notify the receiver that the call attempt has ended
                    SendMessageToPC(receiver, "Your HoloCom stops buzzing.");

                    // Clean up receiver's state
                    SetIsCallReceiver(receiver, false);
                    DeleteLocalObject(receiver, HolocomCallReceiverObject);
                    DeleteLocalObject(receiver, HolocomCallSenderObject);
                    DeleteLocalInt(receiver, HolocomCallAttempt);
                }

                // Clean up sender's state
                SetIsCallSender(player, false);
                DeleteLocalObject(player, HolocomCallSenderObject);
                DeleteLocalObject(player, HolocomCallReceiverObject);
                DeleteLocalInt(player, HolocomCallAttempt);
            }

            // Clean up call receiver state
            if (IsCallReceiver(player))
            {
                var sender = GetCallSender(player);
                if (GetIsObjectValid(sender))
                {
                    // Notify the sender that the call attempt has ended
                    SendMessageToPC(sender, "Your HoloCom call went unanswered.");

                    // Clean up sender's state
                    SetIsCallSender(sender, false);
                    DeleteLocalObject(sender, HolocomCallSenderObject);
                    DeleteLocalObject(sender, HolocomCallReceiverObject);
                    DeleteLocalInt(sender, HolocomCallAttempt);
                }

                // Clean up receiver's state
                SetIsCallReceiver(player, false);
                DeleteLocalObject(player, HolocomCallReceiverObject);
                DeleteLocalObject(player, HolocomCallSenderObject);
                DeleteLocalInt(player, HolocomCallAttempt);
            }

            // Clean up active call state
            if (IsInCall(player))
            {
                var callTarget = GetTargetForActiveCall(player);
                if (GetIsObjectValid(callTarget))
                {
                    SetIsInCall(player, callTarget, false);
                }
                else
                {
                    // If target is no longer valid, just clean up this player's state
                    DeleteLocalInt(player, HolocomCallConnected);
                    DeleteLocalObject(player, HolocomCallConnectedWith);
                    DeleteLocalObject(player, HolocomHologram);
                }
            }
        }

        public const int MaxFavorites = 50;

        public static List<string> GetFavoritePlayerIds(uint observer)
        {
            var dbFavorites = FindFavorites(GetObjectUUID(observer));
            return dbFavorites?.FavoritePlayerIds ?? new List<string>();
        }

        public static bool IsFavorite(uint observer, string targetPlayerId)
        {
            return GetFavoritePlayerIds(observer).Contains(targetPlayerId);
        }

        public static string AddFavorite(uint observer, string targetPlayerId, bool allowSelfFavorite = false)
        {
            if (string.IsNullOrWhiteSpace(targetPlayerId))
                return "Unable to identify that player.";

            var observerId = GetObjectUUID(observer);
            if (targetPlayerId == observerId && !allowSelfFavorite)
                return "You cannot favorite yourself.";

            var dbFavorites = FindFavorites(observerId) ?? new HoloComFavorite(observerId);

            if (dbFavorites.FavoritePlayerIds.Contains(targetPlayerId))
                return string.Empty;

            if (dbFavorites.FavoritePlayerIds.Count >= MaxFavorites)
                return $"You may only have up to {MaxFavorites} favorites.";

            dbFavorites.FavoritePlayerIds.Add(targetPlayerId);
            DB.Set(dbFavorites);
            return string.Empty;
        }

        public static void RemoveFavorite(uint observer, string targetPlayerId)
        {
            var dbFavorites = FindFavorites(GetObjectUUID(observer));
            if (dbFavorites?.FavoritePlayerIds == null)
                return;

            if (!dbFavorites.FavoritePlayerIds.Remove(targetPlayerId))
                return;

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
        /// themselves unless includeSelf is true.
        /// </summary>
        public static IEnumerable<uint> GetCallableOnlinePlayers(uint observer, bool includeSelf = false)
        {
            for (var pc = GetFirstPC(); GetIsObjectValid(pc); pc = GetNextPC())
            {
                if (GetIsDM(pc) || GetIsDMPossessed(pc) || Space.IsPlayerInSpaceMode(pc))
                    continue;

                if (!includeSelf && pc == observer)
                    continue;

                yield return pc;
            }
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

        /// <summary>
        /// Starts a call attempt from sender to receiver. Ported from the old
        /// HoloComDialog's call button handler.
        /// </summary>
        public static void InitiateCall(uint sender, uint receiver)
        {
            if (!GetIsObjectValid(receiver))
                return;

            if (IsInCall(sender) || IsCallSender(sender) || IsCallReceiver(sender))
                return;

            if (IsInCall(receiver) || Space.IsPlayerInSpaceMode(sender) || Space.IsPlayerInSpaceMode(receiver))
                return;

            SetIsCallSender(sender);
            DelayCommand(1.0f, () => CallPlayer(sender, receiver));
        }

        /// <summary>
        /// Rings the receiver, retrying every 5 seconds for up to 15 attempts before giving
        /// up. Ported verbatim from the old HoloComDialog.CallPlayer.
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

            if (IsInCall(sender) || IsInCall(receiver)) return;

            if (!IsCallSender(sender)) return;

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
