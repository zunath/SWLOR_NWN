using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CraftService;

/// <summary>One crafting style: the skill it belongs to, the perk that unlocks it and the status effect that carries it.</summary>
public sealed record CraftingStyleDefinition(SkillType Skill, PerkType Perk, Type StatusEffectType);
