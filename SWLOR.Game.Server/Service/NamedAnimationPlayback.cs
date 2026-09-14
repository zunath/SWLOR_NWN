using System;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service;

/// <summary>The native operations required for a temporary per-creature animation replacement.</summary>
public interface INamedAnimationRuntime
{
    bool IsValid(uint creature);
    string GetToken(uint creature);
    void SetToken(uint creature, string token);
    void Replace(uint creature, string source, string replacement);
    ActionType CurrentAction(uint creature);
    void ClearActions(uint creature);
    void Schedule(float seconds, Action callback);
}

/// <summary>Owns temporary replacements; callbacks from superseded playbacks cannot clear a newer one.</summary>
public sealed class NamedAnimationPlayback
{
    public const string StartSource = "custom1start";
    public const string LoopSource = "custom1lp";
    public const string EndSource = "custom1end";
    public const string RifleCarrySource = "xbowr";
    public const string RifleCarrierSource = "dodges";
    public const string RifleCarryResetSource = "plpause1";
    public const string NoHoldName = "sw_nohold";
    // Keep the authored exit mapped while the engine leaves the custom-emote state.
    // Generated exits take 0.2 seconds; the remaining time allows a deferred script tick.
    public const float ExitGraceSeconds = .5f;
    private const string EndingPrefix = "ending:";
    private readonly INamedAnimationRuntime _runtime;
    private readonly Action<uint, Func<bool>> _releasePose;

    public NamedAnimationPlayback(INamedAnimationRuntime runtime, Action<uint, Func<bool>> releasePose = null)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _releasePose = releasePose;
    }

    public string Begin(uint creature, AnimationClip clip, float duration, bool completeAtDuration = false,
        bool suppressRifleHold = false)
    {
        ArgumentNullException.ThrowIfNull(clip);
        return BeginMapped(creature, clip.StartName, clip.Name, clip.EndName, duration, completeAtDuration,
            rifleCarry: suppressRifleHold ? NoHoldName : "", rifleClip: suppressRifleHold ? clip.Name : "");
    }

    private string BeginMapped(uint creature, string start, string loop, string end, float duration, bool completeAtDuration,
        string rifleCarry = "", string rifleClip = "")
    {
        if (!float.IsFinite(duration) || duration <= 0 || duration > 600) throw new ArgumentOutOfRangeException(nameof(duration));
        if (!_runtime.IsValid(creature)) throw new ArgumentException("Animation target must be a valid creature.", nameof(creature));
        var token = Guid.NewGuid().ToString("N");
        _runtime.SetToken(creature, token);
        try
        {
            _runtime.Replace(creature, StartSource, start);
            _runtime.Replace(creature, LoopSource, loop);
            _runtime.Replace(creature, EndSource, end);
            // DodgeSide clears xbowr and plpause1 before playing dodges. Replacement
            // lookup is a single pass: the plpause1 alias removes the existing physical
            // xbowr layer while its new mapping suppresses subsequent carry refreshes.
            // Install all mappings in this script callback before issuing playback.
            _runtime.Replace(creature, RifleCarrierSource, rifleClip);
            _runtime.Replace(creature, RifleCarrySource, rifleCarry);
            _runtime.Replace(creature, RifleCarryResetSource, string.IsNullOrEmpty(rifleClip) ? "" : RifleCarrySource);
            // Native queue cleanup normally runs first. A module-owned timeout also restores the
            // map when combat, movement, death, or another script clears the queued cleanup action.
            _runtime.Schedule(duration + 1f, () => Complete(creature, token));
            if (completeAtDuration)
                _runtime.Schedule(duration, () => Complete(creature, token));
            return token;
        }
        catch
        {
            Complete(creature, token);
            throw;
        }
    }

    /// <summary>Reserves one-shot native preview ownership without installing authored mappings.</summary>
    public string BeginNative(uint creature)
    {
        if (!_runtime.IsValid(creature)) throw new ArgumentException("Animation target must be a valid creature.", nameof(creature));
        ReleaseForNativePlayback(creature);
        var token = Guid.NewGuid().ToString("N");
        _runtime.SetToken(creature, token);
        try
        {
            // Native one-shots finish at their model-specific length. Expire stale
            // preview ownership silently instead of interrupting their natural exit.
            _runtime.Schedule(10f, () =>
            {
                if (IsCurrent(creature, token)) ReleaseForNativePlayback(creature);
            });
            return token;
        }
        catch
        {
            if (IsCurrent(creature, token)) ReleaseForNativePlayback(creature);
            throw;
        }
    }

    public void Complete(uint creature, string token)
    {
        if (!IsCurrent(creature, token)) return;
        var endingToken = EndingPrefix + token;
        _runtime.SetToken(creature, endingToken);
        try
        {
            _releasePose?.Invoke(creature, () => OwnsExit(creature, endingToken));
            _runtime.Schedule(ExitGraceSeconds, () => Restore(creature, endingToken));
        }
        catch
        {
            Restore(creature, endingToken);
            throw;
        }
    }

    private bool OwnsExit(uint creature, string endingToken) =>
        _runtime.IsValid(creature) && _runtime.GetToken(creature) == endingToken;

    private void Restore(uint creature, string endingToken)
    {
        if (!OwnsExit(creature, endingToken)) return;
        _runtime.Replace(creature, StartSource, "");
        _runtime.Replace(creature, LoopSource, "");
        _runtime.Replace(creature, EndSource, "");
        _runtime.Replace(creature, RifleCarrierSource, "");
        _runtime.Replace(creature, RifleCarrySource, "");
        _runtime.Replace(creature, RifleCarryResetSource, "");
        _runtime.SetToken(creature, "");
    }

    public void Stop(uint creature, bool cancelQueuedAnimation = false)
    {
        if (!_runtime.IsValid(creature)) return;
        var token = _runtime.GetToken(creature);
        if (!IsCurrent(creature, token)) return;
        // Only a started, owned clip may cancel its scripted action. An idle creature can
        // also have unrelated queued script work, so action type alone is not ownership.
        if (cancelQueuedAnimation && _runtime.CurrentAction(creature) == ActionType.Invalid)
            _runtime.ClearActions(creature);
        Complete(creature, token);
    }

    /// <summary>Death must clear ownership immediately, without playing a recovery on resurrection.</summary>
    public void ClearOnDeath(uint creature)
        => ReleaseForNativePlayback(creature);

    /// <summary>Hands control to another animation without issuing an idle/recovery action.</summary>
    public void ReleaseForNativePlayback(uint creature)
    {
        if (!_runtime.IsValid(creature) || string.IsNullOrEmpty(_runtime.GetToken(creature))) return;
        Restore(creature, _runtime.GetToken(creature));
    }

    public bool StopIfCurrent(uint creature, string token)
    {
        if (!IsCurrent(creature, token)) return false;
        Complete(creature, token);
        return true;
    }

    public bool IsCurrent(uint creature, string token) =>
        !string.IsNullOrEmpty(token) && !token.StartsWith(EndingPrefix, StringComparison.Ordinal) &&
        _runtime.IsValid(creature) && _runtime.GetToken(creature) == token;
}
