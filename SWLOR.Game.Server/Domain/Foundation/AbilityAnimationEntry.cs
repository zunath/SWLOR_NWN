using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Foundation;

/// <summary>An authored motion and the exact active ability ranks which reference it.</summary>
public sealed record AbilityAnimationEntry(string Id, string DisplayName, string Category,
    AnimationClip Clip, IReadOnlyList<FeatType> Feats, AnimationEquipmentRequirement EquipmentRequirement = AnimationEquipmentRequirement.Unrestricted);
