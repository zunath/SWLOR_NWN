using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CraftService;

/// <summary>One crafting style: the skill it belongs to, the perk that unlocks it and the status effect that carries it.</summary>
public sealed record CraftingStyleDefinition(SkillType Skill, PerkType Perk, Type StatusEffectType);

/// <summary>
/// The crafting style each crafter selected, kept on the player record (<see cref="Player.SelectedCraftingStyles"/>, one perk per
/// skill) so it outlives the status effect that carries it. Death strips every effect and a relog drops the non-persistent ones,
/// so the effect is applied again from the record after a revive, on login and when a craft window opens, and only while the
/// crafter still owns the style's perk. The craft window records a change, and a refunded perk or a full rebuild clears the entry.
/// A skill's style helper lists its styles in <see cref="Definitions"/>.
/// </summary>
public static class CraftingStyleSelection
{
    /// <summary>Every crafting style of every skill. The Smithery, Agriculture and Gathering helpers add their own here.</summary>
    public static IReadOnlyList<CraftingStyleDefinition> Definitions => CircuitStyles.Definitions;

    /// <summary>The perk of the style selected for <paramref name="skill"/>, or <see cref="PerkType.Invalid"/>.</summary>
    public static PerkType Selected(Player record, SkillType skill) =>
        record.SelectedCraftingStyles != null && record.SelectedCraftingStyles.TryGetValue(skill, out var perk)
            ? perk
            : PerkType.Invalid;

    /// <summary>
    /// Selects the style with <paramref name="perk"/> for <paramref name="skill"/>, replacing the one before it, or clears the
    /// selection when <paramref name="perk"/> is <see cref="PerkType.Invalid"/>. Returns true when the record changed.
    /// </summary>
    public static bool Set(Player record, SkillType skill, PerkType perk)
    {
        record.SelectedCraftingStyles ??= new Dictionary<SkillType, PerkType>();

        if (perk == PerkType.Invalid)
            return record.SelectedCraftingStyles.Remove(skill);

        if (record.SelectedCraftingStyles.TryGetValue(skill, out var current) && current == perk)
            return false;

        record.SelectedCraftingStyles[skill] = perk;
        return true;
    }

    /// <summary>Forgets every selection, as a full rebuild does when it refunds the perks. Returns true when the record changed.</summary>
    public static bool ClearAll(Player record)
    {
        if (record.SelectedCraftingStyles == null || record.SelectedCraftingStyles.Count == 0)
            return false;

        record.SelectedCraftingStyles.Clear();
        return true;
    }

    /// <summary>
    /// Finds the styles that should be active: those the record selects whose perk the player still owns. An entry that names no
    /// style of its skill, or a perk the player no longer owns, is dropped from the record. Returns true when the record changed.
    /// </summary>
    public static bool Reconcile(Player record, IReadOnlyList<CraftingStyleDefinition> definitions, out List<CraftingStyleDefinition> active)
    {
        active = new List<CraftingStyleDefinition>();
        if (record.SelectedCraftingStyles == null)
            return false;

        var changed = false;
        foreach (var (skill, perk) in record.SelectedCraftingStyles.ToList())
        {
            var definition = definitions.FirstOrDefault(candidate => candidate.Skill == skill && candidate.Perk == perk);
            if (definition != null && record.Perks != null && record.Perks.TryGetValue(perk, out var level) && level > 0)
            {
                active.Add(definition);
                continue;
            }

            record.SelectedCraftingStyles.Remove(skill);
            changed = true;
        }

        return changed;
    }

    private static bool IsPlayer(uint creature) =>
        GetIsObjectValid(creature) && GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature);

    /// <summary>
    /// Saves the style the crafter selected for <paramref name="skill"/>; <see cref="PerkType.Invalid"/> records no style.
    /// Creatures without a player record are ignored.
    /// </summary>
    public static void Record(uint creature, SkillType skill, PerkType perk)
    {
        if (!IsPlayer(creature))
            return;

        var record = DB.Get<Player>(GetObjectUUID(creature));
        if (record != null && Set(record, skill, perk))
            DB.Set(record);
    }

    /// <summary>Forgets the selection of <paramref name="skill"/> when it names <paramref name="perk"/>, as a refund of that perk does.</summary>
    public static void ClearIfSelected(uint creature, SkillType skill, PerkType perk)
    {
        if (!IsPlayer(creature))
            return;

        var record = DB.Get<Player>(GetObjectUUID(creature));
        if (record != null && Selected(record, skill) == perk && Set(record, skill, PerkType.Invalid))
            DB.Set(record);
    }

    /// <summary>
    /// Makes the player's style effects match the record: each selected style whose perk is still owned is applied, and any other
    /// style effect is removed. A dead player is left alone until they revive.
    /// </summary>
    public static void Restore(uint creature)
    {
        if (!IsPlayer(creature) || GetIsDead(creature))
            return;

        var record = DB.Get<Player>(GetObjectUUID(creature));
        if (record == null)
            return;

        if (Reconcile(record, Definitions, out var active))
            DB.Set(record);

        foreach (var definition in Definitions)
        {
            var isActive = StatusEffect.HasEffect(definition.StatusEffectType, creature);
            if (active.Contains(definition))
            {
                if (!isActive)
                    StatusEffect.ApplyPermanentStatusEffect(definition.StatusEffectType, creature, creature);
            }
            else if (isActive)
            {
                StatusEffect.RemoveStatusEffect(creature, definition.StatusEffectType);
            }
        }
    }

    /// <summary>Applies the player's selected styles again when they log in; the effects are not kept across a logout.</summary>
    [NWNEventHandler(ScriptName.OnModuleEnter)]
    public static void OnPlayerEnter() => Restore(GetEnteringObject());
}
