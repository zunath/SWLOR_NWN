using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Foundation
{
    /// <summary>
    /// Named animations: the logic and state that used to be static in the <c>NamedAnimation</c> facade, moved here
    /// unchanged. The playback state (the ownership dictionaries) lives in the <see cref="NamedAnimationPlayback"/> the
    /// service owns. The event handler runs on the container's instance; static callers reach it through
    /// <c>GameServices.NamedAnimation</c>.
    /// </summary>
    public class NamedAnimationService : INamedAnimationService
    {
        private const string PlaybackTokenVariable = "_NAMED_ANIMATION_PLAYBACK";
        private readonly NamedAnimationPlayback _playback;

        public NamedAnimationService()
        {
            _playback = new NamedAnimationPlayback(new NativeRuntime(), ReleaseIdlePose);
        }

        public void Queue(uint creature, AnimationClip clip, float? durationSeconds = null,
            AnimationEquipmentRequirement equipmentRequirement = AnimationEquipmentRequirement.Unrestricted)
        {
            var duration = Validate(clip, durationSeconds);
            if (!GetIsObjectValid(creature) || GetObjectType(creature) != ObjectType.Creature)
                throw new ArgumentException("Animation target must be a valid creature.", nameof(creature));
            var request = equipmentRequirement == AnimationEquipmentRequirement.Unrestricted ? null : _playback.ReserveEquipmentPlayback(creature);
            string token = null;
            AssignCommand(creature, () =>
            {
                if (!_playback.CanStartEquipmentPlayback(creature, request)) return;
                ActionDoCommand(() =>
                {
                    // A queued request may begin well after selection, possibly with other equipment:
                    // choose the carrier and its hold mappings together when playback starts.
                    // Invalidated requests leave the carrier native.
                    var hold = AbilityAnimationEquipment.ReadHold(creature);
                    var held = hold != AnimationWeaponHold.None;
                    if (_playback.CanStartEquipmentPlayback(creature, request) && AbilityAnimationEquipment.IsCompatible(equipmentRequirement, creature))
                        token = _playback.Begin(creature, clip, duration, weaponHold: hold, equipmentRequest: request);
                    PlayAnimation(held ? Animation.FireForgetDodgeSide : Animation.PointForward,
                        held ? clip.PlaybackSpeed(duration) : 1f, duration);
                });
                // Holds the queue for the clip, as the queued carrier action did.
                ActionWait(duration);
                ActionDoCommand(() => _playback.Complete(creature, token));
            });
        }

        public string Play(uint creature, AnimationClip clip, float? durationSeconds = null,
            AnimationEquipmentRequirement equipmentRequirement = AnimationEquipmentRequirement.Unrestricted,
            bool fitToDuration = false, NamedAnimationHold holdMode = NamedAnimationHold.Auto)
        {
            var duration = Validate(clip, durationSeconds);
            if (!AbilityAnimationEquipment.IsCompatible(equipmentRequirement, creature)) return null;
            var nativeHold = holdMode == NamedAnimationHold.Keep ? AnimationWeaponHold.None : AbilityAnimationEquipment.ReadHold(creature);
            var liftInPlace = holdMode == NamedAnimationHold.LiftInPlace && nativeHold == AnimationWeaponHold.Shoulder;
            var hold = holdMode == NamedAnimationHold.Auto ? nativeHold : AnimationWeaponHold.None;
            var held = hold != AnimationWeaponHold.None;
            var speed = held || fitToDuration ? clip.PlaybackSpeed(duration) : 1f;
            // AssignCommand is a deferred closure in SWLOR. Reserve ownership now so
            // the caller can stop it even before that closure runs (for example on close).
            var request = equipmentRequirement == AnimationEquipmentRequirement.Unrestricted ? null : _playback.ReserveEquipmentPlayback(creature);
            var token = _playback.Begin(creature, clip, duration, completeAtDuration: true, weaponHold: hold, equipmentRequest: request,
                liftShoulderHoldInPlace: liftInPlace);
            return PlayMapped(creature, token, duration,
                held ? Animation.FireForgetDodgeSide : Animation.PointForward, speed);
        }

        public bool CanLoop(uint creature) => AbilityAnimationEquipment.ReadHold(creature) == AnimationWeaponHold.None;

        private string PlayMapped(uint creature, string token, float duration,
            Animation carrier = Animation.PointForward, float speed = 1f)
        {
            try
            {
                AssignCommand(creature, () =>
                {
                    if (!_playback.IsCurrent(creature, token)) return;
                    try { PlayAnimation(carrier, speed, duration); }
                    catch { _playback.Complete(creature, token); throw; }
                });
            }
            catch { _playback.Complete(creature, token); throw; }
            return token;
        }

        public bool StopIfCurrent(uint creature, string token) => _playback.StopIfCurrent(creature, token);

        public string PlayNativePreview(uint creature, Animation animation, float speed = 1f)
        {
            if (!float.IsFinite(speed) || speed <= 0f) throw new ArgumentOutOfRangeException(nameof(speed));
            var token = _playback.BeginNative(creature);
            try
            {
                AssignCommand(creature, () =>
                {
                    if (!_playback.IsCurrent(creature, token)) return;
                    try
                    {
                        PistolAnimationRemap.PlayAnimationPreservingExplicitThrow(
                            creature, animation, speed, 0f, immediate: true, releaseAuthoredPose: false);
                    }
                    catch
                    {
                        if (_playback.IsCurrent(creature, token)) _playback.ReleaseForNativePlayback(creature);
                        throw;
                    }
                });
            }
            catch
            {
                if (_playback.IsCurrent(creature, token)) _playback.ReleaseForNativePlayback(creature);
                throw;
            }
            return token;
        }

        public void ClearOnDeath(uint creature) => _playback.ClearOnDeath(creature);

        [NWNEventHandler(ScriptName.OnSWLORItemEquipValidBefore)]
        [NWNEventHandler(ScriptName.OnItemUnequipBefore)]
        public void OnEquipmentChanging()
        {
            var creature = OBJECT_SELF;
            if (_playback.InvalidateEquipmentPlayback(creature))
                PlayNativePreview(creature, Animation.LoopingPause);
        }

        public void ReleaseForNativePlayback(uint creature) => _playback.ReleaseForNativePlayback(creature);

        public void Stop(uint creature, bool cancelQueuedAnimation = false) =>
            _playback.Stop(creature, cancelQueuedAnimation);

        private void ReleaseIdlePose(uint creature, Func<bool> stillOwnsExit)
        {
            // Leave the emote while its authored exit remains mapped. Re-check ownership and
            // activity inside AssignCommand: another ability or movement may start before it runs.
            AssignCommand(creature, () =>
            {
                if (!stillOwnsExit() || GetCurrentHitPoints(creature) <= 0 || GetIsInCombat(creature) ||
                    GetCurrentAction(creature) != ActionType.Invalid) return;
                PlayAnimation(Animation.LoopingPause, 1f, .1f);
            });
        }

        private static float Validate(AnimationClip clip, float? duration)
        {
            ArgumentNullException.ThrowIfNull(clip);
            var value = duration ?? clip.Duration;
            if (!float.IsFinite(value) || value <= 0 || value > 600) throw new ArgumentOutOfRangeException(nameof(duration));
            return value;
        }

        private sealed class NativeRuntime : INamedAnimationRuntime
        {
            public bool IsValid(uint creature) => GetIsObjectValid(creature) && GetObjectType(creature) == ObjectType.Creature;
            public string GetToken(uint creature) => GetLocalString(creature, PlaybackTokenVariable);
            public void SetToken(uint creature, string token)
            {
                if (string.IsNullOrEmpty(token)) DeleteLocalString(creature, PlaybackTokenVariable);
                else SetLocalString(creature, PlaybackTokenVariable, token);
            }
            public void Replace(uint creature, string source, string replacement) => ReplaceObjectAnimation(creature, source, replacement);
            public ActionType CurrentAction(uint creature) => GetCurrentAction(creature);
            public void ClearActions(uint creature) => ClearAllActions(oObject: creature);
            public void Schedule(float seconds, Action callback) => AssignCommand(GetModule(), () => DelayCommand(seconds, callback));
        }
    }
}
