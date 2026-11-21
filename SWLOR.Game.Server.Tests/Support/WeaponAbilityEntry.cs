using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>One ability built by a definition under <c>WeaponActiveAbilityDefinitionBase</c>: the definition, the feat and the detail.</summary>
public sealed record WeaponAbilityEntry(string Definition, FeatType Feat, AbilityDetail Detail);
