using System;
using System.Linq;

namespace SWLOR.NWN.API.NWScript;

/// <summary>A named MDL animation installed by the toolset, with its natural playback duration.</summary>
public sealed record AnimationClip
{
    public string Name { get; }
    public float Duration { get; }
    public string StartName => Name + "_in";
    public string EndName => Name + "_out";

    public AnimationClip(string name, float duration)
    {
        if (string.IsNullOrEmpty(name) || name.Length > 59 ||
            name.Any(c => !(c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_')))
            throw new ArgumentException("Animation names require 1–59 ASCII letters, digits or underscores.", nameof(name));
        if (!float.IsFinite(duration) || duration <= 0 || duration > 600)
            throw new ArgumentOutOfRangeException(nameof(duration), "Animation duration must be between 0 and 600 seconds.");
        Name = name; Duration = duration;
    }
}
